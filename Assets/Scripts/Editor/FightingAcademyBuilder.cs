using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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

    [MenuItem("OneShot/Build Fighting Academy V2 Scene")]
    public static void BuildManual()
    {
        ExecuteBuild();
    }

    [InitializeOnLoadMethod]
    private static void InitializeOnLoad()
    {
        EditorApplication.delayCall += CheckAndBuild;
    }

    private static void CheckAndBuild()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TargetPrefabPath);
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScenePath);

        if (prefab == null || sceneAsset == null)
        {
            ExecuteBuild();
        }
    }

    private static void ExecuteBuild()
    {
        EnsurePrefabExists();
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
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, TargetScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
