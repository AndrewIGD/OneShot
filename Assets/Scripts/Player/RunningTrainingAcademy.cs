using UnityEngine;
using System.Collections;
 
public class RunningTrainingAcademy : TrainingAcademy
{
    [SerializeField] private Transform _targetPosition;

    public static float MaxSpawnDistance = 17f;

    private const float MaxWidth = 15f;
    private const float MaxHeight = 6f;

    private StickAgent Agent => Agents[0];

    protected override IEnumerator ResetEnvironmentCoroutine()
    {
        _targetPosition.position = GetRandomTargetSpawnPosition();

        yield return base.ResetEnvironmentCoroutine();
    }

    private Vector2 GetRandomTargetSpawnPosition()
    {
        return new Vector2(Random.Range(-MaxWidth, MaxWidth), Random.Range(0, MaxHeight));
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (Vector2.Distance(Agent.transform.localPosition, _targetPosition.localPosition) > 3f)
            return;

        Agent.AddReward(1f);
        EndEpisode();
    }
}
