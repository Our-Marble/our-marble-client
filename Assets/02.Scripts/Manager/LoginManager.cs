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


    private Transform loginCard, emailVerificationCard, signupCard;

    private TMP_InputField emailInput, passwordInput;
    private Toggle saveEmailToggle;
    private Button loginButton, signupButton, forgotButton;

    private TMP_InputField verificationEmailInput, verificationCodeInput;
    private Button sendCodeButton, verifyCodeButton, backToLoginButton;

    private TMP_InputField signupEmailInput, signupPasswordInput, nicknameInput;
    private Button completeSignupButton, backToEmailVerificationButton;

    private ToastView toast;
    private LoadingView loading;

    private bool busy;
    private int fails;
    private float lockUntil;
    private string verifiedEmail;

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
        ShowCard(loginCard);

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
        if (!Input.GetKeyDown(KeyCode.Tab) || busy) return;

        if (loginCard.gameObject.activeSelf)
            (emailInput.isFocused ? passwordInput : emailInput).ActivateInputField();
        else if (emailVerificationCard.gameObject.activeSelf)
            (verificationEmailInput.isFocused ? verificationCodeInput : verificationEmailInput).ActivateInputField();
        else if (signupCard.gameObject.activeSelf)
            (signupPasswordInput.isFocused ? nicknameInput : signupPasswordInput).ActivateInputField();
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
        var canvas = uiRoot.Find("Canvas_Login");
        loginCard = canvas.Find("LoginCard");
        emailVerificationCard = canvas.Find("EmailVerificationCard");
        signupCard = canvas.Find("SignupCard");

        emailInput = Get<TMP_InputField>(loginCard, "EmailInput");
        passwordInput = Get<TMP_InputField>(loginCard, "PasswordInput");
        saveEmailToggle = Get<Toggle>(loginCard, "SaveIdToggle");
        forgotButton = Get<Button>(loginCard, "ForgotButton");
        loginButton = Get<Button>(loginCard, "LoginButton");
        signupButton = Get<Button>(loginCard, "SignupButton");

        verificationEmailInput = Get<TMP_InputField>(emailVerificationCard, "EmailInput");
        verificationCodeInput = Get<TMP_InputField>(emailVerificationCard, "VerificationCodeInput");
        sendCodeButton = Get<Button>(emailVerificationCard, "SendCodeButton");
        verifyCodeButton = Get<Button>(emailVerificationCard, "VerifyCodeButton");
        backToLoginButton = Get<Button>(emailVerificationCard, "BackToLoginButton");

        signupEmailInput = Get<TMP_InputField>(signupCard, "EmailInput");
        signupPasswordInput = Get<TMP_InputField>(signupCard, "PasswordInput");
        nicknameInput = Get<TMP_InputField>(signupCard, "NicknameInput");
        completeSignupButton = Get<Button>(signupCard, "CompleteSignupButton");
        backToEmailVerificationButton = Get<Button>(signupCard, "BackToEmailVerificationButton");

        toast = Get<ToastView>(uiRoot, "Canvas_Toast");
        loading = Get<LoadingView>(uiRoot, "Canvas_Loading");
    }

    private void WireEvents()
    {
        // 한 줄 입력칸에는 Tab이 글자로 들어가지 않게 한다 (Tab은 칸 이동에만 쓴다)
        emailInput.onValidateInput += BlockTab;
        passwordInput.onValidateInput += BlockTab;
        verificationEmailInput.onValidateInput += BlockTab;
        verificationCodeInput.onValidateInput += BlockTab;
        signupPasswordInput.onValidateInput += BlockTab;
        nicknameInput.onValidateInput += BlockTab;

        emailInput.onValueChanged.AddListener(_ => UpdateLoginEnabled());
        passwordInput.onValueChanged.AddListener(_ => UpdateLoginEnabled());
        emailInput.onSubmit.AddListener(_ => passwordInput.ActivateInputField()); // 엔터: 비밀번호 칸으로
        passwordInput.onSubmit.AddListener(_ => TryLogin());                   // 엔터: 로그인
        loginButton.onClick.AddListener(TryLogin);
        signupButton.onClick.AddListener(ShowEmailVerificationCard);
        forgotButton.onClick.AddListener(() => ShowToast("비밀번호 찾기는 아직 준비 중이에요"));

        verificationEmailInput.onValueChanged.AddListener(_ => SetVerifyCodeEnabled(false));
        verificationEmailInput.onSubmit.AddListener(_ => SendEmailCode());
        verificationCodeInput.onSubmit.AddListener(_ => VerifyEmailCode());
        sendCodeButton.onClick.AddListener(SendEmailCode);
        verifyCodeButton.onClick.AddListener(VerifyEmailCode);
        backToLoginButton.onClick.AddListener(ShowLoginCard);

        signupPasswordInput.onSubmit.AddListener(_ => nicknameInput.ActivateInputField());
        nicknameInput.onSubmit.AddListener(_ => TrySignUp());
        completeSignupButton.onClick.AddListener(TrySignUp);
        backToEmailVerificationButton.onClick.AddListener(ShowEmailVerificationCard);
    }

    private void ShowCard(Transform visibleCard)
    {
        loginCard.gameObject.SetActive(visibleCard == loginCard);
        emailVerificationCard.gameObject.SetActive(visibleCard == emailVerificationCard);
        signupCard.gameObject.SetActive(visibleCard == signupCard);
    }

    private void ShowLoginCard()
    {
        if (busy) return;
        ShowCard(loginCard);
        UpdateLoginEnabled();
        (string.IsNullOrEmpty(emailInput.text) ? emailInput : passwordInput).ActivateInputField();
    }

    private void ShowEmailVerificationCard()
    {
        if (busy) return;
        string email = signupCard.gameObject.activeSelf
            ? signupEmailInput.text.Trim()
            : emailInput.text.Trim();
        verificationEmailInput.SetTextWithoutNotify(email);
        verificationCodeInput.SetTextWithoutNotify(string.Empty);
        SetVerifyCodeEnabled(false);
        ShowCard(emailVerificationCard);
        verificationEmailInput.ActivateInputField();
    }

    private void ShowSignupCard()
    {
        signupEmailInput.SetTextWithoutNotify(verifiedEmail);
        signupPasswordInput.SetTextWithoutNotify(string.Empty);
        nicknameInput.SetTextWithoutNotify(string.Empty);
        ShowCard(signupCard);
        signupPasswordInput.ActivateInputField();
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

    private void SetVerifyCodeEnabled(bool enabled)
    {
        verifyCodeButton.interactable = enabled;
        verifyCodeButton.targetGraphic.color = enabled ? UIPalette.Mint : UIPalette.Neutral;
        verifyCodeButton.GetComponentInChildren<TMP_Text>().color = enabled ? Color.white : UIPalette.InkSub;
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

        yield return StartCoroutine(HttpNetwork.Instance.Login(email, password, HandleLoginResponse));
    }


    private void HandleLoginResponse(string email, bool success, long statusCode, string message)
    {
        Debug.Log($"[Login] HTTP {statusCode}: {message}");

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
            else ShowToast($"{message} ({fails}/{MaxFails})");
            passwordInput.text = "";
            UIFx.Shake(passwordInput.transform);
            passwordInput.ActivateInputField();
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

    private void TrySignUp()
    {
        if (busy) return;
        string email = signupEmailInput.text.Trim();
        string password = signupPasswordInput.text;
        string nickname = nicknameInput.text;
        if (email.Length == 0) { UIFx.Shake(signupEmailInput.transform); ShowToast("인증된 이메일이 필요해요"); return; }
        if (password.Length == 0) { UIFx.Shake(signupPasswordInput.transform); ShowToast("비밀번호를 입력해 주세요"); return; }
        if (nickname.Length == 0) { UIFx.Shake(nicknameInput.transform); ShowToast("닉네임을 입력해 주세요"); return; }

        StartCoroutine(SignUpRoutine(email, password, nickname));
    }

    private IEnumerator SignUpRoutine(string email, string password, string nickname)
    {
        busy = true;
        ShowLoading(true);

        yield return StartCoroutine(HttpNetwork.Instance.Signup(email, password, nickname, HandleSignUpResponse));
    }

    private void HandleSignUpResponse(string email, bool success)
    {
        ShowLoading(false);
        busy = false;

        if (success)
        {
            Debug.Log("[Signup] 회원가입 성공");
            emailInput.SetTextWithoutNotify(email);
            passwordInput.SetTextWithoutNotify(string.Empty);
            ShowLoginCard();
            ShowToast("회원가입이 완료됐어요. 로그인해 주세요");
        }
        else
        {
            ShowToast("회원가입에 실패했어요");
        }
    }

    // ───────────── 이메일 인증 ─────────────

    private void SendEmailCode()
    {
        if (busy) return;
        string email = verificationEmailInput.text.Trim();
        if (email.Length == 0) { UIFx.Shake(verificationEmailInput.transform); ShowToast("이메일을 입력해 주세요"); return; }

        StartCoroutine(SendEmailCodeRoutine(email));
    }

    private IEnumerator SendEmailCodeRoutine(string email)
    {
        busy = true;
        ShowLoading(true, "인증번호를 전송중");

        yield return StartCoroutine(HttpNetwork.Instance.SendCode(email, HandleSendEmailCodeResponse));
    }

    private void HandleSendEmailCodeResponse(bool success, long statusCode, string message)
    {
        Debug.Log($"[Email/Send] HTTP {statusCode}: {message}");
        ShowLoading(false);
        busy = false;

        if (success)
        {
            Debug.Log("[Email] 인증번호 전송 성공");
            SetVerifyCodeEnabled(true);
            verificationCodeInput.ActivateInputField();
            ShowToast(message);
        }
        else
        {
            SetVerifyCodeEnabled(false);
            ShowToast(message);
        }
    }

    private void VerifyEmailCode()
    {
        if (busy) return;
        if (!verifyCodeButton.interactable) { ShowToast("인증번호를 먼저 전송해 주세요"); return; }
        string email = verificationEmailInput.text.Trim();
        string code = verificationCodeInput.text.Trim();
        if (email.Length == 0) { UIFx.Shake(verificationEmailInput.transform); ShowToast("이메일을 입력해 주세요"); return; }
        if (code.Length == 0) { UIFx.Shake(verificationCodeInput.transform); ShowToast("인증번호를 입력해 주세요"); return; }

        verifiedEmail = email;
        StartCoroutine(VerifyEmailCodeRoutine(email, code));
    }

    private IEnumerator VerifyEmailCodeRoutine(string email, string code)
    {
        busy = true;
        ShowLoading(true);

        yield return StartCoroutine(HttpNetwork.Instance.VerifyCode(email, code, HandleVerifyEmailCodeResponse));
    }

    private void HandleVerifyEmailCodeResponse(bool success, long statusCode, string message)
    {
        Debug.Log($"[Email/Verify] HTTP {statusCode}: {message}");
        ShowLoading(false);
        busy = false;

        if (success)
        {
            Debug.Log("[Email] 이메일 인증 성공");
            ShowSignupCard();
            ShowToast(message);
        }
        else
        {
            ShowToast(message);
        }
    }

    // ───────────── 알림 메시지(토스트) / 로딩 화면 ─────────────

    private void ShowToast(string message) => toast.Show(message);

    private void ShowLoading(bool show, string message=null) => loading.Show(show, message);
}
