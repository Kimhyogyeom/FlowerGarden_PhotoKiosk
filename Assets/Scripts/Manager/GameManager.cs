using UnityEngine;

public enum KioskState
{
    WaitingForPayment,  // ���� �� (���� �ȳ� ȭ��)
    Ready,              // Ready �г� (���� ��ư ���)
    Select,
    Frame,              // ������ ���� ȭ��
    Filming,            // ���� �Կ� ����/��� ����
    Printing            // �μ� ��
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField]
    private KioskState _currentState = KioskState.WaitingForPayment;

    public KioskState CurrentState => _currentState;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // DontDestroyOnLoad(gameObject); 
    }

    public void SetState(KioskState newState)
    {
        _currentState = newState;
        Debug.Log($"[KIOSK] State -> {newState}");
    }

    public bool Is(KioskState state) => _currentState == state;
}
