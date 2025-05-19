using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attraction_Field : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.CompareTag("Player"))
        {
            StartCoroutine(AttractCoruntine(collider.transform));
        }
    }

    private IEnumerator AttractCoruntine(Transform player_transform)
    {
        while (Vector2.Distance(transform.position, player_transform.position) > 0.1f)
        {
            player_transform.position = Vector2.MoveTowards(player_transform.position, transform.position, 0.2f);
            yield return null;
        }
    }
}
