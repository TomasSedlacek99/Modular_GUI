using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MixedReality.Toolkit.UX;
using MixedReality.Toolkit;

public class TogglePartController : MonoBehaviour
{
    [SerializeField] GameObject dialog;
    [SerializeField] GameObject part;

    // Start is called before the first frame update
    void Start()
    {
        if (dialog == null)
        {
            Debug.Log("Dialog is null");
        }
        if (part == null) 
        {
            Debug.Log("Part is null");
        }
    }

    public void OnToggleActivated() 
    {
        if (part != null) 
        {
            part.SetActive(true);
            Debug.Log("Switch stlaceny!!");
        }

        if (dialog != null) 
        { 
            dialog.SetActive(true);
        }
    }

    public void OnToggleDeactivated() 
    {
        if (part != null) 
        {
            part.SetActive(false);
        }
        if (dialog != null) 
        {
            dialog.SetActive(false);
        }
    }
}
