using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SplashScreenFade : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Image ImageToTransit;
    [SerializeField] private float displayTime = 3f;
    [SerializeField] private float fadeOutTime = 1f;
    [SerializeField] private string nextScene = "Home";

    void Start()
    {
        ImageToTransit.canvasRenderer.SetAlpha(1f);
        StartCoroutine(RunSplashSequence());
    }

    private IEnumerator RunSplashSequence()
    {
        
        
        
        // Display
        yield return new WaitForSeconds(displayTime);
        
        // Fade out
        ImageToTransit.CrossFadeAlpha(0f, fadeOutTime, false);
        
        
        // Load next scene
        SceneManager.LoadScene(nextScene);
    }
}