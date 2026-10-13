using System.Collections;
using UnityEngine;

public class BlastZone : MonoBehaviour
{
    [SerializeField] private GameObject particles;
    [SerializeField] private float orientation;
    [SerializeField] private TrainingAcademy academy;

    private void Awake()
    {
        if (academy == null)
        {
            academy = GetComponentInParent<TrainingAcademy>();
        }

        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandleContact(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        HandleContact(collision);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleContact(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        HandleContact(collision.collider);
    }

    private void HandleContact(Collider2D collision)
    {
        var agent = collision.GetComponentInParent<StickAgent>();
        if (agent == null && collision.attachedRigidbody != null)
        {
            agent = collision.attachedRigidbody.GetComponent<StickAgent>();
        }

        if (agent != null)
        {
            var targetAcademy = academy != null ? academy : agent.GetComponentInParent<TrainingAcademy>();
            targetAcademy.OnAgentLose(agent);
            return;
        }

        var player = collision.GetComponentInParent<Player>();
        if (player == null && collision.attachedRigidbody != null)
        {
            player = collision.attachedRigidbody.GetComponent<Player>();
        }

        if (player != null)
        {
            player.Blast();

            if (particles != null)
            {
                StartCoroutine(BlastParticles(player.transform.position));
            }
        }
    }

    private IEnumerator BlastParticles(Vector2 position)
    {
        var particleInstance = Instantiate(particles, position, Quaternion.Euler(0, 0, orientation));

        yield return new WaitForSeconds(3f);

        Destroy(particleInstance);
    }
}
