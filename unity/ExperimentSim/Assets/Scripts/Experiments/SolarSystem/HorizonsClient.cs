using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

public class HorizonsClient : MonoBehaviour
{
    [Serializable]
    private class HorizonsResponse
    {
        public string result;
        public string error;
    }

    public IEnumerator GetPositionAtTime(
        string command,
        string center,
        DateTime selectedUtc,
        Action<Vector3> onSuccess,
        Action<string> onError)
    {
        DateTime stopUtc = selectedUtc.AddMinutes(1);

        string url = BuildUrl(
            command,
            center,
            selectedUtc,
            stopUtc,
            "1 min"
        );

        Debug.Log("NASA/JPL URL: " + url);

        using UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(request.error);
            yield break;
        }

        HorizonsResponse response =
            JsonUtility.FromJson<HorizonsResponse>(request.downloadHandler.text);

        if (response == null)
        {
            onError?.Invoke("JSON cevap okunamadı.");
            yield break;
        }

        if (!string.IsNullOrEmpty(response.error))
        {
            onError?.Invoke(response.error);
            yield break;
        }

        if (string.IsNullOrEmpty(response.result))
        {
            onError?.Invoke("Horizons result boş geldi.");
            yield break;
        }

        List<Vector3> vectors = ParseUnityPositionVectors(response.result);

        if (vectors.Count == 0)
        {
            Debug.Log(response.result);
            onError?.Invoke("Vektör parse edilemedi. Console'da result çıktısına bak.");
            yield break;
        }

        onSuccess?.Invoke(vectors[0]);
    }

    public IEnumerator GetRawPositionAtTime(
        string command,
        string center,
        DateTime selectedUtc,
        Action<Vector3> onSuccess,
        Action<string> onError)
    {
        DateTime stopUtc = selectedUtc.AddMinutes(1);

        string url = BuildUrl(
            command,
            center,
            selectedUtc,
            stopUtc,
            "1 min"
        );

        Debug.Log("NASA/JPL RAW URL: " + url);

        using UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(request.error);
            yield break;
        }

        HorizonsResponse response =
            JsonUtility.FromJson<HorizonsResponse>(request.downloadHandler.text);

        if (response == null)
        {
            onError?.Invoke("JSON cevap okunamadı.");
            yield break;
        }

        if (!string.IsNullOrEmpty(response.error))
        {
            onError?.Invoke(response.error);
            yield break;
        }

        if (string.IsNullOrEmpty(response.result))
        {
            onError?.Invoke("Horizons result boş geldi.");
            yield break;
        }

        List<Vector3> vectors = ParseRawPositionVectors(response.result);

        if (vectors.Count == 0)
        {
            Debug.Log(response.result);
            onError?.Invoke("RAW vektör parse edilemedi. Console'da result çıktısına bak.");
            yield break;
        }

        onSuccess?.Invoke(vectors[0]);
    }

    private string BuildUrl(
        string command,
        string center,
        DateTime startUtc,
        DateTime stopUtc,
        string stepSize)
    {
        string baseUrl = "https://ssd.jpl.nasa.gov/api/horizons.api";

        Dictionary<string, string> query = new Dictionary<string, string>
        {
            { "format", "json" },
            { "COMMAND", $"'{command}'" },
            { "OBJ_DATA", "'NO'" },
            { "MAKE_EPHEM", "'YES'" },
            { "EPHEM_TYPE", "'VECTORS'" },
            { "CENTER", $"'{center}'" },
            { "START_TIME", $"'{startUtc:yyyy-MM-dd HH:mm:ss}'" },
            { "STOP_TIME", $"'{stopUtc:yyyy-MM-dd HH:mm:ss}'" },
            { "STEP_SIZE", $"'{stepSize}'" },
            { "TIME_TYPE", "'UT'" },
            { "OUT_UNITS", "'KM-S'" },
            { "REF_PLANE", "'ECLIPTIC'" },
            { "REF_SYSTEM", "'ICRF'" },
            { "VEC_TABLE", "'2'" },
            { "CSV_FORMAT", "'YES'" }
        };

        List<string> parts = new List<string>();

        foreach (var item in query)
        {
            parts.Add(item.Key + "=" + UnityWebRequest.EscapeURL(item.Value));
        }

        return baseUrl + "?" + string.Join("&", parts);
    }

    private List<Vector3> ParseUnityPositionVectors(string result)
    {
        List<Vector3> vectors = new List<Vector3>();

        int startIndex = result.IndexOf("$$SOE", StringComparison.Ordinal);
        int endIndex = result.IndexOf("$$EOE", StringComparison.Ordinal);

        if (startIndex < 0 || endIndex < 0)
        {
            Debug.LogWarning("NASA result içinde $$SOE / $$EOE bulunamadı.");
            return vectors;
        }

        string dataBlock = result.Substring(startIndex + 5, endIndex - startIndex - 5);
        string[] lines = dataBlock.Split('\n');

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] columns = line.Split(',');

            // CSV + VEC_TABLE 2:
            // JD, Date, X, Y, Z, VX, VY, VZ
            if (columns.Length < 5)
                continue;

            bool okX = double.TryParse(
                columns[2].Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double x
            );

            bool okY = double.TryParse(
                columns[3].Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double y
            );

            bool okZ = double.TryParse(
                columns[4].Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double z
            );

            if (!okX || !okY || !okZ)
                continue;

            // NASA: X, Y, Z
            // Unity sahnesinde yörüngeyi yatay XZ düzlemine almak için:
            // Unity X = NASA X
            // Unity Y = NASA Z
            // Unity Z = NASA Y
            Vector3 unityVector = new Vector3(
                (float)x,
                (float)z,
                (float)y
            );

            vectors.Add(unityVector);
        }

        return vectors;
    }

    private List<Vector3> ParseRawPositionVectors(string result)
    {
        List<Vector3> vectors = new List<Vector3>();

        int startIndex = result.IndexOf("$$SOE", StringComparison.Ordinal);
        int endIndex = result.IndexOf("$$EOE", StringComparison.Ordinal);

        if (startIndex < 0 || endIndex < 0)
        {
            Debug.LogWarning("NASA result içinde $$SOE / $$EOE bulunamadı.");
            return vectors;
        }

        string dataBlock = result.Substring(startIndex + 5, endIndex - startIndex - 5);
        string[] lines = dataBlock.Split('\n');

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] columns = line.Split(',');

            // CSV + VEC_TABLE 2:
            // JD, Date, X, Y, Z, VX, VY, VZ
            if (columns.Length < 5)
                continue;

            bool okX = double.TryParse(
                columns[2].Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double x
            );

            bool okY = double.TryParse(
                columns[3].Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double y
            );

            bool okZ = double.TryParse(
                columns[4].Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double z
            );

            if (!okX || !okY || !okZ)
                continue;

            // Burada Unity eksen dönüşümü yapmıyoruz.
            // NASA'nın ham X, Y, Z değerini alıyoruz.
            Vector3 rawVector = new Vector3(
                (float)x,
                (float)y,
                (float)z
            );

            vectors.Add(rawVector);
        }

        return vectors;
    }
}