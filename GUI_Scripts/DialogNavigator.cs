using System.Collections.Generic;
using UnityEngine;
using TMPro; // Pre TextMeshPro komponenty

public class DialogNavigator : MonoBehaviour
{
    [SerializeField] private ModelManager modelManager; // Referencia na ModelManager

    [System.Serializable]
    public class DialogPage
    {
        public string header; // Nadpis stránky
        public string content; // Obsah stránky
    }

    public List<DialogPage> pages; // Zoznam stránok
    public TextMeshProUGUI headerText; // Text pre nadpis (Header)
    public TextMeshProUGUI contentText; // Text pre obsah (Main Text)

    private int currentPage = 0; // Aktuálna stránka
    private bool lastPageTriggered = false;

    private void Start()
    {
        ShowPage(0); // Zobraz prvú stránku
    }

    public void ShowPage(int pageIndex)
    {
        if (pageIndex >= 0 && pageIndex < pages.Count)
        {
            currentPage = pageIndex;
            headerText.text = pages[currentPage].header;
            contentText.text = pages[currentPage].content;
            if (lastPageTriggered)
            {
                lastPageClosed();
            }
            if (pageIndex == pages.Count-1)
            {
                Debug.Log("Last Page?");
                LastPageTrigger();
            }
        }
    }

    private void lastPageClosed()
    {
        if (modelManager != null) 
        {
            //modelManager.DeactivateToggleSwitches();
            modelManager.DeactivateToggleSwitchesMenu();
            modelManager.ShowAllParts();
            modelManager.DeactivateDialogsOfParts();
            lastPageTriggered = false;
        }
    }

    private void LastPageTrigger()
    {
        if (modelManager != null)
        {
            modelManager.HideAllParts();
            //modelManager.ActivateToggleSwitches();
            modelManager.ActivateToggleSwitchesMenu();
            lastPageTriggered = true;
        }
    }

    public void NextPage()
    {
        if (currentPage < pages.Count - 1)
        {
            ShowPage(currentPage + 1);
        }
    }

    public void PreviousPage()
    {
        if (currentPage > 0)
        {
            ShowPage(currentPage - 1);
        }
    }
}

