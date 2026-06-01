using MixedReality.Toolkit.UX;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEngine;

public class ModelManager : MonoBehaviour
{
    [Header("Production Line Components")]
    [SerializeField] private List<GameObject> toggleSwitches; // Toggle tlaËidl·
    [SerializeField] private GameObject toggleSwitchesMenu;
    [SerializeField] private List<GameObject> connectionParts; // Prepojenia medzi Ëasùami
    [SerializeField] private List<GameObject> parts; // JednotlivÈ Ëasti linky
    [SerializeField] private List<GameObject> dialogParts; // Dialog okn· jednotliv˝ch ËastÌ linky

    [Header("Production Line Main Object")]
    [SerializeField] private GameObject wholeModel; // Model celej vyrobnej linky Learning Factory 4.0
    [SerializeField] private Slider sizeSlider;

    private float rotationFactor = 15f;
    private float maxYRotation = 270f;
    private float minYRotation = 90f;

    // Skry vöetky Ëasti linky
    public void HideAllParts()
    {
        foreach (GameObject part in connectionParts)
        {
            if (part != null)
            {
                part.SetActive(false);
            }
        }
        foreach (GameObject part in parts)
        {
            if (part != null)
            {
                part.SetActive(false);
            }
        }
    }

    // Zobraz vöetky Ëasti linky
    public void ShowAllParts()
    {
        foreach (GameObject part in connectionParts)
        {
            if (part != null)
            {
                part.SetActive(true);
            }
        }
        foreach (GameObject part in parts)
        {
            if (part != null)
            {
                part.SetActive(true);
            }
        }
        DeactivateDialogsOfParts();
    }

    // Aktivuj Toggle tlaËidl·
    public void ActivateToggleSwitches()
    {
        foreach (GameObject toggleSwitch in toggleSwitches)
        {
            if (toggleSwitch != null)
            {
                toggleSwitch.SetActive(true);
            }
        }
    }

    // Deaktivuj Toggle tlaËidl·
    public void DeactivateToggleSwitches()
    {
        foreach (GameObject toggleSwitch in toggleSwitches)
        {
            if (toggleSwitch != null)
            {
                toggleSwitch.SetActive(false);
            }
        }
    }

    // Aktivuj vöetky dialog okn· ËastÌ linky
    public void ActivateDialogsOfParts() 
    {
        foreach (GameObject dialog in dialogParts) 
        {
            if (dialog != null) 
            {
                dialog.SetActive(true);
            }
        }
    }

    public void DeactivateDialogsOfParts() 
    {
        foreach (GameObject dialog in dialogParts) 
        {
            if (dialog != null) 
            {
                dialog.SetActive(false);
            }
        }
    }

    public void ChangeSizeOfMainModel() 
    {
        if (wholeModel != null && sizeSlider != null) 
        {
            float sliderValue = sizeSlider.Value;
            wholeModel.transform.localScale = new Vector3(sliderValue, sliderValue, sliderValue);
        }
    }

    public void ActivateToggleSwitchesMenu() 
    {
        if (toggleSwitchesMenu != null) 
        { 
            toggleSwitchesMenu.SetActive(true);
        }
    }

    public void DeactivateToggleSwitchesMenu() 
    {
        if (toggleSwitchesMenu != null) 
        {
            toggleSwitchesMenu.SetActive(false);
        }
    }

    public void RotateLeft() 
    {
        if (wholeModel != null) 
        {
            float currentY = wholeModel.transform.eulerAngles.y;
            Debug.Log(currentY);
            if (currentY < maxYRotation)
            {
                //wholeModel.transform.Rotate(Vector3.up, -rotationFactor, Space.World);
                wholeModel.transform.rotation *= Quaternion.Euler(0, -rotationFactor, 0);
            }
        }
    }

    public void RotateRight()
    {
        if (wholeModel != null)
        {
            float currentY = wholeModel.transform.eulerAngles.y;
            Debug.Log(currentY);
            if (currentY > minYRotation)
            {
                //wholeModel.transform.Rotate(Vector3.up, rotationFactor, Space.World);
                wholeModel.transform.rotation *= Quaternion.Euler(0, rotationFactor, 0);
            }
        }
    }
}

