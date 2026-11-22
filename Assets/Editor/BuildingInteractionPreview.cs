using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;

// Temporary simulation data and existing authored renderers only. Exit Play Mode to restore.
public static class BuildingInteractionPreview
{
    static 신들의전쟁 hoverGame;
    static double hoverUntil;
    static EditorWindow previewWindow;
    static bool originalMaximized;
    static int hoverSlot=전장규칙.시설시작칸;
    static IVisualElementScheduledItem hoverSchedule;
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Field(object o,string name) => o.GetType().GetField(name,Private).GetValue(o);
    static void Set(object o,string name,object value) => o.GetType().GetField(name,Private).SetValue(o,value);
    static void Call(object o,string name,params object[] args) => o.GetType().GetMethod(name,Private).Invoke(o,args);
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Write(string text){Directory.CreateDirectory("Temp/BuildingInteraction");File.WriteAllText("Temp/BuildingInteraction/live.txt",text);}

    [MenuItem("DivineWar/Building Interaction/Prepare Live Preview")]
    public static void Prepare()
    {
        Require(EditorApplication.isPlaying,"Enter Play Mode first");
        var game=UnityEngine.Object.FindFirstObjectByType<신들의전쟁>();
        var rules=(전장규칙)Field(game,"규칙");var state=rules.상태;
        var nav=(격자길찾기)Field(game,"길찾기");var king=state.대상들[0];var p=king.위치;
        bool found=false;float closest=float.MaxValue;
        for(int y=-5;y<=5;y++)for(int x=-5;x<=5;x++)
        {
            var q=king.위치+new Vector2(x,y);
            if(!nav.정적통행가능(q)||!nav.정적통행가능(q+Vector2.right*3)||!nav.정적통행가능(q-Vector2.right*3)||!nav.정적통행가능(q+Vector2.down*3))continue;
            if(x*x+y*y>=closest)continue;
            p=q;found=true;closest=x*x+y*y;
        }
        전장대상 Place(int slot,대상종류 kind,Vector2 point,float hp)
            => (전장대상)typeof(전장규칙).GetMethod("배치",Private).Invoke(rules,new object[]{slot,0,kind,point,hp});
        king.위치=p+Vector2.down*2;king.목적지=king.위치;king.이동중=false;king.표적=-1;
        Place(4,대상종류.훈련소,p-Vector2.right*3,320);
        Place(전장규칙.시설시작칸,대상종류.브루저훈련소,p,320);
        Place(전장규칙.시설시작칸+2,대상종류.아처훈련소,p+Vector2.right*3,320);
        Place(전장규칙.깃발시작칸,대상종류.깃발,p+new Vector2(3,-3),180);
        Place(6,대상종류.주민,p+new Vector2(-2,-2),65);
        Place(7,대상종류.주민,p+new Vector2(-1,-2),65);
        Place(30,대상종류.병사,p+new Vector2(0,-3),105);
        Place(31,대상종류.병사,p+new Vector2(1,-2),165).병과=병과.브루저;
        Place(32,대상종류.병사,p+new Vector2(2,-2),125).병과=병과.아처;
        Set(rules,"적명령시간",99999f);Set(game,"자동추적",false);Set(game,"배치",-1);
        Set(game,"일시정지",true);
        Call(game,"건축목록표시",false);
        var selected=(HashSet<int>)Field(game,"선택부대");selected.Clear();selected.UnionWith(new[]{6,7,30,31,32});
        Call(game,"선택훈련안내갱신");
        // Invoke the actual Clickable callback list, which also backs pointer and keyboard clicks.
        void Submit(string name)
        {
            var root=((UIDocument)game.GetComponent<UIDocument>()).rootVisualElement;
            var button=root.Q<Button>(name);
            typeof(Clickable).GetMethod("Invoke",Private).Invoke(button.clickable,new object[]{null});
        }
        Set(game,"일시정지",false);
        Call(game,"선택훈련안내갱신");
        Submit("select-resident");Require(selected.SetEquals(new[]{6,7}),"Resident UI filter failed");
        Submit("select-bruiser");Require(selected.SetEquals(new[]{31}),"Bruiser UI filter failed");
        Submit("select-archer");Require(selected.SetEquals(new[]{32}),"Archer UI switch failed");
        Submit("select-regular");Require(selected.SetEquals(new[]{30}),"Regular soldier UI filter failed");
        Submit("restore-selection");Require(selected.SetEquals(new[]{6,7,30,31,32}),"Selection restore failed");
        var camera=(Camera)Field(game,"전장카메라");camera.transform.position=new Vector3(p.x,p.y-1,camera.transform.position.z);
        Call(game,"탐사갱신");Call(game,"장면반영",.05f);Call(game,"선택훈련안내갱신");
        var pixel=camera.WorldToScreenPoint(p+Vector2.up*.5f);
        Directory.CreateDirectory("Temp/BuildingInteraction");File.WriteAllText("Temp/BuildingInteraction/pointer.txt",$"{pixel.x:F0},{pixel.y:F0}; screen {Screen.width},{Screen.height}");
        var flagButton=(메뉴버튼)Field(game,"깃발버튼");
        flagButton.onClick.Invoke();Require((int)Field(game,"배치")==전장규칙.깃발건설,"Flag button callback failed");
        flagButton.onClick.Invoke();Require((int)Field(game,"배치")==-1,"Flag button toggle failed");
        Call(game,"건축목록표시",false);
        Write("PASS: Live button callbacks filter residents, bruisers, archers, regular soldiers, restore the original group and toggle flag placement.\nHover target: bruiser school at "+pixel);
    }

