using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MixedReality.Toolkit.UX;

public class HandMenuManager : MonoBehaviour
{
    [SerializeField] GameObject mainModelDialog;
    [SerializeField] GameObject mainModel;
    [SerializeField] GameObject modelUIButtonIcon;
    [SerializeField] PressableButton nextButton;
    [SerializeField] PressableButton previousButton;
    [SerializeField] DialogNavigator dialogNavigator;

    private bool mainModelActive = true;
    private bool mainModelDialogActive = true;

    // Start is called before the first frame update
    void Start()
    {
        if (mainModelDialog == null)
        {
            Debug.Log("Main model dialog window not provided...");
        }
        if (mainModel == null) 
        {
            Debug.Log("Main model not provided...");
        }
        if (nextButton != null) 
        {
            nextButton.OnClicked.AddListener(() =>
            {
                dialogNavigator.NextPage();
            });
        }
        if (previousButton != null) 
        {
            previousButton.OnClicked.AddListener(() =>
            {
                dialogNavigator.PreviousPage();
            });    
        }
    }

    public void ToggleModel() 
    {
        if (mainModel != null) 
        {
            mainModel.SetActive(!mainModelActive);
            if (mainModelActive)
            {
                modelUIButtonIcon.GetComponent<FontIconSelector>().CurrentIconName = "Icon 9";
            }
            else 
            {
                modelUIButtonIcon.GetComponent<FontIconSelector>().CurrentIconName = "Icon 10";
            }
            mainModelActive = !mainModelActive;
        }
    }
    public void ToggleDialog()
    {
        if (mainModelDialog != null) 
        {
            mainModelDialog.SetActive(mainModelDialogActive);
            mainModelDialogActive = !mainModelDialogActive;
        }
    }
}
