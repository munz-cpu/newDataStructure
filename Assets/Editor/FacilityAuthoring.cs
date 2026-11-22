using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Wires objects created directly with Editor commands. Does not generate scene objects.
public static class FacilityAuthoring
{
    const string Folder = "Assets/DivineWar/Resources/Buildings/";
    static SerializedObject Game() => new(UnityEngine.Object.FindFirstObjectByType<신들의전쟁>());
    static Sprite[] Frames(string name) => AssetDatabase.LoadAllAssetsAtPath(Folder + name + ".png")
        .OfType<Sprite>().OrderBy(s => s.name).ToArray();

    [MenuItem("DivineWar/Facilities/Inspect Authoring")]
    public static void Inspect()
    {
        var game = Game();
        var buttons = game.FindProperty("행동버튼");
        string Path(Transform t) => t.parent == null ? "/" + t.name : Path(t.parent) + "/" + t.name;
        var lines = Enumerable.Range(0, buttons.arraySize).Select(i => {
            var b = (메뉴버튼)buttons.GetArrayElementAtIndex(i).objectReferenceValue;
            return $"{i}: {Path(b.transform)} pos={((RectTransform)b.transform).anchoredPosition} size={((RectTransform)b.transform).sizeDelta} labels={string.Join(" | ", b.GetComponentsInChildren<TMP_Text>(true).Select(t => t.text))}";
        }).ToList();
        var r = (SpriteRenderer)game.FindProperty("슬롯").GetArrayElementAtIndex(4).objectReferenceValue;
        lines.Add($"School: {r.sprite.name} scale={r.transform.localScale} ppu={r.sprite.pixelsPerUnit} pivot={r.sprite.pivot}");
        var menu = (GameObject)game.FindProperty("건축목록").objectReferenceValue;
        lines.Add($"Menu: {Path(menu.transform)} rect={((RectTransform)menu.transform).rect}");
        var mr = (RectTransform)menu.transform;
        lines.Add($"Menu active={menu.activeSelf}/{menu.activeInHierarchy} pos={mr.anchoredPosition} anchors={mr.anchorMin}/{mr.anchorMax} pivot={mr.pivot}");
        var corners = new Vector3[4]; mr.GetWorldCorners(corners); lines.Add("Menu corners: " + string.Join(", ", corners.Select(c => c.ToString())));
        Directory.CreateDirectory("Temp/FacilityWork");
        File.WriteAllLines("Temp/FacilityWork/authoring.txt", lines);
        Debug.Log(string.Join("\n", lines));
    }

