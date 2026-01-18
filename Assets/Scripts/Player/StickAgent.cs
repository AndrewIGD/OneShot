using UnityEngine;

public class StickAgent : MonoBehaviour
{
    private StickAgentInputDevice _inputDevice;
    private Player _player;

    private void Awake()
    {
        _inputDevice = new StickAgentInputDevice();
        _player = GetComponent<Player>();

        _player.SetInputDevice(_inputDevice);
    }
}
