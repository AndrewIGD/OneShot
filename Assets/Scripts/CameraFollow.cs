using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private float updateInterval = 1;
    [SerializeField] private Vector2 offset;
    [SerializeField] private float minCameraScale = 6f;
    [SerializeField] private float minDistance = 1f;

    private List<Transform> _characters = new List<Transform>();
    private List<Rigidbody2D> _rbs = new List<Rigidbody2D>();

    private Vector2 _startPosition;
    private Vector2 _endPosition;
    private float _startScale;
    private float _endScale;
    private float _t;
    private Camera _camera;
    private float _cameraSpeed = 1;

    public void AddPlayer(Player character)
    {
        _characters.Add(character.transform);
        _rbs.Add(character.GetComponent<Rigidbody2D>());
    }

    private void Start()
    {
        _camera = Camera.main;

        if (TrainingModeChecker.IsTrainingMode)
        {
            _camera.enabled = false;
        }
    }

    private void Update()
    {
        if (TrainingModeChecker.IsTrainingMode)
        {
            return;
        }

        for (var i = 0; i < _characters.Count; i++)
        {
            if (_characters[i] == null)
            {
                _characters.RemoveAt(i);
                _rbs.RemoveAt(i);
                i--;
            }
        }

        if (_characters.Count == 0)
        {
            return;
        }

        var startPosition = (Vector2)transform.position;
        var endPosition = Vector2.zero;

        for (var i = 0; i < _characters.Count; i++)
        {
            endPosition += (Vector2)_characters[i].position;
        }

        endPosition /= _characters.Count;
        endPosition += offset;

        if (Vector2.Distance(endPosition, startPosition) > minDistance)
        {
            _startPosition = startPosition;
            _endPosition = endPosition;

            var maxD = 0f;

            for (var i = 0; i < _characters.Count; i++)
            {
                var d = Vector2.Distance(_characters[i].position, _endPosition);

                if (d > maxD)
                {
                    maxD = d;
                }
            }

            _startScale = _camera.orthographicSize;
            _endScale = Mathf.Max(maxD, minCameraScale);
            _t = 0;
        }

        if (_t < updateInterval)
        {
            _t += Time.deltaTime / updateInterval;
            transform.position = Vector2.Lerp(_startPosition, _endPosition, _t);
            transform.position = new Vector3(transform.position.x, transform.position.y, -10);
            _camera.orthographicSize = Mathf.Max(Mathf.Lerp(_startScale, _endScale, _t), minCameraScale);
        }
    }
}
