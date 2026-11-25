using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class BuildingInteractionChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Field(object source, string name) => source.GetType().GetField(name, Private).GetValue(source);
    static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Write(string name, string text) { Directory.CreateDirectory("Temp/BuildingInteraction"); File.WriteAllText("Temp/BuildingInteraction/"+name,text); }

    sealed class Fixture
    {
        public readonly 전장규칙 Rules;
        public readonly 격자길찾기 Nav;
        public readonly 자원지점[] Resources;
        public 전장상태 State => Rules.상태;
        public Vector2 Hall => State.대상들[1].위치;
        public Fixture()
        {
            var source=UnityEngine.Object.FindFirstObjectByType<신들의전쟁>();
            Nav=new 격자길찾기((Tilemap)Field(source,"바닥타일맵"),(Tilemap)Field(source,"장애물타일맵"));
            Resources=(자원지점[])Field(source,"자원들");
            Rules=new 전장규칙(Resources,Nav,Array.ConvertAll((Transform[])Field(source,"시작지점들"),t=>(Vector2)t.position),왕직업.수호자,왕직업.수호자,831);
            foreach(var t in State.진영들) {Array.Fill(t.재화,100000);t.심부름=4;t.제재시간=99999;}
            foreach(var u in State.대상들)if(u!=null && !u.건물) {u.체력=0;u.재등장=99999;}
        }
        public 전장대상 Place(int slot,int team,대상종류 type,Vector2 p,float health=180)
            => (전장대상)typeof(전장규칙).GetMethod("배치",Private).Invoke(Rules,new object[]{slot,team,type,p,health});
        public bool Can(int kind,Vector2 p)
        {
            var args=new object[]{State,Nav,Resources,0,kind,p,Vector2.zero};
            return (bool)typeof(전장규칙).GetMethod("건설가능",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);
        }
        public void Build(int kind,Vector2 p) => Rules.명령(0,new 전장명령{종류=명령종류.건설,값=kind,위치=p});
        public void Step(float seconds) {for(int i=0;i<Mathf.CeilToInt(seconds/.05f);i++)Rules.진행(.05f,false);}
        public Vector2 Point(int kind,Vector2 center,float min=0,float max=8)
        {
            for(int y=-8;y<=8;y++)for(int x=-8;x<=8;x++)
            {
                var p=center+new Vector2(x,y);float d=Vector2.Distance(p,center);
                if(d>=min && d<=max && Can(kind,p))return p;
            }
            throw new Exception("No construction fixture point");
        }
    }

    [MenuItem("DivineWar/Validation/Building Interaction")]
    public static void Run()
    {
        var results=new StringBuilder();
        try
        {
            void Check(string name,Action test) {test();results.AppendLine("PASS "+name);}
            Check("Ground orders move selected allies past nearby enemies and reject foreign units",()=> {
                var f=new Fixture();
                var p=f.Point(0,f.Hall,3,6);
                var soldier=f.Place(6,0,대상종류.병사,p);
                var enemy=f.Place(전장규칙.왕번호(1)+6,1,대상종류.병사,p+Vector2.right*2);
                var originalEnemyOrder=(enemy.목적지,enemy.이동중);
                var destination=p+Vector2.down*2;
                f.Rules.명령(0,new 전장명령 {
                    종류=명령종류.부대이동,위치=destination,
                    부대=new[]{6,전장규칙.왕번호(1)+6,-1}
                });
                Require(soldier.이동중 && soldier.목적지==destination,
                    "Selected allied soldier did not accept ground destination");
                Require((enemy.목적지,enemy.이동중)==originalEnemyOrder,"Foreign soldier accepted another team's command");
                f.Rules.진행(.05f,false);
                Require(soldier.표적<0,"Nearby enemy interrupted the explicit ground order");
            });
            Check("Authored flag slots, sprite import, menu and UI controls",()=> {
                var source=UnityEngine.Object.FindFirstObjectByType<신들의전쟁>();
                var slots=(SpriteRenderer[])Field(source,"깃발슬롯");
                Require(slots?.Length==8 && slots.All(r=>r!=null),"Flag slots not authored/wired");
                Require(Field(source,"깃발버튼")!=null,"Flag menu missing");
                Require(Resources.LoadAll<Sprite>("Buildings/flags").Length==16,"Flag art not sliced");
                string uxml=File.ReadAllText("Assets/DivineWar/전장.uxml");
                foreach(var name in new[]{"training-cost","select-regular","select-bruiser","select-archer","restore-selection"})Require(uxml.Contains(name),"Missing UI "+name);
            });
            Check("Large buildings reserve one empty cell and all buildings reject overlap",()=> {
                foreach(int kind in new[]{0,1,4,5,6,7,8,9,10,11,12})
                {
                    var f=new Fixture();var p=f.Point(0,f.Hall,3,6);f.Place(2,0,대상종류.제재소,p,210);
                    if(kind==전장규칙.타워건설)
                        f.Place(전장규칙.병사시작칸,0,대상종류.병사,f.Hall+Vector2.down).병과=병과.아처;
                    Require(!f.Can(kind,p),"Building preview accepts an occupied cell: "+kind);
                    var before=f.State.진영들[0].재화.ToArray();
                    f.Build(kind,p);
                    Require(before.SequenceEqual(f.State.진영들[0].재화),"Overlapping order spent resources: "+kind);
                    foreach(var d in new[]{Vector2.left,Vector2.right,Vector2.up,Vector2.down,Vector2.one})
                        Require(!f.Can(kind,p+d),"Building preview allows placement in large building clearance: "+kind);
                    f.Build(kind,p+Vector2.right);
                    Require(before.SequenceEqual(f.State.진영들[0].재화),"Large building clearance order spent resources: "+kind);
                    bool found=false;
                    foreach(var d in new[]{Vector2.left,Vector2.right,Vector2.up,Vector2.down})
                    {
                        var next=p+d*2;if(!f.Can(kind,next))continue;
                        f.Build(kind,next);
                        Require(f.State.대상들.Any(u=>u is {생존:true,건물:true} && u.위치==next),"Building with clearance rejected: "+kind);
                        found=true;break;
                    }
                    Require(found,"No building fixture with clearance: "+kind);
                }
                var small = new Fixture();
                var fence = small.Point(4,small.Hall,3,6);
                small.Place(전장규칙.울타리시작칸,0,대상종류.나무울타리,fence);
                Require(new[]{Vector2.left,Vector2.right,Vector2.up,Vector2.down}.Any(d=>small.Can(5,fence+d)),
                    "Small fence sections can still be placed side by side");
            });
            Check("Flag requires existing territory, expands only after completion and enables remote building",()=> {
                var f=new Fixture();var p=f.Point(12,f.Hall,6,7.9f);
                Vector2 remote=default;bool found=false;
                f.Build(12,p);var flag=f.State.대상들[전장규칙.깃발시작칸];Require(flag is {생존:true,완공대기:>0},"Flag construction failed");
                for(int y=-7;y<=7 && !found;y++)for(int x=-7;x<=7 && !found;x++)
                {
                    var q=p+new Vector2(x,y);if(Vector2.Distance(q,p)>8 || Vector2.Distance(q,f.Hall)<=9 || !f.Nav.정적통행가능(q))continue;
                    // Temporarily complete only for candidate validation, then restore the real construction timer.
                    float time=flag.완공대기;flag.완공대기=0;bool can=f.Can(0,q);flag.완공대기=time;
                    if(can){remote=q;found=true;}
                }
                Require(found,"No flag extension fixture");Require(!f.Can(0,remote),"Unfinished flag expanded territory");
                int wood=f.State.진영들[0].재화[0];f.Build(0,remote);Require(f.State.진영들[0].재화[0]==wood,"Remote command charged before flag finished");
                f.Step(6.1f);Require(f.Can(0,remote),"Completed flag did not extend construction");f.Build(0,remote);
                Require(f.State.대상들[2] is {생존:true},"Remote building failed");
            });
            Check("Enemy and destroyed flags do not grant territory; flags can chain and stay within cap",()=> {
                var f=new Fixture();var p=f.Hall+Vector2.right*7;var q=f.Hall+Vector2.right*13;
                var flag=f.Place(전장규칙.깃발시작칸,0,대상종류.깃발,p);Require(전장규칙.건설영역안(f.State,0,q),"Flag radius missing");
                Require(!전장규칙.건설영역안(f.State,1,q),"Enemy obtained territory");flag.체력=0;
                Require(!전장규칙.건설영역안(f.State,0,q),"Destroyed flag retained territory");flag.체력=180;
                var chained=f.Place(전장규칙.깃발시작칸+1,0,대상종류.깃발,q);
                Require(전장규칙.건설영역안(f.State,0,q+Vector2.right*7),"Chain did not expand");
                for(int i=2;i<4;i++)f.Place(전장규칙.깃발시작칸+i,0,대상종류.깃발,p+Vector2.up*i*3);
                Require(!f.Can(12,f.Hall+Vector2.down*4),"Flag cap exceeded");
            });
            Check("All school quotes match authoritative training eligibility",()=> {
                foreach(var kind in new[]{대상종류.훈련소,대상종류.아처훈련소,대상종류.브루저훈련소})
                {
                    var school=new 전장대상{팀=0,종류=kind,체력=320};
                    var worker=new 전장대상{팀=0,종류=대상종류.주민,체력=65};
                    Require(전장규칙.훈련가능유닛(0,school,worker)==(kind==대상종류.훈련소),"Resident must train at normal school first");worker.훈련소=4;
                    Require(!전장규칙.훈련가능유닛(0,school,worker),"Queued resident quoted twice");worker.훈련소=-1;worker.팀=1;
                    Require(!전장규칙.훈련가능유닛(0,school,worker),"Enemy quoted");worker.팀=0;worker.종류=대상종류.병사;
                    Require(전장규칙.훈련가능유닛(0,school,worker)==(kind!=대상종류.훈련소),"Wrong normal soldier upgrade quote");
                    worker.병과=병과.아처;Require(!전장규칙.훈련가능유닛(0,school,worker),"Specialist quoted for repeat training");
                    school.완공대기=1;worker.병과=병과.일반;Require(!전장규칙.훈련가능유닛(0,school,worker),"Unfinished school quote is trainable");
                }
                Require(전장규칙.비용[3].SequenceEqual(new[]{15,8,4,3,1}),"Quote base cost changed");
            });
            Check("Exact class filters exclude residents, foreign units, dead units and summons",()=> {
                foreach(var kind in new[]{병과.일반,병과.브루저,병과.아처})
                {
                    var u=new 전장대상{팀=0,종류=대상종류.병사,체력=105,병과=kind};
                    foreach(var filter in new[]{병과.일반,병과.브루저,병과.아처})Require(전장규칙.병과선택가능(u,0,filter)==(kind==filter),"Wrong class filter");
                    Require(!전장규칙.병과선택가능(u,1,kind),"Enemy selected");u.소환=소환종류.데이지;Require(!전장규칙.병과선택가능(u,0,kind),"Summon selected");
                    u.소환=소환종류.없음;u.체력=0;Require(!전장규칙.병과선택가능(u,0,kind),"Dead unit selected");
                    u.체력=65;u.종류=대상종류.주민;Require(!전장규칙.병과선택가능(u,0,kind),"Resident selected");
                }
            });
            Check("Extended slot snapshot roundtrip",()=> {
                var f=new Fixture();f.Place(전장규칙.깃발시작칸,0,대상종류.깃발,f.Hall+Vector2.right*6);
                var json=JsonUtility.ToJson(f.State);var data=전장상태압축.포장(json);
                var copy=JsonUtility.FromJson<전장상태>(전장상태압축.풀기(data,Encoding.UTF8.GetByteCount(json)));
                Require(copy.대상들.Length==전장규칙.전체칸 && copy.대상들[전장규칙.깃발시작칸].종류==대상종류.깃발,"Flag snapshot lost");
            });
            results.AppendLine("ALL PASSED");
        }
        catch(Exception e){results.AppendLine("FAILED "+e);Debug.LogException(e);}
        Write("results.txt",results.ToString());Debug.Log(results.ToString());
    }
}
