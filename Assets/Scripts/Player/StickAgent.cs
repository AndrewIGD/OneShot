using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.InputSystem;

public abstract class StickAgent : Agent
{
    [SerializeField] private int teamId;

    protected float[] Observations => _observations;

    protected Player Player => _player;
    protected Rigidbody2D Rb => _rb;

    private StickAgentInputDevice _inputDevice;
    private Player _player;
    private Rigidbody2D _rb;
    private float[] _observations = new float[40];

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

        if (TryGetComponent(out DecisionRequester decisionRequester))
        {
            decisionRequester.DecisionPeriod = 2;
            decisionRequester.TakeActionsBetweenDecisions = true;
        }
    }

    public override void OnEpisodeBegin()
    {
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
        }

        _player.ResetStamina();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        GatherObservations();

        foreach (var observation in _observations)
        {
            sensor.AddObservation(observation);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var discreteActions = actions.DiscreteActions;

        bool down = discreteActions[0] == 1;
        bool left = discreteActions[1] == 1;
        bool right = discreteActions[2] == 1;
        bool up = discreteActions[3] == 1;
        bool jump = discreteActions[4] == 1;
        bool attack = discreteActions[5] == 1;
        bool dodge = discreteActions[6] == 1;

        _inputDevice.SetState(up, down, left, right, jump, dodge, down, attack);

        _inputDevice.Update();

        AddActionRewards();
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
        discreteActions[3] = keyboard.wKey.isPressed ? 1 : 0; 
        discreteActions[4] = keyboard.spaceKey.isPressed ? 1 : 0; 
        discreteActions[5] = keyboard.slashKey.isPressed ? 1 : 0; 
        discreteActions[6] = keyboard.rightShiftKey.isPressed ? 1 : 0;
    }

    public void Reset()
    {
        _rb.linearVelocity = Vector2.zero;
        _player.Reset();
    }

    protected abstract void GatherObservations();

    protected abstract void AddActionRewards();
}
