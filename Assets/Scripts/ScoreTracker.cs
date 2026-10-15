using UnityEngine;
using UnityEngine.SceneManagement;

public static class ScoreTracker
{
    private static int _blueScore;
    private static int _redScore;

    public static int BlueScore => _blueScore;
    public static int RedScore => _redScore;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnDomainReload()
    {
        ResetScores();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeSceneHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetScores();
    }

    public static void RecordWin(int teamId)
    {
        if (teamId == 0)
        {
            _blueScore++;
        }
        else if (teamId == 1)
        {
            _redScore++;
        }
    }

    public static void RecordBlueWin()
    {
        _blueScore++;
    }

    public static void RecordRedWin()
    {
        _redScore++;
    }

    public static void ResetScores()
    {
        _blueScore = 0;
        _redScore = 0;
    }
}
