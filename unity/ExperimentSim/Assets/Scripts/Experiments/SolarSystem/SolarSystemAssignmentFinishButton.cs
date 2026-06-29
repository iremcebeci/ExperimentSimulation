using UnityEngine;
using UnityEngine.UI;

public class SolarSystemAssignmentFinishButton : MonoBehaviour
{
    public Button finishButton;
    public AssignmentResultSubmitter submitter;

    private void Start()
    {
        if (submitter == null)
            submitter = FindObjectOfType<AssignmentResultSubmitter>();

        if (finishButton != null)
        {
            finishButton.onClick.RemoveAllListeners();
            finishButton.onClick.AddListener(FinishAssignment);
        }
    }

    private void FinishAssignment()
    {
        if (submitter == null)
        {
            Debug.LogError("[SolarSystemAssignmentFinishButton] AssignmentResultSubmitter bulunamadı.");
            return;
        }

        submitter.SubmitResultAndReturn();
    }
}