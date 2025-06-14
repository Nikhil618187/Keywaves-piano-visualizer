using UnityEngine;

public class HitLine : MonoBehaviour {
    void OnTriggerEnter(Collider other) {
        other.GetComponent<NoteController>()?.PlayVanishEffect();
    }
}