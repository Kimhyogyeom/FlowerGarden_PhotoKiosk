using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "수량을 선택해 주세요" 패널 컨트롤러
/// - 버튼 5개 중 하나를 선택하면 선택된 수량(2,4,6,8,10)을 저장
/// - 선택된 버튼은 강조 색(#FA4B86), 나머지는 기본 색(#BDBEBF)으로 표시
/// </summary>
public class QuantitySelectCtrl : MonoBehaviour
{
    [Header("Buttons")]
    [Tooltip("수량 선택 버튼들 (왼쪽부터 순서대로 5개)")]
    [SerializeField] private Button[] _quantityButtons = new Button[5];

    [Header("Colors")]
    [Tooltip("기본 버튼 색상 (BDBEBF)")]
    [SerializeField] private Color _normalColor = new Color32(0xBD, 0xBE, 0xBF, 0xFF);

    [Tooltip("선택된 버튼 색상 (FA4B86)")]
    [SerializeField] private Color _selectedColor = new Color32(0xFA, 0x4B, 0x86, 0xFF);

    [Header("Quantity Values")]
    [Tooltip("각 버튼이 가질 수량 값 (기본: 2,4,6,8,10)")]
    [SerializeField] private int[] _quantities = new int[5] { 2, 4, 6, 8, 10 };

    [Header("Runtime")]
    [SerializeField] private int _selectedQuantity = 2; // 현재 선택된 수량(기본 2)
    [SerializeField] private int _selectedIndex = 0;    // 현재 선택된 인덱스(0~4)

    /// <summary>
    /// 외부에서 읽기용 프로퍼티
    /// 예) 다른 스크립트에서 현재 선택 수량이 필요할 때 사용
    /// </summary>
    public int SelectedQuantity => _selectedQuantity;

    private void Awake()
    {
        // 버튼 리스너 등록
        if (_quantityButtons != null && _quantityButtons.Length > 0)
        {
            for (int i = 0; i < _quantityButtons.Length; i++)
            {
                int index = i; // 람다 캡처용
                if (_quantityButtons[i] != null)
                {
                    _quantityButtons[i].onClick.AddListener(() => OnClickQuantity(index));
                }
                else
                {
                    Debug.LogWarning($"_quantityButtons[{i}] reference is missing");
                }
            }
        }
        else
        {
            Debug.LogWarning("_quantityButtons is empty or missing");
        }

        // 시작 시 기본 선택 상태 적용 (예: 0번 버튼 → 2)
        ApplySelection(_selectedIndex);
    }

    private void OnDestroy()
    {
        // 리스너 해제 (방어용)
        if (_quantityButtons != null && _quantityButtons.Length > 0)
        {
            for (int i = 0; i < _quantityButtons.Length; i++)
            {
                int index = i;
                if (_quantityButtons[i] != null)
                {
                    _quantityButtons[i].onClick.RemoveListener(() => OnClickQuantity(index));
                }
            }
        }
    }

    /// <summary>
    /// 버튼 클릭 시 호출되는 콜백
    /// </summary>
    private void OnClickQuantity(int index)
    {
        if (_quantities == null || _quantities.Length <= index)
        {
            Debug.LogWarning("QuantitySelectCtrl: _quantities 설정이 부족합니다.");
            return;
        }

        ApplySelection(index);
    }

    /// <summary>
    /// 실제 선택 상태 적용 로직
    /// - 선택 인덱스 저장
    /// - 수량 값 갱신
    /// - 버튼 색상 갱신
    /// </summary>
    private void ApplySelection(int index)
    {
        _selectedIndex = index;
        _selectedQuantity = _quantities[index];

        for (int i = 0; i < _quantityButtons.Length; i++)
        {
            var btn = _quantityButtons[i];
            if (btn == null) continue;

            // 버튼의 배경 Image (Button의 targetGraphic 사용)
            var img = btn.targetGraphic as Image;
            if (img == null) continue;

            img.color = (i == _selectedIndex) ? _selectedColor : _normalColor;
        }

        Debug.Log($"[QuantitySelectCtrl] 선택된 수량: {_selectedQuantity}");
    }

    // ─────────────────────────────────────────────────────────────────────
    // 초기화 함수 (맨 아래 추가)
    // 패널이 다시 열릴 때 기본 상태(첫번째 버튼 / 2장)로 되돌리고 싶을 때 사용
    // 예) _quantitySelectCtrl.ResetQuantity();
    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// 수량 선택을 기본 상태(0번 버튼, 기본 수량)로 초기화
    /// </summary>
    public void ResetQuantity()
    {
        if (_quantityButtons == null || _quantityButtons.Length == 0)
        {
            Debug.LogWarning("QuantitySelectCtrl.ResetQuantity: _quantityButtons가 비어 있습니다.");
            return;
        }

        // 기본: 0번 버튼(2장)으로 초기화
        ApplySelection(0);
    }
}
