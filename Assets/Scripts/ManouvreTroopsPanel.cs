using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManouvreTroopsPanel : MonoBehaviour
{
    public GameObject menuPanel;
    public GameObject attackPanel;
    public GameObject mtPanel;

    // Method to hide attack panel
    public void DisableMenuPanel()
    {
        //menuPanel.SetActive(false);
        //attackPanel.SetActive(false);
        mtPanel.SetActive(false);
        ManageGame.instance.manouvreOver = true;
    }
}
