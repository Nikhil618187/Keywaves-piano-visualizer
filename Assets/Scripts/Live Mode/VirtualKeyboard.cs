using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
[System.Runtime.Serialization.DataContract]
public class KeyData
{
    public string keyName;
    public int midiNumber;
    public int octave;
    public AudioClip audioClip;
    public Button keyButton;
    public bool isBlackKey;

    public KeyData(string name, int midi, int oct, bool isBlack)
    {
        keyName = name;
        midiNumber = midi;
        octave = oct;
        isBlackKey = isBlack;
    }
}

public class VirtualKeyboard : MonoBehaviour
{
    [Header("Audio Settings")]
    [Range(0f, 1f)] public float volume = 0.8f;

    [Header("Polyphony Settings")]
    [Range(10, 64)] public int maxPolyphony = 32;

    [Header("Visual Settings")]
    public Color pressedColor = Color.gray;
    public Color normalWhiteColor = Color.white;
    public Color normalBlackColor = Color.black;

    [Header("Keyboard Data")]
    public List<KeyData> keys = new List<KeyData>();

    private AudioSource[] audioSources;
    private int currentSourceIndex = 0;
    private Dictionary<int, KeyData> midiToKeyMap = new Dictionary<int, KeyData>();
    private List<AudioSource> activeVoices = new List<AudioSource>();
    private Dictionary<int, bool> keysPressed = new Dictionary<int, bool>();

    private bool sustainPedal = false;
    private int totalNotesPlayed = 0;

    void Start()
    {
        SetupAudioSources();
        InitializeKeyboard();
    }

    void SetupAudioSources()
    {
        audioSources = new AudioSource[maxPolyphony];
        for (int i = 0; i < maxPolyphony; i++)
        {
            GameObject obj = new GameObject($"Voice_{i}");
            obj.transform.SetParent(transform);
            AudioSource source = obj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.volume = volume;
            source.spatialBlend = 0f;
            audioSources[i] = source;
        }
    }

