using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    /// <summary>씬이 바뀌어도 유지할지. 씬 전용 매니저는 자식 클래스에서 false로 오버라이드한다.</summary>
    protected virtual bool PersistAcrossScenes => true;

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
        // 이미 살아 있는 인스턴스가 있으면 새로 생긴 쪽(예: 다음 씬에 배치된 사본)을 지운다
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this as T;

        if (PersistAcrossScenes)
        {
            transform.SetParent(null);      // DontDestroyOnLoad는 루트 오브젝트에만 동작한다
            DontDestroyOnLoad(gameObject);
        }

        OnAwake();
    }

    protected virtual void OnDestroy()
    {
        // Instance(getter)가 아니라 필드를 쓴다. getter는 null일 때 새 객체를 만들어 버린다
        if (_instance == this) _instance = null;
    }

    /// <summary>자식 클래스의 추가 초기화. 살아남은(진짜) 인스턴스에서만 호출된다.</summary>
    protected virtual void OnAwake() { }
}