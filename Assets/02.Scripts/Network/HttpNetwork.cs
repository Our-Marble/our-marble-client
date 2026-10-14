using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json; // Newtonsoft.Json 사용

public class HttpNetwork : Singleton<HttpNetwork>
{
    public string JwtToken { get; private set; } // 전역으로 접근할 수 있도록 관리

    private const string BaseUrl = "http://localhost:8080"; // 서버 주소

    // 0. 이메일 인증번호 전송
    public IEnumerator SendCode(string email, System.Action<bool> onResult = null)
    {
        EmailSendRequest req = new EmailSendRequest { email = email };
        string jsonBody = JsonConvert.SerializeObject(req);

        using (UnityWebRequest webRequest = new UnityWebRequest($"{BaseUrl}/auth/email/send", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"인증번호 전송 실패: {webRequest.error} | 응답: {webRequest.downloadHandler.text}");
                onResult?.Invoke(false);
            }
            else
            {
                Debug.Log("인증번호 전송 성공!");
                onResult?.Invoke(true);
            }
        }
    }

    //1. 이메일 인증번호 확인
    public IEnumerator VerifyCode(string email, string code, System.Action<bool> onResult = null)
    {
        EmailVerifyRequest req = new EmailVerifyRequest { email = email, code = code };
        string jsonBody = JsonConvert.SerializeObject(req);

        using (UnityWebRequest webRequest = new UnityWebRequest($"{BaseUrl}/auth/email/verify", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"인증번호 확인 실패: {webRequest.error} | 응답: {webRequest.downloadHandler.text}");
                onResult?.Invoke(false);
            }
            else
            {
                Debug.Log("이메일 인증 성공!");
                onResult?.Invoke(true);
            }
        }
    }

    // 2. 회원가입
    public IEnumerator Signup(string email, string password, string nickname, System.Action<string, bool> onResult = null)
    {
        SignupRequest req = new SignupRequest { email = email, password = password, nickname = nickname };
        string jsonBody = JsonConvert.SerializeObject(req);

        using (UnityWebRequest webRequest = new UnityWebRequest($"{BaseUrl}/auth/signup", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"회원가입 실패: {webRequest.error} | 응답: {webRequest.downloadHandler.text}");
                onResult?.Invoke(email, false);
            }
            else
            {
                Debug.Log("회원가입 성공!");
                onResult?.Invoke(email, true);
            }
        }
    }

    // 3. 로그인
    public IEnumerator Login(string email, string password, System.Action<string, bool> onResult = null)
    {
        LoginRequest req = new LoginRequest { email = email, password = password };
        string jsonBody = JsonConvert.SerializeObject(req);

        using (UnityWebRequest webRequest = new UnityWebRequest($"{BaseUrl}/auth/login", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"로그인 실패: {webRequest.error} | 응답: {webRequest.downloadHandler.text}");
                onResult?.Invoke(email, false);
            }
            else
            {
                string responseString = webRequest.downloadHandler.text;
                // JsonConvert로 통일하여 파싱
                LoginResponse res = JsonConvert.DeserializeObject<LoginResponse>(responseString);
                
                JwtToken = res.accessToken; // 토큰 저장
                Debug.Log("로그인 성공! JWT 토큰 저장 완료");
                onResult?.Invoke(email, true);
            }
        }
    }

    // 4. 로그아웃
    public IEnumerator Logout(System.Action<bool> onResult = null)
    {
        if (string.IsNullOrEmpty(JwtToken))
        {
            Debug.LogWarning("저장된 토큰이 없습니다. 이미 로그아웃 상태이거나 로그인하지 않았습니다.");
            onResult?.Invoke(false);
            yield break;
        }

        using (UnityWebRequest webRequest = new UnityWebRequest($"{BaseUrl}/auth/logout", "POST"))
        {
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            
            // 로그아웃은 인증이 필요하므로 헤더에 Bearer 토큰 추가
            webRequest.SetRequestHeader("Authorization", "Bearer " + JwtToken);

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"로그아웃 실패: {webRequest.error}");
                onResult?.Invoke(false);
            }
            else
            {
                // 서버가 204 No Content를 반환하므로 성공 처리 후 토큰 초기화
                JwtToken = null;
                Debug.Log("로그아웃 성공! 토큰 초기화 완료");
                onResult?.Invoke(true);
            }
        }
    }


}

// --- DTO 클래스들 ---

[System.Serializable]
public class SignupRequest 
{ 
    public string email; 
    public string password; 
    public string nickname; 
}

[System.Serializable]
public class LoginRequest 
{ 
    public string email; 
    public string password; 
}

[System.Serializable]
public class LoginResponse 
{ 
    public string accessToken; 
}

[System.Serializable]
public class EmailSendRequest
{
    public string email;

}

[System.Serializable]
public class EmailVerifyRequest
{
    public string email;
    public string code;
}
