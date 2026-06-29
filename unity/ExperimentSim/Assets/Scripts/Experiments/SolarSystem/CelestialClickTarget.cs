using UnityEngine;

public class CelestialClickTarget : MonoBehaviour
{
    public enum TargetType
    {
        Sun,
        Earth,
        Moon
    }

    [Header("Click Settings")]
    public TargetType targetType;
    public CameraSwitcher cameraSwitcher;

    void Start()
    {
        if (cameraSwitcher == null)
        {
            cameraSwitcher = FindObjectOfType<CameraSwitcher>();
        }
    }

    void OnMouseDown()
    {
        if (cameraSwitcher == null)
        {
            Debug.LogWarning("CameraSwitcher bulunamadı.");
            return;
        }

        switch (targetType)
        {
            case TargetType.Sun:
                cameraSwitcher.SwitchToSunView();
                break;

            case TargetType.Earth:
                cameraSwitcher.SwitchToEarthView();
                break;

            case TargetType.Moon:
                cameraSwitcher.SwitchToMoonView();
                break;
        }
    }
}