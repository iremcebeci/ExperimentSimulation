using System;
using System.Collections;
using UnityEngine;

public class EarthRotationFromNASA : MonoBehaviour
{
    [Header("References")]
    public DateTimeSelector dateTimeSelector;
    public HorizonsClient horizonsClient;
    public Transform earthCenter;
    public Transform sun;

    [Header("Texture / Axis Settings")]
    public Vector3 rotationAxis = Vector3.up;

    [Tooltip("Dünya texture'ında 0 derece boylamın baktığı yön. Genelde Z forward.")]
    public Vector3 greenwichDirectionOnTexture = Vector3.forward;

    [Tooltip("Doğu-batı ters görünürse 1 yerine -1 yap.")]
    public float longitudeDirection = 1f;

    [Tooltip("Texture Greenwich hizası kayıksa sadece bunu değiştir.")]
    public float textureOffsetDegrees = 0f;

    [Header("NASA")]
    public bool loadOnStart = true;

    private Quaternion initialLocalRotation;

    private bool isLoading;
    private bool hasNASAData;

    private DateTime loadedUtcTime;
    private double loadedSunRightAscensionDeg;

    void Awake()
    {
        initialLocalRotation = transform.localRotation;

        if (dateTimeSelector == null)
            dateTimeSelector = FindObjectOfType<DateTimeSelector>();

        if (horizonsClient == null)
            horizonsClient = FindObjectOfType<HorizonsClient>();

        if (earthCenter == null && transform.parent != null)
            earthCenter = transform.parent;
    }

    IEnumerator Start()
    {
        yield return null;

        if (loadOnStart)
            LoadNASAData();
    }

    void Update()
    {
        if (!hasNASAData)
            return;

        if (dateTimeSelector == null || earthCenter == null || sun == null)
            return;

        ApplyEarthRotation(dateTimeSelector.SelectedUtcDateTime);
    }

    public void LoadNASAData()
    {
        if (isLoading)
            return;

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

        StartCoroutine(LoadSunVectorFromNASA(dateTimeSelector.SelectedUtcDateTime));
    }

    private IEnumerator LoadSunVectorFromNASA(DateTime selectedUtc)
    {
        isLoading = true;

        Vector3 sunVectorRaw = Vector3.zero;
        bool hasError = false;

        Debug.Log("Dünya dönüşü için NASA Güneş vektörü çekiliyor.");
        Debug.Log("Seçilen UTC: " + selectedUtc.ToString("yyyy-MM-dd HH:mm:ss"));

        // COMMAND 10 = Sun
        // CENTER @399 = Earth merkezinden bakış
        yield return horizonsClient.GetRawPositionAtTime(
            command: "10",
            center: "@399",
            selectedUtc: selectedUtc,
            onSuccess: vector =>
            {
                sunVectorRaw = vector;
            },
            onError: error =>
            {
                Debug.LogError("NASA Güneş vektörü alınamadı: " + error);
                hasError = true;
            }
        );

        if (hasError)
        {
            isLoading = false;
            yield break;
        }

        loadedUtcTime = selectedUtc;
        loadedSunRightAscensionDeg = CalculateSunRightAscensionFromNASAVector(sunVectorRaw);

        hasNASAData = true;

        Debug.Log("NASA Dünya dönüş verisi alındı.");
        Debug.Log("Sun Right Ascension: " + loadedSunRightAscensionDeg.ToString("0.000") + " derece");

        ApplyEarthRotation(selectedUtc);

        isLoading = false;
    }

