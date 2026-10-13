using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public static class AuthApi
{
    private const string BaseUrl = "http://localhost:8080/auth";

    public static string AccessToken { get; private set; }

    [Serializable]
    public class PlayerResponse
    {
        public long id;
        public string email;
        public string nickname;
    }

    [Serializable]
    private class SignupRequest
    {
        public string email;
        public string password;
        public string nickname;
    }

    [Serializable]
    private class LoginRequest
    {
        public string email;
        public string password;
    }

    [Serializable]
    private class AccessTokenResponse
    {
        public string accessToken;
    }

    public static IEnumerator Signup(
        string email,
        string password,
        string nickname,
        Action<PlayerResponse> onSuccess,
        Action<long, string> onError)
    {
        string json = JsonUtility.ToJson(new SignupRequest
        {
            email = email,
            password = password,
            nickname = nickname
        });

        using UnityWebRequest request = UnityWebRequest.Post(
            $"{BaseUrl}/signup", json, "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(request.responseCode, ErrorMessage(request));
            yield break;
        }

        onSuccess?.Invoke(JsonUtility.FromJson<PlayerResponse>(request.downloadHandler.text));
    }

    public static IEnumerator Login(
        string email,
        string password,
        Action onSuccess,
        Action<long, string> onError)
    {
        string json = JsonUtility.ToJson(new LoginRequest
        {
            email = email,
            password = password
        });

        using UnityWebRequest request = UnityWebRequest.Post(
            $"{BaseUrl}/login", json, "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(request.responseCode, ErrorMessage(request));
            yield break;
        }

        AccessToken = JsonUtility.FromJson<AccessTokenResponse>(
            request.downloadHandler.text).accessToken;
        onSuccess?.Invoke();
    }

    public static IEnumerator Logout(Action onSuccess, Action<long, string> onError)
    {
        using UnityWebRequest request = new UnityWebRequest(
            $"{BaseUrl}/logout", UnityWebRequest.kHttpVerbPOST);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", $"Bearer {AccessToken}");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(request.responseCode, ErrorMessage(request));
            yield break;
        }

        AccessToken = null;
        onSuccess?.Invoke();
    }

    private static string ErrorMessage(UnityWebRequest request)
    {
        string response = request.downloadHandler?.text;
        return string.IsNullOrEmpty(response) ? request.error : response;
    }
}
