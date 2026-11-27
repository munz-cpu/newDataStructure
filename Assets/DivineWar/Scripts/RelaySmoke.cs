#if DIVINEWAR_RELAY_SMOKE && !UNITY_EDITOR && !DIVINEWAR_PRODUCTION
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

static class RelaySmoke
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static async void Start()
    {
        string dir = Environment.GetEnvironmentVariable("DIVINEWAR_SMOKE_DIR");
        if (string.IsNullOrEmpty(dir)) return;
        string role = Environment.GetEnvironmentVariable("DIVINEWAR_SMOKE_ROLE");
        try
        {
            if (role == "offline")
            {
                var title = UnityEngine.Object.FindFirstObjectByType<타이틀화면>();
                Field<Button>(title, "시작버튼").onClick.Invoke();
                await Until(() => SceneManager.GetActiveScene().name == "Main", "enter solo game");
                var game = UnityEngine.Object.FindFirstObjectByType<신들의전쟁>();
                await Until(() => Field<bool>(game, "전투중") && Field<전장상태>(game, "상태").시간 > 0,
                    "advance solo simulation");
                if (Field<bool>(game, "멀티") || UnityServices.State != ServicesInitializationState.Uninitialized)
                    throw new InvalidOperationException("Solo game initialized online services.");
                SceneManager.LoadScene("Title");
                await Until(() => UnityEngine.Object.FindFirstObjectByType<타이틀화면>() != null, "leave solo game");
                File.WriteAllText(Path.Combine(dir, "offline.passed"), "Solo game passed.");
                Application.Quit(0);
                return;
            }
            if (role is not ("first" or "second" or "host" or "guest")) throw new ArgumentException("Invalid smoke player role.");
            for (int round = 1; round <= 2; round++)
                await PlayRound(dir, round, role);
            File.WriteAllText(Path.Combine(dir, role + ".passed"), "Both games passed.");
            Application.Quit(0);
        }
        catch (Exception e)
        {
            Debug.LogError($"Relay smoke failed ({role}): {e}");
            Application.Quit(1);
        }
    }

    static async Task PlayRound(string root, int round, string role)
    {
        string dir = Path.Combine(root, round.ToString());
        Directory.CreateDirectory(dir);
        var title = UnityEngine.Object.FindFirstObjectByType<타이틀화면>();
        var input = Field<TMP_InputField>(title, "참가코드");
        string codeFile = Path.Combine(dir, "host.code");
        if (role == "host")
        {
            var create = Field<Button>(title, "방생성버튼");
            if (((RectTransform)create.transform).anchoredPosition.y <= input.GetComponent<RectTransform>().anchoredPosition.y)
                throw new InvalidOperationException("The create button must be above the invitation input.");
            create.onClick.Invoke();
            await Until(() => Field<참가연결>(title, "연결")?.방주인 == true && !string.IsNullOrEmpty(input.text),
                "create a room and show its invitation", title);
            if (create.interactable || !input.readOnly)
                throw new InvalidOperationException("Room creation must be disabled while waiting for a guest.");
            Publish(codeFile, input.text);
            var start = Field<Button>(title, "멀티시작버튼");
            await Until(() => start != null && start.gameObject.activeInHierarchy && start.interactable,
                "wait for guest ready to start", title);
            start.onClick.Invoke();
        }
        else
        {
            string invitation = null;
            await Until(() => (invitation = role == "guest"
                    ? File.Exists(codeFile) ? File.ReadAllText(codeFile) : null
                    : ServerCode(Path.Combine(root, "server.log"), round)) != null,
                "wait for a room invitation");
            input.text = invitation;
            Field<Button>(title, "참가버튼").onClick.Invoke();
        }

        await Until(() => SceneManager.GetActiveScene().name == "Main", "enter Main", title);
        var game = UnityEngine.Object.FindFirstObjectByType<신들의전쟁>();
        await Until(() => Field<bool>(game, "전투중"), "receive initial battle state");
        int team = Field<int>(game, "내팀");
        bool authority = Field<전장규칙>(game, "규칙") != null;
        if (team is < 0 or > 1 || authority != (role == "host") || (role == "host" && team != 0))
            throw new InvalidOperationException("Unexpected team or simulation authority.");

        string other = role switch { "first" => "second", "second" => "first", "host" => "guest", _ => "host" };
        Publish(Path.Combine(dir, role + ".team"), team.ToString());
        string peerTeam = Path.Combine(dir, other + ".team");
        await Until(() => File.Exists(peerTeam), "peer team assignment");
        if (File.ReadAllText(peerTeam) == team.ToString())
            throw new InvalidOperationException("Both clients received the same team.");
#if !DIVINEWAR_LOCAL_QA
        Publish(Path.Combine(dir, role + ".id"), AuthenticationService.Instance.PlayerId);
        string otherId = Path.Combine(dir, other + ".id");
        await Until(() => File.Exists(otherId), "peer authentication");
        if (string.IsNullOrEmpty(AuthenticationService.Instance.PlayerId) ||
            AuthenticationService.Instance.PlayerId == File.ReadAllText(otherId))
            throw new InvalidOperationException("Expected two distinct authenticated players.");
#endif

        // The server processes the menu action from each team and replicates the notice to both clients.
        game.행동고르기(3);
        await Until(() =>
        {
            var teams = Field<전장상태>(game, "상태").진영들;
            return teams[0].알림 == "주민을 선택하고 아군 훈련소로 보내세요." &&
                teams[1].알림 == "주민을 선택하고 아군 훈련소로 보내세요.";
        }, "both server-processed commands and replicated responses");
        Publish(Path.Combine(dir, role + ".received"), "ok");
        await Until(() => File.Exists(Path.Combine(dir, other + ".received")), "peer observation");
        SceneManager.LoadScene("Title");
        await Until(() => UnityEngine.Object.FindFirstObjectByType<타이틀화면>() != null, "return to title");
    }

    static string ServerCode(string log, int round)
    {
        if (!File.Exists(log)) return null;
        using var stream = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        const string prefix = "[DivineWar] JOIN_CODE=";
        int found = 0;
        while (reader.ReadLine() is string line)
        {
            int index = line.IndexOf(prefix, StringComparison.Ordinal);
            if (index < 0 || ++found != round) continue;
            string code = line.Substring(index + prefix.Length).Trim();
            return code.Length > 0 ? code : null;
        }
        return null;
    }

    static T Field<T>(object target, string name) =>
        (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

    static async Task Until(Func<bool> ready, string step, 타이틀화면 title = null)
    {
        float start = Time.realtimeSinceStartup;
        while (!ready())
        {
            if (Time.realtimeSinceStartup - start > 50)
            {
                string status = title ? Field<TMP_Text>(title, "방상태").text : "";
                throw new TimeoutException($"{step}: {status}");
            }
            await Task.Delay(50);
        }
    }

    static void Publish(string path, string value)
    {
        File.WriteAllText(path + ".tmp", value);
        File.Move(path + ".tmp", path);
    }
}
#endif
