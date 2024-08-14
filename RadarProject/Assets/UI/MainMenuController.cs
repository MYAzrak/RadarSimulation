using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    VisualElement ui;
    ShipManager shipManager;
    CSVManager csvManager;

    ScenarioMenuUI ScenarioMenuUI;
    CSVMenuUI CSVMenuUI;

    List<TabViews> tabBtns = new();

    VisualElement menuPanel;
    
    // Start is called before the first frame update
    void Start()
    {
        shipManager = FindObjectOfType<ShipManager>();
        csvManager = FindObjectOfType<CSVManager>();
        ui = GetComponent<UIDocument>().rootVisualElement;

        menuPanel = ui.Q("Panel");
        menuPanel.visible = false;

        ScenarioMenuUI = new(ui, shipManager);
        CSVMenuUI = new(ui, csvManager);

        SetTabs();

        ScenarioMenuUI.SetBtnEvents();
        ScenarioMenuUI.SetDropdownField();

        CSVMenuUI.SetBtnEvents();

        ViewToEnable(tabBtns[0].button);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            menuPanel.visible = !menuPanel.visible;
        }
    }

    void SetTabs()
    {
        AddTabBtnsToList();

        foreach (TabViews tabView in tabBtns)
        {
            tabView.button.RegisterCallback((ClickEvent ClickEvent) => ViewToEnable(tabView.button));
        }
    }

    void AddTabBtnsToList()
    {
        tabBtns.Add(new TabViews(ui.Q("ShipBtn") as Button, ui.Q("ShipsView")));
        tabBtns.Add(new TabViews(ui.Q("CSVBtn") as Button, ui.Q("CsvView")));
    }

    void ViewToEnable(Button button)
    {
        foreach (TabViews tabView in tabBtns)
        {
            if (tabView.button.name.Equals(button.name))
            {
                tabView.visualElement.style.display = DisplayStyle.Flex;
            }
            else
            {
                tabView.visualElement.style.display = DisplayStyle.None;
            }
        }
    }

    struct TabViews
    {
        public Button button;
        public VisualElement visualElement;

        public TabViews(Button button, VisualElement visualElement)
        {
            this.button = button;
            this.visualElement = visualElement;
        }
    }
}
