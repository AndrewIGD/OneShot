using UnityEngine;
using Unity.MLAgents;

public class FpsCounter : MonoBehaviour
{
    private const float UpdateInterval = 0.5f;

    private float _timeAccumulator;
    private int _frameCount;
    private int _fps;
    private GUIStyle _style;
    private GUIStyle _blueStyle;
    private GUIStyle _redStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        var isTraining = TrainingModeChecker.IsTrainingMode;
        if (isTraining)
        {
            return;
        }

        var counterObject = new GameObject("FpsCounter");
        counterObject.AddComponent<FpsCounter>();
        DontDestroyOnLoad(counterObject);
    }

    private void Awake()
    {
        _style = new GUIStyle();
        _style.alignment = TextAnchor.UpperRight;
        _style.fontSize = 18;
        _style.fontStyle = FontStyle.Bold;
        _style.normal.textColor = Color.green;

        _blueStyle = new GUIStyle();
        _blueStyle.alignment = TextAnchor.UpperRight;
        _blueStyle.fontSize = 18;
        _blueStyle.fontStyle = FontStyle.Bold;
        _blueStyle.normal.textColor = new Color(0.35f, 0.7f, 1f);

        _redStyle = new GUIStyle();
        _redStyle.alignment = TextAnchor.UpperRight;
        _redStyle.fontSize = 18;
        _redStyle.fontStyle = FontStyle.Bold;
        _redStyle.normal.textColor = new Color(1f, 0.35f, 0.35f);
    }

    private void Update()
    {
        _frameCount++;
        _timeAccumulator += Time.unscaledDeltaTime;

        if (_timeAccumulator >= UpdateInterval)
        {
            _fps = Mathf.RoundToInt(_frameCount / _timeAccumulator);
            _frameCount = 0;
            _timeAccumulator = 0f;

            if (_fps >= 55)
            {
                _style.normal.textColor = Color.green;
            }
            else if (_fps >= 30)
            {
                _style.normal.textColor = Color.yellow;
            }
            else
            {
                _style.normal.textColor = Color.red;
            }
        }
    }

    private void OnGUI()
    {
        var isTraining = TrainingModeChecker.IsTrainingMode;
        if (isTraining)
        {
            return;
        }

        var fpsRect = new Rect(Screen.width - 150, 10, 140, 24);
        var blueRect = new Rect(Screen.width - 150, 34, 140, 24);
        var redRect = new Rect(Screen.width - 150, 58, 140, 24);

        GUI.Label(fpsRect, $"{_fps} FPS", _style);
        GUI.Label(blueRect, $"Blue: {ScoreTracker.BlueScore}", _blueStyle);
        GUI.Label(redRect, $"Red: {ScoreTracker.RedScore}", _redStyle);
    }
}
