using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class EclipseOrderingActivity : MonoBehaviour
{
    [Header("Güneş Tutulması Slotları")]
    public BodyDropSlot solarSlot1;
    public BodyDropSlot solarSlot2;
    public BodyDropSlot solarSlot3;

    [Header("Ay Tutulması Slotları")]
    public BodyDropSlot lunarSlot1;
    public BodyDropSlot lunarSlot2;
    public BodyDropSlot lunarSlot3;

    [Header("Butonlar")]
    public Button checkButton;
    public Button continueButton;

    [Header("Feedback Görselleri")]
    public GameObject rightFeedbackImage;
    public GameObject falseFeedbackImage;

    [Header("Ses")]
    public AudioSource audioSource;
    public AudioClip introClip;
    public AudioClip correctClip;
    public AudioClip wrongClip;

    private bool answerSaved = false;

    private void Awake()
    {
        if (checkButton != null)
        {
            checkButton.onClick.RemoveListener(CheckAnswers);
            checkButton.onClick.AddListener(CheckAnswers);
        }

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }
    }

    private void OnEnable()
    {
        SetupCorrectAnswers();
        ResetActivityVisuals();
        PlayClip(introClip);
    }

    private void SetupCorrectAnswers()
    {
        if (solarSlot1 != null) solarSlot1.expectedBodyType = CelestialBodyType.Sun;
        if (solarSlot2 != null) solarSlot2.expectedBodyType = CelestialBodyType.Moon;
        if (solarSlot3 != null) solarSlot3.expectedBodyType = CelestialBodyType.Earth;

        if (lunarSlot1 != null) lunarSlot1.expectedBodyType = CelestialBodyType.Sun;
        if (lunarSlot2 != null) lunarSlot2.expectedBodyType = CelestialBodyType.Earth;
        if (lunarSlot3 != null) lunarSlot3.expectedBodyType = CelestialBodyType.Moon;
    }

    private void ResetActivityVisuals()
    {
        answerSaved = false;

        if (rightFeedbackImage != null)
            rightFeedbackImage.SetActive(false);

        if (falseFeedbackImage != null)
            falseFeedbackImage.SetActive(false);

        if (checkButton != null)
            checkButton.gameObject.SetActive(true);

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);
    }

    public void CheckAnswers()
    {
        bool solarCorrect =
            solarSlot1 != null && solarSlot1.IsCorrect() &&
            solarSlot2 != null && solarSlot2.IsCorrect() &&
            solarSlot3 != null && solarSlot3.IsCorrect();

        bool lunarCorrect =
            lunarSlot1 != null && lunarSlot1.IsCorrect() &&
            lunarSlot2 != null && lunarSlot2.IsCorrect() &&
            lunarSlot3 != null && lunarSlot3.IsCorrect();

        bool allCorrect = solarCorrect && lunarCorrect;

        SaveAssignmentAnswerOnce(allCorrect);

        if (rightFeedbackImage != null)
            rightFeedbackImage.SetActive(allCorrect);

        if (falseFeedbackImage != null)
            falseFeedbackImage.SetActive(!allCorrect);

        if (checkButton != null)
            checkButton.gameObject.SetActive(false);

        if (continueButton != null)
            continueButton.gameObject.SetActive(true);

        if (allCorrect)
            PlayClip(correctClip);
        else
            PlayClip(wrongClip);

        if (ExperimentResultTracker.Instance != null)
        {
            ExperimentResultTracker.Instance.AddSingleResult(allCorrect);
        }
    }

    private void SaveAssignmentAnswerOnce(bool allCorrect)
    {
        if (answerSaved)
            return;

        answerSaved = true;

        string studentAnswer =
            "Güneş tutulması: " + BuildOrderText(solarSlot1, solarSlot2, solarSlot3) +
            " | Ay tutulması: " + BuildOrderText(lunarSlot1, lunarSlot2, lunarSlot3);

        string correctAnswer =
            "Güneş tutulması: Güneş - Ay - Dünya | Ay tutulması: Güneş - Dünya - Ay";

        AssignmentResultSubmitter.AddAnswer(
            "Güneş ve Ay tutulmasında gök cisimlerinin doğru sıralamasını yerleştirme",
            studentAnswer,
            correctAnswer,
            allCorrect
        );
    }

    private string BuildOrderText(BodyDropSlot slot1, BodyDropSlot slot2, BodyDropSlot slot3)
    {
        return GetSlotBodyText(slot1) + " - " + GetSlotBodyText(slot2) + " - " + GetSlotBodyText(slot3);
    }

    private string GetSlotBodyText(BodyDropSlot slot)
    {
        if (slot == null)
            return "Boş";

        DraggableBody body = slot.CurrentBody;

        if (body == null)
            body = slot.GetComponentInChildren<DraggableBody>();

        if (body == null)
            return "Boş";

        return BodyTypeToText(body.bodyType);
    }

    private string BodyTypeToText(CelestialBodyType type)
    {
        switch (type)
        {
            case CelestialBodyType.Sun:
                return "Güneş";
            case CelestialBodyType.Moon:
                return "Ay";
            case CelestialBodyType.Earth:
                return "Dünya";
            default:
                return type.ToString();
        }
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource == null)
        {
            Debug.LogWarning("AudioSource bağlı değil.");
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning("AudioClip bağlı değil.");
            return;
        }

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }
}