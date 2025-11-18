using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// - 버튼 클릭 시 현재 패널 닫고, 다음 패널 열기
/// - StepCountdownUI가 찍어둔 8장의 사진을
///   새로운 패널에 있는 8개 버튼의 Image에 1:1 매핑
/// </summary>
public class CapturedPhotoPanelCtrl : MonoBehaviour
{
    [Header("Panels")]
    [Tooltip("현재 열려있는 패널(닫힐 패널)")]
    [SerializeField] private GameObject _currentPanel;

    [Tooltip("새로 열릴 패널(8개 버튼이 있는 패널)")]
    [SerializeField] private GameObject _nextPanel;

    [Header("Reference")]
    [Tooltip("촬영 & 캡처를 담당하는 StepCountdownUI")]
    [SerializeField] private StepCountdownUI _stepCountdownUI;

    [Header("Target Buttons")]
    [Tooltip("새 패널에 있는 사진 선택 버튼들 (버튼의 Image에 사진 매핑)")]
    [SerializeField] private Button[] _photoButtons; // 8개로 맞춰두면 좋음

    [Tooltip("캡처가 안 된 슬롯은 버튼을 비활성화할지 여부")]
    [SerializeField] private bool _disableButtonIfNoSprite = true;

    [Header("Camera Window")]
    [SerializeField] private Button _cameraWindowNextButton;    // 카메라윈도우 패널의 "다음" 버튼

    void Awake()
    {
        _cameraWindowNextButton.onClick.AddListener(OpenNextPanelAndApplyPhotos);
    }

    /// <summary>
    /// UI 버튼 OnClick에 연결할 함수
    /// </summary>
    public void OpenNextPanelAndApplyPhotos()
    {
        if (_stepCountdownUI == null)
        {
            Debug.LogWarning("[CapturedPhotoPanelCtrl] StepCountdownUI reference is missing");
            return;
        }

        if (_photoButtons == null || _photoButtons.Length == 0)
        {
            Debug.LogWarning("[CapturedPhotoPanelCtrl] _photoButtons is empty");
            return;
        }

        // StepCountdownUI에서 캡처된 스프라이트를 버튼들에 매핑
        for (int i = 0; i < _photoButtons.Length; i++)
        {
            Button btn = _photoButtons[i];
            if (btn == null) continue;

            Image targetImg = btn.image;
            if (targetImg == null) continue;

            Sprite captured = _stepCountdownUI.GetCapturedSprite(i);

            if (captured == null)
            {
                // 아직 해당 인덱스 사진이 없으면 처리
                targetImg.sprite = null;

                var c = targetImg.color;
                c.a = 0.3f; // 흐리게 보이게 (혹은 0으로 완전 숨겨도 됨)
                targetImg.color = c;

                if (_disableButtonIfNoSprite)
                    btn.interactable = false;
            }
            else
            {
                targetImg.sprite = captured;
                targetImg.preserveAspect = true;

                var c = targetImg.color;
                c.a = 1f;
                targetImg.color = c;

                btn.interactable = true;
            }
        }

        // 패널 전환
        if (_currentPanel != null)
            _currentPanel.SetActive(false);

        if (_nextPanel != null)
            _nextPanel.SetActive(true);
    }
}
