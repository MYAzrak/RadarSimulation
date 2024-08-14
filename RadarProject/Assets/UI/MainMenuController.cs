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

    bool isMenuVisible = false;
    List<TabViews> tabBtns = new();

    void Awake()
    {
        shipManager = FindObjectOfType<ShipManager>();
        csvManager = FindObjectOfType<CSVManager>();
        ui = GetComponent<UIDocument>().rootVisualElement;

        Visibility visibility = isMenuVisible ? Visibility.Visible : Visibility.Hidden;
        ui.style.visibility = visibility;
        isMenuVisible = !isMenuVisible;

        ScenarioMenuUI = new(ui, shipManager, csvManager);
        CSVMenuUI = new(ui, shipManager, csvManager);
    }
    
    // Start is called before the first frame update
    void Start()
    {
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
            Visibility visibility = isMenuVisible ? Visibility.Visible : Visibility.Hidden;
            ui.style.visibility = visibility;
            isMenuVisible = !isMenuVisible;
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
