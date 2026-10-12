using UnityEngine;
using Unity.MLAgents.Actuators;
using UnityEngine.InputSystem;

public class RunningStickAgent : StickAgent
{
    [SerializeField] private Transform _targetPosition;

    private const float StepPenalty = -0.005f;
    private const float CloseDistanceReward = 0.025f;
    private const float DistancePenalty = -0.025f;
    //private const float AttackPenalty = -0.05f;

    protected override int ObservationSize => 32;
    protected override bool UsesDodgeAction => true;

    private readonly float[] attackObservations = new float[3];

    private float distanceToTarget;
    private bool isAttacking;

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        isAttacking = false;
        distanceToTarget = GetDistanceToTarget();
    }

    public override void Reset()
    {
        base.Reset();
        isAttacking = false;
        distanceToTarget = GetDistanceToTarget();
    }

    protected override void GatherObservations()
    {
        var relativePositionToTarget = transform.localPosition - _targetPosition.localPosition;
        var distanceToTarget = Vector2.Distance(transform.localPosition, _targetPosition.localPosition);
        var relativeVelocityToTarget = Rb.linearVelocity;

        var distanceNormalizationFactor = RunningTrainingAcademy.MaxSpawnDistance;

        int i = 0;
        Observations[i++] = relativePositionToTarget.x / distanceNormalizationFactor;
        Observations[i++] = relativePositionToTarget.y / distanceNormalizationFactor;
        Observations[i++] = distanceToTarget / distanceNormalizationFactor;
        Observations[i++] = Rb.linearVelocity.x / Player.MovementSpeed;
        Observations[i++] = Rb.linearVelocity.y / Player.MaxFallSpeed;
        Observations[i++] = relativeVelocityToTarget.x / Player.MovementSpeed;
        Observations[i++] = relativeVelocityToTarget.y / Player.MaxFallSpeed;
        Observations[i++] = Player.IsGrounded ? 1f : 0f;
        Observations[i++] = Player.JumpCount / (float)Player.MaxJumps;

        GetAttackObservationsArray(attackObservations, Player.AnimationStateIndex);

        foreach (float v in attackObservations)
        {
            Observations[i++] = v;
        }

        // No opponent exists in the running task, so keep opponent-state slots zeroed.
        Observations[i++] = 0f; // opponent grounded
        Observations[i++] = 0f; // opponent jump count
        Observations[i++] = 0f; // opponent attack 0
        Observations[i++] = 0f; // opponent attack 1
        Observations[i++] = 0f; // opponent attack 2
        Observations[i++] = Player.DodgeCount / (float)Player.MaxDodges;
        Observations[i++] = 0f; // opponent dodge count
        Observations[i++] = Player.IsDodging ? 1f : 0f;
        Observations[i++] = 0f; // opponent is dodging
    }

    protected override void AddActionRewards()
    {
        AddReward(StepPenalty);

        float newDistanceToTarget = GetDistanceToTarget();
        float distanceDelta = distanceToTarget - newDistanceToTarget;

        if (distanceDelta > 0f)
        {
            AddReward(CloseDistanceReward * distanceDelta);
        }
        else if (distanceDelta < 0f)
        {
            AddReward(DistancePenalty * Mathf.Abs(distanceDelta));
        }

        distanceToTarget = newDistanceToTarget;

        var isAttackingNow = Player.AnimationStateIndex >= 2f;
        if (isAttackingNow && !isAttacking)
        {
            //AddReward(AttackPenalty);
        }

        isAttacking = isAttackingNow;
    }

    private float GetDistanceToTarget()
    {
        return Vector2.Distance(transform.localPosition, _targetPosition.localPosition);
    }

    private void GetAttackObservationsArray(float[] attackObservations, int animationStateIndex)
    {
        int attackIndex = animationStateIndex - 2;
        attackObservations[0] = attackIndex == 0 ? 1f : 0f;
        attackObservations[1] = attackIndex == 1 ? 1f : 0f;
        attackObservations[2] = attackIndex == 2 ? 1f : 0f;
    }

    protected override void WriteHeuristicActions(ActionSegment<int> discreteActions, Keyboard keyboard)
    {
        base.WriteHeuristicActions(discreteActions, keyboard);
        //discreteActions[5] = keyboard.slashKey.isPressed || keyboard.jKey.isPressed ? 1 : 0;
        discreteActions[6] = keyboard.rightShiftKey.isPressed || keyboard.kKey.isPressed ? 1 : 0;
    }
}
