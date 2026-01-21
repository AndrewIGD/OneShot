using UnityEngine;
using UnityEngine.SceneManagement;

public class TrainingInitiator : MonoBehaviour
{
    private static TrainingInitiator _instance;
    private const string FightSceneName = "FightScene";

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        LoadFightScene();
    }

    private void LoadFightScene()
    {
        if (SceneManager.GetActiveScene().name != FightSceneName)
        {
            Debug.Log("TrainingInitiator: Loading FightScene...");
            SceneManager.LoadScene(FightSceneName);
        }
    }
}
