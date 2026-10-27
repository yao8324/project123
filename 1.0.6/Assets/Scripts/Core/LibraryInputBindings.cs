using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcaneArchive.Core
{
    [DisallowMultipleComponent]
    public sealed class LibraryInputBindings : MonoBehaviour
    {
        public sealed class Entry
        {
            public string id;
            public InputAction action;
            public int index;
        }
        [Serializable] private sealed class SavedBinding { public string id, path; }
        [Serializable] private sealed class SavedBindings { public SavedBinding[] bindings; }
        private readonly List<Entry> entries = new List<Entry>();
        private InputActionRebindingExtensions.RebindingOperation operation;
        private Entry pending;
        private bool wasEnabled;
        private int suppressedFrame = -1;
        public bool IsRebinding => operation != null;
        public bool SuppressInputThisFrame => IsRebinding || suppressedFrame == Time.frameCount;
        public string Status { get; private set; }
        public event Action Changed;
        private string SavePath => Path.Combine(Application.persistentDataPath,"input-bindings.json");
        public InputAction MenuAction { get; private set; }
        public InputAction RelockAction { get; private set; }
        public InputAction InteractAction { get; private set; }
        public InputAction AttackAction { get; private set; }
        public InputAction AbsorbAction { get; private set; }

        public void Initialize(InputActionAsset asset)
        {
            var map = asset.FindActionMap("Player",true);
            Add("Forward",map.FindAction("Move",true),"<Keyboard>/w");
            Add("Backward",map.FindAction("Move",true),"<Keyboard>/s");
            Add("Left",map.FindAction("Move",true),"<Keyboard>/a");
            Add("Right",map.FindAction("Move",true),"<Keyboard>/d");
            Add("Sprint",map.FindAction("Sprint",true),"<Keyboard>/leftShift");
            Add("Jump",map.FindAction("Jump",true),"<Keyboard>/space");
            Add("Crouch",map.FindAction("Crouch",true),"<Keyboard>/c");
            // The template's Interact uses Hold; the demo requires a single press.
            InteractAction=map.AddAction("ArchiveInteract",InputActionType.Button,"<Keyboard>/e",expectedControlLayout:"Button");
            MenuAction=map.AddAction("ArchiveMenu",InputActionType.Button,"<Keyboard>/escape",expectedControlLayout:"Button");
            RelockAction=map.AddAction("ArchiveRelock",InputActionType.Button,"<Mouse>/leftButton",expectedControlLayout:"Button");
            Add("Interact",InteractAction,"<Keyboard>/e");
            Add("Menu",MenuAction,"<Keyboard>/escape");
            Add("Relock",RelockAction,"<Mouse>/leftButton");
            Load();
            AttackAction=map.AddAction("ArchiveAttack",InputActionType.Button,AvailableCombatPath("<Mouse>/leftButton"),expectedControlLayout:"Button");
            entries.Add(new Entry{id="Attack",action=AttackAction,index=0});
            AbsorbAction=map.AddAction("ArchiveAbsorb",InputActionType.Button,AvailableCombatPath("<Mouse>/rightButton"),expectedControlLayout:"Button");
            entries.Add(new Entry{id="Absorb",action=AbsorbAction,index=0});
            Load();
        }
        private string AvailableCombatPath(string preferred)
        {
            foreach(var candidate in new[]{preferred,"<Keyboard>/j","<Keyboard>/k","<Keyboard>/l","<Keyboard>/u"})
                if(!entries.Exists(e=>e.id!="Relock" && string.Equals(e.action.bindings[e.index].effectivePath,candidate,StringComparison.OrdinalIgnoreCase)))return candidate;
            return preferred;
        }
        private static bool CanShare(string a,string b)
            => (a=="Relock" && (b=="Attack"||b=="Absorb")) || (b=="Relock" && (a=="Attack"||a=="Absorb"));
        private void Add(string id,InputAction action,string path)
        {
            for(int i=0;i<action.bindings.Count;i++)
                if(action.bindings[i].path==path){entries.Add(new Entry{id=id,action=action,index=i});return;}
            throw new InvalidOperationException("Missing binding: "+id);
        }
        public Entry Find(string id) => entries.Find(e=>e.id==id);
        public string Display(string id)
        {
            var e=Find(id);if(e==null)return "?";
            var path=e.action.bindings[e.index].effectivePath;
            if(path=="<Mouse>/leftButton")return "左键";
            if(path=="<Mouse>/rightButton")return "右键";
            if(path=="<Mouse>/middleButton")return "中键";
            if(path=="<Keyboard>/escape")return "Esc";
            return e.action.GetBindingDisplayString(e.index,InputBinding.DisplayStringOptions.DontIncludeInteractions);
        }
        public bool TryAssign(string id,string path,bool persist,out string error)
        {
            var entry=Find(id);
            if(entry==null){error="未找到按键";return false;}
            bool keyboard=path!=null&&path.StartsWith("<Keyboard>/",StringComparison.Ordinal);
            bool mouse=path=="<Mouse>/leftButton"||path=="<Mouse>/rightButton"||path=="<Mouse>/middleButton";
            if(!keyboard&&!mouse){error="请选择键盘按键或鼠标按钮";return false;}
            if(path=="<Keyboard>/f8"||path=="<Keyboard>/f9" || (path=="<Keyboard>/escape"&&id!="Menu"))
            {error="该按键保留给菜单或画质切换";return false;}
            foreach(var other in entries)
                if(other!=entry&&!CanShare(other.id,id)&&string.Equals(other.action.bindings[other.index].effectivePath,path,StringComparison.OrdinalIgnoreCase))
                {error="按键已被其他操作使用";return false;}
            // Arrow keys remain alternate movement controls and must not trigger other actions.
            if(path=="<Keyboard>/upArrow"||path=="<Keyboard>/downArrow"||path=="<Keyboard>/leftArrow"||path=="<Keyboard>/rightArrow")
            {error="方向键保留给移动";return false;}
            entry.action.ApplyBindingOverride(entry.index,path);
            error=null;
            if(persist)Save();
            Changed?.Invoke();
            return true;
        }
        public void Begin(string id)
        {
            Cancel();pending=Find(id);if(pending==null)return;
            suppressedFrame=Time.frameCount;
            Status="按下新按键 · Esc 取消";
            wasEnabled=pending.action.enabled;pending.action.Disable();
            operation=pending.action.PerformInteractiveRebinding(pending.index)
                .WithExpectedControlType("Button")
                .WithControlsHavingToMatchPath("<Keyboard>/*")
                .WithControlsHavingToMatchPath("<Mouse>/leftButton")
                .WithControlsHavingToMatchPath("<Mouse>/rightButton")
                .WithControlsHavingToMatchPath("<Mouse>/middleButton")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnApplyBinding((op,path)=>{
                    string error;
                    Status=null;
                    bool applied=TryAssign(pending.id,path,true,out error);
                    if(!applied)Status=error;
                    else if(Status==null)Status="按键已更新";
                })
                .OnComplete(op=>Finish())
                .OnCancel(op=>{Status="已取消换绑";Finish();});
            operation.Start();Changed?.Invoke();
        }
        private void Finish()
        {
            var completed=operation;operation=null;
            if(pending!=null&&wasEnabled)pending.action.Enable();
            pending=null;suppressedFrame=Time.frameCount;
            completed?.Dispose();Changed?.Invoke();
        }
        public void Cancel(){if(operation!=null)operation.Cancel();}
        public void RestoreDefaults()
        {
            Cancel();foreach(var e in entries)e.action.RemoveBindingOverride(e.index);
            Status="已恢复默认按键";suppressedFrame=Time.frameCount;Save();Changed?.Invoke();
        }
        private void Save()
        {
            try
            {
                var saved=new List<SavedBinding>();
                foreach(var e in entries)if(e.action.bindings[e.index].hasOverrides)
                    saved.Add(new SavedBinding{id=e.id,path=e.action.bindings[e.index].effectivePath});
                Directory.CreateDirectory(Application.persistentDataPath);
                string temporary=SavePath+".tmp";
                File.WriteAllText(temporary,JsonUtility.ToJson(new SavedBindings{bindings=saved.ToArray()},true));
                if(File.Exists(SavePath))File.Replace(temporary,SavePath,null);else File.Move(temporary,SavePath);
            }
            catch(Exception e){Status="按键已生效，配置保存失败";Debug.LogWarning("Input binding save failed: "+e.Message);}
        }
        private void Load()
        {
            try
            {
                if(!File.Exists(SavePath))return;
                var data=JsonUtility.FromJson<SavedBindings>(File.ReadAllText(SavePath));
                if(data?.bindings==null)return;
                // Validate the final layout together: valid key swaps must survive a restart.
                var finalPaths=new Dictionary<string,string>();
                foreach(var entry in entries)finalPaths[entry.id]=entry.action.bindings[entry.index].path;
                foreach(var binding in data.bindings)
                    if(binding!=null&&finalPaths.ContainsKey(binding.id)&&!string.IsNullOrEmpty(binding.path))finalPaths[binding.id]=binding.path;
                var used=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
                foreach(var pair in finalPaths)
                {
                    string lower=pair.Value.ToLowerInvariant();
                    bool allowed=lower.StartsWith("<keyboard>/",StringComparison.Ordinal)||lower=="<mouse>/leftbutton"||lower=="<mouse>/rightbutton"||lower=="<mouse>/middlebutton";
                    bool duplicate=used.TryGetValue(pair.Value,out string existing)&&!CanShare(pair.Key,existing);
                    if(!allowed||duplicate||lower=="<keyboard>/f8"||lower=="<keyboard>/f9"||
                        (lower=="<keyboard>/escape"&&pair.Key!="Menu")||lower.Contains("arrow"))
                        throw new InvalidDataException("Invalid saved key layout");
                    if(!used.ContainsKey(pair.Value)||pair.Key!="Relock")used[pair.Value]=pair.Key;
                }
                foreach(var entry in entries)entry.action.ApplyBindingOverride(entry.index,finalPaths[entry.id]);
            }
            catch(Exception e){Debug.LogWarning("Input binding load failed; using defaults: "+e.Message);}
        }
        private void OnDisable(){Cancel();}
        private void OnDestroy(){Cancel();}
    }
}
