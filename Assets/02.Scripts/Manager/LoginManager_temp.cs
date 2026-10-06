using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로그인 화면 기능 테스트용 임시 매니저. 서버 없이 가짜 계정으로 동작한다.
/// 연동할 때는 CheckAccount를 서버 로그인 요청으로 바꾸면 된다.
/// UI_LoginScene의 "UI" 루트를 찾아 이름으로 연결한다.
/// 테스트 계정: marble / 1234, test / 1234
/// </summary>
public class LoginManager_temp : MonoBehaviour
{
    private const string SavedIdKey = "Login.SavedId";
    private const string SaveIdKey = "Login.SaveId";
    private const int MaxFails = 5;
    private const float LockSeconds = 10f;

    // 목업 계정. 비밀번호를 클라이언트가 들고 있는 건 목업이라서만 허용된다.
    private static readonly Dictionary<string, string> Accounts = new Dictionary<string, string>
    {
        { "marble", "1234" },
        { "test", "1234" },
    };

    [Header("연결")]
    [SerializeField] private Transform uiRoot;

    [Header("테스트 옵션")]
    [Tooltip("서버가 응답하기까지 걸리는 시간 흉내(초).")]
    [SerializeField] private float serverDelay = 0.6f;

    private TMP_InputField idInput, passwordInput;
    private Toggle saveIdToggle;
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
        bool save = PlayerPrefs.GetInt(SaveIdKey, 1) == 1;
        saveIdToggle.SetIsOnWithoutNotify(save);
        if (save) idInput.text = PlayerPrefs.GetString(SavedIdKey, "");
        UpdateLoginEnabled();
    }

    private void Start()
    {
        // 처음에는 비어 있는 칸에 바로 입력할 수 있게 한다
        (string.IsNullOrEmpty(idInput.text) ? idInput : passwordInput).ActivateInputField();
    }

    private void Update()
    {
        // Tab / Shift+Tab: 아이디 ↔ 비밀번호 칸 이동 (칸이 두 개뿐이라 방향과 상관없이 서로 넘어간다)
        if (!Input.GetKeyDown(KeyCode.Tab) || busy) return;
        (idInput.isFocused ? passwordInput : idInput).ActivateInputField();
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
        idInput = Get<TMP_InputField>(card, "IdInput");
        passwordInput = Get<TMP_InputField>(card, "PasswordInput");
        saveIdToggle = Get<Toggle>(card, "SaveIdToggle");
        forgotButton = Get<Button>(card, "ForgotButton");
        loginButton = Get<Button>(card, "LoginButton");
        signupButton = Get<Button>(card, "SignupButton");

        toast = uiRoot.Find("Canvas_Toast").GetComponent<ToastView>();
        loading = uiRoot.Find("Canvas_Loading").GetComponent<LoadingView>();
    }

    private void WireEvents()
    {
        // 한 줄 입력칸에는 Tab이 글자로 들어가지 않게 한다 (Tab은 칸 이동에만 쓴다)
        idInput.onValidateInput += (text, index, c) => c == '\t' ? '\0' : c;
        passwordInput.onValidateInput += (text, index, c) => c == '\t' ? '\0' : c;
        idInput.onValueChanged.AddListener(_ => UpdateLoginEnabled());
        passwordInput.onValueChanged.AddListener(_ => UpdateLoginEnabled());
        idInput.onSubmit.AddListener(_ => passwordInput.ActivateInputField()); // 엔터: 비밀번호 칸으로
        passwordInput.onSubmit.AddListener(_ => TryLogin());                   // 엔터: 로그인
        loginButton.onClick.AddListener(TryLogin);
        signupButton.onClick.AddListener(() => ShowToast("회원가입은 아직 준비 중이에요"));
        forgotButton.onClick.AddListener(() => ShowToast("비밀번호 찾기는 아직 준비 중이에요"));
    }

    // 두 칸이 모두 채워져야 로그인 버튼이 켜진다
    private void UpdateLoginEnabled()
    {
        bool ready = idInput.text.Trim().Length > 0 && passwordInput.text.Length > 0;
        loginButton.interactable = ready;
        loginButton.targetGraphic.color = ready ? UIPalette.Mint : UIPalette.Neutral;
        loginButton.GetComponentInChildren<TMP_Text>().color = ready ? Color.white : UIPalette.InkSub;
    }

    // ───────────── 로그인 ─────────────

    private void TryLogin()
    {
        if (busy) return;
        string id = idInput.text.Trim();
        string password = passwordInput.text;
        if (id.Length == 0) { UIFx.Shake(idInput.transform); ShowToast("아이디를 입력해 주세요"); return; }
        if (password.Length == 0) { UIFx.Shake(passwordInput.transform); ShowToast("비밀번호를 입력해 주세요"); return; }
        if (Time.unscaledTime < lockUntil) { ShowToast("잠시 후 다시 시도해 주세요"); return; }

        StartCoroutine(LoginRoutine(id, password));
    }

    private IEnumerator LoginRoutine(string id, string password)
    {
        busy = true;
        ShowLoading(true);
        yield return new WaitForSecondsRealtime(serverDelay);

        // ── 서버 쪽 계정 확인 (목업) ──
        if (!CheckAccount(id, password))
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
        SaveId(id);
        Debug.Log($"[Login] 로그인 성공: {id}");
        if (!SceneFlow.ToLobby())
        {
            ShowLoading(false);
            busy = false;
            ShowToast("로비 화면을 불러오지 못했어요");
        }
    }

    private static bool CheckAccount(string id, string password) =>
        Accounts.TryGetValue(id, out var expected) && expected == password;

    private void SaveId(string id)
    {
        bool save = saveIdToggle.isOn;
        PlayerPrefs.SetInt(SaveIdKey, save ? 1 : 0);
        if (save) PlayerPrefs.SetString(SavedIdKey, id);
        else PlayerPrefs.DeleteKey(SavedIdKey);
    }

    // ───────────── 알림 메시지(토스트) / 로딩 화면 ─────────────

    private void ShowToast(string message) => toast.Show(message);

    private void ShowLoading(bool show) => loading.Show(show);
}
