using UnityEngine;
using Unity.MLAgents.Actuators;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Fighting agent variant that can observe and perform dodges.
/// </summary>
public class FightingDodgingAgent : FightingAgent
{
    [SerializeField] private float successfulDodgeReward = 0.05f;
    [SerializeField] private LayerMask hitboxLayerMask = ~0;

    protected override int ObservationSize => 21;
    protected override bool UsesDodgeAction => true;

    private readonly HashSet<Hitbox> rewardedOverlappingHitboxes = new();
    private readonly HashSet<Hitbox> overlappingHitboxesThisFrame = new();
    private readonly Collider2D[] overlapResults = new Collider2D[16];

    private BoxCollider2D playerBoxCollider;

    private void Start()
    {
        playerBoxCollider = GetComponent<BoxCollider2D>();
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        rewardedOverlappingHitboxes.Clear();
        overlappingHitboxesThisFrame.Clear();
    }

    public override void Reset()
    {
        base.Reset();
        rewardedOverlappingHitboxes.Clear();
        overlappingHitboxesThisFrame.Clear();
    }

    private void Update()
    {
        if (Player == null || playerBoxCollider == null || playerBoxCollider.enabled == false)
        {
            rewardedOverlappingHitboxes.Clear();
            return;
        }

        RefreshOverlappingHitboxes();
        RemoveHitboxesThatStoppedOverlapping();

        if (Player.IsDodging == false)
        {
            return;
        }

        foreach (var hitbox in overlappingHitboxesThisFrame)
        {
            if (rewardedOverlappingHitboxes.Add(hitbox))
            {
                AddReward(successfulDodgeReward);
            }
        }
    }

    protected override void AddExtraObservations(ref int observationIndex)
    {
        Observations[observationIndex++] = Player.DodgeCount / (float)Player.MaxDodges;
        Observations[observationIndex++] = OpponentPlayer.DodgeCount / (float)OpponentPlayer.MaxDodges;
        Observations[observationIndex++] = Player.IsDodging ? 1f : 0f;
        Observations[observationIndex++] = OpponentPlayer.IsDodging ? 1f : 0f;
    }

    protected override void WriteHeuristicActions(ActionSegment<int> discreteActions, Keyboard keyboard)
    {
        base.WriteHeuristicActions(discreteActions, keyboard);
        discreteActions[6] = keyboard.rightShiftKey.isPressed || keyboard.kKey.isPressed ? 1 : 0;
    }

    private void RefreshOverlappingHitboxes()
    {
        overlappingHitboxesThisFrame.Clear();

        var bounds = playerBoxCollider.bounds;
        int count = Physics2D.OverlapBoxNonAlloc(
            bounds.center,
            new Vector2(3, 5),
            transform.eulerAngles.z,
            overlapResults,
            hitboxLayerMask
        );

        for (int i = 0; i < count; i++)
        {
            if (overlapResults[i] == playerBoxCollider)
            {
                continue;
            }

            if (overlapResults[i].TryGetComponent(out Hitbox hitbox) == false)
            {
                continue;
            }

            if (hitbox.Owner == transform || hitbox.transform.IsChildOf(transform))
            {
                continue;
            }

            overlappingHitboxesThisFrame.Add(hitbox);
        }
    }

    private void RemoveHitboxesThatStoppedOverlapping()
    {
        rewardedOverlappingHitboxes.RemoveWhere(
            hitbox => hitbox == null || overlappingHitboxesThisFrame.Contains(hitbox) == false
        );
    }
}
