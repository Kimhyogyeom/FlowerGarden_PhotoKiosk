using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ButtonSpriteChangeCtrl : MonoBehaviour
{
    [Header("Setting Value")]
    [SerializeField] private float _timer = 1f;

    // ─────────────────────────────────────────────────────────────────────
    [Header("Panel Select")]
    [SerializeField] private Button _panelModeBack;
    [SerializeField] private Image _panelModeBackImage;
    [SerializeField] private Sprite _panelModeBackBasic;
    [SerializeField] private Sprite _panelModeBackClick;

    [SerializeField] private Button _panelModeNext;
    [SerializeField] private Image _panelModeNextImage;
    [SerializeField] private Sprite _panelModeNextBasic;
    [SerializeField] private Sprite _panelModeNextClick;

    [SerializeField] private Button _panelModeHome;
    [SerializeField] private Image _panelModeHomeImage;
    [SerializeField] private Sprite _panelModeHomeBasic;
    [SerializeField] private Sprite _panelModeHomeClick;
    // ─────────────────────────────────────────────────────────────────────

    // ─────────────────────────────────────────────────────────────────────
    [Header("Panel Select")]
    [SerializeField] private Button _panelSelectBack;
    [SerializeField] private Image _panelSelectBackImage;
    [SerializeField] private Sprite _panelSelectBackBasic;
    [SerializeField] private Sprite _panelSelectBackClick;

    [SerializeField] private Button _panelSelectNext;
    [SerializeField] private Image _panelSelectNextImage;
    [SerializeField] private Sprite _panelSelectNextBasic;
    [SerializeField] private Sprite _panelSelectNextClick;

    [SerializeField] private Button _panelSelectHome;
    [SerializeField] private Image _panelSelectHomeImage;
    [SerializeField] private Sprite _panelSelectHomeBasic;
    [SerializeField] private Sprite _panelSelectHomeClick;
    // ─────────────────────────────────────────────────────────────────────

    [Header("Panel Chroma Key")]
    [SerializeField] private Button _panelChromakeyBack;
    [SerializeField] private Image _panelChromakeyBackImage;
    [SerializeField] private Sprite _panelChromakeyBackBasic;
    [SerializeField] private Sprite _panelChromakeyBackClick;

    [SerializeField] private Button _panelChromakeyNext;
    [SerializeField] private Image _panelChromakeyNextImage;
    [SerializeField] private Sprite _panelChromakeyNextBasic;
    [SerializeField] private Sprite _panelChromakeyNextClick;

    [SerializeField] private Button _panelChromakeyHome;
    [SerializeField] private Image _panelChromakeyHomeImage;
    [SerializeField] private Sprite _panelChromakeyHomeBasic;
    [SerializeField] private Sprite _panelChromakeyHomeClick;
    // ─────────────────────────────────────────────────────────────────────

    [Header("Panel Quantity")]
    [SerializeField] private Button _panelQuantityBack;
    [SerializeField] private Image _panelQuantityBackImage;
    [SerializeField] private Sprite _panelQuantityBackBasic;
    [SerializeField] private Sprite _panelQuantityBackClick;

    [SerializeField] private Button _panelQuantityNext;
    [SerializeField] private Image _panelQuantityNextImage;
    [SerializeField] private Sprite _panelQuantityNextBasic;
    [SerializeField] private Sprite _panelQuantityNextClick;

    [SerializeField] private Button _panelQuantityHome;
    [SerializeField] private Image _panelQuantityHomeImage;
    [SerializeField] private Sprite _panelQuantityHomeBasic;
    [SerializeField] private Sprite _panelQuantityHomeClick;
    // ─────────────────────────────────────────────────────────────────────

    [Header("Panel Payment")]
    [SerializeField] private Button _panelPaymentBack;
    [SerializeField] private Image _panelPaymentBackImage;
    [SerializeField] private Sprite _panelPaymentBackBasic;
    [SerializeField] private Sprite _panelPaymentBackClick;

    [SerializeField] private Button _panelPaymentNext;
    [SerializeField] private Image _panelPaymentNextImage;
    [SerializeField] private Sprite _panelPaymentNextBasic;
    [SerializeField] private Sprite _panelPaymentNextClick;

    [SerializeField] private Button _panelPaymentHome;
    [SerializeField] private Image _panelPaymentHomeImage;
    [SerializeField] private Sprite _panelPaymentHomeBasic;
    [SerializeField] private Sprite _panelPaymentHomeClick;
    // ─────────────────────────────────────────────────────────────────────

    [Header("Panel Camera Start")]
    [SerializeField] private Button _panelCameraStart;
    [SerializeField] private Image _panelCameraStartImage;
    [SerializeField] private Sprite _panelCameraStartBasic;
    [SerializeField] private Sprite _panelCameraStartClick;

    [SerializeField] private Button _panelCameraNext;
    [SerializeField] private Image _panelCameraNextImage;
    [SerializeField] private Sprite _panelCameraNextBasic;
    [SerializeField] private Sprite _panelCameraNextClick;
    // ─────────────────────────────────────────────────────────────────────

    [Header("Panel Photo Select")]
    [SerializeField] private Button _panelPhotoSelectStart;
    [SerializeField] private Image _panelPhotoSelectStartImage;
    [SerializeField] private Sprite _panelPhotoSelectStartBasic;
    [SerializeField] private Sprite _panelPhotoSelectStartClick;
    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Mode
        SafeAddListener(_panelModeBack, OnClickPanelModeBack);
        SafeAddListener(_panelModeNext, OnClickPanelModeNext);
        SafeAddListener(_panelModeHome, OnClickPanelModeHome);

        // Select
        SafeAddListener(_panelSelectBack, OnClickPanelSelectBack);
        SafeAddListener(_panelSelectNext, OnClickPanelSelectNext);
        SafeAddListener(_panelSelectHome, OnClickPanelSelectHome);

        // Chroma Key
        SafeAddListener(_panelChromakeyBack, OnClickPanelChromakeyBack);
        SafeAddListener(_panelChromakeyNext, OnClickPanelChromakeyNext);
        SafeAddListener(_panelChromakeyHome, OnClickPanelChromakeyHome);

        // Quantity
        SafeAddListener(_panelQuantityBack, OnClickPanelQuantityBack);
        SafeAddListener(_panelQuantityNext, OnClickPanelQuantityNext);
        SafeAddListener(_panelQuantityHome, OnClickPanelQuantityHome);

        // Payment
        SafeAddListener(_panelPaymentBack, OnClickPanelPaymentBack);
        SafeAddListener(_panelPaymentNext, OnClickPanelPaymentNext);
        SafeAddListener(_panelPaymentHome, OnClickPanelPaymentHome);

        // Camera Start
        SafeAddListener(_panelCameraStart, OnClickPanelCameraStart);

        // Camera 2
        SafeAddListener(_panelCameraNext, OnClickPanelCameraNext);

        // PhotoSelect Start
        SafeAddListener(_panelPhotoSelectStart, OnClickPanelPhotoSelect);
    }

    private void SafeAddListener(Button button, Action action)
    {
        if (button == null)
        {
            Debug.LogWarning($"[ButtonSpriteChangeCtrl] Button is null on {name}");
            return;
        }

        button.onClick.AddListener(() => action());
    }

    // ───────────────── Mode ─────────────────
    private void OnClickPanelModeBack()
    {
        SetClickAndReset(_panelModeBackImage, _panelModeBackClick, _panelModeBackBasic);
    }

    private void OnClickPanelModeNext()
    {
        SetClickAndReset(_panelModeNextImage, _panelModeNextClick, _panelModeNextBasic);
    }

    private void OnClickPanelModeHome()
    {
        SetClickAndReset(_panelModeHomeImage, _panelModeHomeClick, _panelModeHomeBasic);
    }

    // ───────────────── Select ─────────────────
    private void OnClickPanelSelectBack()
    {
        SetClickAndReset(_panelSelectBackImage, _panelSelectBackClick, _panelSelectBackBasic);
    }

    private void OnClickPanelSelectNext()
    {
        SetClickAndReset(_panelSelectNextImage, _panelSelectNextClick, _panelSelectNextBasic);
    }

    private void OnClickPanelSelectHome()
    {
        SetClickAndReset(_panelSelectHomeImage, _panelSelectHomeClick, _panelSelectHomeBasic);
    }

    // ───────────────── Chromakey ─────────────────
    private void OnClickPanelChromakeyBack()
    {
        SetClickAndReset(_panelChromakeyBackImage, _panelChromakeyBackClick, _panelChromakeyBackBasic);
    }

    private void OnClickPanelChromakeyNext()
    {
        SetClickAndReset(_panelChromakeyNextImage, _panelChromakeyNextClick, _panelChromakeyNextBasic);
    }

    private void OnClickPanelChromakeyHome()
    {
        SetClickAndReset(_panelChromakeyHomeImage, _panelChromakeyHomeClick, _panelChromakeyHomeBasic);
    }

    // ───────────────── Quantity ─────────────────
    private void OnClickPanelQuantityBack()
    {
        SetClickAndReset(_panelQuantityBackImage, _panelQuantityBackClick, _panelQuantityBackBasic);
    }

    private void OnClickPanelQuantityNext()
    {
        SetClickAndReset(_panelQuantityNextImage, _panelQuantityNextClick, _panelQuantityNextBasic);
    }

    private void OnClickPanelQuantityHome()
    {
        SetClickAndReset(_panelQuantityHomeImage, _panelQuantityHomeClick, _panelQuantityHomeBasic);
    }

    // ───────────────── Payment ─────────────────
    private void OnClickPanelPaymentBack()
    {
        SetClickAndReset(_panelPaymentBackImage, _panelPaymentBackClick, _panelPaymentBackBasic);
    }

    private void OnClickPanelPaymentNext()
    {
        SetClickAndReset(_panelPaymentNextImage, _panelPaymentNextClick, _panelPaymentNextBasic);
    }

    private void OnClickPanelPaymentHome()
    {
        SetClickAndReset(_panelPaymentHomeImage, _panelPaymentHomeClick, _panelPaymentHomeBasic);
    }

    // ───────────────── Camera Start ─────────────────
    private void OnClickPanelCameraStart()
    {
        SetClickAndReset(_panelCameraStartImage, _panelCameraStartClick, _panelCameraStartBasic);
    }
    private void OnClickPanelCameraNext()
    {
        SetClickAndReset(_panelCameraNextImage, _panelCameraNextClick, _panelCameraNextBasic);
    }

    // ───────────────── Photo Select ─────────────────
    private void OnClickPanelPhotoSelect()
    {
        SetClickAndReset(_panelPhotoSelectStartImage, _panelPhotoSelectStartClick, _panelPhotoSelectStartBasic);
    }

    // 공통 처리 함수
    private void SetClickAndReset(Image img, Sprite click, Sprite basic)
    {
        if (img == null)
        {
            Debug.LogWarning("[ButtonSpriteChangeCtrl] Image is null");
            return;
        }

        if (click != null)
            img.sprite = click;

        // 코루틴 실행
        StartCoroutine(AllResetButton(img, basic));
    }

    private IEnumerator AllResetButton(Image image, Sprite sprite)
    {
        if (image == null || sprite == null)
            yield break;

        yield return new WaitForSeconds(_timer);
        image.sprite = sprite;
    }
}
