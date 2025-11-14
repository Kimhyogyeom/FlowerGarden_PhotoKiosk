using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 프레임 변경 버튼 클릭시 실행되는 스크립트
/// 스케일 0 <-> 1 인/아웃 시각화
/// </summary>
public class PhotoFrameSelectCtrl : MonoBehaviour
{
    [Header("Photo Frame Select Images")]
    [SerializeField] private GameObject[] _photoFrameSelectImages;
    // 포토 프레임 선택 버튼(또는 하이라이트용) 오브젝트들
    // 0: 첫 번째 프레임, 1: 두 번째 프레임, 2: 세 번째 프레임

    [SerializeField] private Texture[] _photoFrameTexture;
    // 각 포토 프레임에 대응되는 텍스처 배열
    // 인덱스 0,1,2 순서대로 사용

    [SerializeField] private RawImage _mainRawImage;
    // 선택된 프레임을 실제로 보여주는 메인 RawImage

    [Header("Add Frame RawImage")]
    [SerializeField] private RawImage _addFrameRawImage;
    // 인쇄용 등, 최종 합성에 사용될 추가 프레임 RawImage

    // 현재 어떤 프레임이 선택되어 있는지 표시하는 플래그 (필요 시 외부에서 참조)
    public bool _selectFlag0 = true;
    public bool _selectFlag1 = false;
    public bool _selectFlag2 = false;

    /// <summary>
    /// 첫 번째 사진(프레임) 선택
    /// - 사운드 재생
    /// - 첫 번째 프레임 하이라이트
    /// - 메인/추가 RawImage 텍스처를 첫 번째 프레임으로 설정
    /// </summary>
    public void OnPhotoFrameSelect0()
    {
        // 프레임 선택 버튼 사운드
        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._frameSelectButton);

        // 선택 표시 갱신 (첫 번째만 활성화)
        _photoFrameSelectImages[0].SetActive(true);
        _photoFrameSelectImages[1].SetActive(false);
        _photoFrameSelectImages[2].SetActive(false);

        // 메인 RawImage에 첫 번째 프레임 적용
        _mainRawImage.texture = _photoFrameTexture[0];

        // 선택 상태 플래그 갱신
        _selectFlag0 = true;
        _selectFlag1 = false;
        _selectFlag2 = false;

        // 최종 출력용 추가 프레임에도 동일한 텍스처 적용
        _addFrameRawImage.texture = _photoFrameTexture[0];
    }

    /// <summary>
    /// 두 번째 사진(프레임) 선택
    /// - 사운드 재생
    /// - 두 번째 프레임 하이라이트
    /// - 메인/추가 RawImage 텍스처를 두 번째 프레임으로 설정
    /// </summary>
    public void OnPhotoFrameSelect1()
    {
        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._frameSelectButton);

        _photoFrameSelectImages[0].SetActive(false);
        _photoFrameSelectImages[1].SetActive(true);
        _photoFrameSelectImages[2].SetActive(false);

        _mainRawImage.texture = _photoFrameTexture[1];

        _selectFlag0 = false;
        _selectFlag1 = true;
        _selectFlag2 = false;

        _addFrameRawImage.texture = _photoFrameTexture[1];
    }

    /// <summary>
    /// 세 번째 사진(프레임) 선택
    /// - 사운드 재생
    /// - 세 번째 프레임 하이라이트
    /// - 메인/추가 RawImage 텍스처를 세 번째 프레임으로 설정
    /// </summary>
    public void OnPhotoFrameSelect2()
    {
        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._frameSelectButton);

        _photoFrameSelectImages[0].SetActive(false);
        _photoFrameSelectImages[1].SetActive(false);
        _photoFrameSelectImages[2].SetActive(true);

        _mainRawImage.texture = _photoFrameTexture[2];

        _selectFlag0 = false;
        _selectFlag1 = false;
        _selectFlag2 = true;

        _addFrameRawImage.texture = _photoFrameTexture[2];
    }

    /// <summary>
    /// 리셋 로직
    /// - 항상 첫 번째 프레임이 선택된 상태로 초기화
    /// </summary>
    public void AllReset()
    {
        // 첫 번째 프레임 선택 로직 재사용
        OnPhotoFrameSelect0();

        // 아래 코드는 OnPhotoFrameSelect0()과 동일한 동작이라 주석 처리해둔 상태
        //_photoFrameSelectImages[0].SetActive(true);
        //_photoFrameSelectImages[1].SetActive(false);
        //_photoFrameSelectImages[2].SetActive(false);

        //_mainRawImage.texture = _photoFrameTexture[0];

        //_selectFlag0 = true;
        //_selectFlag1 = false;
        //_selectFlag2 = false;

        //_addFrameRawImage.texture = _photoFrameTexture[0];
    }
}
