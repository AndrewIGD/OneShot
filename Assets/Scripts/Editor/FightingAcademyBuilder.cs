using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

public static class FightingAcademyBuilder
{
    private const string SourcePrefabPath = "Assets/Prefabs/Academy.prefab";
    private const string TargetPrefabPath = "Assets/Prefabs/FightingAcademyV2.prefab";
    private const string SourceScenePath = "Assets/Scenes/FightScene.unity";
    private const string TargetScenePath = "Assets/Scenes/FightSceneV2.unity";
    private const int InstanceCount = 10;
    private const int Columns = 5;
    private const float SpacingX = 80f;
    private const float SpacingY = 50f;
    private const int TargetObservationSize = 40;
    private const string TargetBehaviorName = "Stick";

    [MenuItem("OneShot/Build Fighting Academy V2 Scene")]
    public static void BuildManual()
    {
        ExecuteBuild();
    }

    [MenuItem("OneShot/Configure All Agents in Scene and Prefab")]
    public static void ConfigureAllAgents()
    {
        ConfigureTargetPrefab();
        ConfigureActiveScene();
    }

    [MenuItem("OneShot/ML-Agents/Convert All Agents to DodgingStickV2 (50 obs, 6 actions)")]
    public static void ConvertAllAgentsToDodgingStickV2()
    {
        SwitchAgentType<DodgingStickV2>();
    }

    [MenuItem("OneShot/ML-Agents/Convert All Agents to FightingStickV2 (40 obs, 6 actions)")]
    public static void ConvertAllAgentsToFightingStickV2()
    {
        SwitchAgentType<FightingStickV2>();
    }

    [MenuItem("OneShot/Reset Game Speed (1x TimeScale)")]
    public static void ResetGameSpeed()
    {
        Time.timeScale = 1f;
        AudioListener.volume = 1f;
        AudioListener.pause = false;
        FrameRateConfigurator.ApplyFrameRateSettings();
    }

    [MenuItem("OneShot/ML-Agents/Toggle Inference Only Mode")]
    public static void ToggleInferenceOnlyMode()
    {
        var current = PlayerPrefs.GetInt("OneShot_InferenceOnly", 0);
        var newValue = current == 1 ? 0 : 1;
        PlayerPrefs.SetInt("OneShot_InferenceOnly", newValue);
        PlayerPrefs.Save();
        TrainingModeChecker.ForceInferenceMode = newValue == 1;
        Time.timeScale = 1f;
        AudioListener.volume = 1f;
        AudioListener.pause = false;
        ConfigureAllAgentsBehaviorType(newValue == 1 ? BehaviorType.InferenceOnly : BehaviorType.Default);
        FrameRateConfigurator.ApplyFrameRateSettings();
    }

    [MenuItem("OneShot/ML-Agents/Toggle Inference Only Mode", true)]
    public static bool ToggleInferenceOnlyModeValidate()
    {
        Menu.SetChecked("OneShot/ML-Agents/Toggle Inference Only Mode", PlayerPrefs.GetInt("OneShot_InferenceOnly", 0) == 1);
        return true;
    }

    [MenuItem("OneShot/ML-Agents/Force Training Mode (Enable Training)")]
    public static void ForceTrainingMode()
    {
        PlayerPrefs.SetInt("OneShot_InferenceOnly", 0);
        PlayerPrefs.Save();
        TrainingModeChecker.ForceInferenceMode = false;
        ConfigureAllAgentsBehaviorType(BehaviorType.Default);
        FrameRateConfigurator.ApplyFrameRateSettings();
    }

