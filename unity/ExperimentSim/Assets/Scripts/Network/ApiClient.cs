using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class ApiClient
{
    private readonly string baseUrl;
    private string token;

    public ApiClient(string baseUrl)
    {
        this.baseUrl = baseUrl.TrimEnd('/');
    }

    public void SetToken(string jwt)
    {
        token = jwt;
        PlayerPrefs.SetString("jwt", jwt);
    }

    public void LoadToken()
    {
        token = PlayerPrefs.GetString("jwt", "");
    }

    public void ClearToken()
    {
        token = "";
        PlayerPrefs.DeleteKey("jwt");
    }

    private void AddAuth(UnityWebRequest req)
    {
        if (!string.IsNullOrEmpty(token))
            req.SetRequestHeader("Authorization", $"Bearer {token}");
    }

    public async Task<string> PostJson(string path, string json)
    {
        var url = $"{baseUrl}{path}";

        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        AddAuth(req);

        await req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            throw new System.Exception($"{req.responseCode} {req.error} {req.downloadHandler.text}");

        return req.downloadHandler.text;
    }

    public async Task<string> Get(string path)
    {
        var url = $"{baseUrl}{path}";

        var req = UnityWebRequest.Get(url);
        AddAuth(req);

        await req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            throw new System.Exception($"{req.responseCode} {req.error} {req.downloadHandler.text}");

        return req.downloadHandler.text;
    }
}