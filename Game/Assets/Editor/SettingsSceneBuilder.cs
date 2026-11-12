// 역할: Editor에서 환경 설정 UI와 공통 프리팹을 구성한다.
// 구성: 전용 폰트 준비, 슬라이더·토글·버튼 생성, 참조 연결과 프리팹 저장.

using System;
using System.IO;
using System.Linq;
using DdukDduk.MainMenu;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;

public static class SettingsSceneBuilder
{
    private static TMP_FontAsset font;

    private static readonly Color Ink = new Color32(67, 56, 46, 255);
    private static readonly Color Green = new Color32(63, 98, 78, 255);
    private static readonly Color Paper = new Color32(246, 238, 220, 255);

    public static void Populate(GameObject panel)
    {
        var card = panel.transform.Find("Card");
        if (card == null)
            throw new InvalidOperationException("설정 패널 Card가 없습니다.");

        panel.SetActive(false);

        // 메인 메뉴 글꼴을 변경하지 않고 설정에 필요한 글자를 별도 에셋에 준비한다.
        const string fontPath = "Assets/UI/MainMenu/Fonts/SettingsHandwriting.asset";
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);

        if (font == null)
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Shop/Resources/Shop/Fonts/RecipeHandwriting.ttf");
            font = TMP_FontAsset.CreateFontAsset(
                source,
                80,
                9,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                2048,
                2048);
            font.name = "SettingsHandwriting";

            AssetDatabase.CreateAsset(font, fontPath);
            AssetDatabase.AddObjectToAsset(font.material, font);

