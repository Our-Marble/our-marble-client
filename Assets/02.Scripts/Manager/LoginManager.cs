using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로그인 화면 매니저. AuthApi를 통해 서버 로그인을 요청한다.
/// UI_LoginScene의 "UI" 루트를 찾아 이름으로 연결한다.
/// </summary>
public class LoginManager : MonoBehaviour
{
    private const string SavedEmailKey = "Login.SavedEmail";
    private const string SaveEmailKey = "Login.SaveEmail";
    private const int MaxFails = 5;
    private const float LockSeconds = 10f;

    [Header("연결")]
    [SerializeField] private Transform uiRoot;


    private TMP_InputField emailInput, passwordInput;
    private Toggle saveEmailToggle;
    private Button loginButton, signupButton, forgotButton;

    private ToastView toast;
    private LoadingView loading;

    private bool busy;
    private int fails;
    private float lockUntil;

    private void Reset()
    {
        var ui = GameObject.Find("UI");
        if (ui != null) uiRoot = ui.transform;
    }

    private void Awake()
    {
        if (uiRoot == null) Reset();
        if (uiRoot == null) { Debug.LogError("[Login] 'UI' 루트를 찾을 수 없습니다.", this); enabled = false; return; }

        Bind();
        WireEvents();

        // 저장해 둔 아이디를 채운다
        bool save = PlayerPrefs.GetInt(SaveEmailKey, 1) == 1;
        saveEmailToggle.SetIsOnWithoutNotify(save);
        if (save) emailInput.text = PlayerPrefs.GetString(SavedEmailKey, "");
        UpdateLoginEnabled();
    }

    private void Start()
    {
        // 처음에는 비어 있는 칸에 바로 입력할 수 있게 한다
        (string.IsNullOrEmpty(emailInput.text) ? emailInput : passwordInput).ActivateInputField();
    }

    private void Update()
    {
        // Tab / Shift+Tab: 아이디 ↔ 비밀번호 칸 이동 (칸이 두 개뿐이라 방향과 상관없이 서로 넘어간다)
        if (!Input.GetKeyDown(KeyCode.Tab) || busy) return;
        (emailInput.isFocused ? passwordInput : emailInput).ActivateInputField();
    }

    private static T Get<T>(Transform root, string path) where T : Component
    {
        var t = root.Find(path);
        if (t == null) { Debug.LogWarning($"[Login] '{root.name}/{path}' 없음"); return null; }
        var c = t.GetComponent<T>();
        if (c == null) Debug.LogWarning($"[Login] '{root.name}/{path}'에 {typeof(T).Name} 없음");
        return c;
    }

    private void Bind()
    {
        var card = uiRoot.Find("Canvas_Login/LoginCard");
        emailInput = Get<TMP_InputField>(card, "EmailInput");
        passwordInput = Get<TMP_InputField>(card, "PasswordInput");
        saveEmailToggle = Get<Toggle>(card, "SaveEmailToggle");
        forgotButton = Get<Button>(card, "ForgotButton");
        loginButton = Get<Button>(card, "LoginButton");
        signupButton = Get<Button>(card, "SignupButton");

        toast = Get<ToastView>(uiRoot, "Canvas_Toast");
        loading = Get<LoadingView>(uiRoot, "Canvas_Loading");
    }

    private void WireEvents()
    {
        // 한 줄 입력칸에는 Tab이 글자로 들어가지 않게 한다 (Tab은 칸 이동에만 쓴다)
        emailInput.onValidateInput += BlockTab;
        passwordInput.onValidateInput += BlockTab;
        emailInput.onValueChanged.AddListener(_ => UpdateLoginEnabled());
        passwordInput.onValueChanged.AddListener(_ => UpdateLoginEnabled());
        emailInput.onSubmit.AddListener(_ => passwordInput.ActivateInputField()); // 엔터: 비밀번호 칸으로
        passwordInput.onSubmit.AddListener(_ => TryLogin());                   // 엔터: 로그인
        loginButton.onClick.AddListener(TryLogin);
        signupButton.onClick.AddListener(() => ShowToast("회원가입은 아직 준비 중이에요"));
        forgotButton.onClick.AddListener(() => ShowToast("비밀번호 찾기는 아직 준비 중이에요"));
    }

