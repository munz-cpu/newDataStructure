using System;
using System.Collections.Generic;
using System.IO;
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

    [MenuItem("DivineWar/Validation/Pointer Orders")]
    public static void PointerOrders()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Run pointer target validation in Edit Mode.");
        const string receipt = "Temp/CombatPresentation/pointer-orders.txt";
        Directory.CreateDirectory("Temp/CombatPresentation");
        File.Delete(receipt);
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Main.unity");
        신들의전쟁 game = null;
        FieldInfo fog = typeof(신들의전쟁).GetField("안개", Private);
        try
        {
            foreach (var root in scene.GetRootGameObjects()) game ??= root.GetComponentInChildren<신들의전쟁>(true);
            if (game == null) throw new Exception("Main scene has no game controller.");
            void Set(string name, object value) => typeof(신들의전쟁).GetField(name, Private).SetValue(game, value);
            var authored = (SpriteRenderer[])typeof(신들의전쟁).GetField("슬롯", Private).GetValue(game);
            var renderers = new List<SpriteRenderer>();
            foreach (var renderer in authored)
                if (renderer != null && !renderers.Contains(renderer)) renderers.Add(renderer);
            if (renderers.Count < 3) throw new Exception("Main scene needs three authored unit renderers.");
            var state = new 전장상태
            {
                대상들 = new 전장대상[3],
                짐승들 = Array.Empty<짐승상태>(),
                자원잔량 = Array.Empty<int>()
            };
            // An inert fog plus a scouting zone supplies deterministic visibility without
            // allocating graphics resources or starting any simulation lifecycle methods.
            fog.SetValue(game, System.Runtime.Serialization.FormatterServices.GetUninitializedObject(fog.FieldType));
            state.지대들.Add(new 스킬지대 { 팀 = 0, 종류 = 지대종류.시야, 위치 = Vector2.zero, 반경 = 100 });
            Set("상태", state);
            Set("내팀", 0);
            var terrainField = typeof(신들의전쟁).GetField("지형", Private);
            var terrain = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(terrainField.FieldType);
            terrainField.FieldType.GetField("<자원들>k__BackingField", Private).SetValue(terrain, Array.Empty<자원지점>());
            terrainField.SetValue(game, terrain);
            Set("짐승슬롯", Array.Empty<SpriteRenderer>());
            Set("슬롯", new[] { renderers[0], renderers[1], renderers[2] });
            void Place(int slot, int team, 대상종류 kind, Vector2 position, int order)
            {
                state.대상들[slot] = new 전장대상 { 팀 = team, 종류 = kind, 위치 = position, 체력 = 100 };
                renderers[slot].transform.position = position;
                renderers[slot].enabled = true;
                renderers[slot].sortingOrder = order;
            }
            Place(0, 0, 대상종류.제재소, Vector2.zero, 100);
            Place(1, 1, 대상종류.병사, Vector2.zero, 10);
            Place(2, 0, 대상종류.병사, Vector2.left * 3, 5);
            var target = typeof(신들의전쟁).GetMethod("전장클릭대상", Private);
            int checks = 0;
            void Expect(Vector2 point, int expected, string scenario)
            {
                var actual = ((int, int, int))target.Invoke(game, new object[] { point, false });
                if (actual.Item2 != expected)
                    throw new Exception($"{scenario}: expected target {expected}, got {actual.Item2}.");
                checks++;
            }
            Expect(new Vector2(0, .5f), 1, "Exact enemy wins over a visually foregrounded building");
            Expect(new Vector2(.719f, .5f), 1, "Enemy assistance accepts a miss just inside 0.3 units");
            Expect(new Vector2(.721f, .5f), 0, "Enemy assistance rejects a miss just outside 0.3 units");
            renderers[1].enabled = false;
            Expect(new Vector2(.65f, .5f), 0, "Disabled enemy leaves building selectable");
            renderers[1].enabled = true;
            state.대상들[1].위치 = state.대상들[2].위치 + Vector2.right * .7f;
            renderers[1].transform.position = state.대상들[1].위치;
            Expect(state.대상들[2].위치 + Vector2.up * .5f, 2, "Exact allied unit wins over assisted enemy");
            Expect(new Vector2(4, 4), -1, "Empty ground has no target");
            string result = $"PASS: {checks} deterministic preview-scene target checks (enemy/building overlap, assistance boundary, disabled enemy, exact ally, empty ground).";
            File.WriteAllText(receipt, result);
            Debug.Log("[CombatPresentationChecks.PointerOrders] " + result);
        }
        catch (Exception exception)
        {
            File.WriteAllText(receipt, "FAIL: " + exception);
            throw;
        }
        finally
        {
            if (game != null)
            {
                fog.SetValue(game, null);
                typeof(신들의전쟁).GetField("지형", Private).SetValue(game, null);
            }
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    [MenuItem("DivineWar/Validation/Damage Options")]
    public static void DamageOptions()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Run damage options validation in Edit Mode.");
        const string preference = "DivineWar.ShowDamageNumbers";
        bool hadPreference = PlayerPrefs.HasKey(preference);
        int previousPreference = PlayerPrefs.GetInt(preference);
        var menuScene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/BattleMenu.unity");
        var gameScene = default(UnityEngine.SceneManagement.Scene);
        try
        {
            전투메뉴 menu = null;
            foreach (var root in menuScene.GetRootGameObjects()) menu ??= root.GetComponentInChildren<전투메뉴>(true);
            if (menu == null) throw new Exception("BattleMenu scene has no combat menu.");
            var button = (메뉴버튼)typeof(전투메뉴).GetField("피해표시버튼", Private).GetValue(menu);
            var label = (TMPro.TMP_Text)typeof(전투메뉴).GetField("피해표시문구", Private).GetValue(menu);
            if (button == null || label == null || !label.transform.IsChildOf(button.transform))
                throw new Exception("Damage option must reference its scene button and child label.");
            gameScene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Main.unity");
            신들의전쟁 game = null;
            foreach (var root in gameScene.GetRootGameObjects()) game ??= root.GetComponentInChildren<신들의전쟁>(true);
            if (game == null) throw new Exception("Main scene has no game controller.");
            typeof(전투메뉴).GetMethod("Awake", Private).Invoke(menu, null);
            typeof(신들의전쟁).GetField("메뉴", Private).SetValue(game, menu);
            var enabled = typeof(신들의전쟁).GetField("피해표시", Private);
            enabled.SetValue(game, true);
            menu.피해표시갱신(true);
            var toggle = (Action)Delegate.CreateDelegate(typeof(Action), game, "피해표시변경");
            menu.피해표시전환 += toggle;
            Color selected = button.colors.normalColor;
            if (label.text != "피해 숫자  ·  켜짐") throw new Exception("Enabled damage option label is incorrect.");
            button.onClick.Invoke();
            if ((bool)enabled.GetValue(game) || PlayerPrefs.GetInt(preference, -1) != 0
                || label.text != "피해 숫자  ·  꺼짐" || button.colors.normalColor == selected)
                throw new Exception("Scene damage button did not disable numbers, persist the option, and update its label/selection.");
            button.onClick.Invoke();
            if (!(bool)enabled.GetValue(game) || PlayerPrefs.GetInt(preference, -1) != 1
                || label.text != "피해 숫자  ·  켜짐" || button.colors.normalColor != selected)
                throw new Exception("Scene damage button did not re-enable numbers and restore its label/selection.");
            Debug.Log("[CombatPresentationChecks.DamageOptions] PASS: scene references, button events, damage toggle, labels, selection, and persistence.");
        }
        finally
        {
            if (hadPreference) PlayerPrefs.SetInt(preference, previousPreference);
            else PlayerPrefs.DeleteKey(preference);
            PlayerPrefs.Save();
            if (gameScene.IsValid()) EditorSceneManager.ClosePreviewScene(gameScene);
            EditorSceneManager.ClosePreviewScene(menuScene);
        }
    }

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
