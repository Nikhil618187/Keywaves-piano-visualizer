using UnityEngine;

public class PlayKey : MonoBehaviour
{
    public int midiNote;
    public float duration; // Audio duration 
    private bool hasPlayed = false;

    public float triggerY = -3f;
    public float fallSpeed = 2f; // NoteManager’s fallSpeed

    void Update()
    {
        if (!hasPlayed && transform.position.y < triggerY)
        {
            hasPlayed = true;
            PlayAndReleaseKey();
        }
    }

    void PlayAndReleaseKey()
    {
        VirtualKeyboard keyboard = Object.FindFirstObjectByType<VirtualKeyboard>();
        if (keyboard != null)
        {
            float visualDuration = transform.localScale.y / fallSpeed;
            keyboard.PlayNoteWithVisualDuration(midiNote, duration, visualDuration);
        }
    }
}
