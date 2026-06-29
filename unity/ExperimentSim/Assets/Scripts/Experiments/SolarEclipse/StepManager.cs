using UnityEngine;

public class StepManager : MonoBehaviour
{
    [Header("Step Root Objects")]
    public GameObject[] steps;

    [Header("Camera Settings")]
    public Camera mainCamera;

    [Tooltip("Her step için kamera noktası. Step index ile aynı sırada olmalı.")]
    public Transform[] cameraPoints;

    [Header("Camera Move")]
    public bool smoothCameraMove = false;
    public float cameraMoveSpeed = 3f;

    private int currentStepIndex = 0;

    private Vector3 targetCameraPosition;
    private Quaternion targetCameraRotation;
    private bool isCameraMoving = false;

    private void Start()
    {
        ShowStep(0);
    }

    private void Update()
    {
        if (smoothCameraMove && isCameraMoving && mainCamera != null)
        {
            mainCamera.transform.position = Vector3.Lerp(
                mainCamera.transform.position,
                targetCameraPosition,
                Time.deltaTime * cameraMoveSpeed
            );

            mainCamera.transform.rotation = Quaternion.Slerp(
                mainCamera.transform.rotation,
                targetCameraRotation,
                Time.deltaTime * cameraMoveSpeed
            );

            float distance = Vector3.Distance(mainCamera.transform.position, targetCameraPosition);

            if (distance < 0.02f)
            {
                mainCamera.transform.position = targetCameraPosition;
                mainCamera.transform.rotation = targetCameraRotation;
                isCameraMoving = false;
            }
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
                steps[i].SetActive(false);
        }

        currentStepIndex = index;

        if (steps[currentStepIndex] != null)
            steps[currentStepIndex].SetActive(true);

        MoveCameraToStep(currentStepIndex);

        Debug.Log("Aktif step: " + currentStepIndex);
    }

    public void NextStep()
    {
        if (currentStepIndex < steps.Length - 1)
        {
            ShowStep(currentStepIndex + 1);
        }
        else
        {
            Debug.Log("Son stepteyiz, ileri gidilemez.");
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

    private void MoveCameraToStep(int stepIndex)
    {
        if (mainCamera == null)
        {
            Debug.LogWarning("Main Camera atanmadı.");
            return;
        }

        if (cameraPoints == null || cameraPoints.Length == 0)
        {
            return;
        }

        if (stepIndex < 0 || stepIndex >= cameraPoints.Length)
        {
            return;
        }

        if (cameraPoints[stepIndex] == null)
        {
            return;
        }

        targetCameraPosition = cameraPoints[stepIndex].position;
        targetCameraRotation = cameraPoints[stepIndex].rotation;

        if (smoothCameraMove)
        {
            isCameraMoving = true;
        }
        else
        {
            mainCamera.transform.position = targetCameraPosition;
            mainCamera.transform.rotation = targetCameraRotation;
            isCameraMoving = false;
        }
    }
}