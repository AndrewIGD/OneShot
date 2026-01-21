using UnityEngine;
using Unity.MLAgents;

public class TrainingAcademy : MonoBehaviour
{
    [SerializeField] private StickAgent agent1;
    [SerializeField] private StickAgent agent2;
    [SerializeField] private Transform minSpawn, maxSpawn;
    [SerializeField] private int maxEpisodeSteps = 10000;

    private const float WinReward = 1f;
    private int _episodeStepCount;

    private void Start()
    {
        if (agent1 == null || agent2 == null)
        {
            StickAgent[] agents = FindObjectsOfType<StickAgent>();
            if (agents.Length >= 2)
            {
                agent1 = agents[0];
                agent2 = agents[1];
            }
            else
            {
                Debug.LogWarning("TrainingAcademy: Not enough StickAgent instances found in scene. Need 2 agents for 1v1 training.");
            }
        }

        agent1.Setup(agent2);
        agent2.Setup(agent1);

        Academy.Instance.OnEnvironmentReset += OnEnvironmentReset;
    }

    private void OnDestroy()
    {
        if (Academy.IsInitialized)
        {
            Academy.Instance.OnEnvironmentReset -= OnEnvironmentReset;
        }
    }

    private void FixedUpdate()
    {
        if (maxEpisodeSteps > 0)
        {
            _episodeStepCount++;
            if (_episodeStepCount >= maxEpisodeSteps)
            {
                EndEpisodeDueToTimeout();
            }
        }
    }

    private void OnEnvironmentReset()
    {
        _episodeStepCount = 0;
        
        if (agent1 != null && agent2 != null)
        {
            ResetAgent(agent1);
            ResetAgent(agent2);
        }
    }

    public void TriggerReset()
    {
        OnEnvironmentReset();
    }

    private void EndEpisodeDueToTimeout()
    {
        if (agent1 != null && agent2 != null)
        {
            agent1.EpisodeInterrupted();
            agent2.EpisodeInterrupted();

            OnEnvironmentReset();
        }
    }

    private void ResetAgent(StickAgent agent)
    {
        if (agent == null) return;

        Vector2 spawnPos = GetRandomSpawnPosition();
        agent.transform.position = spawnPos;
        agent.Reset();
    }

    public void OnAgentHit(StickAgent winningAgent)
    {
        winningAgent.AddReward(WinReward);
        winningAgent.EndEpisode();

        if (winningAgent == agent1)
        {
            agent2.AddReward(-WinReward);
            agent2.EndEpisode();
        }
        else
        {
            agent1.AddReward(-WinReward);
            agent1.EndEpisode();
        }

        // Manually reset agents immediately after ending episodes
        // The Academy will handle the episode transition, but we need to reset positions now
        TriggerReset();
    }

    public void OnAgentLose(StickAgent losingAgent)
    {
        losingAgent.AddReward(-WinReward);
        agent1.EndEpisode();
        agent2.EndEpisode();

        // Manually reset agents immediately after ending episodes
        // The Academy will handle the episode transition, but we need to reset positions now
        TriggerReset();
    }

    private Vector2 GetRandomSpawnPosition()
    {
        return new Vector2(Random.Range(minSpawn.position.x, maxSpawn.position.x), Random.Range(minSpawn.position.y, maxSpawn.position.y));
    }
}
