using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CheckpointTrigger : MonoBehaviour
{
    [SerializeField] Vector2 respawnOffset = new(0f, -.94f);

    void OnTriggerEnter2D(Collider2D other)
    {
        var tomato = other.GetComponent<TomatoGame>();
        // The trigger is tall for forgiving discovery; its center is not a
        // valid standing position.  Store the matching counter-top location.
        if (tomato != null) tomato.SetCheckpoint(transform.position + (Vector3)respawnOffset);
    }
}
