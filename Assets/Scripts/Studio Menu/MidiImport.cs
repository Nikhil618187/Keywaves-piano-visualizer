using UnityEngine;
using TMPro;
using SFB;
using Melanchall.DryWetMidi.Core;

public class MidiFileSelector : MonoBehaviour
{
    public TMP_InputField fileNameInput;
    public NoteManager noteManager;

    public void OpenFileExplorer()
    {
        var extensions = new[] {
            new ExtensionFilter("MIDI Files", "mid", "midi"),
            new ExtensionFilter("All Files", "*")
        };

        StandaloneFileBrowser.OpenFilePanelAsync("Select MIDI File", "", extensions, false, (string[] paths) =>
        {
            if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                string selectedFile = paths[0];
                fileNameInput.text = System.IO.Path.GetFileName(selectedFile);

                //  Clean up old state
                noteManager.CancelAllNoteCoroutines();
                noteManager.ClearExistingNotes();

                var keyboard = Object.FindFirstObjectByType<VirtualKeyboard>();
                if (keyboard != null)
                {
                    keyboard.StopAllNotes();
                }

                //  Load new MIDI
                noteManager.LoadMidiFromPath(selectedFile);
            }
        });
    }
}
