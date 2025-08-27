using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlastZone : MonoBehaviour
{
    [SerializeField] GameObject particles;
    [SerializeField] float orientation;

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out Player player) == false)
            return;

        player.Blast();

        StartCoroutine(BlastParticles(player.transform.position));
    }

    IEnumerator BlastParticles(Vector2 position)
    {
        GameObject particles = Instantiate(this.particles, position, Quaternion.Euler(0, 0, orientation));

        NetworkServer.Spawn(particles);

        yield return new WaitForSeconds(3);

        NetworkServer.Destroy(particles);
    }
}
