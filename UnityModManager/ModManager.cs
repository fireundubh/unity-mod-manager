﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using UnityEngine;
using dnlib.DotNet;
//using UnityEngine.SceneManagement;

namespace UnityModManagerNet
{
    public partial class UnityModManager
    {
        private static readonly Version VER_0_13 = new Version(0, 13);

        public static Version unityVersion { get; private set; }

        public static Version version { get; private set; } = typeof(UnityModManager).Assembly.GetName().Version;

        private static ModuleDefMD thisModuleDef = ModuleDefMD.Load(typeof(UnityModManager).Module);

        public class Repository
        {
            [Serializable]
            public class Release : IEquatable<Release>
            {
                public string Id;
                public string Version;
                public string DownloadUrl;

                public bool Equals(Release other)
                {
                    return Id.Equals(other.Id);
                }

                public override bool Equals(object obj)
                {
                    if (ReferenceEquals(null, obj))
                    {
                        return false;
                    }
                    return obj is Release obj2 && Equals(obj2);
                }

                public override int GetHashCode()
                {
                    return Id.GetHashCode();
                }
            }

            public Release[] Releases;
        }

        public class ModSettings
        {
            public virtual void Save(ModEntry modEntry)
            {
                Save(this, modEntry);
            }

            public virtual string GetPath(ModEntry modEntry)
            {
                return Path.Combine(modEntry.Path, "Settings.xml");
            }

            public static void Save<T>(T data, ModEntry modEntry) where T : ModSettings, new()
            {
                var filepath = data.GetPath(modEntry);
                try
                {
                    using (var writer = new StreamWriter(filepath))
                    {
                        var serializer = new XmlSerializer(typeof(T));
                        serializer.Serialize(writer, data);
                    }
                }
                catch (Exception e)
                {
                    modEntry.Logger.Error($"Can't save {filepath}.");
                    Debug.LogException(e);
                }
            }

            public static T Load<T>(ModEntry modEntry) where T : ModSettings, new()
            {
                var t = new T();
                var filepath = t.GetPath(modEntry);
                if (File.Exists(filepath))
                {
                    try
                    {
                        using (var stream = File.OpenRead(filepath))
                        {
                            var serializer = new XmlSerializer(typeof(T));
                            var result = (T)serializer.Deserialize(stream);
                            return result;
                        }
                    }
                    catch (Exception e)
                    {
                        modEntry.Logger.Error($"Can't read {filepath}.");
                        Debug.LogException(e);
                    }
                }

                return t;
            }
        }

        public class ModInfo : IEquatable<ModInfo>
        {
            public string Id;

            public string DisplayName;

            public string Author;

            public string Version;

            public string ManagerVersion;

            public string[] Requirements;

            public string AssemblyName;

            public string EntryMethod;

            public string HomePage;

            public string Repository;

            public static implicit operator bool(ModInfo exists)
            {
                return exists != null;
            }

            public bool Equals(ModInfo other)
            {
                return Id.Equals(other.Id);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }
                return obj is ModInfo modInfo && Equals(modInfo);
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode();
            }
        }

        public partial class ModEntry
        {
            public readonly ModInfo Info;

            /// <summary>
            /// Path to mod folder
            /// </summary>
            public readonly string Path;

            Assembly mAssembly = null;
            public Assembly Assembly => mAssembly;

            public readonly Version Version = null;

            public readonly Version ManagerVersion = null;

            /// <summary>
            /// Not used
            /// </summary>
            public Version NewestVersion;

            /// <summary>
            /// Required mods
            /// </summary>
            public readonly Dictionary<string, Version> Requirements = new Dictionary<string, Version>();

            public readonly ModLogger Logger = null;

            /// <summary>
            /// Not used
            /// </summary>
            public bool HasUpdate = false;

            //public ModSettings Settings = null;

            /// <summary>
            /// Show button to reload the mod [0.14.0]
            /// </summary>
            public bool CanReload { get; private set; }

            /// <summary>
            /// Called to unload old data for reloading mod [0.14.0]
            /// </summary>
            public Func<ModEntry, bool> OnUnload = null;

            /// <summary>
            /// Called to activate / deactivate the mod
            /// </summary>
            public Func<ModEntry, bool, bool> OnToggle = null;

