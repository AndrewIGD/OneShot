using System.Collections;
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

    private Vector2 _startPosition, _endPosition;
    private float startScale, endScale;

    private float t = 0;

    private Camera camera;

    private float _cameraSpeed = 1;

    public void AddPlayer(Player character)
    {
        _characters.Add(character.transform);

        _rbs.Add(character.GetComponent<Rigidbody2D>());
    }

    private void Start()
    {
        camera = Camera.main;
    }

    private void Update()
    {
        for(int i = 0;i<_characters.Count;i++)
        {
            if(_characters[i] == null)
            {
                _characters.RemoveAt(i);
                _rbs.RemoveAt(i);
                i--;
            }
        }

        if (_characters.Count == 0)
            return;

        Vector2 startPosition = transform.position;

        Vector2 endPosition = Vector2.zero;

        for (int i = 0; i < _characters.Count; i++)
        {
            endPosition += (Vector2)_characters[i].position;
        }

        endPosition = endPosition / _characters.Count;

        endPosition += offset;

        if (Vector2.Distance(endPosition, startPosition) > minDistance)
        {
            _startPosition = startPosition;
            _endPosition = endPosition;

            float maxD = 0;

            for (int i = 0; i < _characters.Count; i++)
            {
                float d = Vector2.Distance(_characters[i].position, _endPosition);

                if (d > maxD)
                    maxD = d;
            }

            startScale = camera.orthographicSize;

            endScale = Mathf.Max(maxD, minCameraScale);

            t = 0;
        }

        if (t < updateInterval)
        {
            t += Time.deltaTime / updateInterval;

            transform.position = Vector2.Lerp(_startPosition, _endPosition, t);

            transform.position = new Vector3(transform.position.x, transform.position.y, -10);

            camera.orthographicSize = Mathf.Max(Mathf.Lerp(startScale, endScale, t), minCameraScale);
        }
    }
}
