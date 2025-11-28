using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 크로마키 배경 선택 버튼 컨트롤러
/// - A/B/C 세 가지 버튼 중 하나를 선택
/// - 선택된 버튼에 대응하는:
///   1) 선택/비선택 오브젝트 토글
///   2) 메인 프리뷰 이미지(Sprite) 교체
///   3) 크로마키 배경 모드(ChromaKeyBackgroundSelector) 변경
/// </summary>
public class ChromakeySelectButton : MonoBehaviour
{
    [SerializeField] private VirtualBackgroundController _virtualBg;
    [Header("크로마키 배경 컨트롤러")]
    [SerializeField] private ChromaKeyBackgroundSelector _chromaKeyBackgroundSelector;
    // 실제 크로마키 배경을 변경하는 컨트롤러 (SetMode(int)로 모드 변경)

    [Header("선택 버튼들 (A/B/C)")]
    [SerializeField] private Button _selectButtonA;
    [SerializeField] private Button _selectButtonB;
    [SerializeField] private Button _selectButtonC;
    // UI 상에서 A/B/C 선택을 담당하는 버튼들

    [Header("선택/비선택 표시용 오브젝트들")]
    [SerializeField] private GameObject[] _selectObjects;
    // 각 인덱스에 해당하는 옵션이 "선택됨" 상태일 때 ON 되는 오브젝트 (예: 강조 테두리, 체크마크 등)

    [SerializeField] private GameObject[] _noSelectObjects;
    // 각 인덱스에 해당하는 옵션이 "선택 안됨" 상태일 때 ON 되는 오브젝트

    [Header("메인 프리뷰 이미지")]
    [SerializeField] private Image _mainImage;
    // 현재 선택된 크로마키 배경을 미리 보여주는 Image

    [SerializeField] private Sprite[] _ImageToSprites;
    // 각 모드(0,1,2)에 대응하는 프리뷰용 Sprite 배열
    // index: 0 → A, 1 → B, 2 → C

    [Header("현재 선택 인덱스 (0:A / 1:B / 2:C)")]
    public int _selectNumber = 0;
    // 현재 선택된 배경 번호 (외부에서 읽어 쓸 수 있게 public)

    private void Awake()
    {
        // 버튼 A, B, C에 클릭 이벤트 등록
        _selectButtonA.onClick.AddListener(OnSelectA);
        _selectButtonB.onClick.AddListener(OnSelectB);
        _selectButtonC.onClick.AddListener(OnSelectC);
    }

    /// <summary>
    /// 버튼 A 클릭 시 콜백
    /// </summary>
    private void OnSelectA()
    {
        ForUseCtrl(0); // 0번 옵션 선택
    }

    /// <summary>
    /// 버튼 B 클릭 시 콜백
    /// </summary>
    private void OnSelectB()
    {
        ForUseCtrl(1); // 1번 옵션 선택
    }

    /// <summary>
    /// 버튼 C 클릭 시 콜백
    /// </summary>
    private void OnSelectC()
    {
        ForUseCtrl(2); // 2번 옵션 선택
    }

    /// <summary>
    /// 공통 처리용 함수
    /// - 선택 인덱스를 받아서:
    ///   1) 선택/비선택 오브젝트 토글
    ///   2) 메인 프리뷰 이미지 Sprite 교체
    ///   3) 크로마키 배경 모드 변경
    /// </summary>
    /// <param name="index">선택 Number (0:A / 1:B / 2:C)</param>
    private void ForUseCtrl(int index)
    {
        // 현재 선택 인덱스 업데이트
        _selectNumber = index;
        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._buttonClickSound);
        // 선택/비선택 오브젝트 토글
        // _selectObjects / _noSelectObjects 배열의 길이는 동일하다고 가정
        for (int i = 0; i < _selectObjects.Length; i++)
        {
            if (i == index)
            {
                // 현재 선택된 인덱스
                _selectObjects[i].SetActive(true);   // 선택 표시 ON
                _noSelectObjects[i].SetActive(false); // 비선택 표시 OFF
            }
            else
            {
                // 선택되지 않은 인덱스들
                _selectObjects[i].SetActive(false);  // 선택 표시 OFF
                _noSelectObjects[i].SetActive(true); // 비선택 표시 ON
            }
        }

        // 선택된 인덱스에 따라:
        // - 메인 프리뷰 이미지 Sprite 교체
        // - 크로마키 배경 모드 변경
        if (_selectNumber == 0)
        {
            _mainImage.sprite = _ImageToSprites[0];     // A에 해당하는 이미지
            _chromaKeyBackgroundSelector.SetMode(0);    // 크로마키 모드 0
            _virtualBg.SetBackground(0);
        }
        else if (_selectNumber == 1)
        {
            _mainImage.sprite = _ImageToSprites[1];     // B에 해당하는 이미지
            _chromaKeyBackgroundSelector.SetMode(1);    // 크로마키 모드 1
            _virtualBg.SetBackground(1);
        }
        else if (_selectNumber == 2)
        {
            _mainImage.sprite = _ImageToSprites[2];     // C에 해당하는 이미지
            _chromaKeyBackgroundSelector.SetMode(2);    // 크로마키 모드 2
            _virtualBg.SetBackground(2);
        }
    }

    /// <summary>
    /// 외부 호출용 리셋 함수
    /// - 항상 0번(A)으로 초기화
    ///   (Ready 화면 복귀할 때 기본값으로 돌릴 때 사용)
    /// </summary>
    public void ResetCtrl()
    {
        ForUseCtrl(0);
    }
}
