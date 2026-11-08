using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using CatGame.Debugging;
using CatGame.Idle;
using UnityEngine;

namespace CatGame
{
    /// <summary>Opt-in, authenticated loopback endpoint. All Unity access stays on the main thread.</summary>
    public sealed class LiveDebugServer : MonoBehaviour
    {
        [Serializable] public sealed class Request { public string token,id,command,target; public double value; public bool countAsEarned; }
        [Serializable] public sealed class Option { public string id,name; }
        [Serializable] public sealed class Reply {
            public bool ok,blocked; public string message,backup,savePath; public int pid,visibleCats;
            public double production,clickValue; public GameState state; public Option[] cats,items;
        }
        [Serializable] sealed class Connection { public int protocol=1,port,pid; public string token; }
        [Serializable] sealed class Discovery { public int pid; public string connectionPath; }
        TcpListener _listener; TcpClient _client; string _token,_session;
        string _discovery;
        static string RegistryRoot { get { return Environment.GetEnvironmentVariable("CATGAME_DEBUG_REGISTRY") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"AppData","LocalLow","CatGame","LiveDebug"); } }
        float _deadline; readonly List<byte> _buffer=new List<byte>();
        readonly Dictionary<string,string> _replies=new Dictionary<string,string>(); readonly Queue<string> _ids=new Queue<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Bootstrap()
        {
            string path=Environment.GetEnvironmentVariable("CATGAME_DEBUG_SESSION");
            if(Environment.GetEnvironmentVariable("CATGAME_DEBUG_DISABLE")=="1") return;
#if UNITY_EDITOR
            if(string.IsNullOrEmpty(path)) return;
#endif
            if(string.IsNullOrEmpty(path)) path=Path.Combine(RegistryRoot,"session-"+System.Diagnostics.Process.GetCurrentProcess().Id+"-"+Guid.NewGuid().ToString("N"),"connection.json");
            if(FindObjectOfType<LiveDebugServer>()!=null) return;
            var go=new GameObject("LocalLiveDebugger"); DontDestroyOnLoad(go);
            go.AddComponent<LiveDebugServer>().StartServer(path);
        }
        void StartServer(string path)
        {
            try {
                _session=Path.GetFullPath(path); Directory.CreateDirectory(Path.GetDirectoryName(_session));
                _token=Guid.NewGuid().ToString("N")+Guid.NewGuid().ToString("N");
                Application.runInBackground=true;
                _listener=new TcpListener(IPAddress.Loopback,0); _listener.Start(8);
                File.WriteAllText(_session,JsonUtility.ToJson(new Connection {port=((IPEndPoint)_listener.LocalEndpoint).Port,pid=System.Diagnostics.Process.GetCurrentProcess().Id,token=_token}),new UTF8Encoding(false));
                Directory.CreateDirectory(RegistryRoot);
                _discovery=Path.Combine(RegistryRoot,"session-"+System.Diagnostics.Process.GetCurrentProcess().Id+"-"+Guid.NewGuid().ToString("N")+".json");
                File.WriteAllText(_discovery,JsonUtility.ToJson(new Discovery{pid=System.Diagnostics.Process.GetCurrentProcess().Id,connectionPath=_session}),new UTF8Encoding(false));
                Debug.Log("[Live Debug] Local session enabled: "+_session);
            } catch(Exception e) { Debug.LogError("[Live Debug] "+e.Message); Shutdown(); }
        }
        void Update()
        {
            if(_listener==null) return;
            try {
                if(_client==null) {
                    if(!_listener.Pending()) return;
                    _client=_listener.AcceptTcpClient(); _client.NoDelay=true; _client.SendTimeout=250;
                    _deadline=Time.realtimeSinceStartup+2; _buffer.Clear();
                }
                if(Time.realtimeSinceStartup>_deadline) { CloseClient(); return; }
                var stream=_client.GetStream();
                int budget=65536;
                while(_client.Available>0 && budget-->0) {
                    int b=stream.ReadByte();
                    if(b==10) { Respond(Encoding.UTF8.GetString(_buffer.ToArray())); CloseClient(); return; }
                    if(b<0 || _buffer.Count>=65536) { CloseClient(); return; }
                    _buffer.Add((byte)b);
                }
            } catch(Exception e) when(e is IOException || e is SocketException || e is ObjectDisposedException) { CloseClient(); }
        }
        void Respond(string json)
        {
            Request request=null; string result;
            try {
                request=JsonUtility.FromJson<Request>(json);
                if(request==null || request.token!=_token) throw new InvalidOperationException("连接凭据无效，请重新连接。");
                if(string.IsNullOrEmpty(request.id) || request.id.Length>80) throw new ArgumentException("请求编号无效。");
                if(_replies.TryGetValue(request.id,out result)) { Send(result); return; }
                string backup=null;
                if(request.command!="Status") {
                    DebugCommand command;
                    if(!Enum.TryParse(request.command,out command) || !Enum.IsDefined(typeof(DebugCommand),command)) throw new ArgumentException("不支持的调试命令。");
                    backup=EditorDebugCommands.Apply(CatWorld.I,command,request.target,request.value,request.countAsEarned,Path.Combine(Path.GetDirectoryName(_session),"operations"));
                }
                var reply=Snapshot(); reply.ok=true; reply.backup=backup;
                reply.message=backup==null?"已连接":"已应用，游戏按原有机制自动保存。";
                result=JsonUtility.ToJson(reply);
            } catch(Exception e) { result=JsonUtility.ToJson(new Reply {ok=false,message=e.Message}); }
            if(request!=null && request.token==_token && !string.IsNullOrEmpty(request.id) && request.id.Length<=80) {
                _replies[request.id]=result; _ids.Enqueue(request.id);
                if(_ids.Count>256) _replies.Remove(_ids.Dequeue());
            }
            Send(result);
        }
        Reply Snapshot()
        {
            var world=CatWorld.I; var economy=world!=null?world.Economy:null;
            return new Reply {pid=System.Diagnostics.Process.GetCurrentProcess().Id,state=economy!=null?economy.State:null,
                blocked=world==null || world.Hud==null || world.Hud.Modals.InputBlocked || Time.timeScale<=0,
                visibleCats=world!=null && world.Roster!=null?world.Roster.VisibleCount:0,
                production=economy!=null?economy.ProductionPerSecond:0,clickValue=economy!=null?economy.CurrentClickValue:0,
                savePath=SaveSystem.Instance!=null?SaveSystem.Instance.SavePath:null,
                cats=Array.ConvertAll(CatCatalog.All,c=>new Option{id=c.id,name=c.name}),
                items=Array.ConvertAll(ItemCatalog.All,i=>new Option{id=i.id,name=i.name})};
        }
        void Send(string json) { var bytes=Encoding.UTF8.GetBytes(json+"\n"); _client.GetStream().Write(bytes,0,bytes.Length); }
        void CloseClient() { if(_client!=null) _client.Close(); _client=null; _buffer.Clear(); }
        void Shutdown() { CloseClient(); if(_listener!=null) _listener.Stop(); _listener=null; if(!string.IsNullOrEmpty(_session) && File.Exists(_session)) File.Delete(_session); if(!string.IsNullOrEmpty(_discovery) && File.Exists(_discovery)) File.Delete(_discovery); }
        void OnApplicationQuit() { Shutdown(); }
        void OnDestroy() { Shutdown(); }
    }
}
