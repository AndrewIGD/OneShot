using UnityEngine;
using Unity.MLAgents;

public class TrainingModeChecker
{
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
            CheckTrainingMode();
        }

        private void CheckTrainingMode()
        {
            if (Academy.IsInitialized && Academy.Instance.IsCommunicatorOn)
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
            yield return null;
            yield return null;

            if (Academy.IsInitialized && Academy.Instance.IsCommunicatorOn)
            {
                OnTrainingModeConfirmed();
                SpawnTrainingInitiator();
            }
            else
            {
                Destroy(gameObject);
            }
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