    [MenuItem("DivineWar/Building Interaction/Inspect Live Hover")]
    public static void Hover()
    {
        Require(EditorApplication.isPlaying,"Enter Play Mode first");
        var game=UnityEngine.Object.FindFirstObjectByType<신들의전쟁>();
        hoverGame=game;hoverUntil=EditorApplication.timeSinceStartup+1.5;
        previewWindow=Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(w=>w.GetType().Name=="GameView");
        if(previewWindow!=null) {originalMaximized=previewWindow.maximized;previewWindow.maximized=true;previewWindow.Focus();}
        var root=game.GetComponent<UIDocument>().rootVisualElement;
        hoverSchedule?.Pause();hoverSchedule=root.schedule.Execute(UpdateHover).Every(16);
        EditorApplication.update-=HoverTick;EditorApplication.update+=HoverTick;
        EditorApplication.isPaused=false;
    }

    static void HoverTick()
    {
        if(!EditorApplication.isPlaying || hoverGame==null) {EditorApplication.update-=HoverTick;return;}
        var state=((전장규칙)Field(hoverGame,"규칙")).상태;
        var camera=(Camera)Field(hoverGame,"전장카메라");
        var point=state.대상들[전장규칙.시설시작칸].위치;
        camera.transform.position=new Vector3(point.x,point.y-1,camera.transform.position.z);
        if(EditorApplication.timeSinceStartup<hoverUntil)return;
        EditorApplication.isPaused=true;EditorApplication.update-=HoverTick;hoverSchedule?.Pause();
        var root=hoverGame.GetComponent<UIDocument>().rootVisualElement;
        Write(File.ReadAllText("Temp/BuildingInteraction/live.txt")+"\nHover display="+root.Q("training-cost").resolvedStyle.display+" bounds="+root.Q("training-cost").worldBound+" title="+root.Q<Label>("training-cost-title").text+" total="+root.Q<Label>("training-cost-total").text+" focused="+Application.isFocused);
    }

    static void UpdateHover()
    {
        if(hoverGame==null)return;
        var state=((전장규칙)Field(hoverGame,"규칙")).상태;
        var camera=(Camera)Field(hoverGame,"전장카메라");
        var pixel=(Vector2)camera.WorldToScreenPoint(state.대상들[hoverSlot].위치+Vector2.up*.5f);
        InputState.Change(Mouse.current.position,pixel);Set(hoverGame,"현재마우스",pixel);
        Call(hoverGame,"선택훈련안내갱신");
    }

