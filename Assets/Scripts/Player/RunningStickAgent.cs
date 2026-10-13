using UnityEngine;

public class RunningStickAgent : StickAgent
{
    [SerializeField] private Transform _targetPosition;

    private float[] _attackObservations = new float[8];

    protected override void GatherObservations()
    {
        var relativePositionToTarget = transform.localPosition - _targetPosition.localPosition;
        var distanceToTarget = Vector2.Distance(transform.localPosition, _targetPosition.localPosition);
        var relativeVelocityToTarget = Rb.linearVelocity;

        var distanceNormalizationFactor = RunningTrainingAcademy.MaxSpawnDistance;

        Observations[0] = relativePositionToTarget.x / distanceNormalizationFactor;
        Observations[1] = relativePositionToTarget.y / distanceNormalizationFactor;
        Observations[2] = distanceToTarget / distanceNormalizationFactor;
        Observations[3] = Rb.linearVelocity.x / Player.MovementSpeed;
        Observations[4] = Rb.linearVelocity.y / Player.MaxFallSpeed;
        Observations[5] = relativeVelocityToTarget.x / Player.MovementSpeed;
        Observations[6] = relativeVelocityToTarget.y / Player.MaxFallSpeed;
        Observations[7] = Player.IsGrounded ? 1f : 0f;
        Observations[8] = Player.JumpCount / (float)Player.MaxJumps;
        Observations[9] = Player.DodgeCount / (float)Player.MaxDodges;
        Observations[10] = Player.transform.localEulerAngles.y > 90f ? 1f : 0f;

        GetAttackObservationsArray(_attackObservations, Player.AnimationStateIndex);

        for (var i = 0; i < 8; i++)
        {
            Observations[11 + i] = _attackObservations[i];
        }

        Observations[19] = Player.Stamina;
    }

    protected override void AddActionRewards()
    {
        AddReward(-0.25f * Time.fixedDeltaTime);
    }

    private void GetAttackObservationsArray(float[] attackObservations, int animationStateIndex)
    {
        for (var i = 0; i < 8; i++)
        {
            attackObservations[i] = animationStateIndex == i ? 1f : 0f;
        }
    }
}