            /// <summary>
            /// Called by MonoBehaviour.OnGUI
            /// </summary>
            public Action<ModEntry> OnGUI = null;

            /// <summary>
            /// Called when the game closes
            /// </summary>
            public Action<ModEntry> OnSaveGUI = null;

            /// <summary>
            /// Called by MonoBehaviour.Update [0.13.0]
            /// </summary>
            public Action<ModEntry, float> OnUpdate = null;

            /// <summary>
            /// Called by MonoBehaviour.LateUpdate [0.13.0]
            /// </summary>
            public Action<ModEntry, float> OnLateUpdate = null;

            /// <summary>
            /// Called by MonoBehaviour.FixedUpdate [0.13.0]
            /// </summary>
            public Action<ModEntry, float> OnFixedUpdate = null;

            Dictionary<long, MethodInfo> mCache = new Dictionary<long, MethodInfo>();

            bool mStarted = false;
            public bool Started => mStarted;

            bool mErrorOnLoading = false;
            public bool ErrorOnLoading => mErrorOnLoading;

            /// <summary>
            /// UI checkbox
            /// </summary>
            public bool Enabled = true;
            //public bool Enabled => Enabled;

            /// <summary>
            /// If OnToggle exists
            /// </summary>
            public bool Toggleable => OnToggle != null;

            /// <summary>
            /// If Assembly is loaded [0.13.1]
            /// </summary>
            public bool Loaded => Assembly != null;

            bool mFirstLoading = true;

