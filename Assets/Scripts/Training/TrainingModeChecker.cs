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
        GameObject checkerObject = new GameObject("TrainingModeChecker_Temp");
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
                Debug.Log("TrainingModeChecker: Detected training mode. Spawning TrainingInitiator...");
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
                Debug.Log("TrainingModeChecker: Detected training mode (delayed check). Spawning TrainingInitiator...");
                SpawnTrainingInitiator();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void SpawnTrainingInitiator()
        {
            GameObject initiatorObject = new GameObject("TrainingInitiator");
            initiatorObject.AddComponent<TrainingInitiator>();
            
            Destroy(gameObject);
        }
    }
}
