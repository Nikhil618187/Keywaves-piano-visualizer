using UnityEngine;

public class ExitButton : MonoBehaviour
{
    public void ExitApp()
    {
        Application.Quit();

        // testing in Editor
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #endif
    }
}
