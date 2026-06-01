using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppManager : MonoBehaviour
{
    public void CloseApp() 
    {
        Debug.Log("Closing application...");
        Application.Quit();
    }
}
