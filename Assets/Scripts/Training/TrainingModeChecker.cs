using UnityEngine;
using Unity.MLAgents;

public class TrainingModeChecker
{
    private static bool _forceInferenceMode;

    public static bool ForceInferenceMode
    {
        get
        {
            return _forceInferenceMode;
        }
        set
        {
            _forceInferenceMode = value;
        }
    }

    public static bool IsInferenceMode
    {
        get
        {
            if (HasMlagentsPortArg())
            {
                return false;
            }

            return _forceInferenceMode || PlayerPrefs.GetInt("OneShot_InferenceOnly", 0) == 1;
        }
    }

    public static bool IsTrainingMode
    {
        get
        {
            if (IsInferenceMode)
            {
                return false;
            }

            if (HasMlagentsPortArg())
            {
                return true;
            }

            return Academy.Instance.IsCommunicatorOn;
        }
    }

    private static bool HasMlagentsPortArg()
    {
        var args = System.Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--mlagents-port")
            {
                return true;
            }
        }

        return false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void OnApplicationLoad()
    {
        InitializeTrainingModeChecker();
    }

    private static void InitializeTrainingModeChecker()
    {
        var checkerObject = new GameObject("TrainingModeChecker_Temp");
        checkerObject.AddComponent<TrainingModeCheckerComponent>();
        Object.DontDestroyOnLoad(checkerObject);
    }

    private class TrainingModeCheckerComponent : MonoBehaviour
    {
        private void Start()
        {
            Academy.Instance.OnEnvironmentReset += OnEnvironmentReset;
            CheckTrainingMode();
        }

        private void OnDestroy()
        {
            if (Academy.IsInitialized)
            {
                Academy.Instance.OnEnvironmentReset -= OnEnvironmentReset;
            }
        }

        private void OnEnvironmentReset()
        {
            if (TrainingModeChecker.IsTrainingMode)
            {
                OnTrainingModeConfirmed();
                SpawnTrainingInitiator();
            }
        }

        private void CheckTrainingMode()
        {
            if (TrainingModeChecker.IsTrainingMode)
            {
                OnTrainingModeConfirmed();
                SpawnTrainingInitiator();
            }
            else
            {
                StartCoroutine(CheckTrainingModeDelayed());
            }
        }

        private System.Collections.IEnumerator CheckTrainingModeDelayed()
        {
            var timeout = 2.0f;
            var elapsed = 0f;

            while (elapsed < timeout)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;

                if (TrainingModeChecker.IsTrainingMode)
                {
                    OnTrainingModeConfirmed();
                    SpawnTrainingInitiator();
                    yield break;
                }
            }

            OnGameplayModeConfirmed();
            Destroy(gameObject);
        }

        private void OnGameplayModeConfirmed()
        {
            Time.timeScale = 1f;
            AudioListener.volume = 1f;
            AudioListener.pause = false;
            FrameRateConfigurator.ApplyFrameRateSettings();
        }

        private void OnTrainingModeConfirmed()
        {
            FrameRateConfigurator.ApplyFrameRateSettings();
            AudioListener.volume = 0f;
            AudioListener.pause = true;
        }

        private void SpawnTrainingInitiator()
        {
            var initiatorObject = new GameObject("TrainingInitiator");
            initiatorObject.AddComponent<TrainingInitiator>();

            Destroy(gameObject);
        }
    }
}
