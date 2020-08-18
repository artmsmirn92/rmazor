﻿using UnityEngine;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    #region attributes

    private Canvas m_Canvas;

    #endregion

    #region engine methods

    private void Start()
    {
        CreateCanvas();

        UIFactory.UIImage(
            UIFactory.UIRectTransform(
                m_Canvas.GetComponent<RectTransform>(),
                "background",
                UIAnchor.Create(Vector2.zero, Vector2.one),
                Vector2.zero,
                Utility.HalfOne,
                Vector2.zero
            ),
            "menu_background");

        CreatePlay();
    }

    #endregion

    public void CreateCanvas()
    {
        m_Canvas = UIFactory.UICanvas(
            "canvas",
            RenderMode.ScreenSpaceOverlay,
            false,
            3,
            AdditionalCanvasShaderChannels.Normal,
            CanvasScaler.ScaleMode.ScaleWithScreenSize,
            new Vector2Int(1280, 720),
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight,
            0f,
            100f,
            true,
            GraphicRaycaster.BlockingObjects.All);
    }

    public void CreatePlay()
    {
        RectTransform playNow = UIFactory.UIImage(
            UIFactory.UIRectTransform(
                m_Canvas.rectTransform(),
                "TESTButton",
                UIAnchor.Create(Vector2.one, Vector2.one),
                new Vector2(-180f, -383f),
                Utility.HalfOne,
                new Vector2(340f, 230f)
            ),
            "TESTButton_container").rectTransform;

        UIFactory.UIText(
            UIFactory.UIRectTransform(
                playNow,
                "text",
                UIAnchor.Create(Vector2.zero, Vector2.right),
                new Vector2(0, 26.3f),
                Utility.HalfOne,
                new Vector2(0, 52.6f)),
            "TESTButton");

        UIFactory.UIImage(
            UIFactory.UIRectTransform(
                playNow,
                "icon",
                UIAnchor.Create(Vector2.up, Vector2.one),
                new Vector2(0, -88.7f), 
                Utility.HalfOne,
                new Vector2(0, 177.4f)),
            "TESTButton");

        UIFactory.UIButton(
            UIFactory.UIRectTransform(
                playNow,
                "button",
                UIAnchor.Create(Vector2.zero, Vector2.one),
                Vector2.zero,
                Utility.HalfOne,
                Vector2.zero),
            "TESTButton",
            () =>
            {
                Debug.Log("TESTButton Pushed");
                //Button functionality
            },
            playNow.GetComponent<Image>());
    }
}