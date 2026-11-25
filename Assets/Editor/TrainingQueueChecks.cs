using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// Uses authored Main scene terrain; creates only isolated battle-state data, never scene objects.
public static class TrainingQueueChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly List<string> Results = new();

    [MenuItem("DivineWar/Validation/Training Queue")]
    public static void Run()
    {
        Results.Clear();
        try
        {
            Check("FIFO conversion and training duration", () =>
            {
                var f = new Fixture();
                var first = f.Worker(6); var second = f.Worker(7);
                first.체력 = first.최대체력 / 2;
                f.Train(6, 7, 6);
                Require(f.Team.훈련대기인원 == 2, "Duplicate selection queued twice");
                Require(f.Team.재화[0] == 100000 - 15 - 16, "Second reservation did not increase the training cost");
                f.Step(4.5f);
                Require(f.Soldiers == 0 && first.생존 && second.생존, "Training completed early");
                f.Step(.2f);
                Require(f.Soldiers == 1 && !first.생존 && second.생존, "FIFO order violated");
                Require(Mathf.Abs(f.State.대상들[전장규칙.병사시작칸].체력 - 70f) < .01f, "Conversion healed damage");
                f.Step(4.7f);
                Require(f.Soldiers == 1 && second.생존, "Second resident trained at the first resident's speed");
                f.Step(.25f);
                Require(f.Soldiers == 2 && !second.생존 && f.Team.훈련대기인원 == 0, "Second conversion failed");
            });
            Check("Travel before training", () =>
            {
                var f = new Fixture(); var worker = f.Worker(6);
                worker.위치 += Vector2.right * 5;
                f.Train(6); f.Rules.진행(.05f, false);
                Require(Mathf.Abs(f.Team.훈련남은시간 - 6f / 1.3f) < .01f, "Timer ran before arrival");
                f.Step(20);
                Require(f.Soldiers == 1 && !worker.생존, "Resident failed to walk to training school");
            });
            Check("Per-school queue count and completion time follow shared FIFO", () =>
            {
                var f = new Fixture();
                var school = f.State.대상들[4];
                var otherSchool = f.Place(5, 0, 대상종류.훈련소, school.위치 + Vector2.right * 2, 320);
                f.Worker(6);
                var second = f.Worker(7);
                second.위치 = otherSchool.위치 + Vector2.down;
                f.Train(6);
                f.Rules.명령(0, new 전장명령 { 종류 = 명령종류.주민훈련, 값 = 5, 부대 = new[] { 7 } });
                Require(school.훈련대기수 == 1 && otherSchool.훈련대기수 == 1, "School counts include the other school's residents");
                Require(Mathf.Abs(school.훈련잔여합계 - 6f / 1.3f) < .01f
                    && Mathf.Abs(otherSchool.훈련잔여합계 - 12.3f / 1.3f) < .01f, "Second school ETA ignored the earlier order");
                f.Step(1);
                var copy = JsonUtility.FromJson<전장상태>(JsonUtility.ToJson(f.State));
                Require(copy.대상들[5].훈련대기수 == 1 && Mathf.Abs(copy.대상들[5].훈련잔여합계 - (12.3f / 1.3f - 1)) < .01f,
                    "Replicated school did not retain the updated countdown");
                school.체력 = 0;
                f.Rules.진행(0, false);
                Require(school.훈련대기수 == 0 && school.훈련잔여합계 == 0
                    && Mathf.Abs(otherSchool.훈련잔여합계 - 6.3f / 1.3f) < .01f, "Cancellation did not refresh both schools");
                f.Step(6.4f);
                Require(otherSchool.훈련대기수 == 0 && otherSchool.훈련잔여합계 == 0, "Completion left stale school state");
            });
            Check("Higher intelligence trains faster and carries into the soldier", () =>
            {
                var low = new Fixture(); var skilled = new Fixture();
                var worker = skilled.Worker(6);
                worker.힘 = 2; worker.민첩 = 4; worker.지능 = 16;
                low.Worker(6); low.Train(6); skilled.Train(6);
                Require(skilled.Team.훈련남은시간 < low.Team.훈련남은시간, "Intelligence did not shorten the reservation");
                low.Step(3.6f); skilled.Step(3.6f);
                Require(low.Soldiers == 0 && skilled.Soldiers == 1
                    && skilled.State.대상들[전장규칙.병사시작칸].지능 == 16, "Higher intelligence did not finish earlier or carry over");
            });
            Check("Late training acceleration ramps up but caps at 25 percent", () =>
            {
                var early = new Fixture(); var mid = new Fixture(); var late = new Fixture(); var extended = new Fixture();
                early.State.시간 = 360; mid.State.시간 = 660; late.State.시간 = 960; extended.State.시간 = 2400;
                foreach (var fixture in new[] { early, mid, late, extended }) { fixture.Worker(6); fixture.Train(6); }
                Require(early.Team.훈련남은시간 > mid.Team.훈련남은시간
                    && mid.Team.훈련남은시간 > late.Team.훈련남은시간, "Later match time did not shorten the countdown");
                Require(Mathf.Approximately(late.Team.훈련남은시간, early.Team.훈련남은시간 / 1.25f)
                    && Mathf.Approximately(extended.Team.훈련남은시간, late.Team.훈련남은시간), "Late training cap was not applied");
                late.Step(3.6f); extended.Step(3.6f);
                Require(late.Soldiers == 0 && extended.Soldiers == 0, "Late trainees became soldiers too early");
                late.Step(.2f); extended.Step(.2f); early.Step(3.8f);
                Require(early.Soldiers == 0 && late.Soldiers == 1 && extended.Soldiers == 1, "Capped training did not complete on schedule");
            });
            Check("New command cancels and refunds", () =>
            {
                var f = new Fixture(); var worker = f.Worker(6); f.Train(6);
                f.Rules.명령(0, new 전장명령 { 종류 = 명령종류.부대이동, 위치 = f.Entry, 부대 = new[] { 6 } });
                f.Rules.진행(0, false);
                Require(f.Team.훈련대기인원 == 0 && worker.생존 && worker.훈련소 == -1, "Order did not cancel");
                Require(f.Team.재화[0] == 100000 && f.Team.재화[4] == 100000, "Incomplete refund");
            });
            Check("Destroyed school releases all residents", () =>
            {
                var f = new Fixture(); f.Worker(6); f.Worker(7); f.Train(6, 7);
                f.Step(6.1f);
                f.State.대상들[전장규칙.병사시작칸].체력 = 0;
                f.State.대상들[4].체력 = 0; f.Rules.진행(0, false);
                Require(f.Team.훈련대기인원 == 0 && f.Team.재화[0] == 99985 && f.Soldiers == 0, "Destroyed school did not refund the second resident's original cost");
                Require(f.State.대상들[7].훈련소 < 0, "Waiting resident was not released");
            });
            Check("Dead resident and reused slot", () =>
            {
                var f = new Fixture(); var first = f.Worker(6); f.Worker(7); f.Train(6, 7);
                first.체력 = 0; var replacement = f.Worker(6);
                f.Step(6.4f);
                Require(replacement.생존 && f.Soldiers == 1 && !f.State.대상들[7].생존, "Reused resident slot consumed old order");
            });
            Check("Rejected orders preserve residents and money", () =>
            {
                var f = new Fixture(); f.Worker(6);
                f.Train();
                f.Rules.명령(1, new 전장명령 { 종류 = 명령종류.주민훈련, 값 = 4, 부대 = new[] { 6 } });
                f.State.대상들[4].완공대기 = 3; f.Train(6);
                Require(f.Team.훈련대기인원 == 0 && f.Team.재화[0] == 100000, "Invalid order charged resources");
                f.State.대상들[4].완공대기 = 0; f.Team.재화[4] = 0; f.Train(6);
                Require(f.Team.훈련대기인원 == 0 && f.Team.재화[0] == 100000, "Unaffordable training partially charged");
                f.Rules.명령(0, new 전장명령 { 종류 = 명령종류.모집, 값 = 1 }); f.Step(7);
                Require(f.Soldiers == 0, "Old recruit command spawned a soldier");
            });
            Check("Population slots and reserved army capacity", () =>
            {
                var f = new Fixture();
                for (int i = 0; i < 전장규칙.주민제한; i++) f.Worker(6 + i);
                for (int i = 0; i < 전장규칙.병력제한 - 1; i++) f.Place(전장규칙.병사시작칸 + i, 0, 대상종류.병사, f.Entry + Vector2.right * 4, 105);
                f.Train(6, 7);
                Require(f.Team.훈련대기인원 == 1 && f.Team.재화[0] == 99950, "Queue exceeded army cap or did not price the last slot correctly");
                Require(전장규칙.수량(f.State, 0, 대상종류.주민) == 24 && f.Soldiers == 47, "Resident and soldier slots overlap");
                f.Step(6.1f);
                Require(f.Soldiers == 47, "Training time did not scale with existing soldiers");
                f.Step(14.1f);
                Require(f.Soldiers == 48 && 전장규칙.수량(f.State, 0, 대상종류.주민) == 23, "Capacity conversion failed");
            });
            Check("Snapshot size and training replication", () =>
            {
                var f = new Fixture(); f.Worker(6); f.Train(6);
                for (int i = 0; i < f.State.대상들.Length; i++)
                    f.State.대상들[i] ??= new 전장대상 { 팀 = 1, 종류 = 대상종류.병사, 체력 = 105, 최대체력 = 105 };
                string json = JsonUtility.ToJson(f.State);
                string message = "S" + json;
                int rawBytes = Encoding.UTF8.GetByteCount(message);
                var payload = 전장상태압축.포장(message);
                string received = 전장상태압축.풀기(payload, rawBytes);
                Require(received == message, "Snapshot codec changed the battle state");
                var copy = JsonUtility.FromJson<전장상태>(received.Substring(1));
                Require(copy.진영들[0].훈련대기인원 == 1 && copy.대상들[6].훈련소 == 4, "Training fields missing in snapshot");
                Require(rawBytes <= 전장상태압축.최대원본길이, "Snapshot exceeds network frame limit");
                Results.Add($"Snapshot: {rawBytes} UTF-8 bytes, {payload.Length} compressed bytes");
            });
            Check("Snapshot size boundary and invalid declared length", () =>
            {
                var message = "S" + new string('x', 전장상태압축.최대원본길이 - 1);
                var payload = 전장상태압축.포장(message);
                Require(전장상태압축.풀기(payload, message.Length) == message, "Maximum size snapshot lost");
                RejectInvalid(() => 전장상태압축.포장(message + "x"));
                RejectInvalid(() => 전장상태압축.풀기(payload, message.Length - 1));
                RejectInvalid(() => 전장상태압축.풀기(payload, message.Length + 1));
            });
            Check("Selected unit command JSON round trip", () =>
            {
                var sent = new 전장명령 { 종류 = 명령종류.주민훈련, 위치 = new Vector2(-3.5f, 16.25f),
                    값 = 4, 부대 = new[] { 6, 7 } };
                string wire = "C" + JsonUtility.ToJson(sent);
                var copy = JsonUtility.FromJson<전장명령>(wire.Substring(1));
                Require(copy.종류 == sent.종류 && copy.위치 == sent.위치 && copy.값 == sent.값 &&
                    copy.부대.Length == 2 && copy.부대[0] == 6 && copy.부대[1] == 7, "Command fields changed");
            });
            Check("Authored renderer references", () =>
            {
                var source = UnityEngine.Object.FindFirstObjectByType<신들의전쟁>();
                var renderers = (SpriteRenderer[])typeof(신들의전쟁).GetField("슬롯", Private).GetValue(source);
                Require(renderers.Length == 전장규칙.울타리시작칸
                    || Application.isPlaying && renderers.Length == 전장규칙.전체칸, "Authored unit slot count incorrect");
                var unique = new HashSet<SpriteRenderer>();
                for (int i = 0; i < 전장규칙.울타리시작칸; i++)
                    Require(renderers[i] != null && renderers[i].sprite != null && unique.Add(renderers[i]), "Missing or duplicate renderer");
            });
            Results.Add("ALL PASSED");
        }
        catch (Exception error) { Results.Add("FAILED: " + error); }
        Directory.CreateDirectory("Temp/TrainingWork");
        File.WriteAllLines("Temp/TrainingWork/results.txt", Results);
        Debug.Log(string.Join("\n", Results));
    }

    static void RejectInvalid(Action operation)
    {
        try { operation(); }
        catch (InvalidDataException) { return; }
        throw new Exception("Expected invalid snapshot to be rejected");
    }

    static void Check(string name, Action test) { test(); Results.Add("PASS: " + name); }
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }

    sealed class Fixture
    {
        public readonly 전장규칙 Rules;
        public 전장상태 State => Rules.상태;
        public 진영상태 Team => State.진영들[0];
        public readonly Vector2 Entry;
        public int Soldiers => 전장규칙.수량(State, 0, 대상종류.병사);

        public Fixture()
        {
            var source = UnityEngine.Object.FindFirstObjectByType<신들의전쟁>();
            Require(source != null, "Open Main scene before running checks");
            object Field(string name) => typeof(신들의전쟁).GetField(name, Private).GetValue(source);
            var floor = (Tilemap)Field("바닥타일맵");
            var navigation = new 격자길찾기(floor, (Tilemap)Field("장애물타일맵"));
            var starts = (Transform[])Field("시작지점들");
            var bases = Array.ConvertAll(starts, t => (Vector2)t.position);
            Rules = new 전장규칙((자원지점[])Field("자원들"), navigation, bases, 왕직업.수호자, 왕직업.수호자, 374);
            foreach (var team in State.진영들)
            {
                Array.Fill(team.재화, 100000); team.제재시간 = 999999; team.심부름 = 4;
            }
            foreach (var unit in State.대상들)
                if (unit != null && unit.종류 == 대상종류.주민) unit.체력 = 0;
            Vector2 hall = State.대상들[1].위치;
            bool found = false;
            for (int y = -5; y <= 5 && !found; y++)
            for (int x = -5; x <= 5 && !found; x++)
            {
                var p = hall + new Vector2(x, y);
                if (x * x + y * y < 9 || !navigation.정적통행가능(p)
                    || !navigation.정적통행가능(p + Vector2.down)
                    || !navigation.정적통행가능(p + Vector2.down + Vector2.right * 5)) continue;
                Place(4, 0, 대상종류.훈련소, p, 320);
                Entry = p + Vector2.down; found = true;
            }
            Require(found, "No valid training fixture position");
        }

        public 전장대상 Place(int index, int team, 대상종류 kind, Vector2 point, float health)
            => (전장대상)typeof(전장규칙).GetMethod("배치", Private).Invoke(Rules, new object[] { index, team, kind, point, health });
        public 전장대상 Worker(int index) => Place(index, 0, 대상종류.주민, Entry, 65);
        public void Train(params int[] workers) => Rules.명령(0, new 전장명령 { 종류 = 명령종류.주민훈련, 값 = 4, 부대 = workers });
        public void Step(float seconds)
        {
            for (int i = 0; i < Mathf.RoundToInt(seconds / .05f); i++) Rules.진행(.05f, false);
        }
    }
}
