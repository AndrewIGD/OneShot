using UnityEngine;

public class RunningStickAgent : StickAgent
{
    [SerializeField] private Transform _targetPosition;

    private float[] attackObservations = new float[8];

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

        // Attack observations for self
        GetAttackObservationsArray(attackObservations, Player.AnimationStateIndex);

        for (int i = 0; i < 8; i++)
        {
            Observations[11 + i] = attackObservations[i];
        }
    }

    protected override void AddActionRewards()
    {
        AddReward(-0.005f);
    }

    private void GetAttackObservationsArray(float[] attackObservations, int animationStateIndex)
    {
        for (int i = 0; i < 8; i++)
        {
            attackObservations[i] = animationStateIndex == i ? 1f : 0f;
        }
    }
}
