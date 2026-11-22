using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class BuildingInteractionAuthoring
{
    [MenuItem("DivineWar/Building Interaction/Wire Authored Flag")]
    public static void Wire()
    {
        if (EditorApplication.isPlaying) throw new Exception("Use Edit Mode");
        const string art = "Assets/DivineWar/Resources/Buildings/flags.png";
        AssetDatabase.ImportAsset(art, ImportAssetOptions.ForceUpdate);
        var frames = AssetDatabase.LoadAllAssetsAtPath(art).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        if (frames.Length != 16) throw new Exception("Expected sixteen flag sprites");
        var source = UnityEngine.Object.FindFirstObjectByType<신들의전쟁>();
        var game = new SerializedObject(source);
        var original = (SpriteRenderer)game.FindProperty("슬롯").GetArrayElementAtIndex(4).objectReferenceValue;
        var slots = game.FindProperty("깃발슬롯"); slots.arraySize = 8;
        for (int i = 0; i < 8; i++)
        {
            var r = GameObject.Find("FlagSlot" + (i + 1)).GetComponent<SpriteRenderer>();
            Undo.RecordObject(r, "Assign authored flag renderer");
            r.sharedMaterial = original.sharedMaterial; r.sortingLayerID = original.sortingLayerID;
            r.sprite = frames[i / 4 * 8]; r.enabled = false;
            EditorUtility.SetDirty(r); slots.GetArrayElementAtIndex(i).objectReferenceValue = r;
        }
        var menu = (GameObject)game.FindProperty("건축목록").objectReferenceValue;
        var rect = (RectTransform)menu.transform;
        var button = Resources.FindObjectsOfTypeAll<메뉴버튼>().First(b => b.name == "FlagBuildButton" && b.gameObject.scene.IsValid());
        if (rect.sizeDelta.y < 350)
        {
            Undo.RecordObject(rect, "Expand building menu for flag"); rect.sizeDelta += Vector2.up * 53;
            foreach (Transform child in rect)
                if (child is RectTransform item && child != button.transform)
                { Undo.RecordObject(item, "Move building menu row"); item.anchoredPosition += Vector2.up * 53; PrefabUtility.RecordPrefabInstancePropertyModifications(item); }
        }
        Undo.SetTransformParent(button.transform, rect, "Place flag menu button");
        var rt = (RectTransform)button.transform;
        Undo.RecordObject(rt, "Position flag menu button");
        rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(210, 9); rt.sizeDelta = new Vector2(192, 47); rt.localScale = Vector3.one;
        var so = new SerializedObject(button); so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls").arraySize = 0; so.ApplyModifiedProperties();
        var texts = button.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            Undo.RecordObject(texts[i], "Label flag menu button");
            texts[i].text = i == 0 ? "B  깃발" : "나무 35 · 돌 10 / 건설 영역 +8칸";
            texts[i].fontSize = i == 0 ? 17 : 10.5f; texts[i].enableAutoSizing = true;
            texts[i].fontSizeMin = i == 0 ? 12 : 9; texts[i].fontSizeMax = i == 0 ? 17 : 10.5f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(texts[i]);
        }
        foreach (var image in button.GetComponentsInChildren<Image>(true))
            if (image.gameObject != button.gameObject && image.sprite != null && (image.sprite.name.StartsWith("camp") || image.sprite.name.StartsWith("facility")))
            { Undo.RecordObject(image, "Set flag icon"); image.sprite = frames[0]; image.preserveAspect = true; PrefabUtility.RecordPrefabInstancePropertyModifications(image); }
        button.gameObject.SetActive(true);
        game.FindProperty("깃발버튼").objectReferenceValue = button;
        game.ApplyModifiedProperties();
        PrefabUtility.RecordPrefabInstancePropertyModifications(button); PrefabUtility.RecordPrefabInstancePropertyModifications(rt);
        Undo.FlushUndoRecordObjects(); EditorSceneManager.MarkSceneDirty(source.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();
    }
}
