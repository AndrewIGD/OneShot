using UnityEngine;
using Unity.MLAgents.Actuators;
using UnityEngine.InputSystem;

public class FightingAgent : StickAgent
{
    [SerializeField] private StickAgent _opponentAgent;

    private const float CloseDistanceReward = 0.001f;
    private const float DistancePenalty = -0.0005f;
    private const float AttackReward = 0.05f;
    private const float AttackPenalty = -0.02f;
    protected const float DistanceNormalizationFactor = 20f;

    private static readonly float[] AttackRewardDistance = { 9f, 5f, 6f };

    private readonly float[] _attackObservationsSelf = new float[3];
    private readonly float[] _attackObservationsOpponent = new float[3];

    private float _distanceToOpponent;
    private bool _isAttacking;

    private void Awake()
    {
        PairOpponent();
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        _isAttacking = false;
        PairOpponent();
        _distanceToOpponent = Vector2.Distance(transform.position, Opponent.transform.position);
    }

    public override void Reset()
    {
        base.Reset();
        PairOpponent();
        _distanceToOpponent = Vector2.Distance(transform.position, Opponent.transform.position);
    }

    protected override void GatherObservations()
    {
        PairOpponent();

        var oppRb = Opponent.AgentRigidbody;
        var oppPlayer = OpponentPlayer;

        var relativePositionToOpponent = transform.localPosition - Opponent.transform.localPosition;
        var distanceToOpp = Vector2.Distance(transform.localPosition, Opponent.transform.localPosition);
        var relativeVelocityToOpponent = Rb.linearVelocity - oppRb.linearVelocity;

        var i = 0;
        Observations[i++] = relativePositionToOpponent.x / DistanceNormalizationFactor;
        Observations[i++] = relativePositionToOpponent.y / DistanceNormalizationFactor;
        Observations[i++] = distanceToOpp / DistanceNormalizationFactor;
        Observations[i++] = Rb.linearVelocity.x / Player.MovementSpeed;
        Observations[i++] = Rb.linearVelocity.y / Player.MaxFallSpeed;
        Observations[i++] = relativeVelocityToOpponent.x / Player.MovementSpeed;
        Observations[i++] = relativeVelocityToOpponent.y / Player.MaxFallSpeed;
        Observations[i++] = Player.IsGrounded ? 1f : 0f;
        Observations[i++] = Player.JumpCount / (float)Player.MaxJumps;

        GetAttackObservationsArray(_attackObservationsSelf, Player.AnimationStateIndex);

        foreach (var v in _attackObservationsSelf)
        {
            Observations[i++] = v;
        }

        Observations[i++] = oppPlayer.IsGrounded ? 1f : 0f;
        Observations[i++] = oppPlayer.JumpCount / (float)oppPlayer.MaxJumps;

        GetAttackObservationsArray(_attackObservationsOpponent, oppPlayer.AnimationStateIndex);

        foreach (var v in _attackObservationsOpponent)
        {
            Observations[i++] = v;
        }

        AddExtraObservations(ref i);
    }

    protected override void AddActionRewards()
    {
        var newDistanceToOpponent = Vector2.Distance(transform.position, Opponent.transform.position);
        var distanceDelta = _distanceToOpponent - newDistanceToOpponent;

        if (distanceDelta > 0f)
        {
            AddReward(CloseDistanceReward * distanceDelta);
        }
        else if (distanceDelta < 0f)
        {
            AddReward(DistancePenalty * Mathf.Abs(distanceDelta));
        }

        _distanceToOpponent = newDistanceToOpponent;

        var isAttackingNow = Player.AnimationStateIndex >= 2f;
        var attackStartedThisFrame = isAttackingNow && !_isAttacking;

        if (attackStartedThisFrame)
        {
            var attackIndex = Mathf.Clamp((int)Player.AnimationStateIndex - 2, 0, AttackRewardDistance.Length - 1);

            if (_distanceToOpponent < AttackRewardDistance[attackIndex])
            {
                AddReward(AttackReward);
            }
            else
            {
                AddReward(AttackPenalty);
            }
        }

        _isAttacking = isAttackingNow;
    }

    protected override void WriteHeuristicActions(ActionSegment<int> discreteActions, Keyboard keyboard)
    {
        discreteActions[0] = keyboard.sKey.isPressed ? 1 : 0;
        discreteActions[1] = keyboard.aKey.isPressed ? 1 : 0;
        discreteActions[2] = keyboard.dKey.isPressed ? 1 : 0;
        discreteActions[3] = keyboard.wKey.isPressed ? 1 : 0;
        discreteActions[4] = keyboard.spaceKey.isPressed ? 1 : 0;
        discreteActions[5] = keyboard.slashKey.isPressed || keyboard.jKey.isPressed ? 1 : 0;
    }

    protected Player OpponentPlayer => Opponent.AgentPlayer;

    protected virtual void AddExtraObservations(ref int observationIndex)
    {
    }

    protected void PairOpponent()
    {
        if (Opponent != null)
        {
            return;
        }

        if (_opponentAgent != null)
        {
            SetupOpponent(_opponentAgent);
            return;
        }

        var academy = GetComponentInParent<TrainingAcademy>();
        var agents = academy.GetComponentsInChildren<StickAgent>();
        for (var i = 0; i < agents.Length; i++)
        {
            if (agents[i] != this)
            {
                SetupOpponent(agents[i]);
                break;
            }
        }
    }

    private static void GetAttackObservationsArray(float[] attackObservations, int animationStateIndex)
    {
        var attackIndex = animationStateIndex - 2;
        attackObservations[0] = attackIndex == 0 ? 1f : 0f;
        attackObservations[1] = attackIndex == 1 ? 1f : 0f;
        attackObservations[2] = attackIndex == 2 ? 1f : 0f;
    }
}
