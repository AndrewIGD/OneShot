using UnityEngine;

public class DodgingStickV2 : FightingStickV2
{
    protected override int ObservationSize => 50;
    protected override bool UsesDodgeAction => false;

    private TrainingAcademy _academy;

    public override void Initialize()
    {
        base.Initialize();
        _academy = GetComponentInParent<TrainingAcademy>();
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        _academy = GetComponentInParent<TrainingAcademy>();
    }

    protected override void GatherObservations()
    {
        base.GatherObservations();

        var index = 40;
        PopulateExtraObservations(ref index);
    }

    protected virtual void PopulateExtraObservations(ref int index)
    {
        var localPosition = (Vector2)_academy.transform.InverseTransformPoint(transform.position);
        Observations[index++] = localPosition.x / MaxArenaDistance;
        Observations[index++] = localPosition.y / MaxArenaDistance;

        while (index < 50)
        {
            Observations[index++] = 0f;
        }
    }

    protected override void PopulateResourceObservations(ref int index, Player player)
    {
        Observations[index++] = player.JumpCount / 2f;
        Observations[index++] = 0f;
    }
}
