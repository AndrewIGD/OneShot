using UnityEngine;
using UnityEngine.SceneManagement;

public class TrainingInitiator : MonoBehaviour
{
    private static TrainingInitiator _instance;
    private const string FightSceneName = "FightSceneV2";

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
        var currentScene = SceneManager.GetActiveScene().name;
        if (currentScene != FightSceneName)
        {
            SceneManager.LoadScene(FightSceneName);
        }
    }
}