    [InitializeOnLoadMethod]
    private static void InitializeOnLoad()
    {
        EditorApplication.delayCall += CheckAndBuild;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            Time.timeScale = 1f;
            AudioListener.volume = 1f;
            AudioListener.pause = false;
            FrameRateConfigurator.ApplyFrameRateSettings();
        }
    }

    private static void CheckAndBuild()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TargetPrefabPath);
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScenePath);

        if (prefab == null || sceneAsset == null)
        {
            ExecuteBuild();
        }
        else
        {
            ConfigureTargetPrefab();
            ConfigureActiveScene();
        }
    }

    private static void ExecuteBuild()
    {
        EnsurePrefabExists();
        ConfigureTargetPrefab();
        CreateMultiAcademyScene();
    }

    private static void EnsurePrefabExists()
    {
        var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TargetPrefabPath);
        if (existingPrefab == null)
        {
            AssetDatabase.CopyAsset(SourcePrefabPath, TargetPrefabPath);
            AssetDatabase.ImportAsset(TargetPrefabPath, ImportAssetOptions.ForceUpdate);
        }
    }

    private static void ConfigureTargetPrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(TargetPrefabPath);

        var allBehaviorParams = root.GetComponentsInChildren<BehaviorParameters>(true);
        for (var i = 0; i < allBehaviorParams.Length; i++)
        {
            ConfigureAgentObject(allBehaviorParams[i].gameObject, i % 2);
        }

        var academy = root.GetComponent<TrainingAcademy>();
        if (academy != null)
        {
            var agents = root.GetComponentsInChildren<StickAgent>(true);
            var serializedAcademy = new SerializedObject(academy);
            var agentsProp = serializedAcademy.FindProperty("agents");
            if (agentsProp != null)
            {
                agentsProp.arraySize = agents.Length;
                for (var i = 0; i < agents.Length; i++)
                {
                    agentsProp.GetArrayElementAtIndex(i).objectReferenceValue = agents[i];
                }
                serializedAcademy.ApplyModifiedProperties();
            }
        }

        PrefabUtility.SaveAsPrefabAsset(root, TargetPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureActiveScene()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        var academies = Object.FindObjectsByType<TrainingAcademy>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var academy in academies)
        {
            ConfigureAcademyObject(academy.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureAcademyObject(GameObject academyObject)
    {
        var academy = academyObject.GetComponent<TrainingAcademy>();
        var behaviorParamsList = academyObject.GetComponentsInChildren<BehaviorParameters>(true);

        for (var i = 0; i < behaviorParamsList.Length; i++)
        {
            ConfigureAgentObject(behaviorParamsList[i].gameObject, i % 2);
        }

        if (academy != null)
        {
            var agents = academyObject.GetComponentsInChildren<StickAgent>(true);
            var serializedAcademy = new SerializedObject(academy);
            var agentsProp = serializedAcademy.FindProperty("agents");
            if (agentsProp != null)
            {
                agentsProp.arraySize = agents.Length;
                for (var i = 0; i < agents.Length; i++)
                {
                    agentsProp.GetArrayElementAtIndex(i).objectReferenceValue = agents[i];
                }
                serializedAcademy.ApplyModifiedProperties();
            }
            EditorUtility.SetDirty(academy);

            var blastZones = academyObject.GetComponentsInChildren<BlastZone>(true);
            for (var i = 0; i < blastZones.Length; i++)
            {
                var serializedBz = new SerializedObject(blastZones[i]);
                var academyProp = serializedBz.FindProperty("academy");
                if (academyProp != null)
                {
                    academyProp.objectReferenceValue = academy;
                    serializedBz.ApplyModifiedProperties();
                }
                var col = blastZones[i].GetComponent<Collider2D>();
                if (col != null)
                {
                    col.isTrigger = true;
                    EditorUtility.SetDirty(col);
                }
                EditorUtility.SetDirty(blastZones[i]);
            }
        }
    }

    private static void ConfigureAgentObject(GameObject agentObject, int teamId = 0)
    {
        var behaviorParams = agentObject.GetComponent<BehaviorParameters>();
        if (behaviorParams != null)
        {
            var isDodging = agentObject.GetComponent<DodgingStickV2>() != null;
            behaviorParams.BehaviorName = TargetBehaviorName;
            behaviorParams.TeamId = teamId;
            behaviorParams.BrainParameters.VectorObservationSize = isDodging ? 50 : 40;
            if (behaviorParams.BrainParameters.ActionSpec.NumDiscreteActions != 6)
            {
                behaviorParams.BrainParameters.ActionSpec = ActionSpec.MakeDiscrete(2, 2, 2, 2, 2, 2);
            }
            behaviorParams.BehaviorType = PlayerPrefs.GetInt("OneShot_InferenceOnly", 0) == 1
                ? (behaviorParams.Model != null ? BehaviorType.InferenceOnly : BehaviorType.HeuristicOnly)
                : BehaviorType.Default;
            EditorUtility.SetDirty(behaviorParams);
        }

        var decisionRequesters = agentObject.GetComponents<DecisionRequester>();
        if (decisionRequesters.Length == 0)
        {
            var dr = agentObject.AddComponent<DecisionRequester>();
            dr.DecisionPeriod = 2;
            dr.TakeActionsBetweenDecisions = true;
            EditorUtility.SetDirty(dr);
        }
        else
        {
            decisionRequesters[0].DecisionPeriod = 2;
            decisionRequesters[0].TakeActionsBetweenDecisions = true;
            EditorUtility.SetDirty(decisionRequesters[0]);

            for (var i = 1; i < decisionRequesters.Length; i++)
            {
                Object.DestroyImmediate(decisionRequesters[i], true);
            }
        }

        var player = agentObject.GetComponent<Player>();
        if (player != null)
        {
            player.SetControlling(true);
            var serializedPlayer = new SerializedObject(player);
            var controllingProp = serializedPlayer.FindProperty("_controlling");
            if (controllingProp != null)
            {
                controllingProp.boolValue = true;
            }

            var academyProp = serializedPlayer.FindProperty("_academy");
            if (academyProp != null)
            {
                var academy = agentObject.GetComponentInParent<TrainingAcademy>();
                if (academy != null)
                {
                    academyProp.objectReferenceValue = academy;
                }
            }

            serializedPlayer.ApplyModifiedProperties();
            EditorUtility.SetDirty(player);
        }

        var existingAgent = agentObject.GetComponent<StickAgent>();
        if (existingAgent != null && !(existingAgent is FightingStickV2))
        {
            Object.DestroyImmediate(existingAgent, true);
            existingAgent = agentObject.AddComponent<FightingStickV2>();
            EditorUtility.SetDirty(agentObject);
        }
        else if (existingAgent == null)
        {
            existingAgent = agentObject.AddComponent<FightingStickV2>();
            EditorUtility.SetDirty(agentObject);
        }

        if (existingAgent != null)
        {
            var serializedAgent = new SerializedObject(existingAgent);
            var teamIdProp = serializedAgent.FindProperty("teamId");
            if (teamIdProp != null)
            {
                teamIdProp.intValue = teamId;
                serializedAgent.ApplyModifiedProperties();
            }
            EditorUtility.SetDirty(existingAgent);
        }
    }

    private static void CreateMultiAcademyScene()
    {
        var targetSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScenePath);
        if (targetSceneAsset == null)
        {
            AssetDatabase.CopyAsset(SourceScenePath, TargetScenePath);
            AssetDatabase.ImportAsset(TargetScenePath, ImportAssetOptions.ForceUpdate);
        }

        var scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        var rootObjects = scene.GetRootGameObjects();

        foreach (var rootObject in rootObjects)
        {
            if (rootObject.name.Contains("Academy") || rootObject.name.Contains("Target"))
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TargetPrefabPath);

        for (var i = 0; i < InstanceCount; i++)
        {
            var col = i % Columns;
            var row = i / Columns;
            var position = new Vector3(col * SpacingX, row * SpacingY, 0f);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = $"FightingAcademyV2_{i + 1}";
            instance.transform.position = position;
            ConfigureAcademyObject(instance);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, TargetScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void ConfigureAllAgentsBehaviorType(BehaviorType behaviorType)
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        var allAgents = Object.FindObjectsByType<StickAgent>(FindObjectsSortMode.None);
        for (var i = 0; i < allAgents.Length; i++)
        {
            if (allAgents[i].TryGetComponent(out BehaviorParameters behaviorParams))
            {
                if (behaviorType == BehaviorType.InferenceOnly)
                {
                    behaviorParams.BehaviorType = behaviorParams.Model != null
                        ? BehaviorType.InferenceOnly
                        : BehaviorType.HeuristicOnly;
                }
                else
                {
                    behaviorParams.BehaviorType = BehaviorType.Default;
                }
                EditorUtility.SetDirty(behaviorParams);
            }
        }
        EditorSceneManager.MarkSceneDirty(activeScene);
    }

    private static void SwitchAgentType<T>() where T : StickAgent
    {
        var root = PrefabUtility.LoadPrefabContents(TargetPrefabPath);
        var prefabAgents = root.GetComponentsInChildren<StickAgent>(true);
        for (var i = 0; i < prefabAgents.Length; i++)
        {
            ReplaceAgentComponent<T>(prefabAgents[i].gameObject, i % 2);
        }
        PrefabUtility.SaveAsPrefabAsset(root, TargetPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);

        var activeScene = EditorSceneManager.GetActiveScene();
        var academies = Object.FindObjectsByType<TrainingAcademy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var academy in academies)
        {
            var agents = academy.GetComponentsInChildren<StickAgent>(true);
            for (var i = 0; i < agents.Length; i++)
            {
                ReplaceAgentComponent<T>(agents[i].gameObject, i % 2);
            }
        }

        ConfigureTargetPrefab();
        ConfigureActiveScene();
    }

    private static void ReplaceAgentComponent<T>(GameObject agentObject, int teamId) where T : StickAgent
    {
        var existing = agentObject.GetComponent<StickAgent>();
        if (existing != null && existing.GetType() != typeof(T))
        {
            Object.DestroyImmediate(existing, true);
            existing = agentObject.AddComponent<T>();
        }
        else if (existing == null)
        {
            existing = agentObject.AddComponent<T>();
        }

        var serializedAgent = new SerializedObject(existing);
        var teamIdProp = serializedAgent.FindProperty("teamId");
        if (teamIdProp != null)
        {
            teamIdProp.intValue = teamId;
            serializedAgent.ApplyModifiedProperties();
        }
        EditorUtility.SetDirty(existing);

        var behaviorParams = agentObject.GetComponent<BehaviorParameters>();
        if (behaviorParams != null)
        {
            var isDodging = typeof(T) == typeof(DodgingStickV2);
            behaviorParams.BehaviorName = TargetBehaviorName;
            behaviorParams.TeamId = teamId;
            behaviorParams.BrainParameters.VectorObservationSize = isDodging ? 50 : 40;
            behaviorParams.BrainParameters.ActionSpec = ActionSpec.MakeDiscrete(2, 2, 2, 2, 2, 2);
            behaviorParams.Model = null;
            EditorUtility.SetDirty(behaviorParams);
        }
    }
}
