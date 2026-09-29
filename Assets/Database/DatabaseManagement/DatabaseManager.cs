using Assets.Database.DatabaseManagement.MongoDB;
using Assets.Database.DatabaseManagement.SQLiteDB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using DrawingData = Assets.Database.DatabaseManagement.MongoDB.Drawing;
using DrawingMeta = Assets.Database.DatabaseManagement.SQLiteDB.Drawing;
using PlayerRole = Assets.Database.DatabaseManagement.SQLiteDB.Role;


namespace Assets.Database.DatabaseManagement
{
    public class DatabaseManager : MonoBehaviour
    {
        public enum ServerStatus
        {
            Offline,
            Starting,
            Online,
            Error
        }

        public ServerStatus CurrentStatus { get; private set; } = ServerStatus.Offline;


        // DTOs for API communication

        [System.Serializable]
        public class LoginMessage
        {
            public string username;
            public string password;
        }

        [System.Serializable]
        public class SignUpMessage
        {
            public string username;
            public string password;
            public int role = (int)PlayerRole.Player;
        }

        [System.Serializable]
        private class AuthResponse
        {
            public string message;
            public string playerId;
        }

        [System.Serializable]
        public class DrawingSaveMessage
        {
            public string name;
            public string owner;
            public List<Collaborator> collaborators;
            public string version = "1.0";
            public GameType gameType;
            public string sessionID;
            public List<Line> lines;
            public List<TrackedBehavior> trackedBehaviors;
            public List<PlacedModel> placedModels;
        }

        [System.Serializable]
        public class DrawingUpdateMessage
        {
            public string name;
            public List<Line> lines;
            public List<TrackedBehavior> trackedBehaviors;
            public List<PlacedModel> placedModels;
            public List<Collaborator> collaborators;
        }

        [System.Serializable]
        public class SessionUpdateMessage
        {
            public string id;
            public string name;
            public string description;
            public int? showBoy;
            public int? showGirl;
            public int? multiplayer;
        }

        [System.Serializable]
        public class RoomCreateRequest
        {
            public string name;
            public string roomAddress;
            public string sessionID;
            public string drawingID;
            public string hostID;
        }

        [System.Serializable]
        public class RoomJoinResponse
        {
            public string roomCode;
            public string roomAddress;
            public string sessionID;
            public string drawingID;
            public GameType gameType;
        }


