using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class ClassCombatChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static int passed;
    public static void Batch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        Run();
        EquippedSkills();
        TrainingQueueChecks.Run();
        ArmySelectionChecks.Run();
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        var title = UnityEngine.Object.FindAnyObjectByType<타이틀화면>();
        var select = typeof(타이틀화면).GetMethod("직업고르기", Private);
        var info = (TMPro.TMP_Text)typeof(타이틀화면).GetField("직업정보", Private).GetValue(title);
        var preview = (SpriteRenderer)typeof(타이틀화면).GetField("타이틀문장", Private).GetValue(title);
        const string selectedJob = "DivineWar.SelectedJob.V1";
        bool savedJob = PlayerPrefs.HasKey(selectedJob);
        int previousJob = PlayerPrefs.GetInt(selectedJob);
        try
        {
            foreach (var job in 왕직업목록.선택)
            {
                select.Invoke(title, new object[] { job });
                info.ForceMeshUpdate();
                Require(info.text.Contains(job.ToString()) && !info.isTextOverflowing, $"Title description overflow: {job}");
                Require(preview != null && preview.sprite == 왕외형목록.가져오기().전용외형(job).대기[30], $"Title preview mismatch: {job}");
                var buttons = (스킬선택버튼[])typeof(타이틀화면).GetField("스킬후보버튼", Private).GetValue(title);
                for (int i = 0; i < buttons.Length; i++)
                {
                    int number = job == 왕직업.지휘관
                        ? new[] { 1, 2, 3, 4, 6, 7, 5, 8 }[i]
                        : Array.IndexOf(왕직업목록.선택, job) * 8 + i + 1;
                    var icon = buttons[i].image.sprite;
                    Require(icon != null && icon.name.StartsWith($"{number:00}_"), $"Skill icon mismatch: {job} {i}");
                }
            }
        }
        finally
        {
            if (savedJob) PlayerPrefs.SetInt(selectedJob, previousJob);
            else PlayerPrefs.DeleteKey(selectedJob);
        }
        Debug.Log("[ClassCombatChecks] Title: job selections, skill icons, text bounds and avatar previews passed.");
    }

    [MenuItem("DivineWar/Validation/Class Combat")]
    public static void Run()
    {
        passed = 0;
        foreach (var job in 왕직업목록.이전직업) for (int skill = 0; skill < 4; skill++)
        {
            var f = new Fixture(job);
            if (job == 왕직업.아지르 && skill is 0 or 2) f.Cast(1, f.King.위치 + Vector2.right);
            f.Cast(skill, skill == 1 && job == 왕직업.소라카 ? f.Ally.위치 : f.Enemy.위치);
            Require(f.State.진영들[0].쿨다운[skill] > 0, $"{job} {skill} failed to activate");
            float cd = f.State.진영들[0].쿨다운[skill];
            f.Step(.05f);
            Require(f.State.진영들[0].쿨다운[skill] < cd, "Cooldown failed to tick");
            Require(f.State.진영들[0].재화[4] == 100, "Skill charged economy resources");
        }
        {
            var f = new Fixture(왕직업.애쉬);
            f.Cast(3, f.Enemy.위치); f.Step(.2f);
            Require(f.Enemy.기절 > 0 && f.Enemy.체력 < 500, "Ashe projectile did not stun/damage");
            var before = f.Enemy.위치;
            f.Rules.명령(1, new 전장명령 { 종류 = 명령종류.이동, 위치 = before + Vector2.up * 4 }); f.Step(.1f);
            Require(f.Enemy.위치 == before, "Stunned king moved");
        }
        {
            var f = new Fixture(왕직업.소라카);
            f.Cast(2, f.Enemy.위치); f.Step(.2f);
            Require(f.Enemy.침묵 > 0, "Silence missing");
            f.Rules.명령(1, new 전장명령 { 종류 = 명령종류.스킬, 값 = 0, 위치 = f.King.위치 });
            Require(f.State.진영들[1].쿨다운[0] == 0, "Silenced king cast");
            f.Step(1.9f); Require(f.Enemy.속박 > 0, "Equinox did not root");
            var before = f.Enemy.위치;
            f.Rules.명령(1, new 전장명령 { 종류 = 명령종류.이동, 위치 = before + Vector2.up * 4 }); f.Step(.1f);
            Require(f.Enemy.위치 == before, "Rooted king moved");
        }
        {
            var f = new Fixture(왕직업.트린다미어); f.King.기절 = 2; f.King.침묵 = 2;
            f.Cast(3, f.King.위치); f.Damage(0, 9999);
            Require(f.King.체력 == 1, "Undying failed during CC");
            f.Step(5.1f); f.Damage(0, 9999); Require(!f.King.생존, "Undying failed to expire");
            f.Step(14.1f); Require(f.State.대상들[0].생존 && f.State.대상들[0].기절 == 0 && f.State.대상들[0].불사 <= 0, "Respawn retained CC");
        }
        {
            var f = new Fixture(왕직업.아이번); f.Cast(2, f.King.위치); float hp = f.King.체력;
            f.Damage(0, 30); Require(f.King.체력 == hp && f.King.보호량 == 30, "Shield absorb wrong");
            f.Damage(0, 50); Require(f.King.체력 == hp - 20 && f.King.보호량 == 0, "Shield overflow wrong");
        }
        {
            var f = new Fixture(왕직업.그라가스); f.Cast(0, f.Enemy.위치); float hp = f.Enemy.체력;
            f.Cast(0, f.Enemy.위치); Require(f.Enemy.체력 < hp && f.State.지대들.Count == 0, "Barrel recast failed");
        }
        {
            var f = new Fixture(왕직업.이블린); f.Cast(1, f.Enemy.위치); f.Step(2.6f);
            f.Cast(2, f.Enemy.위치); Require(f.Enemy.매혹 > 0, "Mature charm failed");
        }
        {
            var f = new Fixture(왕직업.하이머딩거); f.Cast(3, f.King.위치); f.Cast(0, f.King.위치 + Vector2.up);
            Require(Array.Exists(f.State.대상들, u => u is { 생존: true, 소환: 소환종류.강화포탑 }), "Upgrade turret missing");
            f.Step(15.2f); Require(!Array.Exists(f.State.대상들, u => u is { 생존: true, 소환: 소환종류.강화포탑 }), "Turret did not expire");
        }
        {
            var f = new Fixture(왕직업.아이번); f.Cast(0, f.Enemy.위치); f.Step(.3f);
            Require(f.Enemy.속박 > 0, "Rootcaller failed");
            float before = Vector2.Distance(f.King.위치, f.Enemy.위치); f.Cast(0, f.Enemy.위치);
            Require(Vector2.Distance(f.King.위치, f.Enemy.위치) < before, "Rootcaller recast failed");
            f.Cast(3, f.King.위치 + Vector2.up); int count = 전장규칙.수량(f.State, 0, 대상종류.병사);
            f.Cast(3, f.King.위치 + Vector2.up * 3); Require(전장규칙.수량(f.State, 0, 대상종류.병사) == count, "Daisy recast duplicated summon");
        }
        {
            var f = new Fixture(왕직업.애쉬);
            f.King.기절 = 1; f.Cast(0, f.King.위치); Require(f.State.진영들[0].쿨다운[0] == 0, "Stunned skill activated");
            f.King.기절 = 0; f.Cast(1, f.King.위치 + Vector2.right * 25); Require(f.State.진영들[0].쿨다운[1] == 0, "Out of range skill charged cooldown");
            for (int i = 0; i < f.State.대상들.Length; i++) f.State.대상들[i] ??= new 전장대상 { 체력 = 100, 최대체력 = 100 };
        }
        {
            var f = new Fixture(왕직업.소라카); f.Cast(2, f.Enemy.위치); f.Enemy.속박 = 2; f.King.보호량 = 30;
            for (int i = 0; i < f.State.대상들.Length; i++) f.State.대상들[i] ??= new 전장대상 { 체력 = 100, 최대체력 = 100 };
            var json = JsonUtility.ToJson(f.State); var bytes = 전장상태압축.포장(json);
            var copy = JsonUtility.FromJson<전장상태>(전장상태압축.풀기(bytes, Encoding.UTF8.GetByteCount(json)));
            Require(copy.대상들[전장규칙.왕번호(1)].속박 == 2 && copy.대상들[0].보호량 == 30 && copy.지대들.Count == 1, "Snapshot lost CC, shields or zones");
        }
        {
            var f = new Fixture(왕직업.소라카); float hp = f.King.체력;
            f.Cast(1, f.Ally.위치); Require(f.King.체력 == hp - 50 && f.Ally.체력 == 100, "Soraka heal health cost wrong");
            var tiny = new Fixture(왕직업.소라카); tiny.King.체력 = 1; tiny.Cast(1, tiny.Ally.위치);
            Require(tiny.State.진영들[0].쿨다운[1] == 0 && tiny.King.생존, "Low health heal should be rejected");
        }
        {
            var f = new Fixture(왕직업.트린다미어);
            var game = UnityEngine.Object.FindAnyObjectByType<신들의전쟁>();
            var floor = (Tilemap)typeof(신들의전쟁).GetField("바닥타일맵", Private).GetValue(game);
            var wall = (Tilemap)typeof(신들의전쟁).GetField("장애물타일맵", Private).GetValue(game);
            var nav = new 격자길찾기(floor, wall);
            bool found = false;
            foreach (var tile in floor.cellBounds.allPositionsWithin)
            {
                var p = (Vector2)floor.GetCellCenterWorld(tile);
                if (!nav.정적통행가능(p) || nav.정적통행가능(p + Vector2.right) || !nav.정적통행가능(p + Vector2.right * 2) || nav.전장안쪽(p) != p) continue;
                f.King.위치 = p; f.Cast(2, p + Vector2.right * 2);
                Require(f.King.위치.x < p.x + 1 && nav.정적통행가능(f.King.위치), "Dash crossed blocked terrain");
                found = true; break;
            }
            Require(found, "Could not find authored barrier for dash test");
        }
        {
            var f = new Fixture(왕직업.애쉬);
            for (int i = 0; i < f.State.대상들.Length; i++) {
                f.State.대상들[i] ??= new 전장대상();
                foreach (var field in typeof(전장대상).GetFields()) if (field.FieldType == typeof(float)) field.SetValue(f.State.대상들[i], 9.876543f);
                f.State.대상들[i].위치 = new Vector2(-25.67494f, 24.98765f);
                f.State.대상들[i].목적지 = new Vector2(24.98765f, -25.67494f);
            }
            for (int i = 0; i < 64; i++) f.State.지대들.Add(new 스킬지대 { 종류 = 지대종류.투사체, 위치 = new Vector2(-25.67494f, 24.98765f), 남은시간 = 9.876543f, 피해량 = 9.876543f });
            var json = JsonUtility.ToJson(f.State);
            Debug.Log($"[ClassCombatChecks] Full battle snapshot bytes: {Encoding.UTF8.GetByteCount(json)}");
            Require(전장상태압축.포장(json).Length > 0, "Full battle snapshot exceeded protocol limit");
        }
        foreach (var job in 왕직업목록.이전직업)
        {
            bool ranged = job is 왕직업.애쉬 or 왕직업.소라카 or 왕직업.아지르 or 왕직업.하이머딩거 or 왕직업.아이번;
            var f = new Fixture(job);
            f.Enemy.위치 = f.King.위치 + Vector2.right * (ranged ? 4 : .75f);
            f.Rules.명령(0, new 전장명령 { 종류 = 명령종류.공격, 값 = 전장규칙.왕번호(1), 위치 = f.Enemy.위치 });
            f.Step(.05f);
            Require(f.Enemy.체력 == 500 && f.King.공격순번 == 1, $"{job}: attack did not wind up before damage");
            f.Step(ranged ? .2f : .35f);
            if (ranged)
            {
                Require(f.Enemy.체력 == 500 && f.State.지대들.Any(z => z.종류 == 지대종류.기본투사체), $"{job}: ranged attack hit before projectile arrived");
                f.Step(.5f); Require(f.Enemy.체력 < 500, $"{job}: ranged projectile failed to hit");
                if (job == 왕직업.애쉬) Require(f.Enemy.둔화량 > 0 && f.Enemy.기절 == 0, "Ashe basic arrow must slow without ultimate stun");
            }
            else Require(f.Enemy.체력 < 500 && f.State.지대들.Any(z => z.종류 == 지대종류.근접타격) && !f.State.지대들.Any(z => z.종류 == 지대종류.기본투사체), $"{job}: melee attack must show its swing and strike without a ranged projectile");
        }
        {
            var f = new Fixture(왕직업.트린다미어); f.Enemy.위치 = f.King.위치 + Vector2.right * .75f;
            f.Rules.명령(0, new 전장명령 { 종류 = 명령종류.공격, 값 = 전장규칙.왕번호(1) }); f.Step(.05f);
            var start = f.King.위치;
            f.Rules.명령(0, new 전장명령 { 종류 = 명령종류.이동, 위치 = start + Vector2.right * 2 });
            f.Cast(2, start + Vector2.right);
            f.Step(.45f);
            Require(f.King.위치 == start && f.Enemy.체력 < 500, "Click or dash moved the king during a melee attack");
            f.Step(.2f);
            Require(f.King.위치 != start, "Queued click did not move the king after recovery");
        }
        {
            var f = new Fixture(왕직업.애쉬);
            f.King.강화시간 = .15f;
            f.Rules.명령(0, new 전장명령 { 종류 = 명령종류.공격, 값 = 전장규칙.왕번호(1) }); f.Step(.05f);
            var start = f.King.위치;
            f.Rules.명령(0, new 전장명령 { 종류 = 명령종류.방향이동, 위치 = Vector2.right });
            f.Step(.25f);
            Require(f.King.위치 == start, "Directional input moved Ashe during her empowered shot");
            f.Step(.1f);
            Require(f.King.위치 != start, "Empowered shot recovery changed when the buff expired");
        }
        {
            var f = new Fixture(왕직업.트린다미어); f.Enemy.위치 = f.King.위치 + Vector2.right * .75f;
            f.Rules.명령(0, new 전장명령 { 종류 = 명령종류.공격, 값 = 전장규칙.왕번호(1) }); f.Step(.05f);
            f.Enemy.위치 += Vector2.right * 4; f.Step(.2f);
            Require(f.Enemy.체력 == 500, "Melee hit a target that left striking range");
            Require(!f.State.지대들.Any(z => z.종류 == 지대종류.근접타격), "Missed swing appeared before windup");
            f.Step(.15f);
            Require(f.Enemy.체력 == 500 && f.State.지대들.Any(z => z.종류 == 지대종류.근접타격), "Missed swing must be visible without out-of-range damage");
        }
        {
            var f = new Fixture(왕직업.트린다미어); f.Enemy.위치 = f.King.위치 + Vector2.right * .75f;
            f.Rules.명령(0, new 전장명령 { 종류 = 명령종류.공격, 값 = 전장규칙.왕번호(1) }); f.Step(.05f);
            f.King.기절 = 1; f.Step(.3f); Require(f.Enemy.체력 == 500, "Stun failed to cancel melee windup");
        }
        {
            var f = new Fixture(왕직업.애쉬); f.Enemy.위치 = f.King.위치 + Vector2.right * 4;
            f.Rules.명령(0, new 전장명령 { 종류 = 명령종류.공격, 값 = 전장규칙.왕번호(1) }); f.Step(.25f);
            f.State.대상들[전장규칙.왕번호(1)] = new 전장대상 { 팀 = 1, 종류 = 대상종류.왕, 위치 = f.Enemy.위치, 체력 = 500, 최대체력 = 500 };
            f.Step(.3f); Require(f.Enemy.체력 == 500, "Projectile hit a replacement reusing the old target slot");
        }
        {
            var f = new Fixture(왕직업.애쉬); f.Enemy.위치 = f.King.위치 + Vector2.right * 4;
            f.Rules.명령(0, new 전장명령 { 종류 = 명령종류.공격, 값 = 전장규칙.왕번호(1) }); f.Step(.25f);
            f.King.체력 = 0; f.Step(.4f); Require(f.Enemy.체력 < 500, "Fired projectile disappeared when shooter died");
        }
        {
            var f = new Fixture(왕직업.애쉬);
            var hall = f.State.대상들[전장규칙.회관번호(1)]; hall.위치 = f.King.위치 + Vector2.right * 4;
            float health = hall.체력;
            f.Rules.명령(0, new 전장명령 { 종류 = 명령종류.공격, 값 = 전장규칙.회관번호(1), 위치 = hall.위치 });
            f.Step(.25f); Require(hall.체력 == health, "Building took ranged damage before projectile arrived");
            f.Step(.5f); Require(hall.체력 < health, "Ranged projectile failed to hit a building");
        }
        {
            var f = new Fixture(왕직업.애쉬);
            f.State.짐승들[0] = new 짐승상태 { 등장번호 = 333, 종류 = 1, 위치 = f.King.위치 + Vector2.right * 4, 체력 = 500 };
            f.Rules.명령(0, new 전장명령 { 종류 = 명령종류.사냥, 값 = 333 });
            f.Step(.25f);
            Require(f.State.짐승들[0].체력 == 500 && f.State.지대들.Any(z => z.종류 == 지대종류.기본투사체 && z.짐승칸 == 0), "Ranged hunting did not emit a delayed projectile");
            f.Step(.6f); Require(f.State.짐승들[0].체력 < 500, "Ranged hunting projectile failed to arrive");
        }
        {
            var f = new Fixture(왕직업.트린다미어); f.Enemy.위치 = f.King.위치 + Vector2.right * .75f;
            f.King.속박 = 1; f.King.침묵 = 1;
            f.Rules.명령(0, new 전장명령 { 종류 = 명령종류.공격, 값 = 전장규칙.왕번호(1) });
            f.Step(.4f); Require(f.Enemy.체력 < 500, "Root or silence incorrectly prohibited a basic attack");
        }
        var catalogue = 왕외형목록.가져오기();
        Require(catalogue != null, "Supplied character assignments missing");
        Require(AssetDatabase.GetAssetPath(catalogue) == PlayerAvatarImporter.CataloguePath, "Wrong player avatar catalogue");
        var textures = new System.Collections.Generic.HashSet<Texture2D>();
        foreach (var job in 왕직업목록.선택)
        {
            var look = catalogue.전용외형(job);
            Require(look != null && look.직업 == job, $"{job}: player appearance missing");
            PlayerFrames(look.대기);
            PlayerFrames(look.이동);
            PlayerFrames(look.공격);
            Require(textures.Add(look.대기[0].texture), "Two selectable jobs share an appearance");
        }
        PlayerFrames(왕외형목록.검사기술외형(장착기술.회전베기));
        PlayerFrames(왕외형목록.검사기술외형(장착기술.앞구르기));
        for (int i = 1; i <= 5; i++)
        {
            var look = catalogue.소환외형((소환종류)i);
            Require(look != null && look.대기.Length == 120 && look.이동.Length == 120 && look.공격.Length == 120, "Summon art incomplete");
            Require(Mathf.Approximately(look.대기[0].pixelsPerUnit, 90), "Summon scale does not match soldier renderer");
        }
        Require(왕직업목록.이전직업.All(job => catalogue.전용외형(job) != null), "Some job art remains unassigned");
        Require(!System.IO.File.Exists("Assets/DivineWar/Resources/Classes/champions.png") && !System.IO.File.Exists("Assets/DivineWar/Resources/Classes/summons.png"), "Generated class art remains in game assets");
        Debug.Log($"[ClassCombatChecks] ALL PASSED: {passed} assertions; {왕직업목록.이전직업.Length * 4} skills, CC, recasts, shields, summons, snapshots and art.");
    }

    static void PlayerFrames(Sprite[] frames)
    {
        Require(frames?.Length == 120, "Expected 8 directions × 15 player frames");
        foreach (var sprite in frames)
            Require(sprite != null && sprite.texture != null
                && AssetDatabase.GetAssetPath(sprite.texture).StartsWith("Assets/Images/", StringComparison.Ordinal),
                "Player frame must render the supplied original texture");
    }

    [MenuItem("DivineWar/Validation/Equipped Skill Shortcut")]
    public static void EquippedSkillShortcut()
    {
        var game = UnityEngine.Object.FindAnyObjectByType<신들의전쟁>();
        Require(EditorApplication.isPlaying && game != null, "Start an AI practice battle first");
        var state = (전장상태)typeof(신들의전쟁).GetField("상태", Private).GetValue(game);
        var team = state.진영들[0];
        Require(team.직업 == 왕직업.아처 && 왕직업목록.장착스킬(team, 0) == 장착기술.폭탄화살, "Equip Bomb Arrow on Q before starting");
        var king = state.대상들[전장규칙.왕번호(0)];
        var previous = UnityEngine.InputSystem.Keyboard.current;
        var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
        try
        {
            team.쿨다운[0] = 0;
            king.폭탄화살준비 = false;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,
                new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Q));
            UnityEngine.InputSystem.InputSystem.Update();
            Require(keyboard.qKey.wasPressedThisFrame && Application.isFocused, "Q was not received on the focused Game view");
            var input = typeof(신들의전쟁).GetMethod("스킬키입력", Private);
            var aimingSlot = typeof(신들의전쟁).GetField("조준스킬칸", Private);
            input.Invoke(game, new object[] { keyboard });
            Require((int)aimingSlot.GetValue(game) == 0, "Q down must begin aiming");
            Require(!king.폭탄화살준비 && team.쿨다운[0] == 0, "Q down must not activate Bomb Arrow");

            UnityEngine.InputSystem.InputSystem.Update();
            input.Invoke(game, new object[] { keyboard });
            Require((int)aimingSlot.GetValue(game) == 0 && !king.폭탄화살준비, "Holding Q must keep aiming without casting");

            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
            UnityEngine.InputSystem.InputSystem.Update();
            input.Invoke(game, new object[] { keyboard });
            Require((int)aimingSlot.GetValue(game) == -1, "Q release must clear aiming");
            Require(king.폭탄화살준비 && team.쿨다운[0] == 40, "Q release must activate the equipped Bomb Arrow");
            Debug.Log("[EquippedSkillShortcut] PASS: Q aimed while held and activated Bomb Arrow on release.");
        }
        finally
        {
            UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
            previous?.MakeCurrent();
        }
    }

    [MenuItem("DivineWar/Validation/Equipped Skills")]
    public static void EquippedSkills()
    {
        passed = 0;
        var commander = new Fixture(왕직업.지휘관);
        var king = commander.King;
        var soldier = commander.Soldier(30, 0, king.위치 + Vector2.up);
        var second = commander.Soldier(31, 0, king.위치 + Vector2.up * 2);
        commander.Cast(0, king.위치);
        Require(commander.Effect(soldier, "사기남음") == 5 && commander.Effect(commander.Ally, "사기남음") == 0, "Morale unit filter");
        commander.Cast(1, king.위치 + Vector2.right, new[] { 30, 전장규칙.왕번호(1), 6, 30 });
        Require(soldier.위치 == king.위치 + Vector2.right && second.위치 == king.위치 + Vector2.up * 2, "Rally selection");
        Require(commander.State.진영들[0].쿨다운[1] == 18, "Rally cooldown");
        king.체력 = 300;
        soldier.체력 = 50;
        commander.Enemy.위치 += Vector2.right * 18;
        commander.Cast(2, soldier.위치);
        commander.Step(.25f);
        Require(king.위치 != soldier.위치 && commander.Effect(king, "호위회복남음") <= 0, "Protect dash moves visibly before healing");
        commander.Step(.4f);
        Require(Vector2.Distance(king.위치, soldier.위치) <= 1.25f, "Protect dash destination");
        commander.Step(3.05f);
        Require(Mathf.Abs(king.체력 - 390) < .05f && Mathf.Abs(soldier.체력 - 68) < .05f, "Protect total healing");
        commander.Equip(1, 6);
        commander.Cast(3, soldier.위치);
        commander.Damage(30, 999);
        Require(commander.Effect(king, "복수남음") == 2 && commander.Effect(second, "복수오라남음") == 3, "Marked death and passive revenge");
        commander.Equip(0, 4);
        commander.Cast(0, king.위치);
        Require(Mathf.Abs(king.체력 - 440) < .05f && king.가드 == 50, "Medic heal and guard");
        commander.Damage(0, 20);
        Require(king.가드 == 30 && Mathf.Abs(king.체력 - 440) < .05f, "Guard absorbs damage");
        commander.Equip(0, 5);
        commander.Cast(0, king.위치);
        commander.Step(.05f);
        Require(king.장막안 && !commander.Enemy.장막안 && commander.State.지대들.Any(z => z.종류 == 지대종류.장막), "Shroud allies and zone");
        commander.Step(10f);
        Require(!king.장막안 && king.가드 == 0, "Shroud expiry and guard decay");

        var archer = new Fixture(왕직업.아처);
        king = archer.King;
        archer.Soldier(30, 0, king.위치 + Vector2.up).병과 = 병과.아처;
        archer.Soldier(31, 0, king.위치 + Vector2.up * 2).병과 = 병과.아처;
        archer.Cast(0, king.위치);
        Require(Mathf.Abs(archer.Effect(king, "여왕공속") - .1f) < .001f && archer.Effect(king, "여왕남음") == 7, "Queen counts nearby allied archers");
        var enemy2 = archer.Soldier(전장규칙.왕번호(1) + 34, 1, king.위치 + Vector2.right * 3.4f);
        var enemy3 = archer.Soldier(전장규칙.왕번호(1) + 35, 1, king.위치 + Vector2.right * 4.6f);
        enemy2.종류 = enemy3.종류 = 대상종류.주민;
        archer.Cast(1, king.위치);
        archer.Cast(2, king.위치);
        archer.Attack(전장규칙.왕번호(1));
        archer.Step(.4f);
        archer.Rules.명령(0, new 전장명령 { 종류 = 명령종류.이동, 위치 = king.위치 });
        archer.Step(.3f);
        Require(archer.Enemy.체력 < 500 && enemy2.체력 < 100 && enemy3.체력 < 100, "Trick Shot two distinct bounces");
        Require(archer.Enemy.둔화량 == .2f && enemy2.둔화량 == .2f && enemy3.둔화량 == .2f, "Poison at each hit without stacking");
        archer.Cast(3, king.위치);
        archer.Attack(6);
        archer.Step(.48f);
        Require(Mathf.Abs(archer.Ally.체력 - 60) < .05f, "Healing arrow hits ally");
        archer.Rules.명령(0, new 전장명령 { 종류 = 명령종류.이동, 위치 = king.위치 });

        var bomb = new Fixture(왕직업.아처);
        int buildingSlot = 전장규칙.왕번호(1) + 4;
        var building = bomb.Soldier(buildingSlot, 1, bomb.King.위치 + Vector2.right * 2.5f);
        building.종류 = 대상종류.훈련소;
        bomb.Attack(buildingSlot);
        Require(bomb.King.표적 < 0, "Ordinary archer shot cannot target a building");
        bomb.Equip(0, 4);
        bomb.Cast(0, bomb.King.위치);
        bomb.Attack(buildingSlot);
        bomb.Step(.5f);
        Require(building.체력 == 100 && !bomb.King.폭탄화살준비 && bomb.State.지대들.Any(z => z.종류 == 지대종류.폭탄화살), "Bomb attaches and waits");
        bomb.Step(3.05f);
        Require(building.체력 < 100 && bomb.Enemy.체력 < 500, "Bomb damages structure and nearby foes");

        var sword = new Fixture(왕직업.검사);
        king = sword.King;
        var start = king.위치;
        sword.Cast(0, sword.Enemy.위치);
        sword.Step(.25f);
        Require(king.위치.x > start.x + 1 && Mathf.Abs(sword.Enemy.체력 - 460) < .05f, "Spin dash strikes an enemy exactly once");
        Require(sword.State.지대들.Any(z => z.종류 == 지대종류.돌진궤적 && z.위치 == king.위치), "Spin trail follows the dash");
        sword.Step(.35f);
        king.위치 = king.목적지 = start;
        sword.Cast(3, start + Vector2.right);
        sword.Step(.2f);
        Require(king.위치.x > start.x + 1 && Mathf.Abs(sword.Enemy.체력 - 460) < .05f, "Roll moves without damage");
        sword.Step(.25f);
        king.위치 = king.목적지 = start;
        king.체력 = 250;
        sword.Enemy.위치 = sword.Enemy.목적지 = start + Vector2.right * 1.2f;
        sword.Cast(1, king.위치);
        sword.Cast(2, king.위치);
        sword.Attack(전장규칙.왕번호(1));
        sword.Step(.4f);
        Require(king.위치 == start && Mathf.Abs(king.체력 - 260.4f) < .05f, "Extended melee reach and 40% lifesteal");
        sword.Equip(0, 4);
        sword.Cast(0, king.위치);
        sword.Enemy.체력 = 5;
        sword.Step(.8f);
        Require(!sword.Enemy.생존 && sword.Effect(king, "분노남음") > 3.5f, "Fury refreshes on a kill");

        commander.Equip(0, 7);
        float 지휘관체력 = commander.King.체력;
        float 병사체력 = second.체력;
        commander.Damage(0, 40);
        Require(Mathf.Approximately(commander.King.체력, 지휘관체력 - 20)
            && Mathf.Approximately(second.체력, 병사체력 - 15), "Liability transfers half at 75 percent");

        var volley = new Fixture(왕직업.아처);
        volley.Enemy.위치 = volley.Enemy.목적지 = volley.King.위치 + Vector2.right * 4;
        volley.Equip(0, 5);
        volley.Cast(0, volley.Enemy.위치);
        volley.Attack(전장규칙.왕번호(1));
        volley.Step(.25f);
        Require(volley.State.지대들.Count(z => z.종류 == 지대종류.기본투사체 && z.팀 == 0) == 4, "Multishot releases four arrows");

        var blade = new Fixture(왕직업.검사);
        blade.Equip(0, 5);
        var behind = blade.Soldier(전장규칙.왕번호(1) + 35, 1, blade.Enemy.위치 + Vector2.right);
        blade.Cast(0, behind.위치);
        blade.Step(.35f);
        Require(blade.Enemy.둔화량 == .2f && behind.둔화량 == .2f, "Thrown blade pierces both targets");

        var assassin = new Fixture(왕직업.암살자);
        assassin.Equip(0, 4);
        assassin.Cast(0, assassin.King.위치);
        Require(assassin.King.은신 && assassin.State.진영들[0].쿨다운[0] == 20, "Camouflage starts immediately");
        assassin.Cast(1, assassin.Enemy.위치);
        assassin.Step(.3f);
        Require(!assassin.King.은신 && assassin.Enemy.체력 < 500 && assassin.State.진영들[0].쿨다운[1] < 19,
            "Piercing dash reveals caster and refunds cooldown once per target");
        assassin.Step(.3f);
        assassin.Enemy.위치 = assassin.Enemy.목적지 = assassin.King.위치 + Vector2.right * .5f;
        assassin.Enemy.체력 = 12;
        assassin.King.체력 = 200;
        assassin.Cast(3, assassin.Enemy.위치);
        assassin.Attack(전장규칙.왕번호(1));
        assassin.Step(.55f);
        Require(!assassin.Enemy.생존 && assassin.State.진영들[0].쿨다운[3] <= 0 && assassin.King.체력 > 207,
            "Empowered kill heals and resets its slot");
        Require(assassin.Effect(assassin.King, "수혈남음") > 0, "Transfusion starts on kill");

        var hook = new Fixture(왕직업.암살자);
        hook.Equip(0, 5);
        var hookStart = hook.Enemy.위치;
        hook.Cast(0, hookStart);
        hook.Step(.1f);
        Require(hook.Enemy.위치 == hookStart && hook.State.지대들.Any(z => z.종류 == 지대종류.투사체
            && z.연출기술 == 장착기술.납치), "Hook has visible travel before impact");
        hook.Step(.15f);
        float firstPull = hook.Enemy.위치.x;
        Require(firstPull < hookStart.x && hook.State.지대들.Any(z => z.종류 == 지대종류.납치당김), "Hook pulls gradually after impact");
        hook.Step(.3f);
        Require(hook.Enemy.위치.x < firstPull && hook.State.지대들.Any(z => z.종류 == 지대종류.납치당김), "Hook pull spans several simulation ticks");
        hook.Step(.4f);
        Require(hook.State.지대들.Any(z => z.종류 == 지대종류.스킬충격 && z.연출기술 == 장착기술.납치)
            && hook.State.지대들.All(z => z.종류 != 지대종류.납치당김), "Pull ends with a visible impact");

        var bleed = new Fixture(왕직업.암살자);
        bleed.Equip(0, 6);
        bleed.Cast(0, bleed.Enemy.위치);
        bleed.Step(.25f);
        float 피격후 = bleed.Enemy.체력;
        bleed.Step(3.1f);
        Require(Mathf.Abs(피격후 - bleed.Enemy.체력 - 60) < .05f, "Bleed deals three four-percent ticks");

        var packed = 왕직업목록.장착변경(왕직업목록.기본장착, 2, 5);
        Require(왕직업목록.유효장착(왕직업.지휘관, packed) && 왕직업목록.유효장착(왕직업.아처, packed)
            && !왕직업목록.유효장착(왕직업.트린다미어, packed) && !왕직업목록.유효장착(왕직업.지휘관, 0), "Loadout class and duplicate validation");
        var roundtrip = JsonUtility.FromJson<전장상태>(JsonUtility.ToJson(commander.State));
        Require(roundtrip.진영들[0].장착구성 == commander.State.진영들[0].장착구성, "Snapshot contains equipped slots");
        Debug.Log($"[EquippedSkills] ALL PASSED: {passed} assertions.");
    }

    public static void GameplayRules()
    {
        var results = new StringBuilder();
        void Check(string name, Action run) { run(); results.AppendLine("PASS " + name); }
        try
        {
            Check("Bomb arrow adds 40 damage to units", () => {
                var ordinary = new Fixture(왕직업.아처);
                ordinary.Attack(전장규칙.왕번호(1)); ordinary.Step(.5f);
                float ordinaryDamage = 500 - ordinary.Enemy.체력;
                var bomb = new Fixture(왕직업.아처);
                bomb.Equip(0, 4); bomb.Cast(0, bomb.King.위치);
                bomb.Attack(전장규칙.왕번호(1)); bomb.Step(.5f);
                Require(Mathf.Abs(500 - bomb.Enemy.체력 - ordinaryDamage - 40) < .05f, "Bomb unit bonus missing");
            });
            Check("Bomb waits three seconds and deals triple structure splash", () => {
                var f = new Fixture(왕직업.아처);
                int slot = 전장규칙.왕번호(1) + 4;
                var building = f.Soldier(slot, 1, f.King.위치 + Vector2.right * 2.5f);
                building.종류 = 대상종류.훈련소; building.체력 = building.최대체력 = 1000;
                f.Equip(0, 4); f.Cast(0, f.King.위치); f.Attack(slot); f.Step(.5f);
                Require(building.체력 == 1000, "Bomb exploded before fuse");
                f.Rules.명령(0, new 전장명령 { 종류 = 명령종류.이동, 위치 = f.King.위치 });
                f.Step(3.05f);
                float unitDamage = 500 - f.Enemy.체력;
                Require(unitDamage > 40 && Mathf.Abs(1000 - building.체력 - unitDamage * 3) < .1f, "Bomb building multiplier failed");
            });
            Check("Peaceful king regenerates after five seconds; combat resets delay", () => {
                var f = new Fixture(왕직업.아처); f.Enemy.체력 = 0;
                f.Damage(0, 100); f.Step(4.9f);
                Require(Mathf.Abs(f.King.체력 - 400) < .01f, "Early regeneration");
                f.Step(1.1f);
                Require(Mathf.Abs(f.King.체력 - 410) < .1f, "Wrong regeneration rate");
                var target = f.Soldier(전장규칙.왕번호(1) + 34, 1, f.King.위치 + Vector2.right * 2);
                target.공격대기 = 999; target.체력 = target.최대체력 = 10000;
                float before = f.King.체력; f.Attack(전장규칙.왕번호(1) + 34); f.Step(1);
                Require(Mathf.Abs(f.King.체력 - before) < .01f, "Attacking king regenerated");
                f.Damage(0, 10); before = f.King.체력; f.Step(1);
                Require(Mathf.Abs(f.King.체력 - before) < .01f, "Incoming damage failed to reset delay");
            });
            Check("AI keeps three defenders, respects preparation and raid interval", () => {
                var f = new Fixture(왕직업.아처);
                Vector2 home = f.State.대상들[전장규칙.회관번호(1)].위치;
                Vector2 rally = home + Vector2.right * -Mathf.Sign(home.x) * 5;
                Vector2 hall = f.State.대상들[전장규칙.회관번호(0)].위치;
                int start = 전장규칙.왕번호(1) + 전장규칙.병사시작칸 + 2;
                var soldiers = Enumerable.Range(start, 8).Select(i => f.Soldier(i, 1, rally)).ToArray();
                void Command(bool ready = true) => typeof(전장규칙).GetMethod("적부대지휘", Private).Invoke(f.Rules, new object[] { f.Enemy, hall, rally, rally, -1, ready });
                f.State.시간 = 149; Command(); Require(soldiers.All(u => !u.이동중), "Early AI raid");
                f.State.시간 = 150; Command(false); Require(soldiers.All(u => !u.이동중), "Unprepared AI raid");
                Command(); Require(soldiers.Take(3).All(u => !u.이동중) && soldiers.Skip(3).All(u => u.이동중), "Missing AI defenders");
                foreach (var u in soldiers.Skip(3)) { u.체력 = 0; u.이동중 = false; }
                Command();
                foreach (var u in soldiers) { u.체력 = 100; u.위치 = u.목적지 = rally; u.이동중 = false; }
                f.State.시간 = 269; Command(); Require(soldiers.All(u => !u.이동중), "AI ignored raid interval");
                f.State.시간 = 270; Command(); Require(soldiers.Skip(3).All(u => u.이동중), "AI failed to resume raid");
            });
            Check("Manual ground order ignores nearby enemies while AI movement still attacks", () => {
                var f = new Fixture(왕직업.아처);
                var soldier = f.Soldier(30, 0, f.King.위치);
                f.Rules.명령(0, new 전장명령 { 종류 = 명령종류.부대이동, 위치 = soldier.위치 + Vector2.right * 4, 부대 = new[] { 30 } });
                f.Step(.1f);
                Require(soldier.표적 < 0 && soldier.이동중, "Manual march was overridden by enemy acquisition");
                typeof(전장규칙).GetMethod("부대이동지정", Private).Invoke(f.Rules, new object[] { new[] { soldier }, soldier.목적지, false });
                f.Step(.1f);
                Require(soldier.표적 >= 0, "AI march suppressed nearby combat");
            });
            results.AppendLine("ALL PASSED");
        }
        catch (Exception e) { results.AppendLine("FAILED " + e); throw; }
        finally
        {
            Directory.CreateDirectory("Temp/GameplayRules");
            File.WriteAllText("Temp/GameplayRules/results.txt", results.ToString());
            Debug.Log(results.ToString());
        }
    }

    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); passed++; }

    sealed class Fixture
    {
        public readonly 전장규칙 Rules;
        public 전장상태 State => Rules.상태;
        public 전장대상 King => State.대상들[0];
        public 전장대상 Enemy => State.대상들[전장규칙.왕번호(1)];
        public 전장대상 Ally => State.대상들[6];
        public Fixture(왕직업 job)
        {
            var game = UnityEngine.Object.FindAnyObjectByType<신들의전쟁>();
            object Field(string n) => typeof(신들의전쟁).GetField(n, Private).GetValue(game);
            var floor = (Tilemap)Field("바닥타일맵");
            var nav = new 격자길찾기(floor, (Tilemap)Field("장애물타일맵"));
            Rules = new 전장규칙((자원지점[])Field("자원들"), nav, Array.ConvertAll((Transform[])Field("시작지점들"), t => (Vector2)t.position), job, 왕직업.애쉬, 374);
            Vector2 point = default; bool found = false;
            foreach (var tile in floor.cellBounds.allPositionsWithin) {
                var p = (Vector2)floor.GetCellCenterWorld(tile);
                if (p.magnitude < 8 || nav.전장안쪽(p) != p || !nav.정적통행가능(p) || !nav.정적통행가능(p + Vector2.right * 2) || !nav.정적통행가능(p + Vector2.up * 3) || !nav.정적통행가능(p + Vector2.right)) continue;
                point = p; found = true; break;
            }
            Require(found, "No test terrain");
            King.위치 = point; King.목적지 = point; King.이동중 = false; King.체력 = King.최대체력 = 500;
            Enemy.위치 = point + Vector2.right * 2; Enemy.목적지 = Enemy.위치; Enemy.체력 = Enemy.최대체력 = 500; Enemy.이동중 = false;
            State.대상들[6] = new 전장대상 { 팀 = 0, 종류 = 대상종류.주민, 위치 = point + Vector2.up, 체력 = 50, 최대체력 = 100 };
            foreach (var team in State.진영들) { Array.Fill(team.재화, 100); team.제재시간 = 99999; team.심부름 = 4; }
        }
        public void Cast(int skill, Vector2 point, int[] selection = null) =>
            Rules.명령(0, new 전장명령 { 종류 = 명령종류.스킬, 값 = skill, 위치 = point, 부대 = selection });
        public void Equip(int slot, int candidate)
        {
            var team = State.진영들[0];
            team.장착구성 = 왕직업목록.장착변경(team.장착구성, slot, candidate);
            team.쿨다운[slot] = 0;
        }
        public 전장대상 Soldier(int slot, int team, Vector2 point)
        {
            var target = new 전장대상 { 팀 = team, 종류 = 대상종류.병사, 위치 = point, 목적지 = point, 체력 = 100, 최대체력 = 100 };
            State.대상들[slot] = target;
            return target;
        }
        public void Attack(int slot) => Rules.명령(0, new 전장명령 { 종류 = 명령종류.공격, 값 = slot, 위치 = State.대상들[slot].위치 });
        public float Effect(전장대상 target, string name) => (float)typeof(전장대상).GetField(name, Private).GetValue(target);
        public void Damage(int target, float amount) => typeof(전장규칙).GetMethod("피해", Private).Invoke(Rules, new object[] { target, amount, 1 });
        public void Step(float seconds) { for (int i = 0; i < Mathf.CeilToInt(seconds / .05f); i++) Rules.진행(.05f, false); }
    }
}
