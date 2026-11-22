using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// Isolated simulation fixtures on the authored Main terrain; no scene objects created.
public static class FacilityChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly List<string> results = new();
    static void Require(bool ok, string why) { if (!ok) throw new Exception(why); }
    static object Field(object o, string name) => o.GetType().GetField(name, Private).GetValue(o);
    static void Check(string name, Action action) { action(); results.Add("PASS " + name); }

    [MenuItem("DivineWar/Validation/Facilities")]
    public static void Run()
    {
        results.Clear();
        try
        {
            Check("Residents cannot skip soldier training at either specialized school", () => {
                foreach (bool archer in new[] { false, true })
                {
                    var f = new Fixture(); int school = f.School(archer);
                    var worker = f.Unit(6, 0, f.Entry);
                    int[] before = (int[])f.State.진영들[0].재화.Clone();
                    f.Train(school, 6); f.Step(2.2f);
                    Require(worker.생존 && worker.종류 == 대상종류.주민 && worker.훈련소 < 0
                        && f.State.진영들[0].훈련대기인원 == 0
                        && 전장규칙.수량(f.State, 0, 대상종류.병사) == 0
                        && before.SequenceEqual(f.State.진영들[0].재화), "Resident skipped normal soldier training");
                }
            });
            foreach (bool archer in new[] { false, true })
            {
                Check((archer ? "Archer" : "Bruiser") + " requires resident to soldier to specialist progression", () => {
                    var f = new Fixture(); int school = f.School(archer); f.Unit(6, 0, f.Entry);
                    f.Place(4, 0, 대상종류.훈련소, f.Entry + Vector2.up, 320);
                    f.Train(4, 6); f.Step(6.1f);
                    var soldier = f.State.대상들[전장규칙.병사시작칸];
                    Require(soldier is { 생존: true, 종류: 대상종류.병사, 병과: 병과.일반 }, "Normal school did not produce a normal soldier");
                    f.Train(school, 전장규칙.병사시작칸); f.Step(2.1f);
                    Require(f.State.대상들[전장규칙.병사시작칸].병과 == (archer ? 병과.아처 : 병과.브루저), "Soldier did not specialize");
                });
                Check((archer ? "Archer" : "Bruiser") + " exactly two seconds, stat gain, health ratio, duplicate prevention", () => {
                    var f = new Fixture(); var school = f.School(archer); var worker = f.Unit(전장규칙.병사시작칸, 0, f.Entry, true);
                    worker.체력 = 52.5f;
                    f.Train(school, 전장규칙.병사시작칸, 전장규칙.병사시작칸); f.Step(1.9f);
                    Require(worker.생존 && worker.훈련남음 > 0 && f.State.진영들[0].훈련대기인원 == 1, "Early conversion or duplicate queue");
                    f.Step(.15f);
                    var soldier = f.State.대상들[전장규칙.병사시작칸];
                    Require(!worker.생존 && soldier != null && soldier.병과 == (archer ? 병과.아처 : 병과.브루저), "Wrong trained class");
                    var stats = 전장규칙.능력치(soldier);
                    Require(stats.힘 >= 7 && stats.민첩 >= 6 && soldier.최대체력 > 105 && Mathf.Abs(soldier.체력 / soldier.최대체력 - .5f) < .001f, "Missing stats or damage ratio changed");
                    f.Train(school, 전장규칙.병사시작칸); Require(soldier.훈련소 < 0, "Repeated upgrade allowed");
                });
            }
            Check("Active training rejects move/attack/gather; destroyed school releases and refunds", () => {
                var f = new Fixture(); int school = f.School(true); var worker = f.Unit(전장규칙.병사시작칸, 0, f.Entry, true);
                f.Train(school, 전장규칙.병사시작칸); f.Step(.1f);
                f.Rules.명령(0, new 전장명령 { 종류 = 명령종류.부대이동, 위치 = f.Entry + Vector2.right * 4, 부대 = new[] { 전장규칙.병사시작칸 } });
                f.Rules.명령(0, new 전장명령 { 종류 = 명령종류.공격, 값 = 전장규칙.왕번호(1), 부대 = new[] { 전장규칙.병사시작칸 } });
                f.Rules.명령(0, new 전장명령 { 종류 = 명령종류.채집, 값 = 0, 부대 = new[] { 전장규칙.병사시작칸 } });
                Require(worker.훈련소 == school && !worker.이동중, "Active trainee accepted command");
                f.State.대상들[school].체력 = 0; f.Step(.05f);
                Require(worker.생존 && worker.훈련소 < 0 && worker.훈련남음 == 0 && f.State.진영들[0].재화[0] == 100000, "Destroyed school lost unit or reservation");
            });
            Check("Existing soldier upgrades in its own slot at full army capacity", () => {
                var f = new Fixture(); int school = f.School(false);
                for (int i = 0; i < 전장규칙.병력제한; i++) f.Unit(전장규칙.병사시작칸 + i, 0, f.Entry + Vector2.right * 5, true);
                f.State.대상들[전장규칙.병사시작칸].위치 = f.Entry;
                f.Train(school, 전장규칙.병사시작칸); f.Step(2.1f);
                Require(f.State.대상들[전장규칙.병사시작칸].병과 == 병과.브루저 && 전장규칙.수량(f.State, 0, 대상종류.병사) == 48, "Upgrade consumed additional army capacity");
            });
            Check("Enemy and unfinished schools reject orders without charge", () => {
                var f = new Fixture(); int school = f.School(true); f.Unit(전장규칙.병사시작칸, 0, f.Entry, true);
                f.State.대상들[school].팀 = 1; f.Train(school, 전장규칙.병사시작칸);
                f.State.대상들[school].팀 = 0; f.State.대상들[school].완공대기 = 1; f.Train(school, 전장규칙.병사시작칸);
                Require(f.State.진영들[0].훈련대기인원 == 0 && f.State.진영들[0].재화[0] == 100000, "Invalid school took payment");
            });
            Check("Tower requires archer, consumes exactly one plus 80 wood, finishes and fires delayed arrows", () => {
                var f = new Fixture(); var p = f.BuildPoint(전장규칙.브루저건설);
                f.Rules.명령(0, new 전장명령 { 종류 = 명령종류.건설, 값 = 전장규칙.타워건설, 위치 = p });
                Require(전장규칙.수량(f.State, 0, 대상종류.아처타워) == 0, "Tower without archer");
                var a = f.Unit(전장규칙.병사시작칸, 0, f.Entry, true); a.병과 = 병과.아처;
                var b = f.Unit(전장규칙.병사시작칸 + 1, 0, f.Entry, true); b.병과 = 병과.아처;
                f.Rules.명령(0, new 전장명령 { 종류 = 명령종류.건설, 값 = 전장규칙.타워건설, 위치 = p });
                var tower = f.State.대상들[전장규칙.시설시작(0) + 4];
                Require(tower != null && !a.생존 && b.생존 && f.State.진영들[0].재화[0] == 99920, "Wrong tower materials");
                f.Step(6.1f); Require(tower.완공대기 <= 0, "Tower never completed");
                var enemy = f.Unit(전장규칙.왕번호(1) + 6, 1, p + Vector2.right * 3); enemy.속박 = 99;
                f.Step(.05f); Require(tower.공격순번 > 0 && enemy.체력 == 65, "No windup or instant damage");
                f.Step(.7f); Require(enemy.체력 < 65, "Tower arrow never hit");
            });
            Check("Mine is walkable, ignores allies, triggers once and damages enemies in radius", () => {
                var f = new Fixture(); var mine = f.Place(전장규칙.시설시작(0) + 8, 0, 대상종류.지뢰, f.Entry, 90);
                var ally = f.Unit(6, 0, f.Entry); f.Step(.05f); Require(mine.생존 && f.CanWalk(f.Entry), "Mine blocked ground or triggered on ally");
                var enemy = f.Unit(전장규칙.왕번호(1) + 6, 1, f.Entry + Vector2.right * .4f); enemy.속박 = 99;
                f.Step(.05f); Require(!mine.생존 && !enemy.생존 && ally.체력 == 65, "Mine damage/ownership failed");
            });
            Check("Pad launches allies to traversable ground with cooldown and replicated arc", () => {
                var f = new Fixture(); var p = f.Entry;
                f.Place(전장규칙.시설시작(0) + 16, 0, 대상종류.점프패드, p, 90);
                var ally = f.Unit(6, 0, p); ally.시선 = Vector2.right;
                var enemy = f.Unit(전장규칙.왕번호(1) + 6, 1, p + Vector2.up * .2f); enemy.속박 = 99;
                f.Step(.05f);
                Require(ally.도약대기 > 0 && ally.도약시간 > 0 && ally.도약출발 == p && f.CanWalk(ally.위치), "Invalid pad landing/arc");
                Require(enemy.도약대기 == 0 && (ally.위치 - p).magnitude >= 1, "Pad launched enemy or failed to move ally");
            });
            Check("Facility snapshot preserves specialization, lock and leap", () => {
                var f = new Fixture(); var u = f.Unit(6, 0, f.Entry); u.병과 = 병과.아처; u.훈련남음 = 1.5f; u.도약시간 = .3f;
                string json = JsonUtility.ToJson(f.State);
                byte[] data = 전장상태압축.포장(json);
                var copy = JsonUtility.FromJson<전장상태>(전장상태압축.풀기(data, System.Text.Encoding.UTF8.GetByteCount(json)));
                Require(copy.대상들[6].병과 == 병과.아처 && copy.대상들[6].훈련남음 == 1.5f && copy.대상들[6].도약시간 == .3f, "Missing snapshot fields");
            });
            Check("Scene references, directional art, distinct authored slots, five functional menu entries", () => {
                var game = UnityEngine.Object.FindFirstObjectByType<신들의전쟁>();
                var slots = (SpriteRenderer[])Field(game, "시설슬롯");
                Require(slots.Length == 48 && slots.All(s => s != null && s.sprite != null) && slots.Distinct().Count() == 48, "Authored facilities missing");
                Require(((Sprite[])Field(game, "시설그림")).Length == 80 && ((Sprite[])Field(game, "병과그림")).Length == 32, "Directional art incomplete");
                var buttons = (메뉴버튼[])Field(game, "시설버튼");
                Require(buttons.Length == 5 && buttons.All(b => b != null && b.onClick.GetPersistentEventCount() == 0), "Menu miswired");
            });
            results.Add("ALL PASSED");
        }
        catch (Exception e) { results.Add("FAILED " + e); }
        Directory.CreateDirectory("Temp/FacilityWork"); File.WriteAllLines("Temp/FacilityWork/results.txt", results);
        Debug.Log(string.Join("\n", results));
    }

    const string PreviewKey = "DivineWar.FacilityPreview";
    [MenuItem("DivineWar/Validation/Facility Regressions")]
    public static void Regressions()
    {
        Directory.CreateDirectory("Temp/FacilityWork");
        try
        {
            TrainingQueueChecks.Run();
            ArmySelectionChecks.Run();
            ClassCombatChecks.Run();
            File.WriteAllText("Temp/FacilityWork/regressions.txt", "Existing training, army selection, and class combat checks completed without exceptions. See each suite output in Console.\n");
        }
        catch (Exception e) { File.WriteAllText("Temp/FacilityWork/regressions.txt", "FAILED " + e); throw; }
    }
    [MenuItem("DivineWar/Facilities/Preview In Game")]
    public static void Preview()
    {
        if (EditorApplication.isPlaying || UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty)
            throw new Exception("Preview needs saved Edit Mode scene.");
        SessionState.SetBool(PreviewKey, true);
        EditorApplication.update -= PreviewTick; EditorApplication.update += PreviewTick;
        EditorApplication.isPaused = false;
        EditorApplication.isPlaying = true;
    }
    [InitializeOnLoadMethod]
    static void RestorePreview() { if (SessionState.GetBool(PreviewKey, false)) { EditorApplication.update -= PreviewTick; EditorApplication.update += PreviewTick; } }
    static void PreviewTick()
    {
        if (!EditorApplication.isPlaying || Time.timeSinceLevelLoad < 1) return;
        var game = UnityEngine.Object.FindFirstObjectByType<신들의전쟁>();
        if (game == null || Field(game, "규칙") is not 전장규칙 rules) return;
        SessionState.EraseBool(PreviewKey); EditorApplication.update -= PreviewTick;
        var state = rules.상태;
        Vector2 p = state.대상들[0].위치;
        var place = typeof(전장규칙).GetMethod("배치", Private);
        int[] offsets = { 0, 2, 4, 8, 16 };
        Vector2[] positions = { new(-4, 1), new(-.5f, 1), new(3, 1), new(-1.8f, -2), new(1.8f, -2) };
        for (int i = 0; i < 5; i++) place.Invoke(rules, new object[] { 전장규칙.시설시작(0) + offsets[i], 0, 전장규칙.시설종류(7 + i), p + positions[i], 320f });
        typeof(전장규칙).GetField("적명령시간", Private).SetValue(rules, 999f);
        typeof(신들의전쟁).GetField("자동추적", Private).SetValue(game, false);
        typeof(신들의전쟁).GetField("일시정지", Private).SetValue(game, true);
        var camera = (Camera)Field(game, "전장카메라");
        camera.transform.position = new Vector3(p.x, p.y + .5f, -10); camera.orthographicSize = 6;
        ((GameObject)Field(game, "건축목록")).SetActive(true);
        typeof(신들의전쟁).GetMethod("장면반영", Private).Invoke(game, new object[] { .016f });
        EditorApplication.isPaused = true;
        Debug.Log("Facility preview ready: authored slots, five buildings, new wood menu. Play Mode only; no scene changes saved.");
    }

    sealed class Fixture
    {
        public readonly 전장규칙 Rules;
        public 전장상태 State => Rules.상태;
        public readonly 격자길찾기 Nav;
        public readonly Vector2 Entry;
        readonly 자원지점[] resources;
        public Fixture()
        {
            var source = UnityEngine.Object.FindFirstObjectByType<신들의전쟁>();
            Nav = new 격자길찾기((Tilemap)Field(source, "바닥타일맵"), (Tilemap)Field(source, "장애물타일맵"));
            resources = (자원지점[])Field(source, "자원들");
            Rules = new 전장규칙(resources, Nav, Array.ConvertAll((Transform[])Field(source, "시작지점들"), t => (Vector2)t.position), 왕직업.수호자, 왕직업.수호자, 374);
            foreach (var t in State.진영들) { Array.Fill(t.재화, 100000); t.제재시간 = 99999; t.심부름 = 4; }
            foreach (var u in State.대상들) if (u != null) { u.표적 = -1; u.공격대기 = 999; if (u.종류 == 대상종류.주민) u.체력 = 0; }
            Entry = BuildPoint(전장규칙.브루저건설) + Vector2.down;
        }
        public Vector2 BuildPoint(int kind)
        {
            var hall = State.대상들[1].위치;
            for (int y = -5; y <= 5; y++) for (int x = -5; x <= 5; x++) {
                var p = hall + new Vector2(x, y);
                if (!CanWalk(p + Vector2.down) || !CanWalk(p + Vector2.down + Vector2.right * 4)) continue;
                object[] args = { State, Nav, resources, 0, kind, p, Vector2.zero };
                if ((bool)typeof(전장규칙).GetMethod("건설가능", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args)) return (Vector2)args[6];
            }
            throw new Exception("No fixture building point");
        }
        public bool CanWalk(Vector2 p) => (bool)typeof(격자길찾기).GetMethod("통행가능", Private, null, new[] { typeof(Vector2) }, null).Invoke(Nav, new object[] { p });
        public 전장대상 Place(int index, int team, 대상종류 kind, Vector2 p, float hp) => (전장대상)typeof(전장규칙).GetMethod("배치", Private).Invoke(Rules, new object[] { index, team, kind, p, hp });
        public 전장대상 Unit(int index, int team, Vector2 p, bool soldier = false) => Place(index, team, soldier ? 대상종류.병사 : 대상종류.주민, p, soldier ? 105 : 65);
        public int School(bool archer) { int index = 전장규칙.시설시작(0) + (archer ? 2 : 0); Place(index, 0, archer ? 대상종류.아처훈련소 : 대상종류.브루저훈련소, Entry + Vector2.up, 320); return index; }
        public void Train(int school, params int[] units) => Rules.명령(0, new 전장명령 { 종류 = 명령종류.주민훈련, 값 = school, 부대 = units });
        public void Step(float seconds) { for (int i = 0; i < Mathf.RoundToInt(seconds / .05f); i++) Rules.진행(.05f, false); }
    }
}
