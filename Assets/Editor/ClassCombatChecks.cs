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
        foreach (var job in 왕직업목록.이전직업.Concat(왕직업목록.선택).Distinct())
        {
            bool ranged = job is 왕직업.애쉬 or 왕직업.아처 or 왕직업.소라카 or 왕직업.아지르 or 왕직업.하이머딩거 or 왕직업.아이번;
            var f = new Fixture(job);
            f.Enemy.위치 = f.King.위치 + Vector2.right * (ranged ? (job is 왕직업.아처 or 왕직업.애쉬 ? 6.5f : 4f) : 2f);
            f.Rules.명령(0, new 전장명령 { 종류 = 명령종류.공격, 값 = 전장규칙.왕번호(1), 위치 = f.Enemy.위치 });
            f.Step(.05f);
            Require(f.Enemy.체력 == 500 && f.King.공격순번 == 1, $"{job}: attack did not wind up before damage");
            f.Step(ranged ? .2f : .35f);
            if (ranged)
            {
                Require(f.Enemy.체력 == 500 && f.State.지대들.Any(z => z.종류 == 지대종류.기본투사체), $"{job}: ranged attack hit before projectile arrived");
                f.Step(.7f); Require(f.Enemy.체력 < 500, $"{job}: ranged projectile failed to hit");
                if (job == 왕직업.애쉬) Require(f.Enemy.둔화량 > 0 && f.Enemy.기절 == 0, "Ashe basic arrow must slow without ultimate stun");
            }
            else Require(f.Enemy.체력 < 500 && f.State.지대들.Any(z => z.종류 == 지대종류.근접타격 && Mathf.Approximately(z.반경, 2.2f))
                && !f.State.지대들.Any(z => z.종류 == 지대종류.기본투사체), $"{job}: extended melee range must apply to damage and swing effects");
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
            const string mounted = "Assets/DivineWar/Resources/Classes/Variants/hecarim_";
            bool commander = job == 왕직업.지휘관;
            PlayerFrames(look.대기, commander ? mounted + "idle.png" : null);
            PlayerFrames(look.이동, commander ? mounted + "walk.png" : null);
            PlayerFrames(look.공격, commander ? mounted + "attack.png" : null);
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

    static void PlayerFrames(Sprite[] frames, string expectedTexture = null)
    {
        Require(frames?.Length == 120, "Expected 8 directions × 15 player frames");
        foreach (var sprite in frames)
        {
            var path = sprite != null && sprite.texture != null ? AssetDatabase.GetAssetPath(sprite.texture) : "";
            Require(expectedTexture == null ? path.StartsWith("Assets/Images/", StringComparison.Ordinal) : path == expectedTexture,
                "Player frame must render the assigned source texture");
        }
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
        DoubleStrike();
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
        sword.Step(.15f);
        Require(Mathf.Approximately(sword.Enemy.체력, 470), "Spin dash deals a single initial hit");
        float contact = king.위치.x;
        sword.Step(.10f);
        Require(king.위치.x > contact && king.위치.x - contact < .4f && Mathf.Approximately(sword.Enemy.체력, 470),
            "Contact slows the dash and spaces out the strikes");
        Require(sword.State.지대들.Any(z => z.종류 == 지대종류.돌진궤적 && z.위치 == king.위치 && z.강화),
            "Spin trail follows the dash and indicates contact");
        sword.Step(.20f);
        Require(Mathf.Approximately(sword.Enemy.체력, 449), "A target staying in reach takes two follow-up strikes");
        sword.Step(.50f);
        Require(Mathf.Approximately(sword.Enemy.체력, 449) && king.위치.x > start.x + 3.8f
            && king.왕기술동작 != 장착기술.회전베기, "Spin caps each target at three hits and completes the dash");

        var evade = new Fixture(왕직업.검사);
        evade.Cast(0, evade.Enemy.위치);
        evade.Step(.15f);
        evade.Enemy.위치 = evade.Enemy.목적지 = evade.King.위치 + Vector2.up * 5;
        float departed = evade.King.위치.x;
        evade.Step(.15f);
        Require(Mathf.Approximately(evade.Enemy.체력, 470) && evade.King.위치.x - departed > .8f,
            "A target leaving reach stops taking damage and the dash recovers its speed");

        king.위치 = king.목적지 = start;
        sword.Cast(3, start + Vector2.right);
        Require(sword.State.진영들[0].스킬표시시간 <= 0, "Roll does not emit a cast pulse");
        sword.Step(.2f);
        Require(king.위치.x > start.x + 1 && Mathf.Approximately(sword.Enemy.체력, 449), "Roll moves without damage");
        sword.Step(.25f);
        Require(sword.State.지대들.Any(z => z.연출기술 == 장착기술.앞구르기 && z.종류 == 지대종류.돌진궤적)
            && !sword.State.지대들.Any(z => z.연출기술 == 장착기술.앞구르기 && z.종류 == 지대종류.스킬충격),
            "Roll keeps its trail without a landing impact");
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

    [MenuItem("DivineWar/Validation/Double Strike")]
    public static void DoubleStrike()
    {
        var fixture = new Fixture(왕직업.검사);
        fixture.Equip(0, 6);
        var rules = fixture.Rules;
        var king = fixture.King;
        var enemy = fixture.Enemy;
        int target = 전장규칙.왕번호(1);
        var start = typeof(전장규칙).GetMethod("기본공격시작", Private);
        var progress = typeof(전장규칙).GetMethod("기본공격진행", Private);
        var hud = typeof(전장규칙).GetMethod("왕기술효과갱신", Private);
        void Advance(float dt)
        {
            fixture.State.시간 += dt;
            progress.Invoke(rules, new object[] {dt});
            hud.Invoke(rules, null);
        }
        float Attack(bool misses = false)
        {
            enemy.위치 = king.위치 + Vector2.right * .75f;
            start.Invoke(rules, new object[] {king, target, 20f});
            if (misses) enemy.위치 += Vector2.right * 2;
            float before = enemy.체력;
            Advance(.31f);
            return before - enemy.체력;
        }
        int Stacks() => fixture.State.진영들[0].기술효과[0].수치;

        float normal = Attack();
        Require(normal > 0 && Stacks() == 1, "First basic hit adds a stack");
        Require(Attack(misses: true) == 0 && Stacks() == 1, "Miss cannot add a Double Strike stack");
        Require(Mathf.Approximately(Attack(), normal) && Stacks() == 2, "Second basic hit adds a stack");
        Require(Mathf.Approximately(Attack(), normal) && Stacks() == 3, "Third basic hit arms Double Strike");
        Require(Mathf.Approximately(Attack(), normal) && Stacks() == 0, "Fourth hit consumes stacks before its delayed second hit");
        float firstStrike = enemy.체력;
        Advance(.04f);
        Require(enemy.체력 == firstStrike, "Double Strike hits must resolve on separate ticks");
        Advance(.08f);
        Require(Mathf.Approximately(firstStrike - enemy.체력, normal * .5f) && Stacks() == 0
            && fixture.State.지대들.Any(z => z.종류 == 지대종류.근접타격 && z.강화 && z.적중수 == 1),
            "Second hit deals half damage and emits its own slash");
        Attack();
        Advance(4.01f);
        Require(Stacks() == 0 && Mathf.Approximately(Attack(), normal) && Stacks() == 1, "Stacks expire out of combat");
        Debug.Log("[DoubleStrike] PASS");
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
            Check("AI raids a discovered hall with specialists, keeps defenders and respects the raid interval", () => {
                var f = new Fixture(왕직업.아처);
                Vector2 rally = f.State.대상들[전장규칙.회관번호(1)].위치;
                Vector2 hall = f.State.대상들[전장규칙.회관번호(0)].위치;
                int start = 전장규칙.왕번호(1) + 전장규칙.병사시작칸 + 2;
                var soldiers = Enumerable.Range(start, 11).Select(i => f.Soldier(i, 1, rally)).ToArray();
                var ai = ((Array)typeof(전장규칙).GetField("진영AI들", Private).GetValue(f.Rules)).GetValue(1);
                void Command(bool ready = true) => typeof(전장규칙).GetMethod("AI부대지휘", Private)
                    .Invoke(f.Rules, new object[] { 1, ai, f.Enemy, rally, rally, -1, ready });
                f.State.시간 = 150; Command();
                Require(soldiers.All(u => !u.이동중), "AI raided a hidden hall");
                ai.GetType().GetField("발견한적회관").SetValue(ai, hall);
                Command();
                Require(soldiers.All(u => !u.이동중), "AI raided without specialists");
                soldiers[3].병과 = 병과.브루저;
                soldiers[4].병과 = 병과.아처;
                f.State.시간 = 149; Command(); Require(soldiers.All(u => !u.이동중), "Early AI raid");
                f.State.시간 = 150; Command(false); Require(soldiers.All(u => !u.이동중), "Unprepared AI raid");
                soldiers[^1].위치 = rally + Vector2.up * 7;
                Command(); Require(!(bool)ai.GetType().GetField("진격").GetValue(ai), "AI raided before its soldiers gathered");
                soldiers[^1].위치 = soldiers[^1].목적지 = rally;
                soldiers[^1].이동중 = false;
                Command(); Require(soldiers.Take(3).All(u => !u.이동중) && soldiers.Skip(3).All(u => u.이동중), "Missing AI defenders");
                var reinforcement = f.Soldier(start + soldiers.Length, 1, rally);
                Command(); Require(!reinforcement.이동중, "AI sent an isolated reinforcement to the active raid");
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

    [MenuItem("DivineWar/Validation/Battle AI")]
    public static void BattleAI()
    {
        MarchMusicChecks();
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Main.unity");
        try
        {
            var game = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<신들의전쟁>(true)).Single();
            T Field<T>(string name) => (T)typeof(신들의전쟁).GetField(name, Private).GetValue(game);
            var floor = Field<Tilemap>("바닥타일맵");
            var obstacles = Field<Tilemap>("장애물타일맵");
            var terrainType = typeof(격자길찾기).Assembly.GetType("전장지형");
            using var terrain = (IDisposable)Activator.CreateInstance(terrainType, floor, obstacles, Field<Tilemap>("지형타일맵"),
                Field<TileBase[]>("해변타일"), Field<TileBase[]>("흙타일"), Field<자원지점[]>("자원들"),
                Field<Array>("전술지형"), false, null);
            var resources = (자원지점[])terrainType.GetProperty("자원들", Private).GetValue(terrain);
            var navConstructor = typeof(격자길찾기).GetConstructor(Private, null,
                new[] { typeof(Tilemap), typeof(Tilemap), terrainType }, null);
            var starts = Array.ConvertAll(Field<Transform[]>("시작지점들"), point => (Vector2)point.position);
            var trials = Array.ConvertAll(Field<시련표식[]>("시련들"), marker => (Vector2)marker.transform.position);
            ResourceExplorationChecks(floor, obstacles, starts);
            bool confirmedBrushWatch = false;
            foreach (int seed in new[] { 374, 2026 })
            {
                var nav = (격자길찾기)navConstructor.Invoke(new object[] { floor, obstacles, terrain });
                var rules = new 전장규칙(resources, nav, starts, 왕직업.지휘관, 왕직업.아처, seed, 디버그연습전: true);
                rules.시련배치(trials);
                var state = rules.상태;
                var teams = (Array)typeof(전장규칙).GetField("진영AI들", Private).GetValue(rules);
                var ai = teams.GetValue(1);
                var knownHall = ai.GetType().GetField("발견한적회관");
                var raid = ai.GetType().GetField("진격");
                var raiders = (System.Collections.Generic.HashSet<전장대상>)ai.GetType().GetField("출격대").GetValue(ai);
                var scouts = (Array)ai.GetType().GetField("정찰들").GetValue(ai);
                var scoutTarget = scouts.GetValue(0).GetType().GetField("목표");
                var scoutUnit = scouts.GetValue(0).GetType().GetField("유닛");
                var watchPosition = ai.GetType().GetField("감시위치");
                var isBrush = terrainType.GetMethod("수풀인가", Private);
                var home = state.대상들[전장규칙.회관번호(1)].위치;
                var enemyHall = state.대상들[전장규칙.회관번호(0)].위치;
                bool sawSeparateScouts = false, sawMixedArmy = false, sawOutpost = false;
                float firstRaid = 0;
                int firstRaidSize = 0, maxResidents = 0, maxBuildings = 0;
                bool heldInBrush = false;
                rules.진행(.125f, true);
                Require(knownHall.GetValue(ai) == null, "AI started with the enemy hall revealed");
                var king = state.대상들[전장규칙.왕번호(1)];
                var previousDestination = king.목적지;
                king.표적 = 전장규칙.왕번호(0);
                rules.진행(.125f, true);
                Require(king.표적 < 0 && king.목적지 == previousDestination, "AI king tracked an enemy outside allied sight");
                while (state.시간 < 600 && state.승자 < 0)
                {
                    rules.진행(.125f, true);
                    if (state.시간 % 1 != 0) continue;
                    bool attacking = (bool)raid.GetValue(ai);
                    Require(!attacking || knownHall.GetValue(ai) is Vector2 found && found == enemyHall, "Raid lacks an observed enemy hall");
                    if (attacking && firstRaid == 0) { firstRaid = state.시간; firstRaidSize = raiders.Count; }
                    var scoutingTargets = scouts.Cast<object>().Select(scout => (int)scoutTarget.GetValue(scout)).Where(target => target >= 0).ToArray();
                    sawSeparateScouts |= scoutingTargets.Distinct().Count() >= 2;
                    if (knownHall.GetValue(ai) != null)
                    {
                        Require(scoutingTargets.Length == 0, "AI continued exploring after finding the enemy hall");
                        var activeScouts = scouts.Cast<object>().Select(scout => scoutUnit.GetValue(scout)).OfType<전장대상>().Where(unit => unit.생존).ToArray();
                        Require(activeScouts.Length <= 1, "AI kept multiple scouts after finding the enemy hall");
                        if (watchPosition.GetValue(ai) is Vector2 watch)
                        {
                            Require((bool)isBrush.Invoke(terrain, new object[] { watch }), "AI watch position is outside brush");
                            heldInBrush |= activeScouts.Any(unit => !unit.이동중 && (unit.위치 - watch).sqrMagnitude <= .25f);
                        }
                    }
                    var allies = state.대상들.Where(unit => unit is { 생존: true, 팀: 1 }).ToArray();
                    maxResidents = Mathf.Max(maxResidents, allies.Count(unit => unit.종류 == 대상종류.주민));
                    maxBuildings = Mathf.Max(maxBuildings, allies.Count(unit => unit.건물 && unit.완공대기 <= 0));
                    sawOutpost |= allies.Any(unit => unit is { 종류: 대상종류.깃발, 완공대기: <= 0 });
                    sawMixedArmy |= allies.Any(unit => unit.병과 == 병과.브루저) && allies.Any(unit => unit.병과 == 병과.아처);
                }
                Debug.Log($"[BattleAI] seed={seed}, home={home}, enemy={enemyHall}, residents={maxResidents}, buildings={maxBuildings}, scouts={sawSeparateScouts}, brush={heldInBrush}, specialists={sawMixedArmy}, outpost={sawOutpost}, first raid={firstRaid}s ({firstRaidSize} soldiers)");
                Require(sawSeparateScouts, "AI did not explore multiple areas at once");
                Require(maxResidents >= 16 && maxBuildings >= 8 && sawOutpost, "AI settlement did not expand");
                Require(sawMixedArmy, "AI did not field both specialist classes");
                Require(firstRaid >= 150 && firstRaidSize >= 6, "AI failed to launch a gathered force after preparation");
                confirmedBrushWatch |= heldInBrush;

                var autoplayNav = (격자길찾기)navConstructor.Invoke(new object[] { floor, obstacles, terrain });
                var autoplay = new 전장규칙(resources, autoplayNav, starts, 왕직업.지휘관, 왕직업.아처, seed, 디버그연습전: true);
                typeof(전장규칙).GetProperty("디버그AI정지", Private).SetValue(autoplay, true);
                Require((bool)typeof(전장규칙).GetMethod("디버그자동플레이전환", Private).Invoke(autoplay, null), "Autoplay toggle failed");
                var playerKing = autoplay.상태.대상들[전장규칙.왕번호(0)];
                var startPosition = playerKing.위치;
                for (int tick = 0; tick < 48; tick++) autoplay.진행(.125f, true);
                float walked = Vector2.Distance(playerKing.위치, startPosition);
                Require(walked >= 1.5f && autoplay.상태.진영들[0].쿨다운.All(time => time <= 0), "Player king did not walk in autoplay without a skill");

                var playerAI = ((Array)typeof(전장규칙).GetField("진영AI들", Private).GetValue(autoplay)).GetValue(0);
                Array.Fill(autoplay.상태.진영들[0].쿨다운, 999);
                while (autoplay.상태.시간 < 600 && autoplay.상태.승자 < 0 && !(bool)raid.GetValue(playerAI))
                    autoplay.진행(.125f, true);
                Require((bool)raid.GetValue(playerAI), "Player autoplay never gathered a raid");
                var launchPosition = playerKing.위치;
                for (int tick = 0; tick < 64; tick++) autoplay.진행(.125f, true);
                float marched = Vector2.Distance(playerKing.위치, launchPosition);
                Require(marched >= 3f, "Player king did not walk with the autoplay raid");
                var harvests = new int[전장규칙.재화이름.Length];
                var gatherCounts = autoplay.상태.진영들[0].자원별채집횟수;
                for (int i = 0; i < gatherCounts.Length; i++) harvests[resources[i].재화번호] += gatherCounts[i];
                Require(harvests.Skip(1).Any(count => count > 0), "Autoplay gathered only wood on the battlefield");
                Debug.Log($"[BattleAI] seed={seed}, autoplay king walked {walked:F1} tiles to rally and {marched:F1} on raid; gathers (wood, stone, copper, iron, mineral)={string.Join(", ", harvests)}");
            }
            Require(confirmedBrushWatch, "No scout reached and held a brush watch position");
            CombatBalanceChecks(floor, obstacles, terrain, navConstructor, resources, starts);
            RaidTargetChecks(floor, obstacles, resources, starts);
            SoldierTacticsChecks(floor, obstacles, resources, starts);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    static void MarchMusicChecks()
    {
        var musicType = typeof(전장규칙).Assembly.GetType("전장음악상황");
        var poll = musicType.GetMethod("갱신");
        var order = musicType.GetMethod("이동명령기록");
        var slots = new 전장대상[전장규칙.왕번호(1) + 2];
        var state = new 전장상태 { 대상들 = slots };
        var army = Enumerable.Range(전장규칙.병사시작칸, 8).ToArray();
        foreach (int slot in army) slots[slot] = new 전장대상 { 종류 = 대상종류.병사, 체력 = 100 };
        var hall = new 전장대상 { 팀 = 1, 종류 = 대상종류.회관, 체력 = 500, 위치 = new Vector2(45, 0) };
        slots[전장규칙.회관번호(1)] = hall;
        object StartMusic()
        {
            var music = Activator.CreateInstance(musicType, state, 0, (Func<전장대상, bool>)(_ => false));
            poll.Invoke(music, new object[] { state, false });
            return music;
        }
        string Advance(object music, Action move = null, bool explored = false)
        {
            object mood = null;
            for (int i = 0; i < 4; i++)
            {
                move?.Invoke();
                state.시간 += .25f;
                mood = poll.Invoke(music, new object[] { state, explored });
            }
            return mood.ToString();
        }
        var distant = new Vector2(25, 0);
        var player = StartMusic();
        order.Invoke(player, new object[] { state, army, distant });
        Require(Advance(player) == "일반", "Unexecuted move order triggered marching music");
        foreach (int slot in army) { slots[slot].이동중 = true; slots[slot].목적지 = distant; }
        Require(Advance(player, () => { foreach (int slot in army) slots[slot].위치.x += .5f; }) == "행군",
            "A large moving army did not trigger music during an accepted long order");
        foreach (int slot in army) { slots[slot].이동중 = false; slots[slot].위치 = hall.위치 - Vector2.right * 8; }
        player = StartMusic();
        Require(Advance(player) == "일반", "March music revealed the enemy hall outside allied vision");
        Require(Advance(player, explored: true) == "행군", "Army near a discovered enemy hall did not trigger march music");
        slots[army[0]].위치 = hall.위치 - Vector2.right * 5;
        Require(Advance(StartMusic()) == "행군", "Stationary army outside the enemy hall did not trigger march music");
        slots[army[0]].체력 = 0;
        Require(Advance(StartMusic(), explored: true) == "일반", "Seven soldiers triggered large-army music");
        Debug.Log("[BattleAI] march music passed long move orders, enemy base presence, and fog/size guards");
    }

    static void ResourceExplorationChecks(Tilemap floor, Tilemap obstacles, Vector2[] starts)
    {
        var nav = new 격자길찾기(floor, obstacles);
        var point = CombatTestPoint(floor, nav);
        var resources = new 자원지점[5];
        for (int i = 0; i < resources.Length; i++)
            resources[i] = new 자원지점 { 숲나무 = true, 숲위치 = point, 재화번호 = i, 최대수량 = 100 };
        var rules = new 전장규칙(resources, nav, starts, 왕직업.지휘관, 왕직업.아처, 374);
        var state = rules.상태;
        var ai = ((Array)typeof(전장규칙).GetField("진영AI들", Private).GetValue(rules)).GetValue(0);
        var home = state.대상들[전장규칙.회관번호(0)].위치;
        var enemy = state.대상들[전장규칙.회관번호(1)].위치;
        void Call(string name, params object[] args) => typeof(전장규칙).GetMethod(name, Private).Invoke(rules, args);

        for (int i = 6; i < 14; i++)
            state.대상들[i] = new 전장대상
            {
                팀 = 0, 종류 = 대상종류.주민, 위치 = home, 체력 = 65, 최대체력 = 65, 채집터 = 0
            };
        var stock = state.진영들[0].재화;
        Array.Clear(stock, 0, stock.Length);
        Call("AI탐사기록", 0, ai);
        var discovered = (bool[])ai.GetType().GetField("발견자원").GetValue(ai);
        Array.Clear(discovered, 0, discovered.Length);
        discovered[0] = true;
        ai.GetType().GetField("탐사완료").SetValue(ai, true);
        ai.GetType().GetField("발견한적회관").SetValue(ai, enemy);
        Call("AI채집배정", 0, ai, home, stock, 0, 0);
        Require((bool)ai.GetType().GetField("자원탐사필요").GetValue(ai), "Wood-only workers did not request resource exploration");
        Call("AI정찰지휘", 0, ai, home, home, (enemy - home).normalized);
        var scouts = (Array)ai.GetType().GetField("정찰들").GetValue(ai);
        Require(scouts.Cast<object>().Any(scout => (int)scout.GetType().GetField("목표").GetValue(scout) >= 0),
            "Resource shortage did not restart scouting after finding the enemy hall");

        for (int i = 6; i < 14; i++) state.대상들[i].위치 = point;
        Call("AI탐사기록", 0, ai);
        Require(discovered.All(value => value), "Scout vision did not discover nearby resources");
        Call("AI채집배정", 0, ai, home, stock, 0, 0);
        for (int kind = 0; kind < resources.Length; kind++)
        {
            var collector = state.대상들.Skip(6).Take(8).FirstOrDefault(unit => unit.채집터 == kind);
            Require(collector != null, $"No worker was reassigned to uncovered resource {kind}");
            Call("자원채집", collector, 1);
            Require(stock[kind] == 1, $"Resource {kind} was not credited to the team");
        }
        Debug.Log("[BattleAI] resource shortage restarted scouting and workers gathered all five discovered resources");
    }

    static void RaidTargetChecks(Tilemap floor, Tilemap obstacles, 자원지점[] resources, Vector2[] starts)
    {
        var rules = new 전장규칙(resources, new 격자길찾기(floor, obstacles), starts, 왕직업.지휘관, 왕직업.애쉬, 374);
        var state = rules.상태;
        state.시간 = 10;
        Array.Fill(state.진영들[1].쿨다운, 999);
        var ai = ((Array)typeof(전장규칙).GetField("진영AI들", Private).GetValue(rules)).GetValue(1);
        ai.GetType().GetField("명령시간").SetValue(ai, 999f);
        ai.GetType().GetField("진격").SetValue(ai, true);
        var raiders = (System.Collections.Generic.HashSet<전장대상>)ai.GetType().GetField("출격대").GetValue(ai);
        var king = state.대상들[전장규칙.왕번호(1)];
        var position = king.위치;
        king.공격대기 = 999;
        state.대상들[0].위치 = position + Vector2.left * 20;

        전장대상 Target(int slot, int team, 대상종류 kind, float distance) =>
            state.대상들[slot] = new 전장대상 { 팀 = team, 종류 = kind, 체력 = 140, 최대체력 = 140,
                공격대기 = 999, 위치 = position + Vector2.right * distance };
        int resident = 6, hall = 전장규칙.회관번호(0), workshop = 2;
        int troop = 전장규칙.병사시작칸, tower = 전장규칙.시설시작(0) + 4;
        Target(resident, 0, 대상종류.주민, .5f);
        Target(hall, 0, 대상종류.회관, 1);
        Target(workshop, 0, 대상종류.제재소, 1.5f);
        Target(troop, 0, 대상종류.병사, 3);
        Target(tower, 0, 대상종류.아처타워, 2.5f);
        var hidden = Target(troop + 1, 0, 대상종류.병사, 2);
        hidden.훈련남음 = 1;
        var raider = Target(전장규칙.왕번호(1) + 전장규칙.병사시작칸, 1, 대상종류.병사, 0);

        rules.진행(0, true);
        Require(raider.표적 == resident, "Non-raiding soldier must retain nearest-target selection");
        raiders.Add(raider);
        var commandKing = typeof(전장규칙).GetMethod("AI왕지휘", Private);
        foreach (int slot in new[] { troop, tower, workshop, hall, resident })
        {
            rules.진행(0, true);
            commandKing.Invoke(rules, new object[] { 1, ai, king, position, position, -1 });
            Require(raider.표적 == slot && king.표적 == slot, $"Raid priority did not select {state.대상들[slot].종류}");
            state.대상들[slot].체력 = 0;
        }
        Debug.Log("[BattleAI] raid soldiers and king prioritized visible soldiers and towers, then buildings, hall, and residents");
    }

    static void SoldierTacticsChecks(Tilemap floor, Tilemap obstacles, 자원지점[] resources, Vector2[] starts)
    {
        var nav = new 격자길찾기(floor, obstacles);
        var rules = new 전장규칙(resources, nav, starts, 왕직업.지휘관, 왕직업.애쉬, 374);
        var state = rules.상태;
        var point = CombatTestPoint(floor, nav);
        int enemy = 전장규칙.왕번호(1), archer = 전장규칙.병사시작칸, bruiser = archer + 1;
        int clicked = enemy + 전장규칙.병사시작칸, wounded = clicked + 1, building = enemy + 2;
        전장대상 Put(int slot, int team, 대상종류 kind, float offset, int intelligence = 2, 병과 combatClass = 병과.일반)
        {
            var unit = new 전장대상 { 종류 = kind, 팀 = team, 병과 = combatClass, 힘 = 5, 민첩 = 5, 지능 = intelligence,
                체력 = 140, 최대체력 = 140, 공격대기 = 999, 위치 = point + Vector2.right * offset };
            return state.대상들[slot] = unit;
        }
        var bow = Put(archer, 0, 대상종류.병사, 0, 14, 병과.아처);
        var fighter = Put(bruiser, 0, 대상종류.병사, 0, 2, 병과.브루저);
        var captain = Put(enemy, 1, 대상종류.왕, 4);
        var ordered = Put(clicked, 1, 대상종류.병사, 3);
        Put(wounded, 1, 대상종류.병사, 2).체력 = 25;
        Put(building, 1, 대상종류.제재소, 1.5f);
        void Attack(int unit, int target) => rules.명령(0, new 전장명령 { 종류 = 명령종류.공격, 값 = target, 부대 = new[] { unit } });
        void Tick() => rules.진행(0, false);
        Attack(archer, building); Tick();
        Require(bow.표적 == building, "Low-intelligence archer used locked target tactics");
        bow.지능 = 15;
        Attack(archer, building); Tick();
        Require(bow.표적 == enemy, "Experienced archer ignored the captain near its objective");
        Attack(archer, clicked); Attack(bruiser, clicked); Tick();
        Require(bow.표적 == clicked && fighter.표적 == clicked, "A specialist ignored a living soldier explicitly targeted by the player");
        ordered.체력 = 0; Tick();
        Require(bow.표적 == enemy, "Experienced archer did not retarget near a dead soldier");
        captain.재등장 = 999; captain.체력 = 0; Tick();
        Require(bow.표적 == wounded, "Experienced archer did not retarget a low-health enemy");

        var nearest = Put(enemy + 6, 1, 대상종류.주민, .8f);
        var autoTarget = typeof(전장규칙).GetMethod("병사자동공격표적", Private);
        int Auto(int stage) => (int)autoTarget.Invoke(rules, new object[] { fighter, bruiser, stage, true });
        Require(Auto(0) == enemy + 6, "Bruiser abandoned an enemy already in melee distance");
        nearest.체력 = 0;
        state.대상들[building].체력 = 0;
        state.대상들[wounded].위치 = point + Vector2.right * 5;
        Require(Auto(0) == wounded && Auto(3) < 0, "Bruiser detection did not narrow with intelligence");

        var grow = typeof(전장규칙).GetMethod("병사지능진행", Private);
        var hurt = typeof(전장규칙).GetMethod("피해", Private);
        grow.Invoke(rules, new object[] { bow, 10f });
        Require(bow.지능 == 16, "Archer did not learn by surviving");
        hurt.Invoke(rules, new object[] { archer, 1f, 1 });
        state.시간 += 7;
        grow.Invoke(rules, new object[] { bow, 0f });
        Require(bow.지능 == 16, "Combat experience was granted before surviving the encounter");
        state.시간++;
        grow.Invoke(rules, new object[] { bow, 0f });
        Require(bow.지능 == 16, "A single hit granted large-battle intelligence");
        var markBattle = typeof(전장규칙).GetMethod("대규모교전확인", Private);
        var originalSlots = (전장대상[])state.대상들.Clone();
        var homeHall = state.대상들[전장규칙.회관번호(0)];
        var originalHallPosition = homeHall.위치;
        try
        {
            void Fight(int count, bool split)
            {
                state.시간 += 2;
                for (int i = 1; i < count; i++)
                {
                    int team = i % 2;
                    int slot = 전장규칙.왕번호(team) + 전장규칙.병사시작칸 + 20 + i / 2;
                    state.대상들[slot] = new 전장대상
                    {
                        종류 = 대상종류.병사, 팀 = team, 체력 = 140, 최대체력 = 140,
                        위치 = bow.위치 + Vector2.right * (split && team == 1 ? 24 : 1)
                    };
                    hurt.Invoke(rules, new object[] { slot, 1f, 1 - team });
                }
                hurt.Invoke(rules, new object[] { archer, 1f, 1 });
                markBattle.Invoke(rules, null);
                state.시간 += 7;
                grow.Invoke(rules, new object[] { bow, 0f });
                Require(bow.지능 == 16, "Combat experience was granted during the encounter");
                state.시간++;
                grow.Invoke(rules, new object[] { bow, 0f });
            }

            Fight(27, false);
            Require(bow.지능 == 16, "27 soldiers met the 28-soldier battle threshold");
            Fight(28, true);
            Require(bow.지능 == 16, "Distant participants were combined into one battle");
            homeHall.위치 = bow.위치;
            Fight(28, false);
            Require(bow.지능 == 28, "Surviving a 28-soldier battle did not grant intelligence");
            grow.Invoke(rules, new object[] { bow, 0f });
            Require(bow.지능 == 28, "The same battle granted intelligence twice");
            bow.지능 = 16;
            homeHall.위치 = bow.위치 + Vector2.up * 30;
            Fight(28, false);
            Require(bow.지능 == 34, "Distant combat did not grant the additional intelligence");
        }
        finally
        {
            Array.Copy(originalSlots, state.대상들, originalSlots.Length);
            homeHall.위치 = originalHallPosition;
        }
        bow.지능 = 99;
        grow.Invoke(rules, new object[] { bow, 30f });
        Require(bow.지능 == 100, "Soldier intelligence cap was not enforced");

        var enemyArcher = Put(wounded, 1, 대상종류.병사, -2, 35, 병과.아처);
        var flight = typeof(전장규칙).GetMethod("기본탄진행", Private);
        var dodge = typeof(전장규칙).GetMethod("브루저회피", Private);
        스킬지대 Arrow() => new() { 팀 = 1, 부착대상 = fighter, 발사자참조 = enemyArcher,
            대상 = bruiser, 공격종류 = 기본공격종류.화살, 남은시간 = 2, 위치 = fighter.위치, 피해량 = 2 };
        var firstArrow = Arrow();
        flight.Invoke(rules, new object[] { firstArrow, .05f });
        Require(firstArrow.팀 == 1 && fighter.체력 == 138, "Low-intelligence bruiser reflected an arrow");
        fighter.지능 = 35;
        스킬지대 rebound = null;
        for (int i = 0; i < 120 && rebound == null; i++)
        {
            var arrow = Arrow();
            fighter.체력 = 140;
            flight.Invoke(rules, new object[] { arrow, .05f });
            if (arrow.팀 == 0) rebound = arrow;
        }
        Require(rebound != null && rebound.대상 == wounded && fighter.체력 == 140, "Bruiser never reflected an incoming archer arrow");
        rebound.위치 = enemyArcher.위치;
        flight.Invoke(rules, new object[] { rebound, .05f });
        Require(enemyArcher.체력 == 139, "Reflected arrow did not return damage to the shooter");
        Require(!(bool)dodge.Invoke(rules, new object[] { enemyArcher, bruiser, enemyArcher.위치 }), "Mid-level bruiser dodged too early");
        fighter.지능 = 65;
        bool dodged = false;
        for (int i = 0; i < 160 && !dodged; i++)
            dodged = (bool)dodge.Invoke(rules, new object[] { enemyArcher, bruiser, enemyArcher.위치 });
        Require(dodged && fighter.위치.x > point.x, "Top-level bruiser did not dodge backward");

        var advance = typeof(전장규칙).GetMethod("유닛진행", Private);
        void ArcherRecovery(int intelligence, float distance)
        {
            bow.지능 = intelligence;
            bow.위치 = point;
            bow.공격대기 = .6f;
            enemyArcher.위치 = point + Vector2.right * distance;
            Attack(archer, wounded);
            advance.Invoke(rules, new object[] { archer, bow, .1f, 0f, false });
        }
        ArcherRecovery(15, 2);
        Require(bow.위치 == point, "Archer kited before unlocking the tactic");
        ArcherRecovery(35, 2);
        Require(bow.위치.x < point.x, "Player archer did not kite with sufficient intelligence");
        ArcherRecovery(35, 4.5f);
        Require(bow.위치 == point, "Mid-level archer kited at the top-level distance");
        ArcherRecovery(65, 4.5f);
        Require(bow.위치.x < point.x, "Top-level archer did not increase its kiting range");
        Debug.Log("[BattleAI] soldier learning, commanded archer priorities, kiting stages, bruiser detection, projectile reflection, and backward dodge passed");
    }

    static void CombatBalanceChecks(Tilemap floor, Tilemap obstacles, IDisposable terrain, ConstructorInfo constructor,
        자원지점[] resources, Vector2[] starts)
    {
        (전장규칙 rules, 격자길찾기 nav) Match()
        {
            var nav = (격자길찾기)constructor.Invoke(new object[] { floor, obstacles, terrain });
            return (new 전장규칙(resources, nav, starts, 왕직업.검사, 왕직업.애쉬, 374), nav);
        }
        object AI(전장규칙 rules) => ((Array)typeof(전장규칙).GetField("진영AI들", Private).GetValue(rules)).GetValue(1);

        foreach (bool king in new[] { false, true })
        {
            var (rules, nav) = Match();
            var targets = rules.상태.대상들;
            int start = 전장규칙.왕번호(1);
            var ai = AI(rules);
            ai.GetType().GetField("명령시간").SetValue(ai, 999f);
            var point = CombatTestPoint(floor, nav);
            var enemy = targets[0];
            enemy.위치 = enemy.목적지 = point + Vector2.right * 2;
            float enemyHealth = enemy.체력;
            var archer = king ? targets[start] : new 전장대상
            {
                팀 = 1, 종류 = 대상종류.병사, 병과 = 병과.아처, 체력 = 160, 최대체력 = 160, 힘 = 5, 민첩 = 5, 지능 = 35
            };
            if (!king) targets[start + 전장규칙.병사시작칸] = archer;
            archer.위치 = archer.목적지 = point;
            archer.표적 = 0;
            rules.진행(.05f, true);
            Require(archer.공격순번 > 0, "AI ranged unit did not fire");
            rules.진행(.1f, true);
            Require(archer.위치 == point, "AI moved during attack wind-up");
            rules.진행(.15f, true);
            Require(enemy.체력 < enemyHealth || rules.상태.지대들.Any(zone => zone.팀 == 1 && zone.종류 == 지대종류.기본투사체),
                "AI did not release its projectile");
            Require((archer.위치 - enemy.위치).sqrMagnitude > 4f, "AI did not step away during attack recovery");
            Debug.Log($"[BattleAI] {(king ? "ranged king" : "archer soldier")} fired before kiting");
        }

        var kingSlot = 전장규칙.왕번호(1);
        var dispatch = typeof(전장규칙).GetMethod("AI부대지휘", Private);
        foreach (var (seconds, required) in new[] { (360f, 20), (419.99f, 20), (420f, 25), (480f, 30),
            (540f, 35), (600f, 40), (660f, 45), (720f, 45) })
        {
            var (late, _) = Match();
            var lateAI = AI(late);
            var lateState = late.상태;
            var home = lateState.대상들[전장규칙.회관번호(1)].위치;
            var enemyHall = lateState.대상들[전장규칙.회관번호(0)].위치;
            lateAI.GetType().GetField("발견한적회관").SetValue(lateAI, enemyHall);
            lateState.시간 = seconds;
            var raiders = (System.Collections.Generic.HashSet<전장대상>)lateAI.GetType().GetField("출격대").GetValue(lateAI);
            전장대상 Soldier(int index)
            {
                var soldier = new 전장대상 { 팀 = 1, 종류 = 대상종류.병사, 체력 = 140, 최대체력 = 140, 위치 = home, 목적지 = home };
                lateState.대상들[kingSlot + 전장규칙.병사시작칸 + index] = soldier;
                return soldier;
            }
            void Dispatch() => dispatch.Invoke(late, new object[] { 1, lateAI, lateState.대상들[kingSlot], home, home, -1, true });
            for (int i = 0; i < required + 2; i++) Soldier(i);
            Soldier(3).병과 = 병과.아처;
            Soldier(4).병과 = 병과.브루저;
            Dispatch();
            Require(raiders.Count == 0, $"AI raided with {required - 1} gathered soldiers at {seconds}s");
            var last = Soldier(required + 2);
            last.위치 += Vector2.up * 10;
            Dispatch();
            Require(raiders.Count == 0, $"AI counted a soldier outside rally range at {seconds}s");
            last.위치 = home;
            Dispatch();
            Require(raiders.Count == required, $"AI did not launch {required} soldiers alongside its garrison at {seconds}s");
        }
        Debug.Log("[BattleAI] late raids require 20, 25, 30, 35, 40, and 45 gathered soldiers at minute boundaries, with a 45-unit cap");

        var (economy, _) = Match();
        var economyState = economy.상태;
        var economyHome = economyState.대상들[전장규칙.회관번호(1)].위치;
        economyState.시간 = 360;
        Array.Fill(economyState.진영들[1].재화, 1000);
        var place = typeof(전장규칙).GetMethod("배치", Private);
        for (int i = 0; i < 15; i++)
            place.Invoke(economy, new object[] { kingSlot + 6 + i, 1, 대상종류.주민, economyHome + Vector2.up, 65f });
        for (int i = 0; i < 10; i++)
            place.Invoke(economy, new object[] { kingSlot + 전장규칙.병사시작칸 + i, 1, 대상종류.병사, economyHome + Vector2.up * 3, 140f });
        place.Invoke(economy, new object[] { 전장규칙.시설시작(1) + 2, 1, 대상종류.아처훈련소, economyHome + Vector2.up * 2, 320f });
        int maxQueued = 0;
        for (int i = 0; i < 320; i++)
        {
            economy.진행(.125f, true);
            maxQueued = Mathf.Max(maxQueued, economyState.진영들[1].훈련대기인원);
        }
        int soldiers = 전장규칙.수량(economyState, 1, 대상종류.병사);
        Require(maxQueued == 2 && soldiers > 10
            && !economyState.대상들.Any(u => u is { 팀: 1, 생존: true, 병과: 병과.아처 or 병과.브루저 }),
            "Late AI did not prioritize filling its army");
        Debug.Log($"[BattleAI] late economy filled two training reservations and expanded from 10 to {soldiers} soldiers");
    }

    static Vector2 CombatTestPoint(Tilemap floor, 격자길찾기 nav)
    {
        foreach (var cell in floor.cellBounds.allPositionsWithin)
        {
            var point = (Vector2)floor.GetCellCenterWorld(cell);
            if (point.sqrMagnitude < 36 && nav.정적통행가능(point) && nav.정적통행가능(point + Vector2.left)
                && nav.정적통행가능(point + Vector2.right * 2)) return point;
        }
        throw new InvalidOperationException("No open terrain for combat checks");
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
