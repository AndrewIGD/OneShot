
using System.Collections;
using UnityEngine;

public class BlastZone : MonoBehaviour
{
    [SerializeField] GameObject particles;
    [SerializeField] float orientation;
    [SerializeField] TrainingAcademy academy;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out Player player) == false)
            return;

        if (player.TryGetComponent(out StickAgent agent))
            academy.OnAgentLose(agent);

        //player.Blast();

        //StartCoroutine(BlastParticles(player.transform.position));
    }

    IEnumerator BlastParticles(Vector2 position)
    {
        GameObject particles = Instantiate(this.particles, position, Quaternion.Euler(0, 0, orientation));

        yield return new WaitForSeconds(3);

        Destroy(particles);
    }
}
