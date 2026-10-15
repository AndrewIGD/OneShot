using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class Hitbox : MonoBehaviour
{
    [SerializeField] private Transform parent;
    [SerializeField] private Vector2 launchDir;
    [SerializeField] private float launchSpeed;
    [SerializeField] private GameObject particles;

    private List<Player> _hit = new List<Player>();
    private BoxCollider2D _coll;

    public Transform Owner => parent;

    private void Awake()
    {
        _coll = GetComponent<BoxCollider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out Player player) == false)
        {
            return;
        }

        if (_hit.Contains(player))
        {
            return;
        }

        if (player.transform == parent)
        {
            return;
        }

        _hit.Add(player);

        if (parent.TryGetComponent(out StickAgent attackerAgent))
        {
            attackerAgent.OnHitLanded(player);
        }

        player.Launch(new Vector2(launchDir.x * (parent.transform.eulerAngles.y > 90 ? -1 : 1), launchDir.y) * launchSpeed);

        var isTraining = TrainingModeChecker.IsTrainingMode;
        if (!isTraining)
        {
            StartCoroutine(BlastParticles(collision.ClosestPoint(transform.position)));
        }
    }

    private IEnumerator BlastParticles(Vector2 position)
    {
        var particleObject = Instantiate(particles, position, Quaternion.identity);

        yield return new WaitForSeconds(3);

        Destroy(particleObject);
    }

    private void Update()
    {
        if (_hit.Count != 0 && _coll.enabled == false)
        {
            _hit.Clear();
        }
    }
}
