using UnityEngine;
using Unity.MLAgents.Actuators;
using UnityEngine.InputSystem;

public class FightingStickV2 : StickAgent
{
    protected const float MaxArenaDistance = 50f;
    private const float StepPenalty = -0.001f;
    private const float CloseDistanceReward = 0.01f;
    private const float DistancePenalty = -0.01f;
    private const float AttackReward = 0.05f;
    private const float AttackPenalty = -0.02f;
    private const float HitReward = 0.5f;

    private static readonly float[] AttackRewardDistances = { 5f, 9f, 6f, 5f, 8f, 6f };

    protected override int ObservationSize => 40;
    protected override bool UsesDodgeAction => false;

    private readonly float[] _attackObservations = new float[8];
    private readonly float[] _opponentAttackObservations = new float[8];
    private float _distanceToOpponent;
    private Vector2 _previousPosition;
    private bool _isAttacking;

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        PairOpponent();
        _isAttacking = false;
        _previousPosition = transform.position;
        _distanceToOpponent = Vector2.Distance(transform.position, Opponent.transform.position);
    }

    public override void Reset()
    {
        base.Reset();
        PairOpponent();
        _isAttacking = false;
        _previousPosition = transform.position;
        _distanceToOpponent = Vector2.Distance(transform.position, Opponent.transform.position);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var discreteActions = actions.DiscreteActions;

        var down = discreteActions[0] == 1;
        var left = discreteActions[1] == 1;
        var right = discreteActions[2] == 1;
        var up = discreteActions[3] == 1;
        var jump = discreteActions.Length > 5 ? discreteActions[4] == 1 : up;
        var attack = discreteActions.Length > 5 ? discreteActions[5] == 1 : discreteActions[4] == 1;

        InputDevice.SetState(up, down, left, right, jump, false, down, attack);
        InputDevice.Update();

        AddActionRewards();
    }

    protected override void GatherObservations()
    {
        PairOpponent();

        var opponentPosition = Opponent.transform.position;
        var opponentRb = Opponent.AgentRigidbody;
        var opponentPlayer = Opponent.AgentPlayer;

        var relativePosition = (Vector2)(opponentPosition - transform.position);
        var distance = relativePosition.magnitude;
        var relativeVelocity = opponentRb.linearVelocity - Rb.linearVelocity;

        var i = 0;
        Observations[i++] = relativePosition.x / MaxArenaDistance;
        Observations[i++] = relativePosition.y / MaxArenaDistance;
        Observations[i++] = distance / MaxArenaDistance;
        Observations[i++] = Rb.linearVelocity.x / Player.MovementSpeed;
        Observations[i++] = Rb.linearVelocity.y / Player.MaxFallSpeed;
        Observations[i++] = relativeVelocity.x / Player.MovementSpeed;
        Observations[i++] = relativeVelocity.y / Player.MaxFallSpeed;
        Observations[i++] = Player.IsGrounded ? 1f : 0f;
        PopulateResourceObservations(ref i, Player);
        Observations[i++] = transform.localEulerAngles.y > 90f ? 1f : 0f;

        PopulateAttackObservations(_attackObservations, Player.AnimationStateIndex);
        foreach (var value in _attackObservations)
        {
            Observations[i++] = value;
        }

        Observations[i++] = Player.Stamina;

        Observations[i++] = -relativePosition.x / MaxArenaDistance;
        Observations[i++] = -relativePosition.y / MaxArenaDistance;
        Observations[i++] = distance / MaxArenaDistance;
        Observations[i++] = opponentRb.linearVelocity.x / opponentPlayer.MovementSpeed;
        Observations[i++] = opponentRb.linearVelocity.y / opponentPlayer.MaxFallSpeed;
        Observations[i++] = -relativeVelocity.x / opponentPlayer.MovementSpeed;
        Observations[i++] = -relativeVelocity.y / opponentPlayer.MaxFallSpeed;
        Observations[i++] = opponentPlayer.IsGrounded ? 1f : 0f;
        PopulateResourceObservations(ref i, opponentPlayer);
        Observations[i++] = Opponent.transform.localEulerAngles.y > 90f ? 1f : 0f;

        PopulateAttackObservations(_opponentAttackObservations, opponentPlayer.AnimationStateIndex);
        foreach (var value in _opponentAttackObservations)
        {
            Observations[i++] = value;
        }

        Observations[i++] = opponentPlayer.Stamina;
    }

    protected override void AddActionRewards()
    {
        AddReward(StepPenalty * Time.fixedDeltaTime);

        var currentPosition = (Vector2)transform.position;
        var movement = currentPosition - _previousPosition;
        var toOpponent = ((Vector2)Opponent.transform.position - _previousPosition).normalized;
        var approachDelta = Vector2.Dot(movement, toOpponent);
        _previousPosition = currentPosition;

        if (approachDelta > 0f)
        {
            AddReward(CloseDistanceReward * approachDelta);
        }
        else if (approachDelta < 0f)
        {
            AddReward(DistancePenalty * Mathf.Abs(approachDelta));
        }

        _distanceToOpponent = Vector2.Distance(transform.position, Opponent.transform.position);

        var animIndex = Player.AnimationStateIndex;
        var isAttackingNow = animIndex >= 0 && animIndex <= 5;
        var attackStartedThisFrame = isAttackingNow && !_isAttacking;

        if (attackStartedThisFrame)
        {
            var attackIndex = Mathf.Clamp(animIndex, 0, AttackRewardDistances.Length - 1);
            if (_distanceToOpponent < AttackRewardDistances[attackIndex])
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

    public override void OnHitLanded(Player target)
    {
        AddReward(HitReward);
    }

    protected override void WriteHeuristicActions(ActionSegment<int> discreteActions, Keyboard keyboard)
    {
        discreteActions[0] = keyboard.sKey.isPressed ? 1 : 0;
        discreteActions[1] = keyboard.aKey.isPressed ? 1 : 0;
        discreteActions[2] = keyboard.dKey.isPressed ? 1 : 0;
        discreteActions[3] = keyboard.wKey.isPressed ? 1 : 0;

        var attack = keyboard.slashKey.isPressed || keyboard.jKey.isPressed;
        if (discreteActions.Length > 5)
        {
            discreteActions[4] = keyboard.spaceKey.isPressed ? 1 : 0;
            discreteActions[5] = attack ? 1 : 0;
            if (discreteActions.Length > 6)
            {
                discreteActions[6] = 0;
            }
        }
        else
        {
            discreteActions[4] = attack ? 1 : 0;
        }
    }

    private void PairOpponent()
    {
        if (Opponent != null)
        {
            return;
        }

        var academy = GetComponentInParent<TrainingAcademy>();
        foreach (var agent in academy.GetComponentsInChildren<StickAgent>())
        {
            if (agent != this)
            {
                SetupOpponent(agent);
                break;
            }
        }
    }

    private void PopulateAttackObservations(float[] array, int animationStateIndex)
    {
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = animationStateIndex == i ? 1f : 0f;
        }
    }

    protected virtual void PopulateResourceObservations(ref int index, Player player)
    {
        Observations[index++] = 0f;
        Observations[index++] = 0f;
    }
}
