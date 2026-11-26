using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

public static class ArmySelectionChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("DivineWar/Validation/Army Selection")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Run outside Play Mode.");
        var game = UnityEngine.Object.FindFirstObjectByType<신들의전쟁>();
        if (game == null) throw new InvalidOperationException("Open Main scene before running checks.");
        FieldInfo Field(string name) => typeof(신들의전쟁).GetField(name, Private);
        void Select(bool all) => typeof(신들의전쟁).GetMethod("병력선택", Private).Invoke(game, new object[] { all });
        var selected = (HashSet<int>)Field("선택부대").GetValue(game);
        var originalSelection = new List<int>(selected);
        var corps = ((int 병사, int 군단)[])Field("군단구성").GetValue(game);
        var corpsCounts = (int[])Field("군단인원").GetValue(game);
        var corpsCenters = (Vector2[])Field("군단중심").GetValue(game);
        var originalCorps = (ValueTuple<int, int>[])corps.Clone();
        var originalCounts = (int[])corpsCounts.Clone();
        var originalCenters = (Vector2[])corpsCenters.Clone();
        string[] fields = { "상태", "내팀", "배치", "드래그중", "드래그선택안내" };
        var originals = Array.ConvertAll(fields, name => Field(name).GetValue(game));
        var buildList = (GameObject)Field("건축목록").GetValue(game);
        bool buildListActive = buildList.activeSelf;
        try
        {
            var state = new 전장상태 { 대상들 = new 전장대상[8] };
            void Unit(int index, int team, 대상종류 kind, float health = 100) =>
                state.대상들[index] = new 전장대상 { 팀 = team, 종류 = kind, 체력 = health, 위치 = new Vector2(9999, 9999) };
            Unit(0, 0, 대상종류.왕);
            Unit(1, 0, 대상종류.주민);
            Unit(2, 0, 대상종류.병사);
            Unit(3, 0, 대상종류.병사, 0);
            Unit(4, 1, 대상종류.병사);
            Unit(5, 0, 대상종류.회관);
            Unit(6, 0, 대상종류.병사);
            Field("상태").SetValue(game, state);
            Field("내팀").SetValue(game, 0);
            selected.Clear(); selected.Add(1);
            Select(true);
            Require(selected.SetEquals(new[] { 2, 6 }), "All army must include off-screen allies and exclude residents, king, buildings, enemies and dead units.");
            selected.Clear(); selected.UnionWith(new[] { 0, 1, 2, 3, 4, 5, 7, -1 });
            Select(false);
            Require(selected.SetEquals(new[] { 2 }), "Filter must preserve only living selected allied soldiers without adding unselected soldiers.");
            selected.Clear(); selected.Add(1); Select(false);
            Require(selected.Count == 0, "Resident-only selection must become empty.");
            Select(false);
            Require(selected.Count == 0, "Filtering an empty selection must stay empty.");
            Field("내팀").SetValue(game, 1); Select(true);
            Require(selected.SetEquals(new[] { 4 }), "All army must use the local player's team.");
            state.대상들[4].체력 = 0; Select(true);
            Require(selected.Count == 0, "No living soldiers must produce an empty selection.");
            var ui = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/DivineWar/전장.uxml").CloneTree();
            var hint = ui.Q<Label>("army-selection-hint");
            Require(hint != null && hint.text.Contains("[Z]") && hint.text.Contains("[X]") && hint.text.Contains("[C]") && hint.text.Contains("[V]") && hint.pickingMode == PickingMode.Ignore,
                "Authored shortcut hint must exist and allow pointer input through it.");
            var markup = System.IO.File.ReadAllText("Assets/DivineWar/전장.uxml");
            Require(markup.Contains("top: 62%;") && markup.Contains("name=\"army-selection-hint\" text=\"[Z] 주민 · [X] 병사 · [C] 브루저 · [V] 아처\" picking-mode=\"Ignore\" style=\"display: none;"),
                "Hint must start hidden and sit slightly below the screen center.");
            var skills = ui.Q<ScrollView>("debug-scroll");
            for (int i = 0; i < 8; i++)
                Require(skills.Q<Button>($"debug-skill-{i}") != null, $"Debug skill option {i + 1} must be reachable in the scroll view.");
            for (int i = 1; i <= 4; i++)
            {
                var emblem = Resources.Load<Texture2D>($"Minimap/corps-{i}");
                var button = ui.Q<Button>($"corps-{i}");
                Require(emblem != null && emblem.width == 24 && emblem.height == 28 && emblem.filterMode == FilterMode.Point
                        && button?.Q(className: "corps-emblem")?.pickingMode == PickingMode.Ignore,
                    $"Corps {i} must use an imported badge and keep its button clickable.");
            }

            void Recount() => typeof(신들의전쟁).GetMethod("군단통계갱신", Private).Invoke(game, null);
            Array.Clear(corps, 0, corps.Length);
            Field("내팀").SetValue(game, 0);
            state.대상들[2].병사번호 = 41;
            state.대상들[2].위치 = new Vector2(-8, 4);
            state.대상들[6].병사번호 = 42;
            state.대상들[6].위치 = new Vector2(4, 10);
            corps[2] = (41, 1);
            corps[6] = (42, 1);
            state.대상들[4].체력 = 100;
            state.대상들[4].병사번호 = 43;
            corps[4] = (43, 1);
            corps[3] = (44, 1);
            Recount();
            Require(corpsCounts[0] == 2 && corpsCenters[0] == new Vector2(-2, 7),
                "Corps marker must average only living allies assigned to that corps.");
            state.대상들[6].위치 = new Vector2(6, 16);
            Recount();
            Require(corpsCenters[0] == new Vector2(-1, 10), "The marker must follow a moving member.");
            state.대상들[6].병사번호 = 45;
            Recount();
            Require(corpsCounts[0] == 1 && corpsCenters[0] == state.대상들[2].위치,
                "A replacement soldier slot must not inherit the old corps badge.");
            state.대상들[2].체력 = 0;
            Recount();
            Require(corpsCounts[0] == 0, "An empty corps must not appear on the minimap.");
            Debug.Log("[ArmySelectionChecks] ALL PASSED: 24 selection, corps, and authored UI checks.");
        }
        finally
        {
            for (int i = 0; i < fields.Length; i++) Field(fields[i]).SetValue(game, originals[i]);
            selected.Clear(); selected.UnionWith(originalSelection);
            Array.Copy(originalCorps, corps, corps.Length);
            Array.Copy(originalCounts, corpsCounts, corpsCounts.Length);
            Array.Copy(originalCenters, corpsCenters, corpsCenters.Length);
            buildList.SetActive(buildListActive);
        }
    }

    public static void Batch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        Run();
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