            bool mActive = false;
            public bool Active
            {
                get => mActive;
                set
                {
                    if (value && !Loaded)
                    {
                        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                        Load();
                        Logger.NativeLog($"Loading time {(stopwatch.ElapsedMilliseconds / 1000f):f2} s.");
                        return;
                    }

                    if (!mStarted || mErrorOnLoading)
                        return;

                    try
                    {
                        if (value)
                        {
                            if (mActive)
                                return;

                            if (OnToggle == null || OnToggle(this, true))
                            {
                                mActive = true;
                                this.Logger.Log($"Active.");
                            }
                            else
                            {
                                this.Logger.Log($"Unsuccessfully.");
                            }
                        }
                        else
                        {
                            if (!mActive)
                                return;

                            if (OnToggle != null && OnToggle(this, false))
                            {
                                mActive = false;
                                this.Logger.Log($"Inactive.");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        this.Logger.Error("OnToggle: " + e.GetType().Name + " - " + e.Message);
                        Debug.LogException(e);
                    }
                }
            }

            public ModEntry(ModInfo info, string path)
            {
                Info = info;
                Path = path;
                Logger = new ModLogger(Info.Id);
                Version = ParseVersion(info.Version);
                ManagerVersion = !string.IsNullOrEmpty(info.ManagerVersion) ? ParseVersion(info.ManagerVersion) : new Version();

                if (info.Requirements != null && info.Requirements.Length > 0)
                {
                    var regex = new Regex(@"(.*)-(\d\.\d\.\d).*");
                    foreach (var id in info.Requirements)
                    {
                        var match = regex.Match(id);
                        if (match.Success)
                        {
                            Requirements.Add(match.Groups[1].Value, ParseVersion(match.Groups[2].Value));
                            continue;
                        }
                        if (!Requirements.ContainsKey(id))
                            Requirements.Add(id, null);
                    }
                }
            }

            public bool Load()
            {
                if (Loaded)
                    return !mErrorOnLoading;

                mErrorOnLoading = false;

                this.Logger.Log($"Version '{Info.Version}'. Loading.");
                if (string.IsNullOrEmpty(Info.AssemblyName))
                {
                    mErrorOnLoading = true;
                    this.Logger.Error($"{nameof(Info.AssemblyName)} is null.");
                }

                if (string.IsNullOrEmpty(Info.EntryMethod))
                {
                    mErrorOnLoading = true;
                    this.Logger.Error($"{nameof(Info.EntryMethod)} is null.");
                }

                if (!string.IsNullOrEmpty(Info.ManagerVersion))
                {
                    if (ManagerVersion > GetVersion())
                    {
                        mErrorOnLoading = true;
                        this.Logger.Error($"Mod Manager must be version '{Info.ManagerVersion}' or higher.");
                    }
                }

                if (Requirements.Count > 0)
                {
                    foreach (var item in Requirements)
                    {
                        var id = item.Key;
                        var mod = FindMod(id);
                        if (mod == null)
                        {
                            mErrorOnLoading = true;
                            this.Logger.Error($"Required mod '{id}' missing.");
                            continue;
                        }
                        else if (item.Value != null && item.Value > mod.Version)
                        {
                            mErrorOnLoading = true;
                            this.Logger.Error($"Required mod '{id}' must be version '{item.Value}' or higher.");
                            continue;
                        }

                        if (!mod.Active)
                        {
                            mod.Enabled = true;
                            mod.Active = true;
                            if (!mod.Active)
                                this.Logger.Log($"Required mod '{id}' inactive.");
                        }
                    }
                }

                if (mErrorOnLoading)
                    return false;

                string assemblyPath = System.IO.Path.Combine(Path, Info.AssemblyName);
                
                if (File.Exists(assemblyPath))
                {
                    try
                    {
                        var assemblyCachePath = assemblyPath;
                        var cacheExists = false;

                        if (mFirstLoading)
                        {
                            var fi = new FileInfo(assemblyPath);
                            var hash = (ushort)((long)fi.LastWriteTimeUtc.GetHashCode() + version.GetHashCode() + ManagerVersion.GetHashCode()).GetHashCode();
                            assemblyCachePath = assemblyPath + $".{hash}.cache";
                            cacheExists = File.Exists(assemblyCachePath);

                            if (!cacheExists)
                            {
                                foreach (var filepath in Directory.GetFiles(Path, "*.cache"))
                                {
                                    try
                                    {
                                        File.Delete(filepath);
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                            }
                        }

                        if (ManagerVersion >= VER_0_13)
                        {
                            if (mFirstLoading)
                            {
                                if (!cacheExists)
                                {
                                    File.Copy(assemblyPath, assemblyCachePath, true);
                                }
                                mAssembly = Assembly.LoadFile(assemblyCachePath);

                                foreach (var type in mAssembly.GetTypes())
                                {
                                    if (type.GetCustomAttributes(typeof(EnableReloadingAttribute), true).Any())
                                    {
                                        CanReload = true;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                mAssembly = Assembly.Load(File.ReadAllBytes(assemblyPath));
                            }
                        }
                        else
                        {
                            //var asmDef = AssemblyDefinition.ReadAssembly(assemblyPath);
                            //var modDef = asmDef.MainModule;
                            //if (modDef.TryGetTypeReference("UnityModManagerNet.UnityModManager", out var typeRef))
                            //{
                            //    var managerAsmRef = new AssemblyNameReference("UnityModManager", version);
                            //    if (typeRef.Scope is AssemblyNameReference asmNameRef)
                            //    {
                            //        typeRef.Scope = managerAsmRef;
                            //        modDef.AssemblyReferences.Add(managerAsmRef);
                            //        asmDef.Write(assemblyCachePath);
                            //    }
                            //}
                            if (!cacheExists)
                            {
                                var modDef = ModuleDefMD.Load(File.ReadAllBytes(assemblyPath));
                                foreach (var item in modDef.GetTypeRefs())
                                {
                                    if (item.FullName == "UnityModManagerNet.UnityModManager")
                                    {
                                        item.ResolutionScope = new AssemblyRefUser(thisModuleDef.Assembly);
                                    }
                                }
                                modDef.Write(assemblyCachePath);
                            }
                            mAssembly = Assembly.LoadFile(assemblyCachePath);
                        }

                        mFirstLoading = false;
                    }
                    catch (Exception exception)
                    {
                        mErrorOnLoading = true;
                        this.Logger.Error($"Error loading file '{assemblyPath}'.");
                        Debug.LogException(exception);
                        return false;
                    }

                    try
                    {
                        object[] param = new object[] { this };
                        Type[] types = new Type[] { typeof(ModEntry) };
                        if (FindMethod(Info.EntryMethod, types, false) == null)
                        {
                            param = null;
                            types = null;
                        }

                        if (!Invoke(Info.EntryMethod, out var result, param, types) || result != null && (bool)result == false)
                        {
                            mErrorOnLoading = true;
                            this.Logger.Log($"Not loaded.");
                        }
                    }
                    catch (Exception e)
                    {
                        mErrorOnLoading = true;
                        this.Logger.Log(e.ToString());
                        return false;
                    }

                    mStarted = true;

                    if (!mErrorOnLoading)
                    {
                        Active = true;
                        return true;
                    }
                }
                else
                {
                    mErrorOnLoading = true;
                    this.Logger.Error($"File '{assemblyPath}' not found.");
                }

                return false;
            }

            internal void Reload()
            {
                if (!mStarted || !CanReload)
                    return;

                try
                {
                    string assemblyPath = System.IO.Path.Combine(Path, Info.AssemblyName);
                    var reflAssembly = Assembly.ReflectionOnlyLoad(File.ReadAllBytes(assemblyPath));
                    if (reflAssembly.GetName().Version == Assembly.GetName().Version)
                    {
                        this.Logger.Log("Reload is not needed. The version is exactly the same as the previous one.");
                        return;
                    }
                }
                catch (Exception e)
                {
                    this.Logger.Error(e.ToString());
                    return;
                }

                if (OnSaveGUI != null)
                    OnSaveGUI.Invoke(this);

                this.Logger.Log("Reloading...");

                if (Toggleable)
                {
                    Active = false;
                }
                else
                {
                    mActive = false;
                }
                
                try
                {
                    if (!Active && (OnUnload == null || OnUnload.Invoke(this)))
                    {
                        mCache.Clear();
                        typeof(Harmony12.Traverse).GetField("Cache", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, new Harmony12.AccessCache());
                        typeof(Harmony.Traverse).GetField("Cache", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, new Harmony.AccessCache());

                        var oldAssembly = Assembly;
                        mAssembly = null;
                        mStarted = false;
                        mErrorOnLoading = false;

                        OnToggle = null;
                        OnGUI = null;
                        OnSaveGUI = null;
                        OnUnload = null;
                        OnUpdate = null;
                        OnFixedUpdate = null;
                        OnLateUpdate = null;

                        if (Load())
                        {
                            var allTypes = oldAssembly.GetTypes();
                            foreach (var type in allTypes)
                            {
                                var t = Assembly.GetType(type.FullName);
                                if (t != null)
                                {
                                    foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                                    {
                                        if (field.GetCustomAttributes(typeof(SaveOnReloadAttribute), true).Any())
                                        {
                                            var f = t.GetField(field.Name);
                                            if (f != null)
                                            {
                                                this.Logger.Log($"Copying field '{field.DeclaringType.Name}.{field.Name}'");
                                                try
                                                {
                                                    if (field.FieldType != f.FieldType)
                                                    {
                                                        if (field.FieldType.IsEnum && f.FieldType.IsEnum)
                                                        {
                                                            f.SetValue(null, Convert.ToInt32(field.GetValue(null)));
                                                        }
                                                        else if (field.FieldType.IsClass && f.FieldType.IsClass)
                                                        {
                                                            //f.SetValue(null, Convert.ChangeType(field.GetValue(null), f.FieldType));
                                                        }
                                                        else if (field.FieldType.IsValueType && f.FieldType.IsValueType)
                                                        {
                                                            //f.SetValue(null, Convert.ChangeType(field.GetValue(null), f.FieldType));
                                                        }
                                                    }
                                                    else
                                                    {
                                                        f.SetValue(null, field.GetValue(null));
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    this.Logger.Error(ex.ToString());
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        return;
                    }
                    else if (Active)
                    {
                        this.Logger.Log("Must be deactivated.");
                    }
                }
                catch (Exception e)
                {
                    this.Logger.Error(e.ToString());
                }

                this.Logger.Log("Reloading canceled.");
            }

            public bool Invoke(string namespaceClassnameMethodname, out object result, object[] param = null, Type[] types = null)
            {
                result = null;
                try
                {
                    var methodInfo = FindMethod(namespaceClassnameMethodname, types);
                    if (methodInfo != null)
                    {
                        result = methodInfo.Invoke(null, param);
                        return true;
                    }
                }
                catch (Exception exception)
                {
                    this.Logger.Error($"Error trying to call '{namespaceClassnameMethodname}'.");
                    this.Logger.Error($"{exception.GetType().Name} - {exception.Message}");
                    Debug.LogException(exception);
                }

                return false;
            }

            MethodInfo FindMethod(string namespaceClassnameMethodname, Type[] types, bool showLog = true)
            {
                long key = namespaceClassnameMethodname.GetHashCode();
                if (types != null)
                {
                    foreach (var val in types)
                    {
                        key += val.GetHashCode();
                    }
                }

                if (!mCache.TryGetValue(key, out var methodInfo))
                {
                    if (mAssembly != null)
                    {
                        string classString = null;
                        string methodString = null;
                        var pos = namespaceClassnameMethodname.LastIndexOf('.');
                        if (pos != -1)
                        {
                            classString = namespaceClassnameMethodname.Substring(0, pos);
                            methodString = namespaceClassnameMethodname.Substring(pos + 1);
                        }
                        else
                        {
                            if (showLog)
                                this.Logger.Error($"Function name error '{namespaceClassnameMethodname}'.");

                            goto Exit;
                        }
                        var type = mAssembly.GetType(classString);
                        if (type != null)
                        {
                            if (types == null)
                                types = new Type[0];

                            methodInfo = type.GetMethod(methodString, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, new ParameterModifier[0]);
                            if (methodInfo == null)
                            {
                                if (showLog)
                                {
                                    if (types.Length > 0)
                                    {
                                        this.Logger.Log($"Method '{namespaceClassnameMethodname}[{string.Join(", ", types.Select(x => x.Name).ToArray())}]' not found.");
                                    }
                                    else
                                    {
                                        this.Logger.Log($"Method '{namespaceClassnameMethodname}' not found.");
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (showLog)
                                this.Logger.Error($"Class '{classString}' not found.");
                        }
                    }
                    else
                    {
                        if (showLog)
                            UnityModManager.Logger.Error($"Can't find method '{namespaceClassnameMethodname}'. Mod '{Info.Id}' is not loaded.");
                    }

                    Exit:

                    mCache[key] = methodInfo;
                }

                return methodInfo;
            }
        }

        public static readonly List<ModEntry> modEntries = new List<ModEntry>();
        public static string modsPath { get; private set; }

        internal static Param Params { get; set; } = new Param();
        internal static GameInfo Config { get; set; } = new GameInfo();

        internal static bool started;
        internal static bool initialized;

        public static void Main()
        {
            AppDomain.CurrentDomain.AssemblyLoad += OnLoad;
        }

        static void OnLoad(object sender, AssemblyLoadEventArgs args)
        {
            if (args.LoadedAssembly.FullName == "Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
            {
                AppDomain.CurrentDomain.AssemblyLoad -= OnLoad;
                Injector.Run(true);
            }
        }

        public static bool Initialize()
        {
            if (initialized)
                return true;

            initialized = true;

            Logger.Clear();

            Logger.Log($"Initialize. Version '{version}'.");

            unityVersion = ParseVersion(Application.unityVersion);

            Config = GameInfo.Load();
            if (Config == null)
            {
                return false;
            }

            Params = Param.Load();

            modsPath = Path.Combine(Environment.CurrentDirectory, Config.ModsDirectory);

            if (!Directory.Exists(modsPath))
                Directory.CreateDirectory(modsPath);

            //SceneManager.sceneLoaded += SceneManager_sceneLoaded; // Incompatible with Unity5

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            return true;
        }

        //private static void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
        //{
        //    Logger.NativeLog($"Scene loaded: {scene.name} ({mode.ToString()})");
        //}

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
            if (assembly != null)
                return assembly;

            if (args.Name.StartsWith("0Harmony,"))
            {
                var regex = new Regex(@"Version=(\d+\.\d+)");
                var match = regex.Match(args.Name);
                if (match.Success)
                {
                    var ver = match.Groups[1].Value;
                    string filepath = Path.Combine(Path.GetDirectoryName(typeof(UnityModManager).Assembly.Location), $"0Harmony-{ver}.dll");
                    if (File.Exists(filepath))
                    {
                        try
                        {
                            return Assembly.LoadFile(filepath);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e.ToString());
                        }
                    }
                }
            }

            return null;
        }

        public static void Start()
        {
            try
            {
                _Start();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                OpenUnityFileLog();
            }
        }

        private static void _Start()
        {
            if (!Initialize())
            {
                Logger.Log($"Cancel start due to an error.");
                OpenUnityFileLog();
                return;
            }
            if (started)
            {
                Logger.Log($"Cancel start. Already started.");
                return;
            }

            started = true;

            if (Directory.Exists(modsPath))
            {
                Logger.Log($"Parsing mods.");

                Dictionary<string, ModEntry> mods = new Dictionary<string, ModEntry>();

                int countMods = 0;

                foreach (string dir in Directory.GetDirectories(modsPath))
                {
                    string jsonPath = Path.Combine(dir, Config.ModInfo);
                    if (!File.Exists(Path.Combine(dir, Config.ModInfo)))
                    {
                        jsonPath = Path.Combine(dir, Config.ModInfo.ToLower());
                    }
                    if (File.Exists(jsonPath))
                    {
                        countMods++;
                        Logger.Log($"Reading file '{jsonPath}'.");
                        try
                        {
                            ModInfo modInfo = JsonUtility.FromJson<ModInfo>(File.ReadAllText(jsonPath));
                            if (string.IsNullOrEmpty(modInfo.Id))
                            {
                                Logger.Error($"Id is null.");
                                continue;
                            }
                            if (mods.ContainsKey(modInfo.Id))
                            {
                                Logger.Error($"Id '{modInfo.Id}' already uses another mod.");
                                continue;
                            }
                            if (string.IsNullOrEmpty(modInfo.AssemblyName))
                                modInfo.AssemblyName = modInfo.Id + ".dll";

                            ModEntry modEntry = new ModEntry(modInfo, dir + Path.DirectorySeparatorChar);
                            mods.Add(modInfo.Id, modEntry);
                        }
                        catch (Exception exception)
                        {
                            Logger.Error($"Error parsing file '{jsonPath}'.");
                            Debug.LogException(exception);
                        }
                    }
                    else
                    {
                        //Logger.Log($"File not found '{jsonPath}'.");
                    }
                }

                if (mods.Count > 0)
                {
                    Logger.Log($"Sorting mods.");
                    TopoSort(mods);

                    Params.ReadModParams();

                    Logger.Log($"Loading mods.");
                    foreach (var mod in modEntries)
                    {
                        if (!mod.Enabled)
                        {
                            mod.Logger.Log("To skip (disabled).");
                        }
                        else
                        {
                            mod.Active = true;
                        }
                    }
                }

                Logger.Log($"Finish. Found {countMods} mods. Successful loaded {modEntries.Count(x => !x.ErrorOnLoading)} mods.".ToUpper());
                Console.WriteLine();
                Console.WriteLine();
            }

            if (!UI.Load())
            {
                Logger.Error($"Can't load UI.");
            }
        }

        private static void DFS(string id, Dictionary<string, ModEntry> mods)
        {
            if (modEntries.Any(m => m.Info.Id == id))
            {
                return;
            }
            foreach (var req in mods[id].Requirements.Keys)
            {
                DFS(req, mods);
            }
            modEntries.Add(mods[id]);
        }

        private static void TopoSort(Dictionary<string, ModEntry> mods)
        {
            foreach (var id in mods.Keys)
            {
                DFS(id, mods);
            }
        }

        public static ModEntry FindMod(string id)
        {
            return modEntries.FirstOrDefault(x => x.Info.Id == id);
        }

        public static Version GetVersion()
        {
            return version;
        }

        public static void SaveSettingsAndParams()
        {
            Params.Save();
            foreach (var mod in modEntries)
            {
                if (mod.Active && mod.OnSaveGUI != null)
                {
                    try
                    {
                        mod.OnSaveGUI(mod);
                    }
                    catch (Exception e)
                    {
                        mod.Logger.Error("OnSaveGUI: " + e.GetType().Name + " - " + e.Message);
                        Debug.LogException(e);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Copies a value from an old assembly to a new one [0.14.0]
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SaveOnReloadAttribute : Attribute
    {
    }

    /// <summary>
    /// Allows reloading [0.14.1]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class EnableReloadingAttribute : Attribute
    {
    }
}