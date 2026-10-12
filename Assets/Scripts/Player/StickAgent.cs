using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.InputSystem;

public abstract class StickAgent : Agent
{
    [SerializeField] private int teamId;

    protected float[] Observations => observations;

    protected Player Player => _player;
    protected Rigidbody2D Rb => _rb;
    protected virtual int ObservationSize => 17;
    protected virtual bool UsesDodgeAction => false;

    /// <summary>For sibling agents (e.g. PvP) to read opponent state without exposing private fields.</summary>
    public Player AgentPlayer => _player;

    /// <summary>For sibling agents (e.g. PvP) to read opponent rigidbody.</summary>
    public Rigidbody2D AgentRigidbody => _rb;

    public StickAgent Opponent { get; private set; }

    private StickAgentInputDevice _inputDevice;
    private Player _player;
    private Rigidbody2D _rb;

    // +2 = relative velocity to opponent
    // +1 = distance to opponent
    // +2 = own velocity
    // +2 = relative velocity to opponent
    // +1 = is grounded
    // +1 = jump count
    // +1 = dodge count
    // +1 = direction facing
    // +8 = attack observations (nlight, slight, dlight, nair, sair, dair, dash, dodge)
    // = 19
    // multiply by 2 for own + opponent observations
    // = 38
    private float[] observations;

    public override void Initialize()
    {
        observations = new float[ObservationSize];
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

    public override void OnEpisodeBegin()
    {
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        System.Array.Clear(observations, 0, observations.Length);
        GatherObservations();

        foreach (float o in observations)
            sensor.AddObservation(o);
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
        bool dodge = UsesDodgeAction && discreteActions[6] == 1;
        //dodge = false; // disabled for now

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

        WriteHeuristicActions(discreteActions, keyboard);
    }

    public virtual void Reset()
    {
        _rb.linearVelocity = Vector2.zero;
        _player.Reset();
    }

    /// <summary>Pair two fighters so each can observe and reward against the other.</summary>
    public void SetupOpponent(StickAgent opponent)
    {
        Opponent = opponent;
    }

    protected virtual void WriteHeuristicActions(ActionSegment<int> discreteActions, Keyboard keyboard)
    {
        discreteActions[0] = keyboard.sKey.isPressed ? 1 : 0; 
        discreteActions[1] = keyboard.aKey.isPressed ? 1 : 0; 
        discreteActions[2] = keyboard.dKey.isPressed ? 1 : 0; 
        discreteActions[3] = keyboard.wKey.isPressed ? 1 : 0; 
        discreteActions[4] = keyboard.spaceKey.isPressed ? 1 : 0; 
        discreteActions[5] = keyboard.slashKey.isPressed ? 1 : 0; 
        discreteActions[6] = keyboard.rightShiftKey.isPressed ? 1 : 0;
    }

    protected abstract void GatherObservations();

    protected abstract void AddActionRewards();
}
