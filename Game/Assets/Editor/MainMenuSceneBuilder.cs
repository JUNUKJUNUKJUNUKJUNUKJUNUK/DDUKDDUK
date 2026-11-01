using System;
using System.IO;
using System.Linq;
using DdukDduk.MainMenu;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;

public static class MainMenuSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string FontPath = "Assets/UI/MainMenu/Fonts/MenuHandwriting.asset";
    private static readonly Color Paper = Hex("F6EEDC");
    private static readonly Color Ink = Hex("43382E");
    private static readonly Color Muted = Hex("8A7966");
    private static readonly Color Green = Hex("3F624E");
    private static readonly Color Red = Hex("8E443B");
    private static readonly Color Light = Hex("FFF9ED");
    private static TMP_FontAsset font;

    // TMP 필수 리소스의 비동기 가져오기가 끝난 뒤 씬을 생성한다.
    [MenuItem("DDUKDDUK/Create Main Menu Scene")]
    public static void BuildWhenReady()
    {
        if (Resources.Load<TMP_Settings>("TMP Settings") != null)
        {
            CompleteBuild();
            return;
        }
        AssetDatabase.importPackageCompleted += OnResourcesImported;
        AssetDatabase.importPackageFailed += OnResourcesFailed;
        TMP_PackageResourceImporter.ImportResources(true, false, false);
    }

    private static void OnResourcesImported(string packageName)
    {
        AssetDatabase.importPackageCompleted -= OnResourcesImported;
        AssetDatabase.importPackageFailed -= OnResourcesFailed;
        EditorApplication.delayCall += CompleteBuild;
    }

    private static void OnResourcesFailed(string packageName, string message)
    {
        AssetDatabase.importPackageCompleted -= OnResourcesImported;
        AssetDatabase.importPackageFailed -= OnResourcesFailed;
        Debug.LogError(message);
        if (Application.isBatchMode) EditorApplication.Exit(1);
    }

    private static void CompleteBuild()
    {
        try
        {
            Build();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
    }

    // Unity Editor API로 편집 가능한 시작 화면 씬을 생성한다.
    public static void Build()
    {
        if (File.Exists(ScenePath))
            throw new InvalidOperationException("기존 시작 화면 씬을 덮어쓰지 않습니다.");
        if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            TMP_PackageResourceImporter.ImportResources(true, false, false);

        Directory.CreateDirectory("Assets/UI/MainMenu/Fonts");
        AssetDatabase.Refresh();
        font = CreateFont();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("Main Camera", typeof(Camera));
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Paper;
        camera.orthographic = true;
        cameraObject.transform.position = new Vector3(0, 0, -10);

        var canvasObject = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var root = canvasObject.GetComponent<RectTransform>();
        Fill(Image("PaperBackground", root, Paper));

        var frame = Rect("Content", root);
        frame.anchorMin = new Vector2(0.08f, 0.13f);
        frame.anchorMax = new Vector2(0.92f, 0.87f);
        frame.offsetMin = frame.offsetMax = Vector2.zero;
        var brand = Rect("Brand", frame);
        brand.anchorMin = Vector2.zero;
        brand.anchorMax = new Vector2(0.56f, 1f);
        brand.offsetMin = brand.offsetMax = Vector2.zero;
        var seal = Image("Seal", brand, Red);
        Position(seal, new Vector2(0.08f, 0.76f), new Vector2(64, 64));
        var sealText = Text("SealText", seal, "떡", 42, Light, TextAlignmentOptions.Center);
        Fill(sealText.rectTransform);
        Label("Eyebrow", brand, "정성으로 빚는 하루", 31, Muted, 0.10f, 0.65f, 720, 70);
        Label("GameTitle", brand, "DDUKDDUK", 108, Ink, 0.10f, 0.49f, 850, 180);
        Label("Subtitle", brand, "새로운 하루를 시작해 볼까요?", 37, Ink, 0.10f, 0.29f, 750, 90);
        var rule = Image("BrandRule", brand, Green);
        Position(rule, new Vector2(0.10f, 0.17f), new Vector2(240, 3));
        var footnote = Text("Footer", root, "DDUKDDUK", 22, Muted, TextAlignmentOptions.Left);
        Position(footnote.rectTransform, new Vector2(0.08f, 0.055f), new Vector2(500, 40));

        var controller = canvasObject.AddComponent<MainMenuController>();
        var menu = Rect("Menu", frame);
        menu.anchorMin = new Vector2(0.65f, 0.1f);
        menu.anchorMax = new Vector2(0.96f, 0.9f);
        menu.offsetMin = menu.offsetMax = Vector2.zero;
        var menuLayout = menu.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        menuLayout.spacing = 18;
        menuLayout.childAlignment = TextAnchor.MiddleCenter;
        menuLayout.childControlWidth = true;
        menuLayout.childControlHeight = true;
        menuLayout.childForceExpandWidth = true;
        menuLayout.childForceExpandHeight = false;
        var newGame = Button("NewGameButton", menu, "새로 하기", Green, Light, 100);
        var resume = Button("ContinueButton", menu, "이어 하기", Light, Ink, 100);
        var settings = Button("SettingsButton", menu, "환경 설정", Light, Ink, 100);
        var quit = Button("QuitButton", menu, "종료", Light, Ink, 100);
        UnityEventTools.AddPersistentListener(newGame.onClick, controller.SelectNewGame);
        UnityEventTools.AddPersistentListener(resume.onClick, controller.OpenSlots);
        UnityEventTools.AddPersistentListener(settings.onClick, controller.OpenSettings);
        UnityEventTools.AddPersistentListener(quit.onClick, controller.Quit);

        // 패널은 씬에 저장하며 평상시에는 비활성화한다.
        RectTransform slotCard;
        var slotsPanel = Modal(root, "SlotPanel", "이어 하기", out slotCard);
        var slotList = Rect("SlotList", slotCard);
        slotList.anchorMin = new Vector2(0.08f, 0.22f);
        slotList.anchorMax = new Vector2(0.92f, 0.78f);
        slotList.offsetMin = slotList.offsetMax = Vector2.zero;
        var slotLayout = slotList.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        slotLayout.spacing = 16;
        slotLayout.childControlWidth = true;
        slotLayout.childControlHeight = true;
        slotLayout.childForceExpandHeight = false;
        slotLayout.childForceExpandWidth = true;
        var slotButtons = new UnityEngine.UI.Button[3];
        var slotLabels = new TMP_Text[3];
        for (int index = 0; index < 3; index++)
        {
            slotButtons[index] = Button("Slot" + (index + 1) + "Button", slotList, "", Paper, Ink, 112);
            slotLabels[index] = slotButtons[index].GetComponentInChildren<TMP_Text>();
            slotLabels[index].fontSize = 32;
            UnityEventTools.AddIntPersistentListener(slotButtons[index].onClick, controller.SelectSlot, index);
        }
        var slotClose = Button("CloseSlotsButton", slotCard, "돌아가기", Green, Light, 72);
        Position(slotClose.GetComponent<RectTransform>(), new Vector2(0.5f, 0.10f), new Vector2(240, 72));
        slotClose.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        UnityEventTools.AddPersistentListener(slotClose.onClick, controller.ClosePanels);

        RectTransform settingsCard;
        var settingsPanel = Modal(root, "SettingsPanel", "환경 설정", out settingsCard);
        var settingsClose = Button("CloseSettingsButton", settingsCard, "돌아가기", Green, Light, 72);
        Position(settingsClose.GetComponent<RectTransform>(), new Vector2(0.5f, 0.10f), new Vector2(240, 72));
        settingsClose.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        UnityEventTools.AddPersistentListener(settingsClose.onClick, controller.ClosePanels);

        var feedback = Text("FeedbackText", root, "", 32, Green, TextAlignmentOptions.Center);
        Position(feedback.rectTransform, new Vector2(0.5f, 0.055f), new Vector2(1100, 60));
        feedback.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("continueButton").objectReferenceValue = resume;
        serialized.FindProperty("slotPanel").objectReferenceValue = slotsPanel;
        serialized.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        serialized.FindProperty("feedback").objectReferenceValue = feedback;
        for (int index = 0; index < 3; index++)
        {
            serialized.FindProperty("slotButtons").arraySize = 3;
            serialized.FindProperty("slotLabels").arraySize = 3;
            serialized.FindProperty("slotButtons").GetArrayElementAtIndex(index).objectReferenceValue = slotButtons[index];
            serialized.FindProperty("slotLabels").GetArrayElementAtIndex(index).objectReferenceValue = slotLabels[index];
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        controller.ClosePanels();
        controller.RefreshSlots();

        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        events.GetComponent<EventSystem>().firstSelectedGameObject = newGame.gameObject;
        Canvas.ForceUpdateCanvases();
        EditorSceneManager.SaveScene(scene, ScenePath);
        // 시작 화면을 첫 씬으로 등록하되 기존 게임 씬의 순서와 활성 여부는 보존한다.
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
            .Concat(EditorBuildSettings.scenes.Where(item => item.path != ScenePath)).ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("시작 화면 생성 완료: " + ScenePath);
    }

    private static TMP_FontAsset CreateFont()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (existing != null) return existing;
        var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Shop/Resources/Shop/Fonts/RecipeHandwriting.ttf");
        if (source == null) throw new InvalidOperationException("프로젝트의 한국어 폰트를 찾을 수 없습니다.");
        var result = TMP_FontAsset.CreateFontAsset(source, 80, 9, GlyphRenderMode.SDFAA, 1024, 1024);
        result.name = "MenuHandwriting";
        AssetDatabase.CreateAsset(result, FontPath);
        AssetDatabase.AddObjectToAsset(result.material, result);
        foreach (var texture in result.atlasTextures)
            AssetDatabase.AddObjectToAsset(texture, result);
        result.TryAddCharacters("DDUK0123456789뚝딱떡정성으로빚는하루새로운시작해볼까요?새로하기이어환경설정종료슬롯저장된기록이없습니다임선택했돌아가. ");
        EditorUtility.SetDirty(result);
        return result;
    }

    private static GameObject Modal(RectTransform root, string name, string title, out RectTransform card)
    {
        var overlay = Image(name, root, new Color(0.16f, 0.12f, 0.08f, 0.55f));
        Fill(overlay);
        overlay.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
        card = Image("Card", overlay, Light);
        card.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
        Position(card, new Vector2(0.5f, 0.5f), new Vector2(780, 760));
        card.pivot = new Vector2(0.5f, 0.5f);
        var heading = Text("PanelTitle", card, title, 52, Ink, TextAlignmentOptions.Center);
        Position(heading.rectTransform, new Vector2(0.5f, 0.88f), new Vector2(600, 90));
        heading.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        return overlay.gameObject;
    }

    private static UnityEngine.UI.Button Button(string name, Transform parent, string caption, Color background, Color foreground, float height)
    {
        var rect = Image(name, parent, background);
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = rect.GetComponent<UnityEngine.UI.Image>();
        button.targetGraphic.raycastTarget = true;
        var colors = button.colors;
        colors.highlightedColor = new Color(0.92f, 0.95f, 0.89f);
        colors.pressedColor = new Color(0.73f, 0.80f, 0.70f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.77f, 0.77f, 0.73f, 0.48f);
        button.colors = colors;
        var layout = rect.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        layout.preferredHeight = height;
        layout.minHeight = height;
        var label = Text("Label", rect, caption, 42, foreground, TextAlignmentOptions.Center);
        Fill(label.rectTransform);
        label.rectTransform.offsetMin = new Vector2(18, 8);
        label.rectTransform.offsetMax = new Vector2(-18, -8);
        return button;
    }

    private static TMP_Text Text(string name, Transform parent, string value, float size, Color color, TextAlignmentOptions alignment)
    {
        var rect = Rect(name, parent);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.text = value;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        return text;
    }

    private static void Label(string name, Transform parent, string value, float size, Color color, float x, float y, float width, float height)
    {
        var text = Text(name, parent, value, size, color, TextAlignmentOptions.Left);
        Position(text.rectTransform, new Vector2(x, y), new Vector2(width, height));
    }

    private static RectTransform Image(string name, Transform parent, Color color)
    {
        var rect = Rect(name, parent);
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var value = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        value.SetParent(parent, false);
        return value;
    }

    private static void Position(RectTransform rect, Vector2 anchor, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
    }

    private static void Fill(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString("#" + value, out var color);
        return color;
    }
}
