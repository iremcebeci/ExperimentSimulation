using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class DashboardsHeaderController : MonoBehaviour
{
    private AppRouter router;
    private VisualElement root;

    private Label usernameLabel;
    private Label roleLabel;

    public event System.Action OnUserLoaded;

    public void Bind(AppRouter router, VisualElement dashboardRoot)
    {
        this.router = router;
        this.root = dashboardRoot;

        usernameLabel = root.Q<Label>("TopUsernameLabel");
        roleLabel = root.Q<Label>("TopRoleLabel");

        // 1) Önce router session'dan bas (hızlı)
        SetLabelsFromSession();
        OnUserLoaded?.Invoke();

        // 2) İstersen API'den güncelle (zorunlu değil)
        StartCoroutine(RefreshFromApi());
    }

    private void SetLabelsFromSession()
    {
        if (usernameLabel != null)
            usernameLabel.text = $"{router.CurrentName} {router.CurrentSurname}".Trim();

        if (roleLabel != null)
            roleLabel.text = (router.CurrentRoleName ?? "").Trim();
    }

    private IEnumerator RefreshFromApi()
    {
        // Token yoksa /me zaten 401 döner, boşuna gitmeyelim
        if (string.IsNullOrEmpty(router.AccessToken))
            yield break;

        string url = router.ApiBaseUrl + "/api/User/me";
        using var req = UnityWebRequest.Get(url);

        req.SetRequestHeader("Authorization", "Bearer " + router.AccessToken);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[Header] /me alınamadı ({req.responseCode}): {req.error}");
            yield break;
        }

        var dto = JsonUtility.FromJson<UserMeDto>(req.downloadHandler.text);

        if (dto == null) yield break;

        if (usernameLabel != null)
            usernameLabel.text = $"{dto.name} {dto.surname}".Trim();

        if (roleLabel != null)
            roleLabel.text = (dto.roleName ?? "").Trim();
    }

    [System.Serializable]
    private class UserMeDto
    {
        public int id;
        public string name;
        public string surname;
        public string roleName;
    }
}