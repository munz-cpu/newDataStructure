using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TrialChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Field(신들의전쟁 source, string name) => typeof(신들의전쟁).GetField(name, Private).GetValue(source);
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    static void Write(string file, string text) { Directory.CreateDirectory("Temp/Trials"); File.WriteAllText("Temp/Trials/" + file, text); }

    [MenuItem("DivineWar/Trials/Prepare Play Preview")]
    static void PreparePreview()
    {
        Require(EditorApplication.isPlaying,"Enter Play Mode first");
        var game=UnityEngine.Object.FindFirstObjectByType<신들의전쟁>();
        var rules=(전장규칙)Field(game,"규칙");var state=rules.상태;
        var point=state.시련들[0].위치;
        var king=state.대상들[0];king.위치=point+Vector2.down*2;king.목적지=king.위치;king.이동중=false;king.표적=-1;
        king.체력=king.최대체력;king.무적=9999;
        typeof(전장규칙).GetField("적명령시간",Private).SetValue(rules,99999f);
        typeof(신들의전쟁).GetField("자동추적",Private).SetValue(game,false);
        var camera=(Camera)Field(game,"전장카메라");camera.transform.position=new Vector3(point.x,point.y,camera.transform.position.z);
        typeof(신들의전쟁).GetMethod("탐사갱신",Private).Invoke(game,null);
        typeof(신들의전쟁).GetMethod("장면반영",Private).Invoke(game,new object[]{.05f});
        var pixel=camera.WorldToScreenPoint(point+Vector2.up*.35f);
        Write("pointer.txt",$"{pixel.x:F0},{pixel.y:F0} screen {Screen.width},{Screen.height}");
    }

    [MenuItem("DivineWar/Trials/Preview Combat")]
    static void PreviewCombat()
    {
        Require(EditorApplication.isPlaying,"Enter Play Mode first");
        var game=UnityEngine.Object.FindFirstObjectByType<신들의전쟁>();var rules=(전장규칙)Field(game,"규칙");
        rules.명령(0,new 전장명령{종류=명령종류.시련활성화,값=0});
        for(int i=0;i<33;i++)rules.진행(.05f,false);
        var boss=rules.상태.대상들[전장규칙.시련보스번호(0)];
        Require(boss is {생존:true},"Trial did not activate");
        rules.상태.시련들[0].패턴대기=0;
        for(int i=0;i<6 && rules.상태.시련들[0].패턴예고<=0;i++) rules.진행(.05f,false);
        typeof(신들의전쟁).GetMethod("장면반영",Private).Invoke(game,new object[]{.05f});
        EditorApplication.isPaused=true;
    }

    [MenuItem("DivineWar/Trials/Wire Authored Renderers")]
    static void Wire()
    {
        if(EditorApplication.isPlaying) throw new Exception("Wire in Edit Mode only");
        var source=UnityEngine.Object.FindFirstObjectByType<신들의전쟁>();
        var unit=((SpriteRenderer[])Field(source,"슬롯"))[전장규칙.병사시작칸];
        var markers=new 시련표식[6];
        var stones=Resources.LoadAll<Sprite>("Trials/trial-stone").OrderBy(s=>s.name).ToArray();
        var bosses=Resources.LoadAll<Sprite>("Trials/trial-warden").OrderBy(s=>s.name).ToArray();
        var warning=Resources.LoadAll<Sprite>("Trials/trial-warning").First();
        for(int i=0;i<6;i++)
        {
            var marker=GameObject.Find("맵 시련/시련석 "+(i+1)).GetComponent<시련표식>();
            Undo.RecordObject(marker,"Wire trial renderers");
            marker.봉인그림=marker.GetComponent<SpriteRenderer>();
            marker.보스그림=GameObject.Find("맵 시련/시련 보스 "+(i+1)).GetComponent<SpriteRenderer>();
            marker.강타그림=GameObject.Find("맵 시련/시련 강타 "+(i+1)).GetComponent<SpriteRenderer>();
            foreach(var r in new[]{marker.봉인그림,marker.보스그림,marker.강타그림})
            {
                Undo.RecordObject(r,"Set trial material");r.sharedMaterial=unit.sharedMaterial;r.sortingLayerID=unit.sortingLayerID;
                r.enabled=false;EditorUtility.SetDirty(r);
            }
            marker.봉인그림.sprite=stones[0];marker.봉인그림.enabled=true;
            marker.봉인그림.sortingOrder=(int)(-marker.transform.position.y*80)+1599;
            marker.보스그림.sprite=bosses[0];marker.강타그림.sprite=warning;
            markers[i]=marker;EditorUtility.SetDirty(marker);
        }
        var so=new SerializedObject(source);var field=so.FindProperty("시련들");field.arraySize=6;
        for(int i=0;i<6;i++)field.GetArrayElementAtIndex(i).objectReferenceValue=markers[i];
        so.ApplyModifiedProperties();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(source.gameObject.scene);
    }

    [MenuItem("DivineWar/Trials/Inspect Layout")]
    static void Inspect()
    {
        var s = UnityEngine.Object.FindFirstObjectByType<신들의전쟁>();
        var nav = new 격자길찾기((Tilemap)Field(s, "바닥타일맵"), (Tilemap)Field(s, "장애물타일맵"));
        var bounds = nav.전장범위;
        var result = "Bounds " + bounds + "\n";
        foreach (var uv in new[] {new Vector2(.25f,.3f),new Vector2(.75f,.7f),new Vector2(.25f,.7f),new Vector2(.75f,.3f),new Vector2(.5f,.3f),new Vector2(.5f,.7f)})
        {
            var target = new Vector2(Mathf.Lerp(bounds.xMin,bounds.xMax,uv.x), Mathf.Lerp(bounds.yMin,bounds.yMax,uv.y));
            Vector2 best = default; float distance = float.MaxValue;
            for (int y = Mathf.CeilToInt(bounds.yMin + 4); y < bounds.yMax - 4; y++)
            for (int x = Mathf.CeilToInt(bounds.xMin + 4); x < bounds.xMax - 4; x++)
            {
                var p = nav.중심정렬(new Vector2(x,y));
                bool open = true;
                for (int dy=-2;dy<=2 && open;dy++) for(int dx=-2;dx<=2;dx++) if(!nav.정적통행가능(p+new Vector2(dx,dy))) { open=false;break; }
                if (!open || (p-target).sqrMagnitude >= distance) continue;
                best=p; distance=(p-target).sqrMagnitude;
            }
            result += "Candidate " + best.ToString("F2") + "\n";
        }
        var renderers = (SpriteRenderer[])Field(s, "슬롯");
        result += "Soldier scale " + renderers[전장규칙.병사시작칸].transform.lossyScale + "\n";
        result += "Soldier material " + AssetDatabase.GetAssetPath(renderers[전장규칙.병사시작칸].sharedMaterial) + "\n";
        result += "Soldier layer " + renderers[전장규칙.병사시작칸].sortingLayerName;
        Write("layout.txt",result);
    }

    sealed class Fixture
    {
        public readonly 전장규칙 Rules;
        public readonly 격자길찾기 Nav;
        public readonly 자원지점[] Resources;
        public 전장상태 State => Rules.상태;
        public 전장대상 King => State.대상들[0];
        public readonly Vector2 Point;
        public Fixture()
        {
            var s = UnityEngine.Object.FindFirstObjectByType<신들의전쟁>();
            var nav = Nav = new 격자길찾기((Tilemap)Field(s,"바닥타일맵"),(Tilemap)Field(s,"장애물타일맵"));
            var starts = (Transform[])Field(s,"시작지점들");
            Resources = (자원지점[])Field(s,"자원들");
            Rules = new 전장규칙(Resources,nav,Array.ConvertAll(starts,t=>(Vector2)t.position),왕직업.수호자,왕직업.수호자,713);
            var markers=(시련표식[])Field(s,"시련들");
            Require(markers?.Length==6,"Six authored trials required");
            Rules.시련배치(Array.ConvertAll(markers,t=>(Vector2)t.transform.position)); Point=State.시련들[0].위치;
            foreach(var u in State.대상들) if(u != null && !u.건물)u.체력=0;
            King.체력=King.최대체력;King.위치=Point+Vector2.down;King.목적지=King.위치;King.재등장=9999;
            State.대상들[전장규칙.왕번호(1)].재등장=9999;
            foreach(var t in State.진영들) { t.심부름=4;t.제재시간=99999; }
        }
        public void Use(int index=0,int team=0) => Rules.명령(team,new 전장명령{종류=명령종류.시련활성화,값=index});
        public void Step(float seconds) { for(int i=0;i<Mathf.CeilToInt(seconds/.05f);i++) Rules.진행(.05f,false); }
        public 전장대상 Boss(int index=0) => State.대상들[전장규칙.시련보스번호(index)];
        public bool Can(int kind,Vector2 point)
        {
            var args=new object[]{State,Nav,Resources,0,kind,point,Vector2.zero};
            return (bool)typeof(전장규칙).GetMethod("건설가능",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);
        }
        public Vector2 BuildPoint(int kind)
        {
            Array.Fill(State.진영들[0].재화,10000);
            var hall=State.대상들[전장규칙.회관번호(0)].위치;
            for(int y=-7;y<=7;y++)for(int x=-7;x<=7;x++)
            {var p=hall+new Vector2(x,y);if(Can(kind,p))return Nav.중심정렬(p);}
            throw new Exception("No valid building point");
        }
        public void Win(int index=0)
        {
            King.위치=State.시련들[index].위치+Vector2.down;Use(index);Step(1.6f);
            Require(Boss(index) is {생존:true},"Boss not spawned");
            King.체력=10000;King.무적=9999;Boss(index).체력=1;
            King.위치=Boss(index).위치+Vector2.down*.7f;
            Rules.명령(0,new 전장명령{종류=명령종류.공격,값=전장규칙.시련보스번호(index),위치=Boss(index).위치});
            Step(1.2f);
            Require(State.시련들[index].단계==시련단계.보상,"Actual combat did not unlock reward");
        }
    }

    [MenuItem("DivineWar/Validation/Trials")]
    public static void Run()
    {
        var log = new System.Text.StringBuilder();
        try
        {
            void Check(string name,Action action) {action();log.AppendLine("PASS "+name);}
            Check("Authored markers and sliced Aseprite art",()=> {
                var s=UnityEngine.Object.FindFirstObjectByType<신들의전쟁>();
                var markers=(시련표식[])Field(s,"시련들");Require(markers.Length==6,"Markers missing");
                foreach(var m in markers)Require(m!=null && m.봉인그림!=null && m.보스그림!=null && m.강타그림!=null,"Missing authored renderer");
                Require(Resources.LoadAll<Sprite>("Trials/trial-stone").Length==4,"Stone art");
                Require(Resources.LoadAll<Sprite>("Trials/trial-warden").Length==24,"Boss art");
                Require(Resources.LoadAll<Sprite>("Trials/skeleton-soldier").Length==24,"Skeleton art");
            });
            Check("Range, invalid commands, delayed summon and duplicate activation",()=> {
                var f=new Fixture();f.King.위치+=Vector2.down*7;f.Use();Require(f.State.시련들[0].단계==시련단계.봉인,"Remote activation");
                f.Use(-1);f.Use(999);f.King.위치=f.Point;f.Use();f.Use();
                f.Step(1);Require(f.Boss()==null,"Boss spawned without warning");f.Step(.6f);
                Require(f.Boss() is {생존:true} && f.State.시련들[0].팀==0,"Spawn failed");
                Require(!f.Boss().건물 && f.Boss().최대체력==950,"Incorrect boss type or health");
            });
            Check("Boss melee, telegraphed slam, evasion and retreat reset",()=> {
                var f=new Fixture();f.Use();f.Step(1.6f);f.King.체력=10000;f.King.최대체력=10000;
                f.King.위치=f.Boss().위치+Vector2.down*.7f;float hp=f.King.체력;f.Step(1.5f);
                Require(f.King.체력<hp,"Boss melee dealt no damage");
                var trial=f.State.시련들[0];trial.패턴대기=0;f.Step(.05f);
                Require(trial.패턴==골렘패턴.지진강타 && trial.패턴예고>1,"No slam warning");
                f.King.위치=f.Boss().위치+Vector2.down*4;hp=f.King.체력;f.Step(1.3f);Require(f.King.체력==hp,"Slam ignored evasion");
                f.King.위치=f.Point+Vector2.down*15;f.Step(8.5f);Require(f.State.시련들[0].단계==시련단계.봉인 && !f.Boss().생존,"Retreat failed reset");
                f.King.위치=f.Point;f.Use();f.Step(1.6f);
                Require(f.Boss().체력==950 && trial.패턴==골렘패턴.없음 && trial.패턴예고==0,"Retry retained the last fight's state");
            });
            Check("Boss cycles slam, targeted rockfall and directional fissure",()=> {
                var f=new Fixture();f.Use();f.Step(1.6f);
                f.King.체력=f.King.최대체력=10000;
                var boss=f.Boss();var trial=f.State.시련들[0];
                void Warn(골렘패턴 expected)
                {
                    boss.공격대기=10;
                    trial.패턴대기=0;
                    f.Step(.3f);
                    Require(trial.패턴==expected && trial.패턴예고>0,"Missing warning for "+expected);
                }
                f.King.위치=boss.위치+Vector2.right;
                Warn(골렘패턴.지진강타);
                float hp=f.King.체력;f.Step(1.3f);
                Require(f.King.체력==hp-54,"Slam did not hit in its radius");

                f.King.위치=boss.위치+Vector2.right*4;
                Warn(골렘패턴.낙석);
                var center=trial.예고중심;
                Require(center==f.King.위치,"Rockfall was not aimed at the target");
                f.King.위치+=Vector2.up*2;hp=f.King.체력;f.Step(.4f);
                Require(trial.예고중심==center,"Rockfall tracked a moving target");
                f.Step(.9f);Require(f.King.체력==hp,"Rockfall hit outside the warning");

                f.King.위치=boss.위치+new Vector2(3,3);
                Warn(골렘패턴.지면균열);
                var copy=JsonUtility.FromJson<전장상태>(JsonUtility.ToJson(f.State)).시련들[0];
                Require(Mathf.Abs(trial.예고각도-45)<.01f,"Fissure was aimed at the wrong angle: "+trial.예고각도);
                Require(copy.패턴==trial.패턴 && (copy.예고중심-trial.예고중심).sqrMagnitude<.0001f
                    && Mathf.Abs(copy.예고각도-trial.예고각도)<.01f && copy.패턴예고>0,"Snapshot lost the directional warning");
                hp=f.King.체력;f.Step(1.15f);
                Require(f.King.체력==hp-61.5f,"Fissure missed inside the rotated warning");
                Warn(골렘패턴.지진강타);
                f.Step(1.3f);
                Warn(골렘패턴.낙석);
                hp=f.King.체력;f.Step(1.3f);
                Require(f.King.체력==hp-46.5f,"Rockfall missed a stationary target");
                f.King.위치=boss.위치+Vector2.up*4;
                Warn(골렘패턴.지면균열);
                f.King.위치+=Vector2.right*1.5f;hp=f.King.체력;f.Step(1.15f);
                Require(f.King.체력==hp,"Fissure hit a target that sidestepped the warning");
            });
            Check("Boss enrage accelerates the pattern timer without shortening the warning",()=> {
                var f=new Fixture();f.Use();f.Step(1.6f);
                var boss=f.Boss();var trial=f.State.시련들[0];
                boss.공격대기=10;trial.패턴대기=.6f;
                f.Step(.45f);Require(trial.패턴예고==0,"Normal phase cast too soon");
                boss.체력=boss.최대체력*.4f;trial.패턴대기=.6f;
                f.Step(.45f);
                Require(trial.패턴예고>1.1f,"Enraged phase did not cast sooner with a full warning");
            });
            Check("Combat victory, relic reward, duplicate reward rejection and respawn",()=> {
                var f=new Fixture();f.Win();f.State.시련들[0].보상=0;f.King.위치=f.Point;float max=f.King.최대체력;f.Use();f.Use();
                Require(f.State.진영들[0].시련유물==1 && 전장규칙.능력치(f.King).힘==12 && f.King.최대체력==max+40,"Relic duplication or missing stats");
                f.King.체력=0;f.King.재등장=.05f;f.Step(.1f);Require(f.King.최대체력==max+40 && 전장규칙.능력치(f.King).힘==12,"Relic lost on respawn");
            });
            Check("Skeleton reward waits for capacity, cannot be stolen, then grants once",()=> {
                var f=new Fixture();f.Win(1);f.State.시련들[1].보상=1;var point=f.State.시련들[1].위치;f.King.위치=point;
                f.State.진영들[0].훈련대기인원=48;f.Use(1);Require(f.State.시련들[1].단계==시련단계.보상,"Full army consumed reward");
                var enemy=f.State.대상들[전장규칙.왕번호(1)];enemy.체력=enemy.최대체력;enemy.위치=point;f.Use(1,1);
                Require(f.State.시련들[1].단계==시련단계.보상,"Enemy stole reward");
                f.State.진영들[0].훈련대기인원=0;f.Use(1);f.Use(1);
                var skeletons=f.State.대상들.Where(u=>u is {생존:true,해골병사:true}).ToArray();
                Require(skeletons.Length==3 && skeletons.All(u=>u.팀==0 && u.최대체력==640 && 전장규칙.능력치(u).힘==36 && 전장규칙.능력치(u).지능==0),"Wrong skeleton reward");
            });
            Check("Snapshot compression roundtrip",()=> {
                var f=new Fixture();f.Win(1);f.State.시련들[1].보상=1;f.Use(1);
                string json=JsonUtility.ToJson(f.State);var bytes=전장상태압축.포장(json);
                var copy=JsonUtility.FromJson<전장상태>(전장상태압축.풀기(bytes,System.Text.Encoding.UTF8.GetByteCount(json)));
                Require(copy.시련들.Length==f.State.시련들.Length && copy.시련들[1].단계==시련단계.완료,"Trial snapshot lost");
                Require(copy.대상들.Count(u=>u is {해골병사:true})==3,"Skeleton snapshot lost");
            });
            Check("Random rewards cover all four outcomes and exclude capped or reserved upgrades",()=> {
                var f=new Fixture();var draw=typeof(전장규칙).GetMethod("시련보상추첨",Private);
                int Draw()=>(int)draw.Invoke(f.Rules,new object[]{0});
                var outcomes=new System.Collections.Generic.HashSet<int>();
                for(int i=0;i<200;i++)outcomes.Add(Draw());
                Require(outcomes.SetEquals(new[]{0,1,2,3}),"Random reward pool missing outcomes");
                f.State.진영들[0].깃발한도증가=2;f.State.진영들[0].울타리한도증가=12;
                for(int i=0;i<100;i++)Require(Draw()<2,"Capped capacity upgrade rolled");
                f.State.진영들[0].깃발한도증가=1;f.State.진영들[0].울타리한도증가=0;
                f.State.시련들[0].팀=0;f.State.시련들[0].단계=시련단계.보상;f.State.시련들[0].보상=2;
                f.State.시련들[1].팀=0;f.State.시련들[1].단계=시련단계.보상;f.State.시련들[1].보상=3;
                for(int i=0;i<100;i++)Require(Draw()<2,"Unclaimed upgrade reservation ignored");
            });
            Check("Capacity rewards unlock authoritative construction once and survive snapshots and respawn",()=> {
                foreach(int reward in new[]{2,3})
                {
                    var f=new Fixture();int kind=reward==2?12:4;
                    var point=f.BuildPoint(kind);int initial=reward==2?전장규칙.기본깃발한도:전장규칙.기본울타리한도;
                    int start=reward==2?전장규칙.깃발시작칸:전장규칙.울타리시작칸;
                    var type=reward==2?대상종류.깃발:대상종류.나무울타리;
                    for(int i=0;i<initial;i++)f.State.대상들[start+i]=new 전장대상{팀=0,종류=type,체력=100,위치=new Vector2(9999+i,9999)};
                    Require(!f.Can(kind,point),"Base construction cap not enforced");
                    int wood=f.State.진영들[0].재화[0];
                    f.Rules.명령(0,new 전장명령{종류=명령종류.건설,값=kind,위치=point});
                    Require(f.State.진영들[0].재화[0]==wood,"Rejected capacity order spent resources");
                    var trial=f.State.시련들[0];trial.단계=시련단계.보상;trial.보상=reward;trial.팀=0;
                    f.Use(0,1);Require(trial.단계==시련단계.보상,"Opponent stole upgrade");
                    f.Use();f.Use();int expected=initial+(reward==2?1:전장규칙.울타리보상증가);
                    Require(전장규칙.건설정원(f.State,0,kind)==expected,"Upgrade missing or duplicated");
                    Require(전장규칙.건설정원(f.State,1,kind)==initial,"Upgrade affected opponent");
                    Require(f.Can(kind,point),"Reward did not unlock construction");
                    f.Rules.명령(0,new 전장명령{종류=명령종류.건설,값=kind,위치=point});
                    Require(f.State.대상들[start+initial] is {생존:true,완공대기:>0},"New building slot not used");
                    var copy=JsonUtility.FromJson<전장상태>(JsonUtility.ToJson(f.State));
                    Require(전장규칙.건설정원(copy,0,kind)==expected,"Snapshot lost capacity upgrade");
                    f.King.체력=0;f.King.재등장=.05f;f.Step(.1f);
                    Require(전장규칙.건설정원(f.State,0,kind)==expected,"Respawn lost capacity upgrade");
                }
            });
            Check("Combat rolls exactly one reward and leaves it stable until collection",()=> {
                var f=new Fixture();f.Win();var trial=f.State.시련들[0];int reward=trial.보상;
                Require(reward>=0 && reward<4,"Combat did not roll reward");f.Step(.5f);
                Require(trial.보상==reward,"Unclaimed reward rerolled");
                Require(f.State.진영들[0].깃발한도증가==0 && f.State.진영들[0].울타리한도증가==0,"Unclaimed upgrade applied early");
            });
            log.AppendLine("ALL PASSED");
        }
        catch(Exception e) {log.AppendLine("FAILED "+e);Debug.LogException(e);}
        Write("results.txt",log.ToString());Debug.Log(log.ToString());
    }
}
