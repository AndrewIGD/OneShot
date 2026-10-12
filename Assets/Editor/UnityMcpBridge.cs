#if UNITY_EDITOR
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace PhoneAssistant.UnityMCP
{
    /// <summary>
    /// Unity MCP Bridge - Embedded HTTP JSON-RPC Server for Editor Automation.
    /// Enables external AI agents (Phone Assistant / Antigravity / Gemini) to inspect scene trees,
    /// spawn and manipulate GameObjects, modify transforms, and control play mode.
    /// </summary>
    [InitializeOnLoad]
    public static class UnityMcpBridge
    {
        private const int DEFAULT_PORT = 8080;
        private static HttpListener _listener;
        private static Thread _listenerThread;
        private static bool _isRunning = false;
        private static readonly Queue<Action> _mainThreadQueue = new Queue<Action>();
        private static readonly object _queueLock = new object();

        static UnityMcpBridge()
        {
            RegisterLifecycleHooks();
            StartServer();
            CheckAutoPlayFlag();
        }

        [InitializeOnLoadMethod]
        private static void InitOnLoad()
        {
            RegisterLifecycleHooks();
            StartServer();
            CheckAutoPlayFlag();
        }

        private static void RegisterLifecycleHooks()
        {
            EditorApplication.update -= ProcessMainThreadQueue;
            EditorApplication.update += ProcessMainThreadQueue;

            EditorApplication.update -= EnsureServerRunning;
            EditorApplication.update += EnsureServerRunning;

            AssemblyReloadEvents.afterAssemblyReload -= OnAssemblyReloaded;
            AssemblyReloadEvents.afterAssemblyReload += OnAssemblyReloaded;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            RegisterLifecycleHooks();
            StartServer();
        }

        private static void OnAssemblyReloaded()
        {
            RegisterLifecycleHooks();
            StartServer();
            CheckAutoPlayFlag();
        }

        private static double _lastServerCheckTime = 0;
        private static void EnsureServerRunning()
        {
            if (EditorApplication.timeSinceStartup - _lastServerCheckTime < 2.0) return;
            _lastServerCheckTime = EditorApplication.timeSinceStartup;

            CheckAutoPlayFlag();

            if (!_isRunning || _listener == null || !_listener.IsListening)
            {
                StartServer();
            }
        }

        private static void CheckAutoPlayFlag()
        {
            try
            {
                string flagFile = Path.Combine(Application.dataPath, "Editor", "mcp_auto_play.flag");
                if (File.Exists(flagFile))
                {
                    if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
                    File.Delete(flagFile);
                    EditorApplication.isPlaying = true;
                    Debug.Log("[Unity MCP Bridge] Auto-entered Play Mode as requested by Phone Assistant.");
                }
            }
            catch { }
        }

        [MenuItem("Tools/Unity MCP Bridge/Start Server")]
        public static void StartServerMenu()
        {
            StartServer();
        }

        [MenuItem("Tools/Unity MCP Bridge/Stop Server")]
        public static void StopServerMenu()
        {
            StopServer();
        }

        [MenuItem("Tools/Unity MCP Bridge/Check Status")]
        public static void CheckStatusMenu()
        {
            Debug.Log($"[Unity MCP Bridge] Server is {(_isRunning ? "RUNNING on port " + DEFAULT_PORT : "STOPPED")}.");
        }

        public static void StartServer(int port = DEFAULT_PORT)
        {
            if (_isRunning) return;

            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                try { _listener.Prefixes.Add($"http://localhost:{port}/"); } catch { }
                _listener.Start();
                _isRunning = true;

                _listenerThread = new Thread(ListenLoop)
                {
                    IsBackground = true,
                    Name = "UnityMcpListener"
                };
                _listenerThread.Start();
                Debug.Log($"<color=#00f2fe><b>[Unity MCP Bridge]</b></color> Listening for agent requests on http://127.0.0.1:{port}/");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Unity MCP Bridge] Failed to bind HTTP listener on port {port}: {ex.Message}");
                _isRunning = false;
            }
        }

        public static void StopServer()
        {
            _isRunning = false;
            if (_listener != null)
            {
                try
                {
                    _listener.Stop();
                    _listener.Close();
                }
                catch { }
                _listener = null;
            }
            Debug.Log("[Unity MCP Bridge] Server stopped.");
        }

        private static void ListenLoop()
        {
            while (_isRunning && _listener != null && _listener.IsListening)
            {
                try
                {
                    var context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(state => HandleRequest(context));
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Unity MCP Bridge] Listener exception: {ex.Message}");
                }
            }
        }

        private static void HandleRequest(HttpListenerContext context)
        {
            var req = context.Request;
            var res = context.Response;
            res.Headers.Add("Access-Control-Allow-Origin", "*");
            res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            res.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            if (req.HttpMethod == "OPTIONS")
            {
                res.StatusCode = 200;
                res.Close();
                return;
            }

            string rawPath = req.Url.AbsolutePath.ToLower().TrimEnd('/');
            string reqBody = "";
            if (req.HasEntityBody)
            {
                using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                {
                    reqBody = reader.ReadToEnd();
                }
            }

            // Route to main thread
            var waitHandle = new ManualResetEvent(false);
            string responseJson = "{\"status\":\"error\",\"error\":\"Unhandled request\"}";
            int statusCode = 200;

            EnqueueMainThread(() =>
            {
                try
                {
                    switch (rawPath)
                    {
                        case "/status":
                            responseJson = GetStatusJson();
                            break;
                        case "/hierarchy":
                            responseJson = GetHierarchyJson();
                            break;
                        case "/inspect":
                            responseJson = InspectObjectJson(reqBody);
                            break;
                        case "/create":
                            responseJson = CreateObjectJson(reqBody);
                            break;
                        case "/transform":
                            responseJson = ModifyTransformJson(reqBody);
                            break;
                        case "/component":
                            responseJson = AddComponentJson(reqBody);
                            break;
                        case "/playmode":
                            responseJson = SetPlayModeJson(reqBody);
                            break;
                        case "/menu":
                            responseJson = ExecuteMenuJson(reqBody);
                            break;
                        default:
                            statusCode = 404;
                            responseJson = "{\"status\":\"error\",\"error\":\"Endpoint not found\"}";
                            break;
                    }
                }
                catch (Exception ex)
                {
                    responseJson = $"{{\"status\":\"error\",\"error\":{EscapeJson(ex.Message)}}}";
                }
                finally
                {
                    waitHandle.Set();
                }
            });

            bool ok = waitHandle.WaitOne(6000);
            if (!ok)
            {
                responseJson = "{\"status\":\"error\",\"error\":\"Timeout waiting for Unity main thread (busy or transitioning)\"}";
            }

            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(responseJson);
                res.ContentType = "application/json";
                res.StatusCode = statusCode;
                res.ContentLength64 = buffer.Length;
                res.OutputStream.Write(buffer, 0, buffer.Length);
                res.OutputStream.Close();
            }
            catch { }
        }

        private static void EnqueueMainThread(Action action)
        {
            lock (_queueLock)
            {
                _mainThreadQueue.Enqueue(action);
            }
        }

        private static void ProcessMainThreadQueue()
        {
            lock (_queueLock)
            {
                while (_mainThreadQueue.Count > 0)
                {
                    var action = _mainThreadQueue.Dequeue();
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Unity MCP Bridge] Error processing action: {ex}");
                    }
                }
            }
        }

        // --- Handlers ---

        private static string GetStatusJson()
        {
            var scene = EditorSceneManager.GetActiveScene();
            return $"{{\"status\":\"ok\",\"unity_version\":\"{Application.unityVersion}\",\"project_name\":\"{Application.productName}\",\"active_scene\":\"{scene.name}\",\"is_playing\":{EditorApplication.isPlaying.ToString().ToLower()},\"is_paused\":{EditorApplication.isPaused.ToString().ToLower()}}}";
        }

        private static string GetHierarchyJson()
        {
            var roots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
            var sb = new StringBuilder();
            sb.Append("{\"status\":\"ok\",\"scene\":\"" + EditorSceneManager.GetActiveScene().name + "\",\"game_objects\":[");
            for (int i = 0; i < roots.Length; i++)
            {
                if (i > 0) sb.Append(",");
                SerializeGameObject(roots[i], sb);
            }
            sb.Append("]}");
            return sb.ToString();
        }

        private static void SerializeGameObject(GameObject go, StringBuilder sb)
        {
            sb.Append("{");
            sb.Append($"\"name\":\"{go.name}\",");
            sb.Append($"\"instance_id\":{go.GetInstanceID()},");
            sb.Append($"\"active\":{go.activeSelf.ToString().ToLower()},");
            sb.Append($"\"tag\":\"{go.tag}\",");
            sb.Append($"\"layer\":{go.layer},");
            sb.Append($"\"position\":[{go.transform.position.x:F3},{go.transform.position.y:F3},{go.transform.position.z:F3}],");
            sb.Append($"\"rotation\":[{go.transform.eulerAngles.x:F3},{go.transform.eulerAngles.y:F3},{go.transform.eulerAngles.z:F3}],");
            sb.Append($"\"scale\":[{go.transform.localScale.x:F3},{go.transform.localScale.y:F3},{go.transform.localScale.z:F3}],");

            sb.Append("\"components\":[");
            var comps = go.GetComponents<Component>();
            for (int j = 0; j < comps.Length; j++)
            {
                if (j > 0) sb.Append(",");
                sb.Append(comps[j] != null ? $"\"{comps[j].GetType().Name}\"" : "\"Missing\"");
            }
            sb.Append("],");

            sb.Append("\"children\":[");
            for (int k = 0; k < go.transform.childCount; k++)
            {
                if (k > 0) sb.Append(",");
                SerializeGameObject(go.transform.GetChild(k).gameObject, sb);
            }
            sb.Append("]");
            sb.Append("}");
        }

        private static string CreateObjectJson(string json)
        {
            string primType = ExtractString(json, "primitive_type") ?? "Cube";
            string name = ExtractString(json, "name") ?? primType;
            float[] pos = ExtractVector3(json, "position") ?? new float[] { 0, 0, 0 };
            float[] rot = ExtractVector3(json, "rotation") ?? new float[] { 0, 0, 0 };
            float[] scale = ExtractVector3(json, "scale") ?? new float[] { 1, 1, 1 };
            string colorHex = ExtractString(json, "color");

            GameObject newObj = null;
            if (Enum.TryParse<PrimitiveType>(primType, true, out var parsedPrim))
            {
                newObj = GameObject.CreatePrimitive(parsedPrim);
            }
            else
            {
                newObj = new GameObject(name);
            }

            newObj.name = name;
            newObj.transform.position = new Vector3(pos[0], pos[1], pos[2]);
            newObj.transform.eulerAngles = new Vector3(rot[0], rot[1], rot[2]);
            newObj.transform.localScale = new Vector3(scale[0], scale[1], scale[2]);

            if (!string.IsNullOrEmpty(colorHex))
            {
                var renderer = newObj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color newCol;
                    if (ColorUtility.TryParseHtmlString(colorHex, out newCol))
                    {
                        var mat = new Material(Shader.Find("Standard"));
                        mat.color = newCol;
                        renderer.material = mat;
                    }
                }
            }

            Undo.RegisterCreatedObjectUndo(newObj, $"Create {name} via Unity MCP");
            Selection.activeGameObject = newObj;

            return $"{{\"status\":\"ok\",\"name\":\"{name}\",\"instance_id\":{newObj.GetInstanceID()},\"position\":[{pos[0]},{pos[1]},{pos[2]}]}}";
        }

        private static string ModifyTransformJson(string json)
        {
            string target = ExtractString(json, "target");
            var go = FindGameObject(target);
            if (go == null) return "{\"status\":\"error\",\"error\":\"Target GameObject not found\"}";

            Undo.RecordObject(go.transform, "Modify Transform via Unity MCP");
            float[] pos = ExtractVector3(json, "position");
            if (pos != null) go.transform.position = new Vector3(pos[0], pos[1], pos[2]);

            float[] rot = ExtractVector3(json, "rotation");
            if (rot != null) go.transform.eulerAngles = new Vector3(rot[0], rot[1], rot[2]);

            float[] scale = ExtractVector3(json, "scale");
            if (scale != null) go.transform.localScale = new Vector3(scale[0], scale[1], scale[2]);

            return $"{{\"status\":\"ok\",\"target\":\"{go.name}\",\"position\":[{go.transform.position.x},{go.transform.position.y},{go.transform.position.z}]}}";
        }

        private static string AddComponentJson(string json)
        {
            string target = ExtractString(json, "target");
            string compName = ExtractString(json, "component");
            var go = FindGameObject(target);
            if (go == null) return "{\"status\":\"error\",\"error\":\"Target GameObject not found\"}";

            Type compType = Type.GetType(compName) ?? Type.GetType($"UnityEngine.{compName}, UnityEngine");
            if (compType == null)
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    compType = asm.GetType(compName) ?? asm.GetType($"UnityEngine.{compName}");
                    if (compType != null) break;
                }
            }

            if (compType == null)
                return $"{{\"status\":\"error\",\"error\":\"Component type '{compName}' not recognized.\"}}";

            var added = Undo.AddComponent(go, compType);
            return $"{{\"status\":\"ok\",\"target\":\"{go.name}\",\"component\":\"{compType.Name}\"}}";
        }

        private static string SetPlayModeJson(string json)
        {
            string state = (ExtractString(json, "state") ?? "play").ToLower();
            if (state == "play")
            {
                EditorApplication.isPlaying = true;
            }
            else if (state == "pause")
            {
                EditorApplication.isPaused = !EditorApplication.isPaused;
            }
            else if (state == "stop")
            {
                EditorApplication.isPlaying = false;
            }
            else if (state == "restart" || state == "reset")
            {
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += () =>
                {
                    EditorApplication.isPlaying = true;
                };
            }
            return $"{{\"status\":\"ok\",\"action\":\"{state}\",\"is_playing\":{EditorApplication.isPlaying.ToString().ToLower()},\"is_paused\":{EditorApplication.isPaused.ToString().ToLower()}}}";
        }

        private static string ExecuteMenuJson(string json)
        {
            string item = ExtractString(json, "menu_item");
            bool executed = EditorApplication.ExecuteMenuItem(item);
            return $"{{\"status\":\"{(executed ? "ok" : "failed")}\",\"menu_item\":\"{item}\"}}";
        }

        private static string InspectObjectJson(string json)
        {
            string target = ExtractString(json, "target");
            var go = FindGameObject(target);
            if (go == null) return "{\"status\":\"error\",\"error\":\"Target GameObject not found\"}";
            var sb = new StringBuilder();
            SerializeGameObject(go, sb);
            return $"{{\"status\":\"ok\",\"object\":{sb}}}";
        }

        private static GameObject FindGameObject(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) return Selection.activeGameObject;
            if (int.TryParse(identifier, out int id))
            {
                var obj = EditorUtility.InstanceIDToObject(id) as GameObject;
                if (obj != null) return obj;
            }
            return GameObject.Find(identifier);
        }

        private static string EscapeJson(string s) => $"\"{s.Replace("\"", "\\\"").Replace("\n", "\\n")}\"";

        private static string ExtractString(string json, string key)
        {
            int idx = json.IndexOf($"\"{key}\"");
            if (idx < 0) return null;
            int colon = json.IndexOf(':', idx);
            if (colon < 0) return null;
            int startQuote = json.IndexOf('"', colon);
            if (startQuote < 0) return null;
            int endQuote = json.IndexOf('"', startQuote + 1);
            if (endQuote < 0) return null;
            return json.Substring(startQuote + 1, endQuote - startQuote - 1);
        }

        private static float[] ExtractVector3(string json, string key)
        {
            int idx = json.IndexOf($"\"{key}\"");
            if (idx < 0) return null;
            int startBracket = json.IndexOf('[', idx);
            if (startBracket < 0) return null;
            int endBracket = json.IndexOf(']', startBracket);
            if (endBracket < 0) return null;

            string content = json.Substring(startBracket + 1, endBracket - startBracket - 1);
            var parts = content.Split(',');
            if (parts.Length >= 3 &&
                float.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float y) &&
                float.TryParse(parts[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float z))
            {
                return new float[] { x, y, z };
            }
            return null;
        }
    }
}
#endif