    [MenuItem("DivineWar/Facilities/Show Preview Menu")]
    public static void ShowMenu()
    {
        var g = UnityEngine.Object.FindFirstObjectByType<신들의전쟁>();
        typeof(신들의전쟁).GetMethod("건축목록표시", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(g, new object[] { true });
        Canvas.ForceUpdateCanvases();
        Inspect();
    }

    [MenuItem("DivineWar/Facilities/Check Live Buttons")]
    public static void LiveButtons()
    {
        if (!EditorApplication.isPlaying) throw new Exception("Enter Play Mode first.");
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var source = UnityEngine.Object.FindFirstObjectByType<신들의전쟁>();
        var buttons = (메뉴버튼[])typeof(신들의전쟁).GetField("시설버튼", flags).GetValue(source);
        var placement = typeof(신들의전쟁).GetField("배치", flags);
        placement.SetValue(source, -1);
        for (int i = 0; i < 5; i++)
        {
            buttons[i].onClick.Invoke();
            if ((int)placement.GetValue(source) != 7 + i) throw new Exception("Wrong building menu callback " + i);
        }
        buttons[4].onClick.Invoke();
        if ((int)placement.GetValue(source) != -1) throw new Exception("Placement toggle failed.");
        Directory.CreateDirectory("Temp/FacilityWork");
        File.WriteAllText("Temp/FacilityWork/live-buttons.txt", "PASS: Five live menu callbacks select correct build IDs; repeat click cancels placement.\n");
        ShowMenu();
    }

    [MenuItem("DivineWar/Facilities/Assign Authored Slots")]
    public static void Assign()
    {
        var sprites = Frames("facilities");
        var units = Frames("specialists");
        if (sprites.Length != 80 || units.Length != 32) throw new Exception("Expected 80 building and 32 specialist sprites.");
        var game = Game();
        var slots = game.FindProperty("시설슬롯"); slots.arraySize = 48;
        var original = (SpriteRenderer)game.FindProperty("슬롯").GetArrayElementAtIndex(4).objectReferenceValue;
        for (int i = 0; i < 48; i++)
        {
            var obj = GameObject.Find("FacilitySlot" + (i + 1));
            if (obj == null) throw new Exception($"Create FacilitySlot{i + 1} directly in Main first.");
            var renderer = obj.GetComponent<SpriteRenderer>();
            int local = i % 24;
            int kind = local < 2 ? 0 : local < 4 ? 1 : local < 8 ? 2 : local < 16 ? 3 : 4;
            Undo.RecordObject(renderer, "Assign facility renderer");
            renderer.sharedMaterial = original.sharedMaterial;
            renderer.sortingLayerID = original.sortingLayerID;
            renderer.sprite = sprites[kind * 16 + i / 24 * 8];
            renderer.enabled = false;
            slots.GetArrayElementAtIndex(i).objectReferenceValue = renderer;
        }
        void Set(string field, Sprite[] frames)
        {
            var array = game.FindProperty(field); array.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        }
        Set("시설그림", sprites); Set("병과그림", units);
        game.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Assigned 48 authored facility slots and all directional sprites.");
    }

    [MenuItem("DivineWar/Facilities/Create Button Prefab")]
    public static void ButtonPrefab()
    {
        var game = Game();
        var button = (메뉴버튼)game.FindProperty("행동버튼").GetArrayElementAtIndex(1).objectReferenceValue;
        PrefabUtility.SaveAsPrefabAsset(button.gameObject, Folder + "FacilityButton.prefab");
        AssetDatabase.SaveAssets();
    }

    [MenuItem("DivineWar/Facilities/Assign Menu Buttons")]
    public static void Buttons()
    {
        var game = Game();
        var menu = (GameObject)game.FindProperty("건축목록").objectReferenceValue;
        var rect = (RectTransform)menu.transform;
        Undo.RecordObject(rect, "Expand building menu");
        rect.sizeDelta = new Vector2(410, 304);
        // The original menu is right-aligned beside the minimap. Grow toward the
        // battlefield so the new column remains inside the screen at every scale.
        rect.anchoredPosition = new Vector2(-202, rect.anchoredPosition.y);
        var buttons = game.FindProperty("시설버튼"); buttons.arraySize = 5;
        string[] labels = { "8  브루저 훈련소", "9  아처 훈련소", "0  아처타워", "-  지뢰", "=  점프패드" };
        string[] descriptions = { "나무 65 · 돌 30  /  훈련 2초", "나무 65 · 돌 30  /  훈련 2초", "아처 1명 + 나무 80", "나무 15 · 돌 5 · 구리 3", "나무 25 · 돌 10 · 철 2" };
        var sprites = Frames("facilities");
        for (int i = 0; i < 5; i++)
        {
            var obj = Resources.FindObjectsOfTypeAll<메뉴버튼>().FirstOrDefault(b => b.name == "FacilityButton" + i && b.gameObject.scene.IsValid())?.gameObject;
            if (obj == null) throw new Exception("Instantiate all five button prefabs first.");
            Undo.SetTransformParent(obj.transform, rect, "Place facility button");
            var transform = (RectTransform)obj.transform;
            Undo.RecordObject(transform, "Position facility button");
            transform.anchorMin = transform.anchorMax = Vector2.zero;
            transform.pivot = Vector2.zero;
            transform.anchoredPosition = new Vector2(210, 221 - i * 53);
            transform.sizeDelta = new Vector2(192, 47);
            transform.localScale = Vector3.one;
            var button = obj.GetComponent<메뉴버튼>();
            var serial = new SerializedObject(button);
            serial.FindProperty("m_OnClick.m_PersistentCalls.m_Calls").arraySize = 0;
            serial.ApplyModifiedProperties();
            var texts = obj.GetComponentsInChildren<TMP_Text>(true);
            for (int t = 0; t < texts.Length; t++)
            {
                Undo.RecordObject(texts[t], "Label facility button");
                texts[t].text = t == 0 ? labels[i] : descriptions[i];
                texts[t].fontSize = t == 0 ? 17 : 10.5f;
                texts[t].enableAutoSizing = true;
                texts[t].fontSizeMin = t == 0 ? 12 : 9;
                texts[t].fontSizeMax = t == 0 ? 17 : 10.5f;
                PrefabUtility.RecordPrefabInstancePropertyModifications(texts[t]);
            }
            foreach (var image in obj.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                if (image.gameObject != obj && image.sprite != null && (image.sprite.name.StartsWith("camp") || image.sprite.name.StartsWith("facility")))
                {
                    Undo.RecordObject(image, "Set building icon"); image.sprite = sprites[i * 16]; image.preserveAspect = true;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(image);
                }
            PrefabUtility.RecordPrefabInstancePropertyModifications(button);
            PrefabUtility.RecordPrefabInstancePropertyModifications(transform);
            buttons.GetArrayElementAtIndex(i).objectReferenceValue = button;
            obj.SetActive(true);
        }
        game.ApplyModifiedProperties();
        Undo.FlushUndoRecordObjects();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Assigned all five facility menu buttons with original wood styling.");
    }
}
