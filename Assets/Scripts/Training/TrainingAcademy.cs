using UnityEngine;
using Unity.MLAgents;
using System.Collections;

public class TrainingAcademy : MonoBehaviour
{
    [SerializeField] private StickAgent[] agents;
    [SerializeField] private Transform minSpawn, maxSpawn;
    [SerializeField] private int maxEpisodeSteps = 10000;

    protected StickAgent[] Agents => agents;

    private int _episodeStepCount;
    private bool _isEpisodeOver;

    private void Start()
    {
        Academy.Instance.OnEnvironmentReset += OnEnvironmentReset;

        OnEnvironmentReset();
    }

    private void OnDestroy()
    {
        if (Academy.IsInitialized)
        {
            Academy.Instance.OnEnvironmentReset -= OnEnvironmentReset;
        }
    }

    protected virtual void FixedUpdate()
    {
        if (maxEpisodeSteps > 0)
        {
            _episodeStepCount++;
            if (_episodeStepCount >= maxEpisodeSteps)
            {
                InterruptEpisode();
            }
        }
    }

    private void OnEnvironmentReset()
    {
        _episodeStepCount = 0;
        
        StartCoroutine(ResetEnvironmentCoroutine());
    }

    protected virtual IEnumerator ResetEnvironmentCoroutine()
    {
        yield return null;

        foreach (var agent in agents)
        {
            ResetAgent(agent);
        }

        _isEpisodeOver = false;
    }

    public void TriggerReset()
    {
        OnEnvironmentReset();
    }

    protected void EndEpisode()
    {
        foreach (var agent in agents)
        {
            agent.EndEpisode();
        }

        OnEnvironmentReset();
    }

    protected void InterruptEpisode()
    {
        if (_isEpisodeOver)
        {
            return;
        }
        _isEpisodeOver = true;

        foreach (var agent in agents)
        {
            agent.EpisodeInterrupted();
        }

        OnEnvironmentReset();
    }

    private void ResetAgent(StickAgent agent)
    {
        Vector2 spawnPos = GetRandomSpawnPosition();
        agent.transform.position = spawnPos;
        agent.Reset();
        agent.OnEpisodeBegin();
    }

    public Vector2 GetRandomSpawnPosition()
    {
        return new Vector2(Random.Range(minSpawn.position.x, maxSpawn.position.x), Random.Range(minSpawn.position.y, maxSpawn.position.y));
    }

    public void OnAgentLose(StickAgent agent)
    {
        if (_isEpisodeOver)
        {
            return;
        }
        _isEpisodeOver = true;

        agent.AddReward(-1f);
        foreach (var a in agents)
        {
            if (a != agent)
            {
                a.AddReward(1f);
                ScoreTracker.RecordWin(a.TeamId);
            }
        }
        EndEpisode();
    }
}
