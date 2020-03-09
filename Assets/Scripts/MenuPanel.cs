using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuPanel : MonoBehaviour
{ 
    public GameObject menuPanel;
    public GameObject attackPanel;

    // Method to hide attack panel
    public void DisableMenuPanel()
    {
        menuPanel.SetActive(false);
        attackPanel.SetActive(false);
    }
}
