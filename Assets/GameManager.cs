using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Transform minSpawn, maxSpawn;

    public static GameManager Instance;

    public Vector2 GetRandomSpawnPosition()
    {
        return new Vector2(Random.Range(minSpawn.position.x, maxSpawn.position.x), Random.Range(minSpawn.position.y, maxSpawn.position.y));
    }

    private void Start()
    {
        Instance = this;
    }
}
