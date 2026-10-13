using UnityEngine;
using Unity.MLAgents;

public class FpsCounter : MonoBehaviour
{
    private const float UpdateInterval = 0.5f;

    private float _timeAccumulator;
    private int _frameCount;
    private int _fps;
    private GUIStyle _style;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        var isTraining = Academy.IsInitialized && Academy.Instance.IsCommunicatorOn;
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
        var isTraining = Academy.IsInitialized && Academy.Instance.IsCommunicatorOn;
        if (isTraining)
        {
            return;
        }

        var rect = new Rect(Screen.width - 120, 10, 110, 30);
        GUI.Label(rect, $"{_fps} FPS", _style);
    }
}
