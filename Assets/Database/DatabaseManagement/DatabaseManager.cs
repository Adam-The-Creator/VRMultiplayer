using Assets.Database.DatabaseManagement.MongoDB;
using Assets.Database.DatabaseManagement.SQLiteDB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        private static readonly string apiUrl = "http://127.0.0.1:8000";
        private static readonly string _database = "VRDrawingDB.db";
        private static string _databasePath;
        
        void Start()
        {
            _databasePath = Path.Combine(Application.persistentDataPath, _database);
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

            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"API Error ({method} {endpoint}): {request.error}\nResponse: {request.downloadHandler.text}");
                return null;
            }

            return request.downloadHandler.text;
        }


        // Miscellaneous methods
        public static string DatabasePath() { return _databasePath; }

        public void Initialize()
        {
            // TODO: Update this method to call the API to update the database changes
            if (File.Exists(_databasePath))
            {
                Debug.Log("Database has already been initialized");
                return;
            }

            try
            {
                File.Copy(Path.Combine(Application.streamingAssetsPath, _database), _databasePath);
                Debug.Log($"Database file copied to persistent data path:\n{_databasePath}");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }


        // -------------------------------------------------------------------
        // API calls - Drawings
        // -------------------------------------------------------------------

        public async Task<bool> SaveDrawing(DrawingSaveMessage saveMessage)
        {
            if (saveMessage == null)
            {
                Debug.LogError("Invalid drawing data provided for saving.");
                return false;
            }

            // Using the new settings to ignore nulls when serializing optional properties
            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            string json = JsonConvert.SerializeObject(saveMessage, settings);
            string response = await SendRequest("/drawings/", "POST", json);

            if (response != null)
            {
                Debug.Log("Drawing saved successfully via API.");
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
                Debug.Log($"Drawing imported successfully via API: {drawingData.metadata.id}");
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
                Debug.Log($"Drawing record deleted successfully via API: ID {drawingId}");
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
                Debug.LogError("Invalid session name.");
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
                Debug.Log($"Session saved successfully: {session.name}");
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
                Debug.Log($"Session {sessionId} closed successfully.");
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
                Debug.Log($"Session {sessionId} deleted successfully.");
                return true;
            }
            return false;
        }

        public async Task<string> GetLatestSessionId()
        {
            string response = await SendRequest("/sessions/latest/id", "GET");
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
                Debug.LogError("Login message is invalid.");
                return null;
            }

            string json = JsonConvert.SerializeObject(loginMessage);
            string response = await SendRequest("/auth/login", "POST", json);

            if (response != null)
            {
                var authRes = JsonConvert.DeserializeObject<AuthResponse>(response);
                Debug.Log("Login successful.");
                return authRes.playerId;
            }
            return null;
        }

        public async Task<bool> SignUp(SignUpMessage signUpMessage)
        {
            if (signUpMessage == null || string.IsNullOrEmpty(signUpMessage.username) || string.IsNullOrEmpty(signUpMessage.password))
            {
                Debug.LogError("Sign up message is invalid.");
                return false;
            }

            string json = JsonConvert.SerializeObject(signUpMessage);
            string response = await SendRequest("/auth/signup", "POST", json);

            if (response != null)
            {
                Debug.Log("User signed up successfully.");
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
                Debug.Log($"PlayerInfo updated successfully via API (ID: {userId}).");
                return true;
            }
            return false;
        }

        public async Task<List<DrawingMeta>> GetDrawingsForUser(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return new List<DrawingMeta>();

            // Updated path to reflect the players_3.py router layout
            string response = await SendRequest($"/players/{userId}/drawings", "GET");
            return response != null ? JsonConvert.DeserializeObject<List<DrawingMeta>>(response) : new List<DrawingMeta>();
        }

    }
}