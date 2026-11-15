// 역할: 공통 대화 UI 프리팹과 독립 테스트 씬을 Unity Editor에서 생성한다.
using System;
using System.IO;
using DdukDduk.Dialogue;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public static class DialogueUIBuilder
{
    private const string PrefabPath = "Assets/Resources/UI/DialoguePanel.prefab";
    private const string ScenePath = "Assets/Scenes/DialogueUITest.unity";
    private const string FontPath = "Assets/UI/Dialogue/DialogueHandwriting.asset";
    private static readonly Color Paper = new Color32(246, 238, 220, 255);
    private static readonly Color Ink = new Color32(67, 56, 46, 255);
    private static readonly Color Green = new Color32(63, 98, 78, 255);
    private static TMP_FontAsset font;

    // 기존 결과물을 덮어쓰지 않고 프리팹과 테스트 씬을 생성한 뒤 원래 열린 씬으로 돌아간다.
    [MenuItem("DDUKDDUK/Create Dialogue UI")]
    public static void Build()
    {
        if (File.Exists(PrefabPath) || File.Exists(ScenePath))
        {
            throw new InvalidOperationException("기존 대화 UI 또는 테스트 씬을 덮어쓰지 않습니다.");
        }

        var settings = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/SettingsPanel.prefab");
        var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Shop/Resources/Shop/Fonts/RecipeHandwriting.ttf");
        if (settings == null || source == null)
        {
            throw new InvalidOperationException("공통 설정 프리팹 또는 한국어 원본 폰트가 없습니다.");
        }

        Directory.CreateDirectory("Assets/UI/Dialogue");
        AssetDatabase.Refresh();
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            font = TMP_FontAsset.CreateFontAsset(source, 80, 9,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048);
            font.name = "DialogueHandwriting";
            font.isMultiAtlasTexturesEnabled = true;
            AssetDatabase.CreateAsset(font, FontPath);
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var atlas in font.atlasTextures)
            {
                AssetDatabase.AddObjectToAsset(atlas, font);
            }
            AssetDatabase.SaveAssets();
        }

        var previous = SceneManager.GetActiveScene();
        // 배치 실행의 저장되지 않은 기본 씬은 교체하고, 사용 중인 Editor에서는 기존 씬을 유지한다.
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
            Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0, 0, -10);
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(213, 222, 204, 255);
            camera.orthographic = true;

            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            var root = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            root.SetActive(false);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            var controller = root.AddComponent<DialogueUIController>();
            var serialized = new SerializedObject(controller);

            var content = Rect("DialogueContent", root.transform, Vector2.zero, Vector2.one);
            SetReference(serialized, "dialogueContent", content.gameObject);
            var dialogue = Rect("DialogueBox", content, new Vector2(.055f, .16f), new Vector2(.945f, .39f));
            var dialogueImage = Paint(dialogue, Paper, true);
            var advance = dialogue.gameObject.AddComponent<UnityEngine.UI.Button>();
            advance.targetGraphic = dialogueImage;
            advance.transition = UnityEngine.UI.Selectable.Transition.None;
            UnityEventTools.AddPersistentListener(advance.onClick, controller.OnDialogueClicked);

            var namePlate = Rect("NamePlate", dialogue, new Vector2(.025f, .83f), new Vector2(.2f, 1.13f));
            Paint(namePlate, Green);
            Label("SpeakerName", namePlate, "화자 이름", Vector2.zero, Vector2.one, 34, Color.white, true);
            Label("DialogueText", dialogue, "대화 UI를 확인하는 임시 문구입니다.\n아래 버튼을 눌러 패널을 열어 보세요.",
                new Vector2(.035f, .24f), new Vector2(.965f, .79f), 38, Ink);
            Label("NextHint", dialogue, "클릭하여 다음 대사", new Vector2(.7f, .04f), new Vector2(.965f, .22f), 25, Green, true);

            var bar = Rect("Toolbar", content, new Vector2(.055f, .075f), new Vector2(.945f, .135f));
            var layout = bar.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            string[] names = { "Save", "Load", "Auto", "Skip", "History", "Hide", "Settings" };
            string[] captions = { "저장", "불러오기", "자동 넘김", "빨리 감기", "대화 기록", "창 숨기기", "설정" };
            UnityAction[] actions = { controller.OpenSave, controller.OpenLoad, controller.RequestAuto,
                controller.RequestSkip, controller.OpenHistory, controller.HideDialogue, controller.OpenSettings };
            for (int i = 0; i < names.Length; i++)
            {
                var button = Button(names[i] + "Button", bar, captions[i], Vector2.zero, Vector2.one, actions[i]);
                button.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 1;
            }

            var feedback = Label("Feedback", content, "", new Vector2(.055f, .015f), new Vector2(.945f, .065f), 25, Ink, true);
            SetReference(serialized, "feedback", feedback);

            var slots = Modal("SaveLoadPanel", root.transform, "저장", controller.ClosePanels, out var slotCard, out var slotTitle);
            SetReference(serialized, "saveLoadPanel", slots.gameObject);
            SetReference(serialized, "saveLoadTitle", slotTitle);
            SetReference(serialized, "slotFeedback", Label("SlotFeedback", slotCard, "",
                new Vector2(.08f, .04f), new Vector2(.92f, .14f), 27, Ink, true));
            Label("Notice", slotCard, "스토리 연결 전 / 실제 파일을 저장하거나 불러오지 않습니다.",
                new Vector2(.07f, .69f), new Vector2(.93f, .79f), 27, Ink, true);
            for (int slot = 1; slot <= 3; slot++)
            {
                float top = .66f - (slot - 1) * .17f;
                var button = Button("Slot" + slot, slotCard, "슬롯 " + slot + "    연결 대기",
                    new Vector2(.08f, top - .13f), new Vector2(.92f, top), null);
                UnityEventTools.AddIntPersistentListener(button.onClick, controller.SelectSlot, slot);
            }
            slots.gameObject.SetActive(false);

            var history = Modal("HistoryPanel", root.transform, "대화 기록", controller.ClosePanels, out var historyCard, out _);
            SetReference(serialized, "historyPanel", history.gameObject);
            Label("EmptyHistory", historyCard, "아직 기록된 대사가 없습니다.\n스토리 연결 후 지난 대화를 표시합니다.",
                new Vector2(.08f, .25f), new Vector2(.92f, .74f), 34, Ink, true);
            history.gameObject.SetActive(false);

            var settingsInstance = (GameObject)PrefabUtility.InstantiatePrefab(settings, root.transform);
            settingsInstance.name = "SettingsPanel";
            settingsInstance.SetActive(false);
            SetReference(serialized, "settingsPanel", settingsInstance);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ConnectDialogue(root);

            // 피드백에만 등장하는 글자도 미리 포함해 최초 실행 시 글리프 생성 부담을 줄인다.
            EnsureCharacters("슬롯 123 / 저장 불러오기 연결 대기 다음 대사 스토리 자동 넘김 빨리 감기 구현 대기");
            EditorUtility.SetDirty(font);
            root.SetActive(true);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            PrefabUtility.InstantiatePrefab(prefab, scene);

            // 테스트 안내는 프리팹에 포함하지 않아 실제 씬에 재사용할 때 노출되지 않는다.
            var testCanvas = new GameObject("TestSceneGuide", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
            testCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            testCanvas.GetComponent<Canvas>().sortingOrder = -1;
            var testScaler = testCanvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            testScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            testScaler.referenceResolution = new Vector2(1920, 1080);
            testScaler.matchWidthOrHeight = .5f;
            Label("Title", testCanvas.transform, "대화 UI 테스트", new Vector2(.08f, .81f), new Vector2(.92f, .9f), 50, Green, true);
            Label("Description", testCanvas.transform, "임시 대사 / 패널 열기와 닫기 / 대화창 숨기기와 복귀",
                new Vector2(.08f, .74f), new Vector2(.92f, .81f), 30, Ink, true);
            AddTestDriver(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("DIALOGUE_UI_CREATED: " + PrefabPath + " | " + ScenePath);
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded)
            {
                SceneManager.SetActiveScene(previous);
            }
            if (SceneManager.sceneCount > 1)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    // 기존 프리팹의 출력·진행 모드·투명 복귀 입력을 갱신하고 테스트 씬의 임시 대사 공급자를 연결한다.
    // 저장되지 않은 씬 변경이나 Play Mode가 있으면 작업을 중단해 사용자 작업을 보호한다.
    [MenuItem("DDUKDDUK/Update Dialogue Text UI")]
    public static void UpdateTextUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Play Mode를 종료한 뒤 실행해야 합니다.");
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).isDirty)
            {
                throw new InvalidOperationException("저장되지 않은 씬 변경이 있어 갱신을 중단합니다.");
            }
        }

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            ConnectDialogue(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        var scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.isLoaded;
        if (opened)
        {
            scene = EditorSceneManager.OpenScene(ScenePath,
                Application.isBatchMode ? OpenSceneMode.Single : OpenSceneMode.Additive);
        }
        try
        {
            AddTestDriver(scene);
            EditorSceneManager.SaveScene(scene);
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssetIfDirty(font);
            Debug.Log("DIALOGUE_TEXT_UI_UPDATED");
        }
        finally
        {
            if (opened && SceneManager.sceneCount > 1)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    // 대사·모드 표시 참조를 연결하고 이전 복귀 버튼을 시각 요소가 없는 전체 화면 입력 영역으로 교체한다.
    private static void ConnectDialogue(GameObject root)
    {
        var controller = root.GetComponent<DialogueUIController>();
        var serialized = new SerializedObject(controller);
        var box = root.transform.Find("DialogueContent/DialogueBox");
        SetReference(serialized, "speakerLabel", box.Find("NamePlate/SpeakerName").GetComponent<TMP_Text>());
        SetReference(serialized, "dialogueLabel", box.Find("DialogueText").GetComponent<TMP_Text>());
        SetReference(serialized, "speakerPlate", box.Find("NamePlate").gameObject);
        SetReference(serialized, "nextHint", box.Find("NextHint").gameObject);
        SetReference(serialized, "autoLabel", root.transform.Find("DialogueContent/Toolbar/AutoButton/Label").GetComponent<TMP_Text>());
        SetReference(serialized, "skipLabel", root.transform.Find("DialogueContent/Toolbar/SkipButton/Label").GetComponent<TMP_Text>());

        var previousRestore = root.transform.Find("RestoreButton");
        if (previousRestore != null)
        {
            UnityEngine.Object.DestroyImmediate(previousRestore.gameObject);
        }

        var restore = root.transform.Find("RestoreInput");
        if (restore == null)
        {
            restore = Rect("RestoreInput", root.transform, Vector2.zero, Vector2.one);
            Paint((RectTransform)restore, Color.clear, true);
            restore.gameObject.AddComponent<DialogueRestoreInput>();
        }
        restore.SetAsLastSibling();
        restore.gameObject.SetActive(false);
        var input = new SerializedObject(restore.GetComponent<DialogueRestoreInput>());
        SetReference(input, "dialogueUI", controller);
        input.ApplyModifiedPropertiesWithoutUndo();
        SetReference(serialized, "restoreInput", restore.gameObject);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EnsureCharacters("자동 넘김 켜짐 빨리 감기 켜짐 읽지 않은 대사는 빨리 감기를 할 수 없습니다.");

        var click = box.GetComponent<UnityEngine.UI.Button>().onClick;
        while (click.GetPersistentEventCount() > 0)
        {
            UnityEventTools.RemovePersistentListener(click, 0);
        }
        UnityEventTools.AddPersistentListener(click, controller.OnDialogueClicked);
    }

    // 테스트 씬에 연속 대사 공급자와 재생 버튼을 연결한다. 숨김 중에는 안내와 테스트 버튼도 함께 감춘다.
    private static void AddTestDriver(Scene scene)
    {
        GameObject guide = null;
        DialogueUIController controller = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "TestSceneGuide")
            {
                guide = root;
            }
            var candidate = root.GetComponent<DialogueUIController>();
            if (candidate != null)
            {
                controller = candidate;
            }
        }
        if (guide == null || controller == null)
        {
            throw new InvalidOperationException("테스트 안내 또는 대화 UI 객체가 없습니다.");
        }

        var driver = guide.GetComponent<DialogueUITestDriver>();
        if (driver == null)
        {
            driver = guide.AddComponent<DialogueUITestDriver>();
        }
        var serialized = new SerializedObject(driver);
        SetReference(serialized, "dialogueUI", controller);
        var group = guide.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = guide.AddComponent<CanvasGroup>();
        }
        SetReference(serialized, "guideGroup", group);
        SetReference(serialized, "testStatus", guide.transform.Find("Description").GetComponent<TMP_Text>());
        var lines = serialized.FindProperty("sampleLines");
        for (int i = 0; i < lines.arraySize; i++)
        {
            EnsureCharacters(lines.GetArrayElementAtIndex(i).stringValue.Replace("\n", ""));
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        if (guide.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
        {
            guide.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        if (guide.transform.Find("SampleButton") == null)
        {
            Button("SampleButton", guide.transform, "다시 출력", new Vector2(.27f, .61f), new Vector2(.41f, .68f), driver.ShowSample);
            Button("NarrationButton", guide.transform, "문장 교체", new Vector2(.43f, .61f), new Vector2(.57f, .68f), driver.ShowNarration);
            Button("EmptyButton", guide.transform, "빈 대사", new Vector2(.59f, .61f), new Vector2(.73f, .68f), driver.ShowEmpty);
        }
        EnsureCharacters("화자 이름이 없는 대사입니다.출력 중에 클릭하면 문장 전체를 표시합니다.");
        EnsureCharacters("테스트 대사 1234 테스트 대화 끝 / 다시 출력으로 재시작 화자 없는 대사 테스트 빈 대사 테스트");
    }

    // 부모 RectTransform 크기에 대한 0~1 앵커 범위로 자식을 배치하고 내부 여백은 없앤다.
    private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    // 지정 사각형에 단색 배경을 추가한다. 입력을 받을 배경만 raycast를 참으로 지정한다.
    private static UnityEngine.UI.Image Paint(RectTransform rect, Color color, bool raycast = false)
    {
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }

    // 부모의 앵커 영역에 한국어 TMP 문구를 생성하고 필요한 글자를 전용 폰트에 등록한다.
    private static TMP_Text Label(string name, Transform parent, string text, Vector2 min, Vector2 max,
        float size, Color color, bool center = false)
    {
        if (!string.IsNullOrEmpty(text))
        {
            EnsureCharacters(text.Replace("\n", ""));
        }

        var label = Rect(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = size;
        label.color = color;
        label.text = text;
        label.alignment = center ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        return label;
    }

    // 이미 등록된 문자에 대한 TryAddCharacters 반환값 대신 실제 폰트 문자 포함 여부로 검증한다.
    private static void EnsureCharacters(string text)
    {
        font.TryAddCharacters(text);
        if (!font.HasCharacters(text))
        {
            throw new InvalidOperationException("대화 UI 폰트에 필요한 문자를 추가하지 못했습니다: " + text);
        }
    }

    // 녹색 버튼과 흰색 문구를 만들고 제공된 콜백을 프리팹에 보존되는 클릭 이벤트로 연결한다.
    private static UnityEngine.UI.Button Button(string name, Transform parent, string caption,
        Vector2 min, Vector2 max, UnityAction action)
    {
        var rect = Rect(name, parent, min, max);
        var image = Paint(rect, Green, true);
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        Label("Label", rect, caption, new Vector2(.03f, 0), new Vector2(.97f, 1), 29, Color.white, true);
        if (action != null)
        {
            UnityEventTools.AddPersistentListener(button.onClick, action);
        }
        return button;
    }

    // 화면 전체 입력 차단 배경, 중앙 카드, 제목과 닫기 버튼으로 구성된 공통 팝업을 만든다.
    private static RectTransform Modal(string name, Transform parent, string title, UnityAction close,
        out RectTransform card, out TMP_Text titleLabel)
    {
        var panel = Rect(name, parent, Vector2.zero, Vector2.one);
        Paint(panel, new Color(0, 0, 0, .55f), true);
        card = Rect("Card", panel, new Vector2(.23f, .22f), new Vector2(.77f, .83f));
        Paint(card, Paper, true);
        titleLabel = Label("Title", card, title, new Vector2(.08f, .82f), new Vector2(.75f, .95f), 42, Ink);
        Button("CloseButton", card, "닫기", new Vector2(.78f, .83f), new Vector2(.93f, .94f), close);
        return panel;
    }

    // private SerializeField를 이름으로 찾아 씬 내부 참조를 직렬화한다. 없는 필드는 즉시 실패시킨다.
    private static void SetReference(SerializedObject target, string field, UnityEngine.Object value)
    {
        var property = target.FindProperty(field);
        if (property == null)
        {
            throw new InvalidOperationException("UI 참조 필드가 없습니다: " + field);
        }
        property.objectReferenceValue = value;
    }
}
