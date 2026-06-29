using System;
using System.Collections;
using UnityEngine;

public class NASAOrbitController : MonoBehaviour
{
    [Header("Scene Bodies")]
    public Transform sun;
    public Transform earth;
    public Transform moon;

    [Header("Controllers")]
    public DateTimeSelector dateTimeSelector;
    public HorizonsClient horizonsClient;
    public TimeSpeedController speedController;

    [Header("Scene Distances")]
    public float earthSunDistance = 37.5f;
    public float moonEarthDistance = 5f;

    [Header("Real Orbital Periods")]
    public float earthOrbitPeriodDays = 365.25f;
    public float moonOrbitPeriodDays = 27.3f;

    [Tooltip("Üstten bakınca yörünge ters görünüyorsa 1 veya -1 yap.")]
    public float orbitDirection = -1f;

    [Header("NASA Request Settings")]
    public bool loadOnStart = true;

    [Header("Plane Settings")]
    public bool keepOnXZPlane = true;

    private bool isLoading;

    private bool hasBaseData;
    private DateTime baseUtcTime;
    private Vector3 baseEarthDirection;
    private Vector3 baseMoonDirection;

    void Awake()
    {
        if (horizonsClient == null)
        {
            horizonsClient = GetComponent<HorizonsClient>();
        }

        if (dateTimeSelector == null)
        {
            dateTimeSelector = FindObjectOfType<DateTimeSelector>();
        }

        if (speedController == null)
        {
            speedController = FindObjectOfType<TimeSpeedController>();
        }
    }

    IEnumerator Start()
    {
        yield return null;

        if (loadOnStart)
        {
            LoadNASAData();
        }
    }

    void Update()
    {
        if (!hasBaseData)
            return;

        if (dateTimeSelector == null)
            return;

        ApplyPositionsFromSimulationTime(dateTimeSelector.SelectedUtcDateTime);
    }

    public void LoadNASAData()
    {
        if (isLoading)
        {
            Debug.Log("NASA verisi zaten yükleniyor.");
            return;
        }

        if (dateTimeSelector == null)
        {
            Debug.LogWarning("DateTimeSelector atanmadı.");
            return;
        }

        if (horizonsClient == null)
        {
            Debug.LogWarning("HorizonsClient atanmadı.");
            return;
        }

        DateTime selectedUtc = dateTimeSelector.SelectedUtcDateTime;

        if (selectedUtc.Year < 1900)
        {
            Debug.LogWarning("Geçersiz tarih yakalandı. DateTimeSelector henüz hazır olmayabilir.");
            return;
        }

        StartCoroutine(LoadPositionsRoutine(selectedUtc));
    }

    private IEnumerator LoadPositionsRoutine(DateTime selectedUtc)
    {
        isLoading = true;

        Vector3 earthFromSunKm = Vector3.zero;
        Vector3 moonFromEarthKm = Vector3.zero;

        bool hasError = false;

        Debug.Log("NASA başlangıç verisi çekiliyor. UTC: " + selectedUtc.ToString("yyyy-MM-dd HH:mm:ss"));

        yield return horizonsClient.GetPositionAtTime(
            command: "399",
            center: "@10",
            selectedUtc: selectedUtc,
            onSuccess: vector =>
            {
                earthFromSunKm = vector;
            },
            onError: error =>
            {
                Debug.LogError("Dünya verisi alınamadı: " + error);
                hasError = true;
            }
        );

        if (hasError)
        {
            isLoading = false;
            yield break;
        }

        yield return horizonsClient.GetPositionAtTime(
            command: "301",
            center: "@399",
            selectedUtc: selectedUtc,
            onSuccess: vector =>
            {
                moonFromEarthKm = vector;
            },
            onError: error =>
            {
                Debug.LogError("Ay verisi alınamadı: " + error);
                hasError = true;
            }
        );

        if (hasError)
        {
            isLoading = false;
            yield break;
        }

        SetBaseData(selectedUtc, earthFromSunKm, moonFromEarthKm);
        ApplyPositionsFromSimulationTime(selectedUtc);

        isLoading = false;
    }

    private void SetBaseData(DateTime selectedUtc, Vector3 earthFromSunKm, Vector3 moonFromEarthKm)
    {
        baseUtcTime = selectedUtc;

        baseEarthDirection = GetDirectionOnPlane(earthFromSunKm);
        baseMoonDirection = GetDirectionOnPlane(moonFromEarthKm);

        hasBaseData = true;

        Debug.Log("NASA referans konumu alındı.");
        Debug.Log("Base UTC: " + baseUtcTime.ToString("yyyy-MM-dd HH:mm:ss"));
        Debug.Log("Base Earth Direction: " + baseEarthDirection);
        Debug.Log("Base Moon Direction: " + baseMoonDirection);
    }

    private void ApplyPositionsFromSimulationTime(DateTime currentUtc)
    {
        double deltaDays = (currentUtc - baseUtcTime).TotalDays;

        float earthAngle = (float)(deltaDays * 360.0 / earthOrbitPeriodDays) * orbitDirection;
        float moonAngle = (float)(deltaDays * 360.0 / moonOrbitPeriodDays) * orbitDirection;

        Vector3 earthDirection = Quaternion.AngleAxis(earthAngle, Vector3.up) * baseEarthDirection;
        Vector3 moonDirection = Quaternion.AngleAxis(moonAngle, Vector3.up) * baseMoonDirection;

        Vector3 earthUnityPosition = earthDirection.normalized * earthSunDistance;
        Vector3 moonUnityPosition = earthUnityPosition + moonDirection.normalized * moonEarthDistance;

        if (sun != null)
        {
            sun.position = Vector3.zero;
        }

        if (earth != null)
        {
            earth.position = earthUnityPosition;
        }

        if (moon != null)
        {
            moon.position = moonUnityPosition;
        }
    }

    private Vector3 GetDirectionOnPlane(Vector3 vector)
    {
        if (keepOnXZPlane)
        {
            vector.y = 0f;
        }

        if (vector.sqrMagnitude < 0.000001f)
        {
            return Vector3.right;
        }

        return vector.normalized;
    }
}