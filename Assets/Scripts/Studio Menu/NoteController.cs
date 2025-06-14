using UnityEngine;

public class NoteController : MonoBehaviour {
    public float fallSpeed = 2f;
    private float hitLineY = 0f; 

    void Update() {
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

        if (transform.position.y < hitLineY - 10f) Destroy(gameObject);
    }
    public void PlayVanishEffect() {
        GetComponent<Renderer>().material.SetFloat("_ClipY", hitLineY);
    }
}