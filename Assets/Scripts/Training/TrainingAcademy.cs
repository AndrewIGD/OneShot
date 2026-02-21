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
                EndEpisode();
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

    private void ResetAgent(StickAgent agent)
    {
        Vector2 spawnPos = GetRandomSpawnPosition();
        agent.transform.position = spawnPos;
        agent.OnEpisodeBegin();
    }

    private Vector2 GetRandomSpawnPosition()
    {
        return new Vector2(Random.Range(minSpawn.position.x, maxSpawn.position.x), Random.Range(minSpawn.position.y, maxSpawn.position.y));
    }
}
