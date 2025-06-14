using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private String SceneToLoad;
    public void LoadSpecificScene(string SceneToLoad)
    {
        SceneManager.LoadScene(SceneToLoad);
    }
}