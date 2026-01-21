using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.InputSystem;

public class StickAgent : Agent
{
    [SerializeField] private int teamId;

    private const float IdlePenalty = -0.001f;

    private StickAgentInputDevice _inputDevice;
    private Player _player;
    private Rigidbody2D _rb;
    private StickAgent _opponentAgent;

    public override void Initialize()
    {
        _inputDevice = new StickAgentInputDevice();
        _player = GetComponent<Player>();
        _rb = GetComponent<Rigidbody2D>();

        switch (teamId)
        {
            case 0:
                _player.ChangeAppearance("Blue", Color.blue);
                break;
            case 1:
                _player.ChangeAppearance("Red", Color.red);
                break;
        }

        _player.SetInputDevice(_inputDevice);
    }

    public void Setup(StickAgent otherAgent)
    {
        _opponentAgent = otherAgent;
    }

    public override void OnEpisodeBegin()
    {
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition.x);
        sensor.AddObservation(transform.localPosition.y);

        if (_rb != null)
        {
            sensor.AddObservation(_rb.linearVelocity.x);
            sensor.AddObservation(_rb.linearVelocity.y);
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }

        sensor.AddObservation(_player.IsGrounded ? 1f : 0f);
        sensor.AddObservation(_player.JumpCount);
        //sensor.AddObservation(_player.DodgeCount);
        sensor.AddObservation(_player.AnimationStateIndex);

        sensor.AddObservation(_opponentAgent.transform.localPosition.x);
        sensor.AddObservation(_opponentAgent.transform.localPosition.y);

        if (_opponentAgent._rb != null)
        {
            sensor.AddObservation(_opponentAgent._rb.linearVelocity.x);
            sensor.AddObservation(_opponentAgent._rb.linearVelocity.y);
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }

        sensor.AddObservation(_opponentAgent._player.IsGrounded ? 1f : 0f);
        sensor.AddObservation(_opponentAgent._player.JumpCount);
        //sensor.AddObservation(_opponentAgent._player.DodgeCount);
        sensor.AddObservation(_opponentAgent._player.AnimationStateIndex);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var discreteActions = actions.DiscreteActions;

        bool down = discreteActions[0] == 1;
        bool left = discreteActions[1] == 1;
        bool right = discreteActions[2] == 1;
        bool jump = discreteActions[3] == 1;
        bool attack = discreteActions[4] == 1;

        _inputDevice.SetState(false, down, left, right, jump, false, down, attack);

        _inputDevice.Update();

        AddReward(IdlePenalty);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        var keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        discreteActions[0] = keyboard.sKey.isPressed ? 1 : 0; 
        discreteActions[1] = keyboard.aKey.isPressed ? 1 : 0; 
        discreteActions[2] = keyboard.dKey.isPressed ? 1 : 0; 
        discreteActions[3] = keyboard.spaceKey.isPressed ? 1 : 0; 
        discreteActions[4] = keyboard.slashKey.isPressed ? 1 : 0; 
    }

    public void Reset()
    {
        _rb.linearVelocity = Vector2.zero;
        _player.Reset();
    }
}
