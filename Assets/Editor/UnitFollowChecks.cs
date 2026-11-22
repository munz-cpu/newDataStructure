using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class UnitFollowChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Field(object owner, string name) => owner.GetType().GetField(name, Private).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Private).SetValue(owner, value);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethod(name, Private).Invoke(owner, args);
    static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    static void Report(string message)
    {
        Directory.CreateDirectory("Temp/UnitFollow");
        File.WriteAllText("Temp/UnitFollow/result.txt", message);
        Debug.Log("[UnitFollowChecks] " + message);
    }

    [MenuItem("DivineWar/Validation/Unit Follow/Open Main")]
    public static void OpenMain()
    {
        Require(!EditorApplication.isPlaying, "Exit Play Mode first.");
        AssetDatabase.ImportAsset("Assets/DivineWar/Resources/UnitFollowButton.prefab", ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        var prefab = Resources.Load<GameObject>("UnitFollowButton")?.GetComponent<Button>();
        Require(prefab != null && prefab.image.sprite != null, "Authored camera button and wood sprite must import.");
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        Report("PASS: authored camera button and sprite imported.");
    }

    [MenuItem("DivineWar/Validation/Unit Follow/Run Live")]
    public static void RunLive()
    {
        Require(EditorApplication.isPlaying, "Enter Main Play Mode first.");
        var game = UnityEngine.Object.FindFirstObjectByType<신들의전쟁>();
        var state = (전장상태)Field(game, "상태");
        var camera = (Camera)Field(game, "전장카메라");
        var selected = (HashSet<int>)Field(game, "선택부대");
        int team = (int)Field(game, "내팀");
        int index = 전장규칙.왕번호(team) + 6;
        var unit = state.대상들[index];
        Require(unit is { 생존: true }, "Initial resident must be alive.");
        Set(game, "일시정지", true);
        Set(game, "현재마우스", new Vector2(Screen.width / 2f, Screen.height / 2f));
        selected.Clear(); selected.Add(index);
        camera.transform.position = (Vector3)Call(game, "카메라한계", unit.위치);
        Call(game, "장면반영", .05f);
        Call(game, "유닛상태창갱신");
        var window = Field(game, "상태창");
        var button = ((GameObject)window.GetType().GetProperty("오브젝트").GetValue(window)).GetComponentInChildren<Button>();
        Require(button != null && button.IsInteractable(), "Camera button must be visible and interactive.");
        button.onClick.Invoke();
        Require((bool)Field(game, "자동추적") && (int)Field(game, "카메라추적번호") == index, "Camera click must follow the displayed resident.");
        var originalPosition = unit.위치;
        unit.위치 += Vector2.right * 2;
        var before = camera.transform.position;
        Call(game, "카메라이동", .1f);
        var target = (Vector3)Call(game, "카메라한계", unit.위치);
        Require(Vector3.Distance(camera.transform.position, target) < Vector3.Distance(before, target), "Camera must move toward the moving resident.");
        button.onClick.Invoke();
        Require(!(bool)Field(game, "자동추적"), "Repeated camera click must stop following.");
        button.onClick.Invoke();
        game.시점복귀();
        Require((bool)Field(game, "자동추적") && (int)Field(game, "카메라추적번호") == -1, "Space/return must restore king follow.");
        button.onClick.Invoke();
        float hp = unit.체력;
        unit.체력 = 0;
        Call(game, "카메라이동", .1f);
        Require(!(bool)Field(game, "자동추적") && (int)Field(game, "카메라추적번호") == -1, "Death must release follow.");
        unit.체력 = hp; unit.위치 = originalPosition;
        Call(game, "장면반영", .05f);
        Call(game, "유닛상태창갱신");
        Canvas.ForceUpdateCanvases();
        Report("PASS: camera callback, moving resident follow, toggle off, king return and death release. Ready for pointer check.");
    }

    [MenuItem("DivineWar/Validation/Unit Follow/Pointer Position")]
    public static void PointerPosition()
    {
        var game = UnityEngine.Object.FindFirstObjectByType<신들의전쟁>();
        var window = Field(game, "상태창");
        var button = ((GameObject)window.GetType().GetProperty("오브젝트").GetValue(window)).GetComponentInChildren<Button>();
        var rect = (RectTransform)button.transform;
        var point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
        Require(hits.Exists(hit => hit.gameObject == button.gameObject), "Authored camera button must receive pointer raycasts.");
        Report($"PASS: pointer raycast. Button center {point.x:F0},{point.y:F0}; screen {Screen.width},{Screen.height}; follow={(bool)Field(game, "자동추적")}; selection={((HashSet<int>)Field(game, "선택부대")).Count}; drag={(bool)Field(game, "드래그중")}");
    }
}
