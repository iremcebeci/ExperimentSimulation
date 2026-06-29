using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ensures there is exactly one active AudioListener at runtime.
/// This prevents Unity from spamming the console with duplicate listener warnings.
/// </summary>
public sealed class AudioListenerGuard : MonoBehaviour
{
    private static AudioListenerGuard instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
            return;

        var go = new GameObject("AudioListenerGuard");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<AudioListenerGuard>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnforceSingleListener();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void LateUpdate()
    {
        EnforceSingleListener();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnforceSingleListener();
    }

    private static void EnforceSingleListener()
    {
        var all = FindObjectsOfType<AudioListener>(true);
        if (all == null || all.Length == 0)
            return;

        AudioListener keep = null;

        // Prefer an active & enabled listener on a MainCamera tagged object.
        for (int i = 0; i < all.Length; i++)
        {
            var listener = all[i];
            if (listener == null)
                continue;

            if (!listener.gameObject.activeInHierarchy || !listener.enabled)
                continue;

            if (listener.CompareTag("MainCamera"))
            {
                keep = listener;
                break;
            }
        }

        // Fallback: first active & enabled listener.
        if (keep == null)
        {
            for (int i = 0; i < all.Length; i++)
            {
                var listener = all[i];
                if (listener == null)
                    continue;

                if (listener.gameObject.activeInHierarchy && listener.enabled)
                {
                    keep = listener;
                    break;
                }
            }
        }

        // If no active listener exists, promote one.
        if (keep == null)
        {
            keep = all[0];
            if (keep != null)
            {
                if (!keep.gameObject.activeSelf)
                    keep.gameObject.SetActive(true);
                keep.enabled = true;
            }
        }

        if (keep == null)
            return;

        for (int i = 0; i < all.Length; i++)
        {
            var listener = all[i];
            if (listener == null || listener == keep)
                continue;

            if (listener.enabled)
                listener.enabled = false;
        }
    }
}
