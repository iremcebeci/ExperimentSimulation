using UnityEngine;

public class silinecek : MonoBehaviour
{
    [Header("Step Root Objects")]
    public GameObject[] steps;

    [Header("Editor Preview")]
    public int editorPreviewStepIndex = 0;

    [Header("Settings")]
    public bool showFirstStepOnPlay = true;

    private int currentStepIndex = 0;

    private void Start()
    {
        if (Application.isPlaying && showFirstStepOnPlay)
        {
            ShowStep(0);
        }
    }

    public void ShowStep(int index)
    {
        if (steps == null || steps.Length == 0)
        {
            Debug.LogWarning("Step listesi boş.");
            return;
        }

        if (index < 0 || index >= steps.Length)
        {
            Debug.LogWarning("Geçersiz step index: " + index);
            return;
        }

        for (int i = 0; i < steps.Length; i++)
        {
            if (steps[i] != null)
            {
                steps[i].SetActive(i == index);
            }
        }

        currentStepIndex = index;
        editorPreviewStepIndex = index;

        Debug.Log("Aktif step: " + index);
    }

    public void ShowAllSteps()
    {
        if (steps == null)
            return;

        foreach (GameObject step in steps)
        {
            if (step != null)
                step.SetActive(true);
        }

        Debug.Log("Tüm stepler açıldı.");
    }

    public void HideAllSteps()
    {
        if (steps == null)
            return;

        foreach (GameObject step in steps)
        {
            if (step != null)
                step.SetActive(false);
        }

        Debug.Log("Tüm stepler kapatıldı.");
    }

    public void NextStep()
    {
        if (currentStepIndex < steps.Length - 1)
        {
            ShowStep(currentStepIndex + 1);
        }
    }

    public void PreviousStep()
    {
        if (currentStepIndex > 0)
        {
            ShowStep(currentStepIndex - 1);
        }
    }

    public void RestartSteps()
    {
        ShowStep(0);
    }
}