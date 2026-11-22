using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// Temporary Play Mode fixtures use existing king renderers; never create or save scene objects.
public static class CombatPresentationChecks
{
    const string Key = "DivineWar.CombatPresentation";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static bool placed;
    static 전장규칙 configured;

    [MenuItem("DivineWar/Validation/Preview Ranged Combat")]
    public static void Ranged() => Start(왕직업.애쉬);
    [MenuItem("DivineWar/Validation/Preview Melee Combat")]
    public static void Melee() => Start(왕직업.트린다미어);

    static void Start(왕직업 job)
    {
        if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Run from Edit Mode with no unsaved scene changes.");
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        SessionState.SetInt(Key, (int)job);
        placed = false;
        configured = null;
        EditorApplication.isPaused = false;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }

    [InitializeOnLoadMethod]
    static void Reconnect()
    {
        if (SessionState.GetInt(Key, -1) < 0) return;
        placed = false;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying)
        {
            // Play Mode may begin several editor updates after the menu command returns.
            if (SessionState.GetInt(Key, -1) >= 0) return;
            EditorApplication.update -= Tick; return;
        }
        Application.runInBackground = true;
        if (Time.timeSinceLevelLoad < .75f) return;
        var game = UnityEngine.Object.FindAnyObjectByType<신들의전쟁>();
        if (game == null) return;
        var rules = (전장규칙)typeof(신들의전쟁).GetField("규칙", Private).GetValue(game);
        if (rules == null) return;
        if (placed && configured != rules) placed = false;
        var state = rules.상태;
        if (!placed)
        {
            var floor = (Tilemap)typeof(신들의전쟁).GetField("바닥타일맵", Private).GetValue(game);
            var wall = (Tilemap)typeof(신들의전쟁).GetField("장애물타일맵", Private).GetValue(game);
            var nav = new 격자길찾기(floor, wall);
            var job = (왕직업)SessionState.GetInt(Key, (int)왕직업.애쉬);
            var king = state.대상들[0]; var enemy = state.대상들[전장규칙.왕번호(1)];
            Vector2 p = king.위치;
            float closest = float.MaxValue;
            for (int y = -5; y <= 5; y++) for (int x = -5; x <= 5; x++)
            {
                var candidate = king.위치 + new Vector2(x, y);
                if (!nav.정적통행가능(candidate) || !nav.정적통행가능(candidate + Vector2.right * 4) || x*x+y*y >= closest) continue;
                p = candidate;
                closest = x*x+y*y;
            }
            king.위치 = p; king.목적지 = p; king.체력 = king.최대체력 = 9999;
            king.이동중 = false; king.표적 = -1; king.공격대기 = 0;
            state.진영들[0].직업 = job;
            enemy.위치 = p + Vector2.right * (job == 왕직업.애쉬 ? 4 : .75f);
            enemy.목적지 = enemy.위치; enemy.체력 = enemy.최대체력 = 9999; enemy.속박 = 999;
            enemy.이동중 = false; enemy.표적 = -1;
            typeof(전장규칙).GetField("적명령시간", Private).SetValue(rules, 999f);
            typeof(신들의전쟁).GetField("자동추적", Private).SetValue(game, true);
            var camera = (Camera)typeof(신들의전쟁).GetField("전장카메라", Private).GetValue(game);
            camera.transform.position = new Vector3(p.x + (job == 왕직업.애쉬 ? 1.5f : 0), p.y, camera.transform.position.z);
            rules.명령(0, new 전장명령 { 종류 = 명령종류.공격, 값 = 전장규칙.왕번호(1), 위치 = enemy.위치 });
            Debug.Log($"[CombatPresentationChecks] Fixture {job}, time {state.시간}, king {king.위치}, enemy {enemy.위치}.");
            placed = true;
            configured = rules;
        }
        foreach (var effect in state.지대들)
        {
            bool pause = effect.팀 == 0 && (effect.종류 == 지대종류.기본투사체 && effect.남은시간 < 2.86f
                || effect.종류 == 지대종류.근접타격 && effect.남은시간 < .11f);
            if (!pause) continue;
            Debug.Log($"[CombatPresentationChecks] {effect.종류}: visible attack fixture paused for capture.");
            EditorApplication.isPaused = true;
            SessionState.EraseInt(Key); EditorApplication.update -= Tick; return;
        }
    }
}
