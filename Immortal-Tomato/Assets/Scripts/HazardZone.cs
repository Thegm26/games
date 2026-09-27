using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class HazardZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        var tomato = other.GetComponent<TomatoGame>();
        if (tomato != null) tomato.HazardRespawn(tomato.CurrentCheckpoint);
    }
}
