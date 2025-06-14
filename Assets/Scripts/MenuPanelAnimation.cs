using UnityEngine;

public class PanelController : MonoBehaviour
{
    [SerializeField] private GameObject panelToShow; 

    public void TogglePanel()
    {
        panelToShow.SetActive(!panelToShow.activeSelf);
    }
    
    // Alternative 
    public void ShowPanel()
    {
        panelToShow.SetActive(true);
    }
    
    public void HidePanel()
    {
        panelToShow.SetActive(false);
    }
}