        // Singleton pattern for DatabaseManager
        private static DatabaseManager _instance;
        public static DatabaseManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new("DatabaseManager");
                    _instance = obj.AddComponent<DatabaseManager>();
                }
                return _instance;
            }
        }


        // Configuration
        private static string apiUrl = "http://127.0.0.1:8000";
        private static readonly string _database = "VRDrawingDB.db";
        private static string _databasePath;
        private Process pythonServerProcess;

        void Start()
        {
            _databasePath = Path.Combine(Application.persistentDataPath, _database);
        }


        // -------------------------------------------------------------------
        // Server Management
        // -------------------------------------------------------------------

        public async Task StartDBServerIfNeeded()
        {
            // Apply config URL if available
            var profile = ConfigurationManager.ActiveServerProfile;
            if (profile != null && !string.IsNullOrEmpty(profile.ServerURL)) apiUrl = profile.GetNormalizedUrl();

            UnityEngine.Debug.Log($"[DB] Active profile '{profile?.Name}' -> {apiUrl} ({profile?.Mode})");

            CurrentStatus = ServerStatus.Starting;
            UnityEngine.Debug.Log("[DB] Checking if Database Server is online...");

            // 1. Try to ping the server first to see if it's already running
            bool isOnline = await PingServer();

            if (isOnline)
            {
                CurrentStatus = ServerStatus.Online;
                UnityEngine.Debug.Log("[DB] Database server is already running and responding.");
                return;
            }

            // 2. If it's not online, and we are in Local mode, start it
            if (profile != null && profile.Mode == DatabaseMode.Local)
            {
                UnityEngine.Debug.Log("[DB] Server not found. Attempting to start local Python process...");
                StartLocalProcess(profile);

                // Wait a few seconds for the Python app to fully boot up
                await Task.Delay(3000);

                // Verify it started successfully
                if (await PingServer())
                {
                    CurrentStatus = ServerStatus.Online;
                    UnityEngine.Debug.Log("[DB] Local Python server started and verified online.");
                }
                else
                {
                    CurrentStatus = ServerStatus.Error;
                    UnityEngine.Debug.LogError("[DB] Python process launched, but API is not responding.");
                }
            }
            else
            {
                CurrentStatus = ServerStatus.Error;
                UnityEngine.Debug.LogError($"[DB] Remote server at {apiUrl} is offline or unreachable.");
            }
        }

        private static void ApplyProfile(UnityWebRequest request)
        {
            var p = ConfigurationManager.ActiveServerProfile;
            request.timeout = Mathf.Max(1, p != null ? p.TimeoutSeconds : 5);
            if (p?.ExtraHeaders == null) return;
            foreach (var kv in p.ExtraHeaders) request.SetRequestHeader(kv.Key, kv.Value);
        }

        private async Task<bool> PingServer()
        {
            using UnityWebRequest request = new(apiUrl + "/", "GET");
            ApplyProfile(request);
            request.downloadHandler = new DownloadHandlerBuffer();

            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            return request.result == UnityWebRequest.Result.Success;
        }

        private void StartLocalProcess(ServerProfile profile)
        {
            try
            {
                // TODO: Use the Python executable path from the .venv or configuration settings if needed
                ProcessStartInfo startInfo = new()
                {
                    FileName = "python",
                    Arguments = profile.LocalPythonScriptPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                pythonServerProcess = new Process { StartInfo = startInfo };
                pythonServerProcess.Start();
            }
            catch (Exception e)
            {
                CurrentStatus = ServerStatus.Error;
                UnityEngine.Debug.LogError($"[DB] Failed to start Python server process: {e.Message}");
            }
        }

        private void OnApplicationQuit()
        {
            if (pythonServerProcess != null && !pythonServerProcess.HasExited)
            {
                pythonServerProcess.Kill();
                UnityEngine.Debug.Log("[DB] Local Python server safely shut down.");
            }
        }


        // -------------------------------------------------------------------
        // HTTP Request Helper
        // -------------------------------------------------------------------

        private async Task<string> SendRequest(string endpoint, string method, string jsonBody = null)
        {
            using UnityWebRequest request = new(apiUrl + endpoint, method);
            if (jsonBody != null)
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            ApplyProfile(request);

            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                if (request.responseCode == 404)
                {
                    string body = request.downloadHandler.text?.TrimStart();
                    if (!string.IsNullOrEmpty(body) && (body.StartsWith("{") || body.StartsWith("[")))
                        return request.downloadHandler.text;   // a real "not found" answer from the API
                        UnityEngine.Debug.LogError($"API Error ({method} {endpoint}): 404 not from the API " +
                        $"(tunnel offline?) ngrok code='{request.GetResponseHeader("Ngrok-Error-Code")}'");
                    return null;
                }
                UnityEngine.Debug.LogError($"API Error ({method} {endpoint}): {request.error}\nResponse: {request.downloadHandler.text}");
                return null;
            }

            return request.downloadHandler.text;
        }


        // Miscellaneous methods
        [Obsolete("Use the API to update the database instead.")]
        public static string DatabasePath() { return _databasePath; }

        [Obsolete("Use the API to update the database instead.")]
        public void Initialize()
        {
            // TODO: Update this method to call the API to update the database changes
            if (File.Exists(_databasePath))
            {
                UnityEngine.Debug.Log("Database has already been initialized");
                return;
            }

            try
            {
                File.Copy(Path.Combine(Application.streamingAssetsPath, _database), _databasePath);
                UnityEngine.Debug.Log($"Database file copied to persistent data path:\n{_databasePath}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
        }


        // -------------------------------------------------------------------
        // API calls - Drawings
        // -------------------------------------------------------------------

        public async Task<bool> SaveDrawing(DrawingSaveMessage saveMessage)
        {
            if (saveMessage == null)
            {
                UnityEngine.Debug.LogError("Invalid drawing data provided for saving.");
                return false;
            }

            // Using the new settings to ignore nulls when serializing optional properties
            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            string json = JsonConvert.SerializeObject(saveMessage, settings);
            string response = await SendRequest("/drawings/", "POST", json);

            if (response != null)
            {
                UnityEngine.Debug.Log("Drawing saved successfully via API.");
                return true;
            }
            return false;
        }

        public async Task<bool> ImportDrawing(DrawingData drawingData)
        {
            if (drawingData == null) return false;

            string json = JsonConvert.SerializeObject(drawingData);
            string response = await SendRequest("/drawings/import", "POST", json);

            if (response != null)
            {
                UnityEngine.Debug.Log($"Drawing imported successfully via API: {drawingData.metadata.id}");
                return true;
            }
            return false;
        }

        public async Task<DrawingData> UpdateDrawing(string drawingId, DrawingUpdateMessage updateMessage)
        {
            if (string.IsNullOrEmpty(drawingId) || updateMessage == null) return null;

            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            string json = JsonConvert.SerializeObject(updateMessage, settings);
            string response = await SendRequest($"/drawings/{drawingId}", "PUT", json);

            return response != null ? JsonConvert.DeserializeObject<DrawingData>(response) : null;
        }

        public async Task<DrawingMeta> GetDrawingMetaById(string drawingId)
        {
            if (string.IsNullOrEmpty(drawingId)) return null;

            string response = await SendRequest($"/drawings/{drawingId}/meta", "GET");
            return response != null ? JsonConvert.DeserializeObject<DrawingMeta>(response) : null;
        }

        public async Task<DrawingData> GetDrawingDataById(string drawingId)
        {
            if (string.IsNullOrEmpty(drawingId)) return null;
            string response = await SendRequest($"/drawings/{drawingId}", "GET");
            return response != null ? JsonConvert.DeserializeObject<DrawingData>(response) : null;
        }

        public async Task<bool> DeleteDrawing(string drawingId, string filePath = null)
        {
            if (string.IsNullOrEmpty(drawingId)) return false;

            string response = await SendRequest($"/drawings/{drawingId}", "DELETE");

            if (response != null)
            {
                UnityEngine.Debug.Log($"Drawing record deleted successfully via API: ID {drawingId}");
                return true;
            }
            return false;
        }


        // -------------------------------------------------------------------
        // API calls - Sessions
        // -------------------------------------------------------------------

        public async Task<string> SaveSession(string sessionName, string description = "", bool showBoy = true, bool showGirl = true, bool multiplayer = false)
        {
            if (string.IsNullOrEmpty(sessionName))
            {
                UnityEngine.Debug.LogError("Invalid session name.");
                return null;
            }

            var sessionPayload = new
            {
                name = sessionName,
                description = description,
                showBoy = showBoy ? 1 : 0,
                showGirl = showGirl ? 1 : 0,
                multiplayer = multiplayer ? 1 : 0
            };

            string json = JsonConvert.SerializeObject(sessionPayload);
            string response = await SendRequest("/sessions/", "POST", json);

            if (response != null)
            {
                Session session = JsonConvert.DeserializeObject<Session>(response);
                UnityEngine.Debug.Log($"Session saved successfully: {session.name}");
                return session.id;
            }
            return null;
        }

        public async Task<Session> UpdateSession(string sessionId, SessionUpdateMessage updateMessage)
        {
            if (string.IsNullOrEmpty(sessionId) || updateMessage == null) return null;

            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            string json = JsonConvert.SerializeObject(updateMessage, settings);
            string response = await SendRequest($"/sessions/{sessionId}", "PUT", json);

            return response != null ? JsonConvert.DeserializeObject<Session>(response) : null;
        }

        public async Task<bool> CloseSession(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) return false;

            string response = await SendRequest($"/sessions/{sessionId}/close", "PUT");
            if (response != null)
            {
                UnityEngine.Debug.Log($"Session {sessionId} closed successfully.");
                return true;
            }
            return false;
        }

        public async Task<bool> DeleteSession(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) return false;

            string response = await SendRequest($"/sessions/{sessionId}", "DELETE");
            if (response != null)
            {
                UnityEngine.Debug.Log($"Session {sessionId} deleted successfully.");
                return true;
            }
            return false;
        }

        public async Task<string> GetLatestSessionId()
        {
            string response = await SendRequest("/sessions/latest/id", "GET");
            if (response != null && response.Contains("\"detail\""))
            {
                UnityEngine.Debug.Log("[DB] No sessions found in the database. Returning null.");
                return null;
            }

            return response != null ? JsonConvert.DeserializeObject<string>(response) : null;
        }

        public async Task<Session> GetSessionById(string sessionId)
        {
            string response = await SendRequest($"/sessions/{sessionId}", "GET");
            return response != null ? JsonConvert.DeserializeObject<Session>(response) : null;
        }

        public async Task<string> GetSessionNameById(string sessionId)
        {
            var session = await GetSessionById(sessionId);
            return session?.name;
        }

        public async Task<string> GetSessionDescriptionById(string sessionId)
        {
            var session = await GetSessionById(sessionId);
            return session?.description;
        }

        public async Task<bool> GetShowBoyForSession(string sessionId)
        {
            var session = await GetSessionById(sessionId);
            return session != null && session.showBoy;
        }

        public async Task<bool> GetShowGirlForSession(string sessionId)
        {
            var session = await GetSessionById(sessionId);
            return session != null && session.showGirl;
        }

        public async Task<List<string>> GetAllSessionIds()
        {
            string response = await SendRequest("/sessions/", "GET");
            return response != null ? JsonConvert.DeserializeObject<List<string>>(response) : new List<string>();
        }

        public async Task<List<DrawingMeta>> GetDrawingsForSession(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) return new List<DrawingMeta>();

            string response = await SendRequest($"/sessions/{sessionId}/drawings", "GET");
            return response != null ? JsonConvert.DeserializeObject<List<DrawingMeta>>(response) : new List<DrawingMeta>();
        }


        // -------------------------------------------------------------------
        // API calls - Auth
        // -------------------------------------------------------------------

        public async Task<string> Login(LoginMessage loginMessage)
        {
            if (loginMessage == null || string.IsNullOrEmpty(loginMessage.username) || string.IsNullOrEmpty(loginMessage.password))
            {
                UnityEngine.Debug.LogError("Login message is invalid.");
                return null;
            }

            string json = JsonConvert.SerializeObject(loginMessage);
            string response = await SendRequest("/auth/login", "POST", json);

            if (response != null)
            {
                var authRes = JsonConvert.DeserializeObject<AuthResponse>(response);
                UnityEngine.Debug.Log("Login successful.");
                return authRes.playerId;
            }
            return null;
        }

        public async Task<bool> SignUp(SignUpMessage signUpMessage)
        {
            if (signUpMessage == null || string.IsNullOrEmpty(signUpMessage.username) || string.IsNullOrEmpty(signUpMessage.password))
            {
                UnityEngine.Debug.LogError("Sign up message is invalid.");
                return false;
            }

            string json = JsonConvert.SerializeObject(signUpMessage);
            string response = await SendRequest("/auth/signup", "POST", json);

            if (response != null)
            {
                UnityEngine.Debug.Log("User signed up successfully.");
                return true;
            }
            return false;
        }


        // -------------------------------------------------------------------
        // API calls - Players
        // -------------------------------------------------------------------

        public async Task<PlayerRole> GetRole(string playerId)
        {
            string response = await SendRequest($"/players/{playerId}/role", "GET");
            return response != null ? (PlayerRole)JsonConvert.DeserializeObject<int>(response) : PlayerRole.None;
        }

        public async Task<PlayerInfo> GetPlayerInfo(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return null;

            string response = await SendRequest($"/players/{playerId}/info", "GET");
            return response != null ? JsonConvert.DeserializeObject<PlayerInfo>(response) : null;
        }

        public async Task<int> GetPlayerAge(string playerId)
        {
            string response = await SendRequest($"/players/{playerId}/age", "GET");
            return response != null ? JsonConvert.DeserializeObject<int>(response) : 0;
        }

        public async Task<string> GetPlayerGender(string playerId)
        {
            string response = await SendRequest($"/players/{playerId}/gender", "GET");
            return response != null ? JsonConvert.DeserializeObject<string>(response) : "";
        }

        public async Task<string> GetPlayerDominantHand(string playerId)
        {
            string response = await SendRequest($"/players/{playerId}/dominant-hand", "GET");
            return response != null ? JsonConvert.DeserializeObject<string>(response) : "";
        }

        public async Task<string> GetPlayerInfoIdByPlayerId(string playerId)
        {
            var info = await GetPlayerInfo(playerId);
            return info?.name ?? "";
        }

        public async Task<bool> UpdatePlayerInfo(string userId, string name, string gender, int age, string dominantHand)
        {
            if (string.IsNullOrEmpty(userId)) return false;

            var infoUpdate = new
            {
                name = name,
                gender = gender,
                age = age,
                dominantHand = dominantHand
            };

            string json = JsonConvert.SerializeObject(infoUpdate);
            string response = await SendRequest($"/players/{userId}/info", "PUT", json);

            if (response != null)
            {
                UnityEngine.Debug.Log($"PlayerInfo updated successfully via API (ID: {userId}).");
                return true;
            }
            return false;
        }

        public async Task<List<DrawingMeta>> GetDrawingsForUser(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return new List<DrawingMeta>();

            string response = await SendRequest($"/players/{userId}/drawings", "GET");
            return response != null ? JsonConvert.DeserializeObject<List<DrawingMeta>>(response) : new List<DrawingMeta>();
        }


        // -------------------------------------------------------------------
        // API calls - Rooms
        // -------------------------------------------------------------------

        public async Task<string> CreateRoom(string name, string roomAddress, string sessionId, string drawingId, string hostId)
        {
            var req = new RoomCreateRequest { name = name, roomAddress = roomAddress, sessionID = sessionId, drawingID = drawingId, hostID = hostId };
            string json = JsonConvert.SerializeObject(req);
            string response = await SendRequest("/rooms/", "POST", json);

            if (response != null)
            {
                // Deserialize using a temporary dictionary or directly to an ActiveRoom class if defined
                var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
                return result.ContainsKey("roomCode") ? result["roomCode"] : null;
            }
            return null;
        }

        public async Task<RoomJoinResponse> JoinRoom(string roomCode)
        {
            string response = await SendRequest($"/rooms/{roomCode}", "GET");

            // If the response contains an error "detail" (like a 404), return null so the UI knows it failed.
            if (response != null && response.Contains("\"detail\"")) return null;
            return response != null ? JsonConvert.DeserializeObject<RoomJoinResponse>(response) : null;
        }

        public async Task<bool> DeleteRoom(string roomCode)
        {
            if (string.IsNullOrEmpty(roomCode)) return false;

            string response = await SendRequest($"/rooms/{roomCode}", "DELETE");

            // Check for success and ensure it isn't returning a 404 error
            if (response != null && !response.Contains("\"detail\""))
            {
                UnityEngine.Debug.Log($"Room {roomCode} deleted successfully via API.");
                return true;
            }
            return false;
        }

        public async Task<string> GetRoomIdByCode(string roomCode)
        {
            if (string.IsNullOrEmpty(roomCode)) return null;

            string response = await SendRequest($"/rooms/code/{roomCode}/id", "GET");
            if (response != null && !response.Contains("\"detail\""))
            {
                return JsonConvert.DeserializeObject<string>(response);
            }
            return null;
        }

        public async Task<string> GetRoomName(string roomId)
        {
            if (string.IsNullOrEmpty(roomId)) return null;

            string response = await SendRequest($"/rooms/{roomId}/name", "GET");
            if (response != null && !response.Contains("\"detail\""))
            {
                return JsonConvert.DeserializeObject<string>(response);
            }
            return null;
        }

        public async Task<string> GetRoomAddress(string roomId)
        {
            if (string.IsNullOrEmpty(roomId)) return null;

            string response = await SendRequest($"/rooms/{roomId}/address", "GET");
            if (response != null && !response.Contains("\"detail\""))
            {
                return JsonConvert.DeserializeObject<string>(response);
            }
            return null;
        }

        public async Task<string> GetRoomSessionId(string roomId)
        {
            if (string.IsNullOrEmpty(roomId)) return null;

            string response = await SendRequest($"/rooms/{roomId}/session", "GET");
            if (response != null && !response.Contains("\"detail\""))
            {
                return JsonConvert.DeserializeObject<string>(response);
            }
            return null;
        }

        public async Task<string> GetRoomDrawingId(string roomId)
        {
            if (string.IsNullOrEmpty(roomId)) return null;

            string response = await SendRequest($"/rooms/{roomId}/drawing", "GET");
            if (response != null && !response.Contains("\"detail\""))
            {
                return JsonConvert.DeserializeObject<string>(response);
            }
            return null;
        }

        public async Task<string> GetRoomHostId(string roomId)
        {
            if (string.IsNullOrEmpty(roomId)) return null;

            string response = await SendRequest($"/rooms/{roomId}/host", "GET");
            if (response != null && !response.Contains("\"detail\""))
            {
                return JsonConvert.DeserializeObject<string>(response);
            }
            return null;
        }

        public async Task<GameType?> GetRoomGameType(string roomId)
        {
            if (string.IsNullOrEmpty(roomId)) return null;

            string response = await SendRequest($"/rooms/{roomId}/gametype", "GET");
            if (response != null && !response.Contains("\"detail\""))
            {
                return (GameType)JsonConvert.DeserializeObject<int>(response);
            }
            return null;
        }

    }
}