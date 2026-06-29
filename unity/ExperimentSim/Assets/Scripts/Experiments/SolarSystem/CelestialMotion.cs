using UnityEngine;

public class CelestialMotion : MonoBehaviour
{
    [Header("Self Rotation")]
    public bool selfRotationEnabled = true;

    [Tooltip("Cismin kendi ekseni etrafında tam tur atma süresi. Dünya için 1, Ay için 27.3, Güneş için 25.")]
    public float selfRotationPeriodDays = 1f;

    [Tooltip("Üstten bakınca ters görünüyorsa -1 veya 1 yap.")]
    public float rotationDirection = -1f;

    public Vector3 selfRotationAxis = Vector3.up;

    [Header("Speed Controller")]
    public TimeSpeedController speedController;

    void Awake()
    {
        if (speedController == null)
        {
            speedController = FindObjectOfType<TimeSpeedController>();
        }
    }

    void Update()
    {
        if (!selfRotationEnabled)
            return;

        if (selfRotationPeriodDays <= 0f)
            return;

        float speed = 1f;

        if (speedController != null)
        {
            speed = speedController.SpeedMultiplier;
        }

        if (speed <= 0f)
            return;

        // Gerçek zaman hesabı:
        // 1 gün = 86400 saniye
        // derece/saniye = 360 / (periyotGün * 86400)
        float degreesPerRealSecond = 360f / (selfRotationPeriodDays * 86400f);

        float angle = degreesPerRealSecond * speed * Time.deltaTime * rotationDirection;

        transform.Rotate(
            selfRotationAxis,
            angle,
            Space.Self
        );
    }
}