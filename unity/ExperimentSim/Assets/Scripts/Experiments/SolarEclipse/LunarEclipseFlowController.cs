using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class LunarEclipseFlowController : MonoBehaviour
{
    [Header("Akış")]
    public bool playOnStart = true;

    [Tooltip("Açıksa zaman AudioSource.time üzerinden ilerler. Kapalıysa normal oyun zamanı kullanılır.")]
    public bool useAudioTime = true;

    [Header("Ses")]
    public AudioSource narrationAudio;

    [Header("Ay Hareket Parent")]
    public Transform moonMover;

    [Header("Ay Görünümleri")]
    public GameObject moonStart;
    public GameObject moonHalfShadow;
    public GameObject moonTotalShadow;

    [Header("Koordinat Noktaları")]
    public Transform p0_Start;
    public Transform p1_YariGolge_Donusum;
    public Transform p2_YariGolge_Dur;
    public Transform p3_TamGolge_Donusum;
    public Transform p4_TamGolge_Dur;
    public Transform p5_YariGolge_Donusum2;
    public Transform p6_Moon_Donusum;
    public Transform p7_Bitis;

    [Header("Yarı Gölge Objeleri")]
    public GameObject[] halfShadowObjects;

    [Header("Tam Gölge Objeleri")]
    public GameObject[] totalShadowObjects;

    [Header("Hareket Zamanları - Saniye")]
    public float p0ToP1StartSecond = 6f;
    public float p0ToP1EndSecond = 12f;

    public float p1ToP2StartSecond = 12f;
    public float p1ToP2EndSecond = 18f;

    public float p2ToP3StartSecond = 23f;
    public float p2ToP3EndSecond = 30f;

    public float p3ToP4StartSecond = 30f;
    public float p3ToP4EndSecond = 36f;

    public float p4ToP5StartSecond = 43f;
    public float p4ToP5EndSecond = 49f;

    public float p5ToP6StartSecond = 49f;
    public float p5ToP6EndSecond = 55f;

    public float p6ToP7StartSecond = 55f;
    public float p6ToP7EndSecond = 61f;

    [Header("Devam Et Butonu")]
    public float continueButtonShowSecond = 65f;

    public Button continueButton;
    public UnityEvent onContinueClicked;

    private bool isPlaying;
    private float startTime;

    private void Awake()
    {
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        }
    }

    private void Start()
    {
        if (AssignmentSession.IsAssignmentMode)
        {
            AssignmentResultSubmitter.ClearAnswers();
            Debug.Log("[ASSIGNMENT ANSWERS] Deney başlangıcında cevap listesi temizlendi.");
        }

        ResetScene();

        if (playOnStart)
        {
            StartFlow();
        }
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        float currentSecond = GetCurrentSecond();

        UpdateMoonPosition(currentSecond);
        UpdateMoonVisual(currentSecond);
        UpdateShadowObjects(currentSecond);
        UpdateContinueButton(currentSecond);
    }

    public void StartFlow()
    {
        ResetScene();

        isPlaying = true;
        startTime = Time.time;

        if (narrationAudio != null)
        {
            narrationAudio.Stop();
            narrationAudio.Play();
        }
    }

    public void ResetScene()
    {
        isPlaying = false;

        if (moonMover != null && p0_Start != null)
        {
            moonMover.position = p0_Start.position;
        }

        SetMoonVisual(moonStart);

        SetObjectsActive(halfShadowObjects, false);
        SetObjectsActive(totalShadowObjects, false);

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
        }
    }

    private float GetCurrentSecond()
    {
        if (useAudioTime && narrationAudio != null)
            return narrationAudio.time;

        return Time.time - startTime;
    }

    private void UpdateMoonPosition(float currentSecond)
    {
        if (moonMover == null)
            return;

        if (IsInTimeRange(currentSecond, p0ToP1StartSecond, p0ToP1EndSecond))
        {
            MoveByTime(p0_Start, p1_YariGolge_Donusum, p0ToP1StartSecond, p0ToP1EndSecond, currentSecond);
            return;
        }

        if (currentSecond < p1ToP2StartSecond)
        {
            SetPositionByTime(currentSecond, p0ToP1EndSecond, p1_YariGolge_Donusum, p0_Start);
            return;
        }

        if (IsInTimeRange(currentSecond, p1ToP2StartSecond, p1ToP2EndSecond))
        {
            MoveByTime(p1_YariGolge_Donusum, p2_YariGolge_Dur, p1ToP2StartSecond, p1ToP2EndSecond, currentSecond);
            return;
        }

        if (currentSecond < p2ToP3StartSecond)
        {
            SetPosition(p2_YariGolge_Dur);
            return;
        }

        if (IsInTimeRange(currentSecond, p2ToP3StartSecond, p2ToP3EndSecond))
        {
            MoveByTime(p2_YariGolge_Dur, p3_TamGolge_Donusum, p2ToP3StartSecond, p2ToP3EndSecond, currentSecond);
            return;
        }

        if (currentSecond < p3ToP4StartSecond)
        {
            SetPosition(p3_TamGolge_Donusum);
            return;
        }

        if (IsInTimeRange(currentSecond, p3ToP4StartSecond, p3ToP4EndSecond))
        {
            MoveByTime(p3_TamGolge_Donusum, p4_TamGolge_Dur, p3ToP4StartSecond, p3ToP4EndSecond, currentSecond);
            return;
        }

        if (currentSecond < p4ToP5StartSecond)
        {
            SetPosition(p4_TamGolge_Dur);
            return;
        }

        if (IsInTimeRange(currentSecond, p4ToP5StartSecond, p4ToP5EndSecond))
        {
            MoveByTime(p4_TamGolge_Dur, p5_YariGolge_Donusum2, p4ToP5StartSecond, p4ToP5EndSecond, currentSecond);
            return;
        }

        if (currentSecond < p5ToP6StartSecond)
        {
            SetPosition(p5_YariGolge_Donusum2);
            return;
        }

        if (IsInTimeRange(currentSecond, p5ToP6StartSecond, p5ToP6EndSecond))
        {
            MoveByTime(p5_YariGolge_Donusum2, p6_Moon_Donusum, p5ToP6StartSecond, p5ToP6EndSecond, currentSecond);
            return;
        }

        if (currentSecond < p6ToP7StartSecond)
        {
            SetPosition(p6_Moon_Donusum);
            return;
        }

        if (IsInTimeRange(currentSecond, p6ToP7StartSecond, p6ToP7EndSecond))
        {
            MoveByTime(p6_Moon_Donusum, p7_Bitis, p6ToP7StartSecond, p6ToP7EndSecond, currentSecond);
            return;
        }

        SetPosition(p7_Bitis);
    }

    private void UpdateMoonVisual(float currentSecond)
    {
        if (currentSecond < p0ToP1EndSecond)
        {
            SetMoonVisual(moonStart);
        }
        else if (currentSecond < p2ToP3EndSecond)
        {
            SetMoonVisual(moonHalfShadow);
        }
        else if (currentSecond < p4ToP5EndSecond)
        {
            SetMoonVisual(moonTotalShadow);
        }
        else if (currentSecond < p5ToP6EndSecond)
        {
            SetMoonVisual(moonHalfShadow);
        }
        else
        {
            SetMoonVisual(moonStart);
        }
    }

    private void UpdateShadowObjects(float currentSecond)
    {
        bool showHalfShadow = currentSecond >= p0ToP1EndSecond;
        bool showTotalShadow = currentSecond >= p2ToP3EndSecond;

        SetObjectsActive(halfShadowObjects, showHalfShadow);
        SetObjectsActive(totalShadowObjects, showTotalShadow);
    }

    private void UpdateContinueButton(float currentSecond)
    {
        if (continueButton == null)
            return;

        if (currentSecond >= continueButtonShowSecond)
        {
            continueButton.gameObject.SetActive(true);
        }
    }

    private void MoveByTime(Transform fromPoint, Transform toPoint, float startSecond, float endSecond, float currentSecond)
    {
        if (fromPoint == null || toPoint == null || moonMover == null)
            return;

        if (endSecond <= startSecond)
        {
            moonMover.position = toPoint.position;
            return;
        }

        float t = Mathf.InverseLerp(startSecond, endSecond, currentSecond);
        moonMover.position = Vector3.Lerp(fromPoint.position, toPoint.position, t);
    }

    private bool IsInTimeRange(float currentSecond, float startSecond, float endSecond)
    {
        return currentSecond >= startSecond && currentSecond <= endSecond;
    }

    private void SetPosition(Transform point)
    {
        if (moonMover != null && point != null)
        {
            moonMover.position = point.position;
        }
    }

    private void SetPositionByTime(float currentSecond, float targetSecond, Transform afterPoint, Transform beforePoint)
    {
        if (currentSecond >= targetSecond)
        {
            SetPosition(afterPoint);
        }
        else
        {
            SetPosition(beforePoint);
        }
    }

    private void SetMoonVisual(GameObject activeMoon)
    {
        if (moonStart != null)
            moonStart.SetActive(activeMoon == moonStart);

        if (moonHalfShadow != null)
            moonHalfShadow.SetActive(activeMoon == moonHalfShadow);

        if (moonTotalShadow != null)
            moonTotalShadow.SetActive(activeMoon == moonTotalShadow);
    }

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        foreach (GameObject obj in objects)
        {
            if (obj != null)
                obj.SetActive(active);
        }
    }

    private void OnContinueButtonClicked()
    {
        onContinueClicked?.Invoke();
    }
}