    void InitializeKeyboard()
    {
        AudioClip[] clips = Resources.LoadAll<AudioClip>("Audio/Keys");
        Button[] buttons = GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            KeyData key = ParseButtonName(button.name);
            if (key != null)
            {
                key.audioClip = FindMatchingAudioClip(clips, key);
                if (key.audioClip != null)
                {
                    key.keyButton = button;
                    keys.Add(key);
                    midiToKeyMap[key.midiNumber] = key;
                    int midi = key.midiNumber;
                    button.onClick.AddListener(() => PlayNote(midi));
                }
            }
        }
    }

    KeyData ParseButtonName(string name)
    {
        string[] parts = name.Split('_');
        if (parts.Length != 3) return null;

        if (!int.TryParse(parts[2], out int midi)) return null;
        bool isBlack = parts[0].ToLower().Contains("black");

        string label = parts[1];
        int octave = -1;
        string key = "";

        for (int i = label.Length - 1; i >= 0; i--)
        {
            if (char.IsDigit(label[i]))
            {
                octave = int.Parse(label[i].ToString());
                key = label.Substring(0, i);
                break;
            }
        }

        if (octave == -1) return null;

        return new KeyData(key, midi, octave, isBlack);
    }

    AudioClip FindMatchingAudioClip(AudioClip[] clips, KeyData key)
    {
        List<string> patterns = new List<string>
        {
            $"{key.keyName}{key.octave}_{key.midiNumber}",
            $"{key.keyName.Replace("s", "#")}{key.octave}_{key.midiNumber}",
            $"{ConvertSharpToFlat(key.keyName)}{key.octave}_{key.midiNumber}"
        };

        foreach (var pattern in patterns)
        {
            AudioClip clip = clips.FirstOrDefault(c => c.name.Equals(pattern, System.StringComparison.OrdinalIgnoreCase));
            if (clip != null) return clip;
        }

        return null;
    }

    string ConvertSharpToFlat(string note)
    {
        return note.Replace("#", "s").ToUpper() switch
        {
            "CS" => "Db", "DS" => "Eb", "FS" => "Gb", "GS" => "Ab", "AS" => "Bb",
            _ => note
        };
    }

    public void PlayNote(int midiNumber, float duration)
    {
        if (!midiToKeyMap.TryGetValue(midiNumber, out KeyData key) || key.audioClip == null)
            return;

        AudioSource source = GetNextAvailableSource();
        if (source == null) return;

        source.clip = key.audioClip;
        source.volume = volume;
        source.time = 0f;
        source.Play();

        activeVoices.Add(source);
        SetKeyPressed(midiNumber, true);
        StartCoroutine(ReleaseNoteAfterDelay(source, midiNumber, duration));

        totalNotesPlayed++;
    }

    public void PlayNote(int midiNumber)
    {
        if (!midiToKeyMap.TryGetValue(midiNumber, out KeyData key) || key.audioClip == null)
            return;

        AudioSource source = GetNextAvailableSource();
        if (source == null) return;

        source.clip = key.audioClip;
        source.volume = volume;
        source.time = 0f;
        source.Play();

        activeVoices.Add(source);
        SetKeyPressed(midiNumber, true);
        totalNotesPlayed++;
    }

    public void PlayNoteWithVisualDuration(int midiNumber, float audioDuration, float visualDuration)
    {
        if (!midiToKeyMap.TryGetValue(midiNumber, out KeyData key) || key.audioClip == null)
            return;

        AudioSource source = GetNextAvailableSource();
        if (source == null) return;

        source.clip = key.audioClip;
        source.volume = volume;
        source.time = 0f;
        source.Play();

        activeVoices.Add(source);
        SetKeyPressed(midiNumber, true);

        StartCoroutine(ReleaseNoteAfterDelay(source, midiNumber, visualDuration));

        StartCoroutine(ReleaseAudioAfterDelay(source, Mathf.Max(1f, audioDuration)));


        totalNotesPlayed++;
    }

    private IEnumerator ReleaseAudioAfterDelay(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (source.isPlaying)
            StartCoroutine(FadeOutAndStop(source, 0.1f));
        activeVoices.Remove(source);
    }

    private IEnumerator ReleaseNoteAfterDelay(AudioSource source, int midiNumber, float delay)
    {
        yield return new WaitForSeconds(delay);
        SetKeyPressed(midiNumber, false);
    }

    public void StopNote(int midiNumber)
    {
        SetKeyPressed(midiNumber, false);
    }

    AudioSource GetNextAvailableSource()
    {
        for (int i = 0; i < maxPolyphony; i++)
        {
            if (!audioSources[i].isPlaying)
                return audioSources[i];
        }

        AudioSource source = audioSources[currentSourceIndex];
        currentSourceIndex = (currentSourceIndex + 1) % maxPolyphony;

        if (source.isPlaying)
            StartCoroutine(FadeOutAndStop(source, 0.05f));

        return source;
    }

    IEnumerator FadeOutAndStop(AudioSource source, float fadeTime)
    {
        float startVolume = source.volume;
        float timer = 0f;

        while (timer < fadeTime && source.isPlaying)
        {
            timer += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, timer / fadeTime);
            yield return null;
        }

        if (source.isPlaying)
            source.Stop();

        source.volume = volume;
    }

    void SetKeyPressed(int midiNumber, bool pressed)
    {
        keysPressed[midiNumber] = pressed;

        if (midiToKeyMap.TryGetValue(midiNumber, out KeyData key))
            UpdateKeyVisual(key, pressed);
    }

    void UpdateKeyVisual(KeyData key, bool pressed)
    {
        if (key.keyButton == null) return;

        Image img = key.keyButton.GetComponent<Image>();
        if (img == null) return;

        img.color = pressed ? pressedColor :
            (key.isBlackKey ? normalBlackColor : normalWhiteColor);
        img.SetAllDirty(); // Force UI to refresh
            Debug.Log($"Key: {key.keyName}, Black: {key.isBlackKey}, Pressed: {pressed}, Color: {(pressed ? pressedColor : (key.isBlackKey ? normalBlackColor : normalWhiteColor))}");

    }

    void Update()
    {
        HandleKeyboardInput();
        activeVoices.RemoveAll(v => !v.isPlaying);
    }

    void HandleKeyboardInput()
    {
        int o = 4;
        HandleKey(KeyCode.A, "C", o);
        HandleKey(KeyCode.W, "C#", o);
        HandleKey(KeyCode.S, "D", o);
        HandleKey(KeyCode.E, "D#", o);
        HandleKey(KeyCode.D, "E", o);
        HandleKey(KeyCode.F, "F", o);
        HandleKey(KeyCode.T, "F#", o);
        HandleKey(KeyCode.G, "G", o);
        HandleKey(KeyCode.Y, "G#", o);
        HandleKey(KeyCode.H, "A", o);
        HandleKey(KeyCode.U, "A#", o);
        HandleKey(KeyCode.J, "B", o);
        HandleKey(KeyCode.K, "C", o + 1);

        HandleKey(KeyCode.Z, "C", o - 1);
        HandleKey(KeyCode.X, "D", o - 1);
        HandleKey(KeyCode.C, "E", o - 1);
        HandleKey(KeyCode.V, "F", o - 1);
        HandleKey(KeyCode.B, "G", o - 1);
        HandleKey(KeyCode.N, "A", o - 1);
        HandleKey(KeyCode.M, "B", o - 1);
    }

    void HandleKey(KeyCode code, string note, int octave)
    {
        KeyData key = keys.FirstOrDefault(k => k.keyName == note && k.octave == octave);
        if (key == null) return;

        if (Input.GetKeyDown(code)) PlayNote(key.midiNumber);
        else if (Input.GetKeyUp(code)) StopNote(key.midiNumber);
    }

    public void SetVolume(float newVol)
    {
        volume = Mathf.Clamp01(newVol);
        foreach (AudioSource src in audioSources)
            src.volume = volume;
    }

    public void StopAllNotes()
    {
        foreach (AudioSource src in audioSources)
        {
            if (src.isPlaying)
                StartCoroutine(FadeOutAndStop(src, 0.1f));
        }

        activeVoices.Clear();
        foreach (var midi in keysPressed.Keys.ToList())
            SetKeyPressed(midi, false);

        keysPressed.Clear();
    }

    public bool IsSustainPedalOn() => sustainPedal;
    public void SetSustainPedal(bool value) => sustainPedal = value;
}