            foreach (var atlas in font.atlasTextures)
                AssetDatabase.AddObjectToAsset(atlas, font);
        }

        font.isMultiAtlasTexturesEnabled = true;
        if (!font.TryAddCharacters("Master BGM SFX 0123456789%환경설정마스터배경음효과음전체화면창모드기본값복원완료변경즉시적용닫으면저장", out string missing))
            throw new InvalidOperationException("설정 문구 폰트 생성 실패: " + missing);

        EditorUtility.SetDirty(font);

        var controller = panel.GetComponent<SettingsPanelController>();

        if (controller == null)
            controller = panel.AddComponent<SettingsPanelController>();
        var serialized = new SerializedObject(controller);

        string[] names =
        {
            "Master",
            "Bgm",
            "Sfx"
        };

        string[] labels =
        {
            "마스터",
            "BGM",
            "SFX"
        };

        for (int i = 0; i < 3; i++)
        {
            float y = 0.72f - i * 0.13f;
            Label(names[i] + "Label", card, labels[i], new Vector2(.10f, y), new Vector2(180, 54));

            var sliderRect = Rect(names[i] + "Slider", card, new Vector2(.36f, y), new Vector2(330, 54));
            var hitArea = Paint(sliderRect, Color.clear);
            hitArea.raycastTarget = true;

            var slider = sliderRect.gameObject.AddComponent<UnityEngine.UI.Slider>();
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 1;

            // 배경과 채움 영역의 너비를 맞춰 100%에서 빈 구간이 남지 않게 한다.
            var track = Rect("Track", sliderRect, new Vector2(.5f, .5f), new Vector2(306, 8));
            track.pivot = new Vector2(.5f, .5f);
            Paint(track, new Color32(211, 196, 169, 255));

            var fillArea = Rect("FillArea", sliderRect, new Vector2(.5f, .5f), new Vector2(306, 8));
            fillArea.pivot = new Vector2(.5f, .5f);
            var fill = Stretch("Fill", fillArea);
            Paint(fill, Green);

            var handleArea = Rect("HandleArea", sliderRect, new Vector2(.5f, .5f), new Vector2(306, 54));
            handleArea.pivot = new Vector2(.5f, .5f);

            // Slider가 세로 앵커를 늘리므로 음수 여백으로 손잡이 높이를 제한한다.
            var handle = Rect("Handle", handleArea, new Vector2(1, .5f), new Vector2(28, -16));
            handle.pivot = new Vector2(.5f, .5f);
            var handleImage = Paint(handle, Green);
            handleImage.raycastTarget = true;

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;

            var value = Label(names[i] + "Value", card, "100%", new Vector2(.83f, y), new Vector2(100, 54));
            serialized.FindProperty(char.ToLowerInvariant(names[i][0]) + names[i].Substring(1) + "Slider").objectReferenceValue = slider;
            serialized.FindProperty(char.ToLowerInvariant(names[i][0]) + names[i].Substring(1) + "Value").objectReferenceValue = value;
        }

        Label("DisplayLabel", card, "화면 모드", new Vector2(.1f, .31f), new Vector2(220, 54));
        var toggleRect = Rect("FullscreenToggle", card, new Vector2(.46f, .31f), new Vector2(330, 60));
        var toggle = toggleRect.gameObject.AddComponent<UnityEngine.UI.Toggle>();
        var background = Rect("Box", toggleRect, new Vector2(0, .5f), new Vector2(48, 48));
        var boxImage = Paint(background, new Color32(211, 196, 169, 255));
        boxImage.raycastTarget = true;

        var check = Stretch("Checkmark", background);
        check.offsetMin = Vector2.one * 9;
        check.offsetMax = Vector2.one * -9;
        toggle.graphic = Paint(check, Green);
        toggle.targetGraphic = boxImage;

        var mode = Label("DisplayValue", toggleRect, "창 모드", new Vector2(0, .5f), new Vector2(230, 54));
        mode.rectTransform.anchoredPosition = new Vector2(66, 0);
        mode.raycastTarget = true;

        serialized.FindProperty("fullscreenToggle").objectReferenceValue = toggle;
        serialized.FindProperty("modeValue").objectReferenceValue = mode;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Label("SaveHint", card, "변경 즉시 적용  닫으면 저장", new Vector2(.1f, .20f), new Vector2(650, 45), 26);
        var restore = Button("RestoreDefaultsButton", card, "기본값 복원", new Vector2(.1f, .10f));
        UnityEventTools.AddPersistentListener(restore.onClick, controller.RestoreDefaults);

        var close = card.Find("CloseSettingsButton").GetComponent<UnityEngine.UI.Button>();
        SetRect(close.GetComponent<RectTransform>(), new Vector2(.55f, .10f), new Vector2(280, 72));
        var caption = close.GetComponentInChildren<TMP_Text>();
        caption.text = "설정 완료";
        caption.font = font;
        caption.fontSize = 34;

        // 프리팹에 메인 메뉴 씬 객체의 이벤트 참조가 남지 않게 닫기 대상을 교체한다.
        while (close.onClick.GetPersistentEventCount() > 0)
            UnityEventTools.RemovePersistentListener(close.onClick, 0);
        UnityEventTools.AddPersistentListener(close.onClick, controller.Close);

        Directory.CreateDirectory("Assets/Resources/UI");
        AssetDatabase.Refresh();

        var clone = UnityEngine.Object.Instantiate(panel);
        clone.name = "SettingsPanel";

        var cloneRect = clone.GetComponent<RectTransform>();
        cloneRect.anchorMin = Vector2.zero;
        cloneRect.anchorMax = Vector2.one;
        cloneRect.offsetMin = cloneRect.offsetMax = Vector2.zero;

        // Resources로 연 팝업도 최초 OnEnable에서 설정을 읽도록 활성 상태로 저장한다.
        clone.SetActive(true);

        PrefabUtility.SaveAsPrefabAsset(clone, "Assets/Resources/UI/SettingsPanel.prefab");
        UnityEngine.Object.DestroyImmediate(clone);
        AssetDatabase.SaveAssets();
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetRect(rect, anchor, size);
        return rect;
    }

    private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0, .5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
    }

    private static RectTransform Stretch(string name, Transform parent)
    {
        var rect = Rect(name, parent, Vector2.zero, Vector2.zero);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static UnityEngine.UI.Image Paint(RectTransform rect, Color color)
    {
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text Label(
        string name,
        Transform parent,
        string value,
        Vector2 anchor,
        Vector2 size,
        float fontSize = 34)
    {
        var label = Rect(name, parent, anchor, size).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = fontSize;
        label.text = value;
        label.color = Ink;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        return label;
    }

    private static UnityEngine.UI.Button Button(string name, Transform parent, string text, Vector2 anchor)
    {
        var rect = Rect(name, parent, anchor, new Vector2(280, 72));
        var image = Paint(rect, Paper);
        image.raycastTarget = true;
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        var label = Label("Label", rect, text, new Vector2(0, .5f), new Vector2(280, 72));
        label.alignment = TextAlignmentOptions.Center;
        return button;
    }
}
