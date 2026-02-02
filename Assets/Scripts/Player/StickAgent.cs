using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.InputSystem;

public class StickAgent : Agent
{
    [SerializeField] private int teamId;

    //private const float IdlePenalty = -0.001f;

    private const float CloseDistanceReward = 0.001f;
    private const float DistancePenalty = -0.0005f; // Penalty for moving away
    private const float AttackReward = 0.05f;
    private const float AttackPenalty = -0.02f;

    private const float DistanceNormalizationFactor = 20f;

    private float[] AttackRewardDistance = new float[3] { 9f, 5f, 6f };

    private StickAgentInputDevice _inputDevice;
    private Player _player;
    private Rigidbody2D _rb;
    private StickAgent _opponentAgent;

    private float distanceToOpponent;

    private bool isAttacking = false;

    private float[] attackObservationsSelf = new float[3];
    private float[] attackObservationsOpponent = new float[3];

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

        float obs1 = relativePositionToOpponent.x / DistanceNormalizationFactor;
        float obs2 = relativePositionToOpponent.y / DistanceNormalizationFactor;
        float obs3 = distanceToOpponent / DistanceNormalizationFactor;
        float obs4 = _rb.linearVelocity.x / _player.MovementSpeed;
        float obs5 = _rb.linearVelocity.y / _player.MaxFallSpeed;
        float obs6 = relativeVelocityToOpponent.x / _player.MovementSpeed;
        float obs7 = relativeVelocityToOpponent.y / _player.MaxFallSpeed;
        float obs8 = _player.IsGrounded ? 1f : 0f;
        float obs9 = _player.JumpCount / (float)_player.MaxJumps;
        //sensor.AddObservation(_player.DodgeCount);

        // Attack observations for self
        GetAttackObservationsArray(attackObservationsSelf, _player.AnimationStateIndex);

        float obs10 = _opponentAgent._player.IsGrounded ? 1f : 0f;
        float obs11 = _opponentAgent._player.JumpCount / (float)_opponentAgent._player.MaxJumps;
        //sensor.AddObservation(_opponentAgent._player.DodgeCount);

        // Attack observations for opponent
        GetAttackObservationsArray(attackObservationsOpponent, _opponentAgent._player.AnimationStateIndex);

        // Print all observations
        /*Debug.LogFormat(
            "[Observations] relPos: ({0:F3}, {1:F3}), dist: {2:F3}, vel: ({3:F3},{4:F3}), relVel: ({5:F3},{6:F3}), grounded: {7:F1}, jumps: {8:F3}, selfAttack: {9}, oppGrounded: {10:F1}, oppJumps: {11:F3}, oppAttack: {12}",
            obs1, obs2, obs3, obs4, obs5, obs6, obs7, obs8, obs9, 
            string.Join(",", attackObsSelf),
            obs10, obs11, 
            string.Join(",", attackObsOpponent)
        );*/

        sensor.AddObservation(obs1);
        sensor.AddObservation(obs2);
        sensor.AddObservation(obs3);
        sensor.AddObservation(obs4);
        sensor.AddObservation(obs5);
        sensor.AddObservation(obs6);
        sensor.AddObservation(obs7);
        sensor.AddObservation(obs8);
        sensor.AddObservation(obs9);
        foreach (float a in attackObservationsSelf)
            sensor.AddObservation(a);
        sensor.AddObservation(obs10);
        sensor.AddObservation(obs11);
        foreach (float a in attackObservationsOpponent)
            sensor.AddObservation(a);

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
        float distanceDelta = distanceToOpponent - newDistanceToOpponent;
        
        if (distanceDelta > 0f)
        {
            // Getting closer - reward
            float closeReward = CloseDistanceReward * distanceDelta;
            AddReward(closeReward);
            rewardThisStep += closeReward;
            //dDebug.Log($"[StickAgent][Reward] CloseDistanceReward: {closeReward:F4} (distance {distanceToOpponent:F2} -> {newDistanceToOpponent:F2})");
        }
        else if (distanceDelta < 0f)
        {
            // Moving away - small penalty
            float distancePenalty = DistancePenalty * Mathf.Abs(distanceDelta);
            AddReward(distancePenalty);
            rewardThisStep += distancePenalty;
            //Debug.Log($"[StickAgent][Reward] DistancePenalty: {distancePenalty:F4} (distance {distanceToOpponent:F2} -> {newDistanceToOpponent:F2})");
        }

        distanceToOpponent = newDistanceToOpponent;

        // Only treat actual attacks (index >= 2), not dodging (index 1)
        var isAttackingNow = _player.AnimationStateIndex >= 2f;
        bool attackStartedThisFrame = isAttackingNow && !isAttacking;

        if (attackStartedThisFrame)
        {
            // AnimationStateIndex is 2, 3, or 4 for attacks, so index 0, 1, 2 in array
            int attackIndex = Mathf.Clamp((int)_player.AnimationStateIndex - 2, 0, AttackRewardDistance.Length - 1);
            
            if (distanceToOpponent < AttackRewardDistance[attackIndex])
            {
                AddReward(AttackReward);
                rewardThisStep += AttackReward;
                //Debug.Log($"[StickAgent][Reward] AttackReward: {AttackReward:F4} (distance {distanceToOpponent:F2} < {AttackRewardDistance[attackIndex]})");
            }
            else
            {
                AddReward(AttackPenalty);
                rewardThisStep += AttackPenalty;
                //Debug.Log($"[StickAgent][Reward] AttackPenalty: {AttackPenalty:F4} (distance {distanceToOpponent:F2} >= {AttackRewardDistance[attackIndex]})");
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

    private void GetAttackObservationsArray(float[] attackObservations, int attackIndex)
    {
        attackIndex = attackIndex - 2;
        attackObservations[0] = attackIndex == 0 ? 1f : 0f;
        attackObservations[1] = attackIndex == 1 ? 1f : 0f;
        attackObservations[2] = attackIndex == 2 ? 1f : 0f;
    }
}