    private void ApplyEarthRotation(DateTime currentUtc)
    {
        double sunRightAscensionDeg = GetSunRightAscensionForCurrentTime(currentUtc);
        double gmstDeg = CalculateGMSTDegrees(currentUtc);

        // Güneş'in Dünya üzerinde tam dik geldiği boylam.
        // Pozitif değer doğu boylamıdır.
        double subSolarLongitude = NormalizeLongitude(sunRightAscensionDeg - gmstDeg);

        Debug.Log(
            "UTC: " + currentUtc.ToString("yyyy-MM-dd HH:mm:ss") +
            " | Subsolar longitude: " + subSolarLongitude.ToString("0.000") + " derece"
        );

        Vector3 axisObject = rotationAxis.normalized;

        Quaternion baseRotation = initialLocalRotation;

        Vector3 axisParent = baseRotation * axisObject;
        axisParent.Normalize();

        Transform parentSpace = transform.parent != null ? transform.parent : earthCenter;

        Vector3 sunDirectionWorld = (sun.position - earthCenter.position).normalized;
        Vector3 sunDirectionParent = parentSpace.InverseTransformDirection(sunDirectionWorld);
        sunDirectionParent = ProjectOnPlaneNormalized(sunDirectionParent, axisParent);

        float longitudeAngle =
            (float)(subSolarLongitude * longitudeDirection + textureOffsetDegrees);

        Vector3 subSolarDirectionOnTexture =
            Quaternion.AngleAxis(longitudeAngle, axisObject) *
            greenwichDirectionOnTexture.normalized;

        Vector3 subSolarDirectionParent =
            baseRotation * subSolarDirectionOnTexture;

        subSolarDirectionParent =
            ProjectOnPlaneNormalized(subSolarDirectionParent, axisParent);

        float angleToSun = Vector3.SignedAngle(
            subSolarDirectionParent,
            sunDirectionParent,
            axisParent
        );

        transform.localRotation =
            Quaternion.AngleAxis(angleToSun, axisParent) * baseRotation;
    }

    private double GetSunRightAscensionForCurrentTime(DateTime currentUtc)
    {
        // NASA'dan alınan RA değerini baz alıyoruz.
        // Simülasyon ilerlerken Güneş'in RA'sı günde yaklaşık 0.9856 derece değişir.
        double daysPassed = (currentUtc - loadedUtcTime).TotalDays;
        double estimatedRA = loadedSunRightAscensionDeg + daysPassed * 0.98564736;

        return Normalize360(estimatedRA);
    }

    private double CalculateSunRightAscensionFromNASAVector(Vector3 sunVectorRaw)
    {
        // HorizonsClient.GetFirstRawVector NASA'nın ham X,Y,Z değerini verir.
        // Bu değer ecliptic düzlemdedir. RA için equatorial düzleme çeviriyoruz.

        double x = sunVectorRaw.x;
        double y = sunVectorRaw.y;
        double z = sunVectorRaw.z;

        double obliquityDeg = 23.439291;
        double eps = obliquityDeg * Math.PI / 180.0;

        double xEq = x;
        double yEq = y * Math.Cos(eps) - z * Math.Sin(eps);

        double raDeg = Math.Atan2(yEq, xEq) * 180.0 / Math.PI;

        return Normalize360(raDeg);
    }

    private double CalculateGMSTDegrees(DateTime utcTime)
    {
        double jd = DateTimeToJulianDate(utcTime);
        double t = (jd - 2451545.0) / 36525.0;

        double gmst =
            280.46061837
            + 360.98564736629 * (jd - 2451545.0)
            + 0.000387933 * t * t
            - (t * t * t) / 38710000.0;

        return Normalize360(gmst);
    }

    private double DateTimeToJulianDate(DateTime dateTime)
    {
        DateTime utc = dateTime.Kind == DateTimeKind.Utc
            ? dateTime
            : dateTime.ToUniversalTime();

        int year = utc.Year;
        int month = utc.Month;

        double day =
            utc.Day
            + utc.Hour / 24.0
            + utc.Minute / 1440.0
            + utc.Second / 86400.0;

        if (month <= 2)
        {
            year -= 1;
            month += 12;
        }

        int a = year / 100;
        int b = 2 - a + (a / 4);

        double jd =
            Math.Floor(365.25 * (year + 4716))
            + Math.Floor(30.6001 * (month + 1))
            + day
            + b
            - 1524.5;

        return jd;
    }

    private double Normalize360(double angle)
    {
        angle %= 360.0;

        if (angle < 0)
            angle += 360.0;

        return angle;
    }

    private double NormalizeLongitude(double longitude)
    {
        longitude %= 360.0;

        if (longitude > 180.0)
            longitude -= 360.0;

        if (longitude < -180.0)
            longitude += 360.0;

        return longitude;
    }

    private Vector3 ProjectOnPlaneNormalized(Vector3 vector, Vector3 planeNormal)
    {
        Vector3 projected = Vector3.ProjectOnPlane(vector, planeNormal);

        if (projected.sqrMagnitude < 0.000001f)
            return Vector3.forward;

        return projected.normalized;
    }
}