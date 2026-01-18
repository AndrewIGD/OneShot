using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Hitbox : MonoBehaviour
{
    [SerializeField] Transform parent;
    [SerializeField] Vector2 launchDir;
    [SerializeField] float launchSpeed;
    [SerializeField] GameObject particles;

    List<Player> hit = new List<Player>();

    BoxCollider2D coll;

    private void Awake()
    {
        coll = GetComponent<BoxCollider2D>();
    }

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out Player player) == false)
            return;

        if (hit.Contains(player))
            return;

        if (player.transform == parent)
            return;

        hit.Add(player);

        player.Launch(new Vector2(launchDir.x * (parent.transform.eulerAngles.y > 90 ? -1 : 1), launchDir.y) * launchSpeed);

        StartCoroutine(BlastParticles(collision.ClosestPoint(transform.position)));
    }

    IEnumerator BlastParticles(Vector2 position)
    {
        GameObject particles = Instantiate(this.particles, position, Quaternion.identity);

        NetworkServer.Spawn(particles);

        yield return new WaitForSeconds(3);

        NetworkServer.Destroy(particles);
    }

    private void Update()
    {
        if (hit.Count != 0 && coll.enabled == false)
            hit.Clear();
    }
}
