using UnityEngine;
using Unity.MLAgents.Actuators;
using UnityEngine.InputSystem;

/// <summary>
/// PvP stick fighter: observations and rewards vs <see cref="_opponentAgent"/>.
/// Assign the other player's <see cref="StickAgent"/> in the inspector on both agents (cross-reference).
/// Observations follow the legacy layout (17 floats) then zero padding to match scenes using VectorObservationSize 38.
/// </summary>
public class FightingAgent : StickAgent
{
    [SerializeField] private StickAgent _opponentAgent;

    private const float CloseDistanceReward = 0.001f;
    private const float DistancePenalty = -0.0005f;
    private const float AttackReward = 0.05f;
    private const float AttackPenalty = -0.02f;
    protected const float DistanceNormalizationFactor = 20f;

    private static readonly float[] AttackRewardDistance = { 9f, 5f, 6f };

    private readonly float[] attackObservationsSelf = new float[3];
    private readonly float[] attackObservationsOpponent = new float[3];

    private float distanceToOpponent;
    private bool isAttacking;

    private void Awake()
    {
        if (_opponentAgent != null)
        {
            SetupOpponent(_opponentAgent);
        }
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        isAttacking = false;
        if (Opponent != null)
        {
            distanceToOpponent = Vector2.Distance(transform.position, Opponent.transform.position);
        }
    }

    public override void Reset()
    {
        base.Reset();
        if (Opponent != null)
        {
            distanceToOpponent = Vector2.Distance(transform.position, Opponent.transform.position);
        }
    }

    protected override void GatherObservations()
    {
        if (Opponent == null)
        {
            return;
        }

        var oppRb = Opponent.AgentRigidbody;
        var oppPlayer = OpponentPlayer;

        var relativePositionToOpponent = transform.localPosition - Opponent.transform.localPosition;
        var distanceToOpp = Vector2.Distance(transform.localPosition, Opponent.transform.localPosition);
        var relativeVelocityToOpponent = Rb.linearVelocity - oppRb.linearVelocity;

        int i = 0;
        Observations[i++] = relativePositionToOpponent.x / DistanceNormalizationFactor;
        Observations[i++] = relativePositionToOpponent.y / DistanceNormalizationFactor;
        Observations[i++] = distanceToOpp / DistanceNormalizationFactor;
        Observations[i++] = Rb.linearVelocity.x / Player.MovementSpeed;
        Observations[i++] = Rb.linearVelocity.y / Player.MaxFallSpeed;
        Observations[i++] = relativeVelocityToOpponent.x / Player.MovementSpeed;
        Observations[i++] = relativeVelocityToOpponent.y / Player.MaxFallSpeed;
        Observations[i++] = Player.IsGrounded ? 1f : 0f;
        Observations[i++] = Player.JumpCount / (float)Player.MaxJumps;

        GetAttackObservationsArray(attackObservationsSelf, Player.AnimationStateIndex);

        foreach (float v in attackObservationsSelf)
        {
            Observations[i++] = v;
        }

        Observations[i++] = oppPlayer.IsGrounded ? 1f : 0f;
        Observations[i++] = oppPlayer.JumpCount / (float)oppPlayer.MaxJumps;

        GetAttackObservationsArray(attackObservationsOpponent, oppPlayer.AnimationStateIndex);

        foreach (float v in attackObservationsOpponent)
        {
            Observations[i++] = v;
        }

        AddExtraObservations(ref i);
    }

    protected override void AddActionRewards()
    {
        if (Opponent == null)
        {
            return;
        }

        var newDistanceToOpponent = Vector2.Distance(transform.position, Opponent.transform.position);
        float distanceDelta = distanceToOpponent - newDistanceToOpponent;

        if (distanceDelta > 0f)
        {
            AddReward(CloseDistanceReward * distanceDelta);
        }
        else if (distanceDelta < 0f)
        {
            AddReward(DistancePenalty * Mathf.Abs(distanceDelta));
        }

        distanceToOpponent = newDistanceToOpponent;

        var isAttackingNow = Player.AnimationStateIndex >= 2f;
        bool attackStartedThisFrame = isAttackingNow && !isAttacking;

        if (attackStartedThisFrame)
        {
            int attackIndex = Mathf.Clamp((int)Player.AnimationStateIndex - 2, 0, AttackRewardDistance.Length - 1);

            if (distanceToOpponent < AttackRewardDistance[attackIndex])
            {
                AddReward(AttackReward);
            }
            else
            {
                AddReward(AttackPenalty);
            }
        }

        isAttacking = isAttackingNow;
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

    private static void GetAttackObservationsArray(float[] attackObservations, int animationStateIndex)
    {
        int attackIndex = animationStateIndex - 2;
        attackObservations[0] = attackIndex == 0 ? 1f : 0f;
        attackObservations[1] = attackIndex == 1 ? 1f : 0f;
        attackObservations[2] = attackIndex == 2 ? 1f : 0f;
    }
}
