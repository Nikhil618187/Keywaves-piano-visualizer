using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

public class NoteManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Vector3 spawnOffset = new Vector3(0, 5f, 0);
    public float fallSpeed = 2f;
    public GameObject notePrefab;

    public float _blackkeywidth = 0.6f;
    public float _whitekeywidth = 1f;

    private Dictionary<int, Transform> _midiToLane;
    private List<Coroutine> activeNoteCoroutines = new List<Coroutine>();

    void Awake()
    {
        _midiToLane = new Dictionary<int, Transform>();
        AutoMapLanes();
    }

    void AutoMapLanes()
    {
        foreach (Transform lane in transform)
        {
            string[] parts = lane.name.Split('_');
            if (parts.Length == 3 && int.TryParse(parts[2], out int midiNote))
            {
                _midiToLane[midiNote] = lane;
            }
        }
    }

    public void LoadMidiFromPath(string fullPath)
    {
        if (!System.IO.File.Exists(fullPath))
        {
            Debug.LogError($"MIDI file not found at: {fullPath}");
            return;
        }

        var midiFile = MidiFile.Read(fullPath);
        var tempoMap = midiFile.GetTempoMap();
        var notes = midiFile.GetNotes();
        var timedEvents = midiFile.GetTimedEvents().ToList();

        bool sustainPedalOn = false;

        foreach (var timedEvent in timedEvents)
        {
            var midiEvent = timedEvent.Event;
            double time = TimeConverter.ConvertTo<MetricTimeSpan>(timedEvent.Time, tempoMap).TotalSeconds;

            if (midiEvent is ControlChangeEvent cc && cc.ControlNumber == 64)
            {
                sustainPedalOn = cc.ControlValue >= 64;

                var keyboard = Object.FindFirstObjectByType<VirtualKeyboard>();
                if (keyboard != null)
                {
                    keyboard.SetSustainPedal(sustainPedalOn);
                }
            }

            if (midiEvent is NoteOnEvent noteOn && noteOn.Velocity > 0)
            {
                int midiNote = noteOn.NoteNumber;
                var note = notes.FirstOrDefault(n => n.NoteNumber == midiNote && n.Time == timedEvent.Time);
                if (note == null) continue;

                float visualDuration = (float)note.LengthAs<MetricTimeSpan>(tempoMap).TotalSeconds;
                float audioDuration = visualDuration;

                if (sustainPedalOn)
                {
                    audioDuration += 5f;
                }

                Coroutine routine = StartCoroutine(SpawnNoteDelayed(time, midiNote, visualDuration, audioDuration));
                activeNoteCoroutines.Add(routine);
            }
        }
    }

    IEnumerator SpawnNoteDelayed(double delay, int midiNote, float visualDuration, float audioDuration)
    {
        yield return new WaitForSeconds((float)delay);
        SpawnNote(midiNote, visualDuration, audioDuration);
    }

    void SpawnNote(int midiNote, float visualDuration, float audioDuration)
    {
        if (_midiToLane.TryGetValue(midiNote, out Transform lane))
        {
            GameObject note = Instantiate(notePrefab, lane.position + spawnOffset, Quaternion.identity);

            PlayKey controller = note.GetComponent<PlayKey>();
            if (controller != null)
            {
                controller.midiNote = midiNote;
                controller.duration = audioDuration;
            }

            float width = lane.name.StartsWith("Black_") ? _blackkeywidth : _whitekeywidth;
            float height = visualDuration * fallSpeed;

            note.transform.localScale = new Vector3(width, height, note.transform.localScale.z);
            note.transform.SetParent(lane);
            note.name = lane.name + "_Note";
        }
        else
        {
            Debug.LogWarning($"No lane mapped for MIDI note: {midiNote}");
        }
    }

    public void CancelAllNoteCoroutines()
    {
        foreach (var coroutine in activeNoteCoroutines)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
        activeNoteCoroutines.Clear();
    }

    public void ClearExistingNotes()
    {
        foreach (Transform lane in transform)
        {
            foreach (Transform child in lane)
            {
                if (child.name.EndsWith("_Note") || child.GetComponent<PlayKey>() != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }
}
