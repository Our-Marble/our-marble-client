using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<T>();
                
                if (_instance == null)
                {
                    GameObject go = new GameObject(typeof(T).Name);
                    _instance = go.AddComponent<T>();
                }
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        // 중복 인스턴스 검사 및 파괴 로직 공통화
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this as T;

        // 추가적인 Awake 로직을 수행할 수 있도록 하위 클래스에 열어줌 (옵션)
        OnAwake();
    }

    /// <label>자식 클래스에서 추가적인 초기화 코드가 필요할 때 오버라이드하여 사용합니다.</label>
    protected virtual void OnAwake() { }
}