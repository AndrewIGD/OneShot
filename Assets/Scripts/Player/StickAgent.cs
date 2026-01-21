using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.InputSystem;

public class StickAgent : Agent
{
    [SerializeField] private int teamId;

    //private const float IdlePenalty = -0.001f;

    private const float CloseDistanceReward = 0.01f;
    private const float AttackReward = 0.02f;
    private const float AttackPenalty = -0.01f;

    private float[] AttackRewardDistance = new float[3] { 9f, 5f, 6f };

    private StickAgentInputDevice _inputDevice;
    private Player _player;
    private Rigidbody2D _rb;
    private StickAgent _opponentAgent;

    private float distanceToOpponent;

    private bool isAttacking = false;

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

        distanceToOpponent = Vector2.Distance(transform.position, _opponentAgent.transform.position);
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
        var relativePositionToOpponent = transform.localPosition - _opponentAgent.transform.localPosition;
        var distanceToOpponent = Vector2.Distance(transform.localPosition, _opponentAgent.transform.localPosition);
        var relativeVelocityToOpponent = _rb.linearVelocity - _opponentAgent._rb.linearVelocity;

        sensor.AddObservation(relativePositionToOpponent.x);
        sensor.AddObservation(relativePositionToOpponent.y);

        sensor.AddObservation(distanceToOpponent);

        sensor.AddObservation(_rb.linearVelocity.x / _player.MovementSpeed);
        sensor.AddObservation(_rb.linearVelocity.y / _player.MaxFallSpeed);

        sensor.AddObservation(relativeVelocityToOpponent.x / _player.MovementSpeed);
        sensor.AddObservation(relativeVelocityToOpponent.y / _player.MaxFallSpeed);

        sensor.AddObservation(_player.IsGrounded ? 1f : 0f);
        sensor.AddObservation(_player.JumpCount / (float)_player.MaxJumps);
        //sensor.AddObservation(_player.DodgeCount);
        sensor.AddObservation(_player.AnimationStateIndex);

        sensor.AddObservation(_opponentAgent._player.IsGrounded ? 1f : 0f);
        sensor.AddObservation(_opponentAgent._player.JumpCount / (float)_opponentAgent._player.MaxJumps);
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

        float rewardThisStep = 0f;

        var newDistanceToOpponent = Vector2.Distance(transform.position, _opponentAgent.transform.position);
        if (newDistanceToOpponent < distanceToOpponent)
        {
            float closeReward = CloseDistanceReward * (distanceToOpponent - newDistanceToOpponent);
            AddReward(closeReward);
            rewardThisStep += closeReward;
            //Debug.Log($"[StickAgent][Reward] CloseDistanceReward: {closeReward:F4} (distance {distanceToOpponent:F2} -> {newDistanceToOpponent:F2})");
        }

        distanceToOpponent = newDistanceToOpponent;

        var isAttackingNow = _player.AnimationStateIndex > 0;
        bool attackStartedThisFrame = isAttackingNow && !isAttacking;

        if (attackStartedThisFrame)
        {
            if (distanceToOpponent < AttackRewardDistance[_player.AnimationStateIndex - 2])
            {
                AddReward(AttackReward);
                rewardThisStep += AttackReward;
                //Debug.Log($"[StickAgent][Reward] AttackReward: {AttackReward:F4} (distance {distanceToOpponent:F2} < {AttackRewardDistance[_player.AnimationStateIndex - 2]})");
            }
            else
            {
                AddReward(AttackPenalty);
                rewardThisStep += AttackPenalty;
                //Debug.Log($"[StickAgent][Reward] AttackPenalty: {AttackPenalty:F4} (distance {distanceToOpponent:F2} >= {AttackRewardDistance})");
            }
        }

        isAttacking = isAttackingNow;

        // Uncomment if you want to apply idle penalty and log it
        //AddReward(IdlePenalty);
        //rewardThisStep += IdlePenalty;
        //Debug.Log($"[StickAgent][Reward] IdlePenalty: {IdlePenalty:F4}");

        if (Mathf.Abs(rewardThisStep) > 0f)
        {
            //Debug.Log($"[StickAgent][Reward] Total step reward: {rewardThisStep:F4}");
        }
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

        distanceToOpponent = Vector2.Distance(transform.position, _opponentAgent.transform.position);
    }
}
