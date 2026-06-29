using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class DashboardSimulationMenuOpener : MonoBehaviour
{
    [Header("Scene")]
    public string simulationMenuSceneName = "SimulationMenuScene";

    [Header("UI Toolkit Button Name")]
    public string startButtonName = "StartSimulationButton";

    private UIDocument document;
    private Button startButton;
    private bool isBound;

    private void Awake()
    {
        document = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        StartCoroutine(BindWhenReady());
    }

    private IEnumerator BindWhenReady()
    {
        isBound = false;

        while (!isBound)
        {
            TryBindButton();
            yield return new WaitForSeconds(0.25f);
        }
    }

    private void TryBindButton()
    {
        if (document == null || document.rootVisualElement == null)
            return;

        startButton = document.rootVisualElement.Q<Button>(startButtonName);

        if (startButton == null)
            return;

        startButton.clicked -= OpenSimulationMenu;
        startButton.clicked += OpenSimulationMenu;

        isBound = true;

        Debug.Log("[DashboardSimulationMenuOpener] Simülasyonu Başlat butonu bağlandı.");
    }

    private void OpenSimulationMenu()
    {
        AssignmentSession.ClearAssignmentOnly();

        Debug.Log("[DashboardSimulationMenuOpener] SimulationMenuScene açılıyor.");

        SceneManager.LoadScene(simulationMenuSceneName);
    }
}