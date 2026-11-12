using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PrologueSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/Prologue.unity";

    [MenuItem("DDUKDDUK/Create Prologue Scene")]
    public static void Build()
    {
        if (File.Exists(ScenePath))
            throw new InvalidOperationException("기존 프롤로그 씬을 덮어쓰지 않습니다.");
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/MainMenu/Fonts/MenuHandwriting.asset");
        if (font == null) throw new InvalidOperationException("시작 화면의 한국어 폰트를 찾을 수 없습니다.");
        // 프롤로그 전용 폰트를 복제해 시작 화면의 폰트는 변경하지 않는다.
        const string fontPath = "Assets/UI/MainMenu/Fonts/PrologueHandwriting.asset";
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath) == null
            && !AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(font), fontPath))
            throw new InvalidOperationException("프롤로그 폰트를 생성하지 못했습니다.");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        if (!font.TryAddCharacters("대충 프롤로그 씬"))
            throw new InvalidOperationException("프롤로그 문구에 필요한 글자를 추가하지 못했습니다.");
        EditorUtility.SetDirty(font);

        var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0, 0, -10);
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(246, 238, 220, 255);
            camera.orthographic = true;

            var canvasObject = new GameObject("PrologueCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<DdukDduk.Prologue.PrologueController>();
            var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var label = new GameObject("PrologueLabel", typeof(RectTransform), typeof(TextMeshProUGUI))
                .GetComponent<TextMeshProUGUI>();
            label.transform.SetParent(canvasObject.transform, false);
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(1200, 160);
            label.font = font;
            label.fontSize = 64;
            label.color = new Color32(67, 56, 46, 255);
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.text = "대충 프롤로그 씬";

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = EditorBuildSettings.scenes
                .Where(item => item.path != ScenePath)
                .Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded)
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