    // 입력칸에 탭 문자가 들어가지 않게 막는다
    private static char BlockTab(string text, int index, char c) => c == '\t' ? '\0' : c;

    // 두 칸이 모두 채워져야 로그인 버튼이 켜진다
    private void UpdateLoginEnabled()
    {
        bool ready = emailInput.text.Trim().Length > 0 && passwordInput.text.Length > 0;
        loginButton.interactable = ready;
        loginButton.targetGraphic.color = ready ? UIPalette.Mint : UIPalette.Neutral;
        loginButton.GetComponentInChildren<TMP_Text>().color = ready ? Color.white : UIPalette.InkSub;
    }

    // ───────────── 로그인 ─────────────

    private void TryLogin()
    {
        if (busy) return;
        string email = emailInput.text.Trim();
        string password = passwordInput.text;
        if (email.Length == 0) { UIFx.Shake(emailInput.transform); ShowToast("이메일를 입력해 주세요"); return; }
        if (password.Length == 0) { UIFx.Shake(passwordInput.transform); ShowToast("비밀번호를 입력해 주세요"); return; }
        if (Time.unscaledTime < lockUntil) { ShowToast("잠시 후 다시 시도해 주세요"); return; }

        StartCoroutine(LoginRoutine(email, password));
    }

    private IEnumerator LoginRoutine(string email, string password)
    {
        busy = true;
        ShowLoading(true);

        // ── 서버 쪽 계정 확인 (목업) ──
        yield return StartCoroutine(HttpNetwork.Instance.Login(email, password, HandleLoginResponse));
    }

    private void HandleLoginResponse(string email, bool success)
    {
        if (success)
        {
            fails = 0;
            SaveEmail(email);
            Debug.Log($"[Login] 로그인 성공: {email}");
            
            Debug.Log($"TCP 연결을 시도합니다...");
            WebSocketNetwork.Instance.Connect();
            
            if (!SceneFlow.ToLobby())
            {
                ShowLoading(false);
                busy = false;
                ShowToast("로비 화면을 불러오지 못했어요");
            }
        }
        else
        {
            ShowLoading(false);
            busy = false;
            fails++;
            if (fails >= MaxFails)
            {
                fails = 0;
                lockUntil = Time.unscaledTime + LockSeconds;
                ShowToast("여러 번 틀렸어요. 잠시 후 다시 시도해 주세요");
            }
            else ShowToast($"아이디 또는 비밀번호가 맞지 않아요 ({fails}/{MaxFails})");
            passwordInput.text = "";
            UIFx.Shake(passwordInput.transform);
            passwordInput.ActivateInputField();
            yield break;
        }

        fails = 0;
        SaveId(email);
        Debug.Log($"[Login] 로그인 성공: {email}");
        if (!SceneFlow.ToLobby())
        {
            ShowLoading(false);
            busy = false;
            ShowToast("로비 화면을 불러오지 못했어요");
        }
    }

    private void SaveEmail(string email)
    {
        bool save = saveEmailToggle.isOn;
        PlayerPrefs.SetInt(SaveEmailKey, save ? 1 : 0);
        if (save) PlayerPrefs.SetString(SavedEmailKey, email);
        else PlayerPrefs.DeleteKey(SavedEmailKey);
    }

    // ───────────── 회원가입 ─────────────
    
    
    
    // ───────────── 알림 메시지(토스트) / 로딩 화면 ─────────────

    private void ShowToast(string message) => toast.Show(message);

    private void ShowLoading(bool show) => loading.Show(show);
}
