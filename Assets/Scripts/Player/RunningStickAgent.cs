using UnityEngine;
using Unity.MLAgents.Actuators;
using UnityEngine.InputSystem;

public class RunningStickAgent : StickAgent
{
    [SerializeField] private Transform _targetPosition;

    private const float StepPenalty = -0.005f;
    private const float CloseDistanceReward = 0.025f;
    private const float DistancePenalty = -0.025f;

    protected override int ObservationSize => 32;
    protected override bool UsesDodgeAction => true;

    private readonly float[] _attackObservations = new float[3];
    private float _distanceToTarget;
    private bool _isAttacking;

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        _isAttacking = false;
        _distanceToTarget = GetDistanceToTarget();
    }

    public override void Reset()
    {
        base.Reset();
        _isAttacking = false;
        _distanceToTarget = GetDistanceToTarget();
    }

    protected override void GatherObservations()
    {
        var relativePositionToTarget = transform.localPosition - _targetPosition.localPosition;
        var distanceToTarget = Vector2.Distance(transform.localPosition, _targetPosition.localPosition);
        var relativeVelocityToTarget = Rb.linearVelocity;

        var distanceNormalizationFactor = RunningTrainingAcademy.MaxSpawnDistance;

        var i = 0;
        Observations[i++] = relativePositionToTarget.x / distanceNormalizationFactor;
        Observations[i++] = relativePositionToTarget.y / distanceNormalizationFactor;
        Observations[i++] = distanceToTarget / distanceNormalizationFactor;
        Observations[i++] = Rb.linearVelocity.x / Player.MovementSpeed;
        Observations[i++] = Rb.linearVelocity.y / Player.MaxFallSpeed;
        Observations[i++] = relativeVelocityToTarget.x / Player.MovementSpeed;
        Observations[i++] = relativeVelocityToTarget.y / Player.MaxFallSpeed;
        Observations[i++] = Player.IsGrounded ? 1f : 0f;
        Observations[i++] = Player.JumpCount / (float)Player.MaxJumps;

        GetAttackObservationsArray(_attackObservations, Player.AnimationStateIndex);

        foreach (var v in _attackObservations)
        {
            Observations[i++] = v;
        }

        Observations[i++] = 0f;
        Observations[i++] = 0f;
        Observations[i++] = 0f;
        Observations[i++] = 0f;
        Observations[i++] = 0f;
        Observations[i++] = Player.DodgeCount / (float)Player.MaxDodges;
        Observations[i++] = 0f;
        Observations[i++] = Player.IsDodging ? 1f : 0f;
        Observations[i++] = 0f;
        Observations[i++] = Player.Stamina;
    }

    protected override void AddActionRewards()
    {
        AddReward(StepPenalty * Time.fixedDeltaTime);

        var newDistanceToTarget = GetDistanceToTarget();
        var distanceDelta = _distanceToTarget - newDistanceToTarget;

        if (distanceDelta > 0f)
        {
            AddReward(CloseDistanceReward * distanceDelta);
        }
        else if (distanceDelta < 0f)
        {
            AddReward(DistancePenalty * Mathf.Abs(distanceDelta));
        }

        _distanceToTarget = newDistanceToTarget;

        var isAttackingNow = Player.AnimationStateIndex >= 2f;
        _isAttacking = isAttackingNow;
    }

    private float GetDistanceToTarget()
    {
        return Vector2.Distance(transform.localPosition, _targetPosition.localPosition);
    }

    private void GetAttackObservationsArray(float[] attackObservations, int animationStateIndex)
    {
        var attackIndex = animationStateIndex - 2;
        attackObservations[0] = attackIndex == 0 ? 1f : 0f;
        attackObservations[1] = attackIndex == 1 ? 1f : 0f;
        attackObservations[2] = attackIndex == 2 ? 1f : 0f;
    }

    protected override void WriteHeuristicActions(ActionSegment<int> discreteActions, Keyboard keyboard)
    {
        base.WriteHeuristicActions(discreteActions, keyboard);
        discreteActions[6] = keyboard.rightShiftKey.isPressed || keyboard.kKey.isPressed ? 1 : 0;
    }
}
