#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[InitializeOnLoad]
internal static class CodexCommandFeedbackProbeTemp
{
    const string Running = "CodexCommandFeedbackProbeTemp.Running";
    const string Report = "Temp/CodexCommandFeedbackProbe.txt";
    static int phase, frames;
    static float clicked;
    static 신들의전쟁 battle;
    static 전장규칙 rules;
    static 전장대상 king;
    static int team;
    static Vector2 point;
    static readonly List<Vector2> route = new();
    static CodexCommandFeedbackProbeTemp() { EditorApplication.update += Tick; }

    [MenuItem("Tools/Codex Verify Command Feedback Temporarily")]
    static void Run()
    {
        File.WriteAllText(Report, "");
        frames = phase = 0;
        point = Vector2.zero;
        SessionState.SetBool(Running, true);
        EditorApplication.EnterPlaymode();
    }

    static FieldInfo Field(string name) => typeof(신들의전쟁).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
    static MethodInfo Method(string name) => typeof(신들의전쟁).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Record(string value) => File.AppendAllText(Report, value + Environment.NewLine);

    static void RightClick(VisualElement element, Vector2 panelPoint)
    {
        var input = new Event { type = EventType.MouseDown, button = 1, mousePosition = panelPoint };
        using var down = PointerDownEvent.GetPooled(input);
        down.target = element;
        element.SendEvent(down);
        input.type = EventType.MouseUp;
        using var up = PointerUpEvent.GetPooled(input);
        up.target = element;
        element.SendEvent(up);
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying || !SessionState.GetBool(Running, false)) return;
        try
        {
            if (++frames > 1200) throw new Exception("Timeout at phase " + phase);
            if (phase == 0)
            {
                Application.runInBackground = true;
                if (!SceneManager.GetSceneByName("Main").isLoaded) SceneManager.LoadScene("Main");
                phase++;
            }
            else if (phase == 1)
            {
                battle = UnityEngine.Object.FindAnyObjectByType<신들의전쟁>();
                if (frames == 90) Record($"Warmup: {SceneManager.GetActiveScene().name} / battle {battle != null}, frame {Time.frameCount}");
                if (battle == null) return;
                rules = (전장규칙)Field("규칙").GetValue(battle);
                var root = battle.GetComponent<UIDocument>().rootVisualElement;
                var draw = root.Q("draw");
                if (frames == 90) Record($"Warmup: rules {rules != null} / draw {draw.worldBound} / map {root.Q("minimap").worldBound}");
                if (rules == null || !(draw.worldBound.width > 0)) return;
                team = (int)Field("내팀").GetValue(battle);
                king = rules.상태.대상들[전장규칙.왕번호(team)];
                var grid = (격자길찾기)Field("길찾기").GetValue(battle);
                var map = root.Q("minimap");
                if (!(map.worldBound.width > 0)) return;
                float sign = -Mathf.Sign(king.위치.x);
                foreach (float dy in new[] { 2.1f, -2.1f, 0f, 3.4f, -3.4f })
                {
                    var candidate = king.위치 + new Vector2(sign * 8.3f, dy);
                    if (!grid.정적통행가능(candidate)) continue;
                    grid.구하기(king.위치, candidate, route);
                    if (route.Count < 4) continue;
                    point = candidate;
                    break;
                }
                Check(point != Vector2.zero, "reachable map destination");
                Field("키보드이동").SetValue(battle, false);
                var bounds = 전장안개.전장범위;
                var rect = map.contentRect;
                var onMap = new Vector2(rect.x + Mathf.InverseLerp(bounds.xMin, bounds.xMax, point.x) * rect.width,
                    rect.y + Mathf.InverseLerp(bounds.yMax, bounds.yMin, point.y) * rect.height);
                RightClick(map, map.LocalToWorld(onMap));
                clicked = Time.unscaledTime;
                Check(king.이동중 && Vector2.Distance(king.목적지, point) < .015f, "minimap click sets destination");
                Check((bool)Field("지도에서명령").GetValue(battle) && (float)Field("명령표시시작").GetValue(battle) == clicked,
                    "minimap click starts indicator");
                Field("명령표시시작").SetValue(battle, clicked - .02f);
                ScreenCapture.CaptureScreenshot("Temp/CodexCommandFeedbackEarly.png");
                phase++;
            }
            else if (phase == 2 && Time.unscaledTime - clicked >= .19f)
            {
                rules.왕이동경로복사(team, route);
                Check(route.Count > 2 && Vector2.Distance(route[^1], point) < .015f, "remaining route has waypoints and exact endpoint");
                rules.상태.손님왕경로 = route;
                var decoded = JsonUtility.FromJson<전장상태>(JsonUtility.ToJson(rules.상태));
                rules.상태.손님왕경로 = null;
                Check(decoded.손님왕경로.Count == route.Count && decoded.손님왕경로[^1] == route[^1], "route survives state serialization");
                Field("명령표시시작").SetValue(battle, Time.unscaledTime - .22f);
                ScreenCapture.CaptureScreenshot("Temp/CodexCommandFeedbackSettled.png");
                Record("PASS: pointer destination, click feedback, route waypoints, exact endpoint, guest serialization");
                phase++;
            }
            else if (phase == 3 && Time.unscaledTime - clicked >= .9f)
            {
                ScreenCapture.CaptureScreenshot("Temp/CodexCommandFeedbackExpired.png");
                phase++;
            }
            else if (phase == 4 && !king.이동중)
            {
                rules.왕이동경로복사(team, route);
                Check(route.Count == 0, "completed route clears");
                var draw = battle.GetComponent<UIDocument>().rootVisualElement.Q("draw");
                var other = king.위치 + Vector2.down * 2;
                var panelPoint = (Vector2)Method("위치변환").Invoke(battle, new object[] { other });
                RightClick(draw, panelPoint);
                Check(!(bool)Field("지도에서명령").GetValue(battle) && (float)Field("명령표시시작").GetValue(battle) == Time.unscaledTime,
                    "field click restarts indicator");
                rules.명령(team, new 전장명령 { 종류 = 명령종류.방향이동, 위치 = Vector2.right });
                rules.왕이동경로복사(team, route);
                Check(route.Count == 0, "WASD cancels the click route");
                Record("PASS: route cleared on arrival and on WASD, battlefield click restarts the indicator");
                Finish();
            }
        }
        catch (Exception error)
        {
            Record(error.ToString());
            Finish();
        }
    }

    static void Finish()
    {
        SessionState.EraseBool(Running);
        EditorApplication.ExitPlaymode();
    }
}
#endif