    [MenuItem("DivineWar/Building Interaction/Check All Live Quotes")]
    static void AllQuotes()
    {
        Require(EditorApplication.isPlaying,"Enter Play Mode first");
        hoverGame=UnityEngine.Object.FindFirstObjectByType<신들의전쟁>();
        var root=hoverGame.GetComponent<UIDocument>().rootVisualElement;
        var state=((전장규칙)Field(hoverGame,"규칙")).상태;
        var selected=(HashSet<int>)Field(hoverGame,"선택부대");
        selected.Clear();selected.UnionWith(new[]{6,7});
        foreach(int slot in new[]{전장규칙.시설시작칸,전장규칙.시설시작칸+2})
        {
            Call(hoverGame,"훈련비용안내표시",slot);
            Require(root.Q<Label>("training-cost-total").text.Contains("일반 병사")
                && root.Q<Label>("training-cost-detail").text.Contains("먼저"),"Resident-only quote did not explain soldier prerequisite");
        }
        selected.Clear();selected.UnionWith(new[]{6,7,30,31,32});
        foreach(int slot in new[]{4,전장규칙.시설시작칸,전장규칙.시설시작칸+2})
        {
            Call(hoverGame,"훈련비용안내표시",slot);
            string total=root.Q<Label>("training-cost-total").text;
            Require(total.Contains(slot==4?"나무 30":"나무 15"),"Wrong total quote for school "+slot+": "+total);
        }
        int iron=state.진영들[0].재화[3];state.진영들[0].재화[3]=0;Call(hoverGame,"훈련비용안내표시",전장규칙.시설시작칸);
        Require(root.Q<Label>("training-cost-detail").text.Contains("부족"),"Shortage not explained");
        state.진영들[0].재화[3]=iron;hoverSlot=전장규칙.시설시작칸;Call(hoverGame,"훈련비용안내표시",hoverSlot);
        Write(File.ReadAllText("Temp/BuildingInteraction/live.txt")+"\nPASS: Normal school quotes two residents; both specialized schools quote only one normal soldier and exclude residents; shortage state is explained.");
    }

    [MenuItem("DivineWar/Building Interaction/Restore Preview Layout")]
    static void RestoreLayout()
    {
        EditorApplication.update-=HoverTick;
        hoverSchedule?.Pause();
        if(previewWindow!=null)previewWindow.maximized=originalMaximized;
    }

    [MenuItem("DivineWar/Building Interaction/Check Filter Keys")]
    static void FilterKeys()
    {
        Require(EditorApplication.isPlaying,"Enter Play Mode first");
        var game=UnityEngine.Object.FindFirstObjectByType<신들의전쟁>();
        var selected=(HashSet<int>)Field(game,"선택부대");
        var previous=Keyboard.current;
        var keyboard=InputSystem.AddDevice<Keyboard>();
        try
        {
            var keys=new[]{Key.Z,Key.X,Key.C,Key.V};
            int[][] expected={new[]{6,7},new[]{30},new[]{31},new[]{32}};
            for(int i=0;i<keys.Length;i++)
            {
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys[i]));InputSystem.Update();
                bool handled=(bool)game.GetType().GetMethod("병과선택단축키",Private).Invoke(game,new object[]{keyboard});
                Require(handled && selected.SetEquals(expected[i]),"Wrong filter key "+keys[i]);
            }
            const int woodenFenceBuildId=4;
            Set(game,"배치",woodenFenceBuildId);
            bool ignored=(bool)game.GetType().GetMethod("병과선택단축키",Private).Invoke(game,new object[]{keyboard});
            Require(!ignored && (int)Field(game,"배치")==woodenFenceBuildId,"Filter stole the fence rotation key");
            Set(game,"배치",-1);Call(game,"선택복원");
            Require(selected.SetEquals(new[]{6,7,30,31,32}),"Key filters lost the original selection");
            Write(File.ReadAllText("Temp/BuildingInteraction/live.txt")+"\nPASS: Z residents, X regular soldiers, C bruisers, V archers; original selection restores; building placement keeps V for rotation.");
        }
        finally {InputSystem.RemoveDevice(keyboard);previous?.MakeCurrent();}
    }
}
