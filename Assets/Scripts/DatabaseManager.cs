using DrawingData;
using Mono.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class DatabaseManager : MonoBehaviour
{
    /*
     *  Database Model Classes  
     */

    public enum Role : int
    {
        None = -1,
        Player = 0,
        Psychologist = 1
    }

    private class Player
    {
        public string Id { get; private set; }
        public string Username { get; set; }
        public PasswordData PasswordData { get; private set; }
        public PlayerInfo PlayerInfo { get; private set; }
        public Drawing[] Drawings { get; private set; }
        public string SignedIn { get ; private set; }
        public string Created {  get; private set; }
        public Role Role { get; set; }

        public Player(string username, string password, PlayerInfo playerInfo = null, Drawing[] drawings = null, Role role = 0)
        {
            this.Id = Guid.NewGuid().ToString();
            this.Username = username;
            this.PasswordData = new PasswordData(password);
            this.PlayerInfo = playerInfo;
            this.Drawings = drawings;
            if (drawings != null) foreach (var drawing in drawings) drawing.PlayerId = this.Id;
            this.SignedIn = null;
            this.Created = GetDate();
            Role = role;
        }
    }

    private class PlayerInfo
    {
        public string Id { get; private set; }
        public string Fullname { get; set; }
        public string Gender { get; set; }
        public int Age { get; set; }
        public string DominantHand { get; set; }

        public PlayerInfo(string fullname = "", string gender = "", int age = 0, string dominantHand = "")
        {
            this.Id = Guid.NewGuid().ToString();
            this.Fullname = fullname;
            this.Gender = gender;
            this.Age = age;
            this.DominantHand = dominantHand;
        }
    }

    private class PasswordData
    {
        public string Id { get; private set; }
        public string Salt { get; private set; }
        public string Password { get; private set; }

        public PasswordData(string password)
        {
            Id = Guid.NewGuid().ToString();
            Salt = GenerateSalt();
            Password = Encrypt(password + Salt);
        }
    }

    public class Drawing
    {
        public string Id { get; private set; }
        public string PlayerId { get; set; } 
        public string Name { get; set; }
        public string Path { get; set; }
        public int GameType { get; set; }
        public string SessionId { get; set; }


        public Drawing(string name, string path, int gameType = 0, string sessionId = null)
        {
            this.Id = Guid.NewGuid().ToString();
            this.Name = name;
            this.Path = path;
            this.GameType = gameType;
            this.SessionId = sessionId;
        }

   
        public Drawing(string id, string playerId, string name, string path, int gameType = 0, string sessionId = null)
        {
            this.Id = id;
            this.PlayerId = playerId;
            this.Name = name;
            this.Path = path;
            this.GameType = gameType;
            this.SessionId = sessionId;
        }
    }

    public class Session
    {
        public string Id { get; private set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string StartDate { get; private set; }
        public bool ShowBoy { get; set; } = true;
        public bool ShowGirl { get; set; } = true;
        public Session(string name, string description = "", bool showBoy = true, bool showGirl = true)
        {
            this.Id = Guid.NewGuid().ToString();
            this.Name = name;
            this.Description = description;
            this.StartDate = GetDate();
            this.ShowBoy = showBoy;
            this.ShowGirl = showGirl;
        }
    }

    /*
     *  Message Classes
     */

    public class LoginMessage
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class SignUpMessage
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public int Role { get; set; } = 0;
    }

    private static readonly string _database = "VRDrawingDB.db";
    private static readonly string _databasePath = Path.Combine(Application.persistentDataPath, _database);
    private static readonly string _connectionString = $"data source={_databasePath}";
    private IDbConnection dbConnection = null;

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

    public static string DatabasePath() { return _databasePath; }

    public void Initialize()
    {
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

    private static string GetDate()
    {
        return DateTime.Now.ToString("yyyy.MM.dd-HH:mm");
    }

    private static string GenerateSalt()
    {
        System.Random rand = new System.Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-?.:+%/=()[]{}<>#&@$*";
        char[] salt = new char[rand.Next(8, 32)];

        for (int i = 0; i < salt.Length; ++i) salt[i] = chars[rand.Next(0, salt.Length)];

        return new string(salt);
    }

    private static string Encrypt(string str)
    {
        using SHA256 sha256Hash = SHA256.Create();
        byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(str));
        StringBuilder builder = new();
        foreach (byte b in bytes) builder.Append(b.ToString("x2"));
        return builder.ToString();
    }

    private bool OpenConnection()
    {
        if (dbConnection == null)
        {
            try
            {
                dbConnection = new SqliteConnection(_connectionString);
                dbConnection.Open();
                
                MigrateDatabase();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
            Debug.Log("Database connection opened.");
        }
        else Debug.LogWarning("Database is already connected.");
        return true;
    }

    private void MigrateDatabase()
    {
        try
        {
            // Check if GameType column exists in Drawings table
            using (IDbCommand command = dbConnection.CreateCommand())
            {
                command.CommandText = "PRAGMA table_info(Drawings)";
                using (IDataReader reader = command.ExecuteReader())
                {
                    bool gameTypeColumnExists = false;
                    while (reader.Read())
                    {
                        string columnName = reader.GetString(1); 
                        if (columnName == "GameType")
                        {
                            gameTypeColumnExists = true;
                            break;
                        }
                    }
                    reader.Close();

                    if (!gameTypeColumnExists)
                    {
                        Debug.Log("Adding GameType column to Drawings table...");
                        command.CommandText = "ALTER TABLE Drawings ADD COLUMN GameType INTEGER NOT NULL DEFAULT 0";
                        command.ExecuteNonQuery();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Database migration failed: {ex.Message}");
        }
    }

    private bool CloseConnection()
    {
        if (dbConnection != null)
        {
            try
            {
                dbConnection.Close();
                dbConnection = null;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
            Debug.Log("Database connection closed.");
        }
        else Debug.LogWarning("Database is already closed.");
        return true;
    }

    public bool SaveDrawing(string userId, string drawingName, string filePath, GameType gameType = 0, string sessionId = null)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(drawingName) || string.IsNullOrEmpty(filePath))
        {
            Debug.LogError("Invalid parameters for saving drawing.");
            return false;
        }

        if (!OpenConnection()) return false;

        bool success = false;
        try
        {
            Drawing newDrawing = new(drawingName, filePath, (int)gameType, sessionId)
            {
                PlayerId = userId
            };

            using (IDbCommand command = dbConnection.CreateCommand())
            {
                command.CommandText = "INSERT INTO Drawings (ID, PlayerId, Name, Path, GameType, SessionID) VALUES (@Id, @PlayerId, @Name, @Path, @GameType, @SessionID)";
                command.Parameters.Add(new SqliteParameter("@Id", newDrawing.Id));
                command.Parameters.Add(new SqliteParameter("@PlayerId", newDrawing.PlayerId));
                command.Parameters.Add(new SqliteParameter("@Name", newDrawing.Name));
                command.Parameters.Add(new SqliteParameter("@Path", newDrawing.Path));
                command.Parameters.Add(new SqliteParameter("@GameType", newDrawing.GameType));
                command.Parameters.Add(new SqliteParameter("@SessionID", string.IsNullOrEmpty(newDrawing.SessionId) ? (object)DBNull.Value : newDrawing.SessionId));

                command.ExecuteNonQuery();
                success = true;
                Debug.Log($"Drawing saved successfully: {drawingName} for user {userId} in GameType {gameType}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            success = false;
        }
        finally
        {
            CloseConnection();
        }
        return success;
    }

    public string SaveSession(string sessionName, string description = "", bool showBoy = true, bool showGirl = true)
    {
        if (string.IsNullOrEmpty(sessionName))
        {
            Debug.LogError("Invalid session name.");
            return null;
        }
        if (!OpenConnection()) return null;
        string sessionId = null;
        try
        {
            Session newSession = new(sessionName, description, showBoy, showGirl);
            using IDbCommand command = dbConnection.CreateCommand();
            command.CommandText = "INSERT INTO Sessions (ID, Name, Description, StartDate, ShowBoy, ShowGirl) VALUES (@Id, @Name, @Description, @StartDate, @ShowBoy, @ShowGirl)";
            command.Parameters.Add(new SqliteParameter("@Id", newSession.Id));
            command.Parameters.Add(new SqliteParameter("@Name", newSession.Name));
            command.Parameters.Add(new SqliteParameter("@Description", newSession.Description));
            command.Parameters.Add(new SqliteParameter("@StartDate", newSession.StartDate));
            command.Parameters.Add(new SqliteParameter("@ShowBoy", newSession.ShowBoy ? 1 : 0));
            command.Parameters.Add(new SqliteParameter("@ShowGirl", newSession.ShowGirl ? 1 : 0));
            command.ExecuteNonQuery();
            sessionId = newSession.Id;
            Debug.Log($"Session saved successfully: {sessionName}");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            sessionId = null;
        }
        finally
        {
            CloseConnection();
        }
        return sessionId;
    }

    public string GetLatestSessionId()
    {
        if (!OpenConnection()) return null;
        string sessionId = null;
        try
        {
            using IDbCommand command = dbConnection.CreateCommand();
            // This only works if StartDate is stored in a format described in ISO-8601 (lexicogrammatically orderable)
            command.CommandText = "SELECT ID FROM Sessions ORDER BY StartDate DESC LIMIT 1";
            using IDataReader reader = command.ExecuteReader();
            if (reader.Read()) sessionId = reader.GetString(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            sessionId = null;
        }
        finally
        {
            CloseConnection();
        }
        return sessionId;
    }

    public string GetSessionNameById(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            Debug.LogError("Invalid session ID.");
            return null;
        }

        if (!OpenConnection()) return null;

        string sessionName = null;
        try
        {
            using IDbCommand command = dbConnection.CreateCommand();
            command.CommandText = "SELECT Name FROM Sessions WHERE ID = @Id LIMIT 1";
            command.Parameters.Add(new SqliteParameter("@Id", sessionId));

            using IDataReader reader = command.ExecuteReader();
            if (reader.Read()) sessionName = reader.GetString(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            sessionName = null;
        }
        finally
        {
            CloseConnection();
        }

        return sessionName;
    }

    public string GetSessionDescriptionById(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            Debug.LogError("Invalid session ID.");
            return null;
        }
        if (!OpenConnection()) return null;
        string sessionDescription = null;
        try
        {
            using IDbCommand command = dbConnection.CreateCommand();
            command.CommandText = "SELECT Description FROM Sessions WHERE ID = @Id LIMIT 1";
            command.Parameters.Add(new SqliteParameter("@Id", sessionId));
            using IDataReader reader = command.ExecuteReader();
            if (reader.Read()) sessionDescription = reader.GetString(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            sessionDescription = null;
        }
        finally
        {
            CloseConnection();
        }
        return sessionDescription;
    }

    public bool GetShowBoyForSession(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            Debug.LogError("Invalid session ID.");
            return false;
        }

        if (!OpenConnection()) return false;

        int showBoy = 0;

        try
        {
            using IDbCommand command = dbConnection.CreateCommand();
            command.CommandText = "SELECT ShowBoy FROM Sessions WHERE ID = @Id LIMIT 1";
            command.Parameters.Add(new SqliteParameter("@Id", sessionId));
            using IDataReader reader = command.ExecuteReader();
            if (reader.Read()) showBoy = reader.GetInt32(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            showBoy = 0;
        }
        finally
        {
            CloseConnection();
        }

        return showBoy == 1;
    }

    public bool GetShowGirlForSession(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            Debug.LogError("Invalid session ID.");
            return false;
        }

        if (!OpenConnection()) return false;

        int showGirl = 0;

        try
        {
            using IDbCommand command = dbConnection.CreateCommand();
            command.CommandText = "SELECT ShowGirl FROM Sessions WHERE ID = @Id LIMIT 1";
            command.Parameters.Add(new SqliteParameter("@Id", sessionId));
            using IDataReader reader = command.ExecuteReader();
            if (reader.Read()) showGirl = reader.GetInt32(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            showGirl = 0;
        }
        finally
        {
            CloseConnection();
        }

        return showGirl == 1;
    }

    public List<string> GetAllSessionIds()
    {
        List<string> sessionIds = new();
        if (!OpenConnection()) return sessionIds;
        try
        {
            using IDbCommand command = dbConnection.CreateCommand();
            command.CommandText = "SELECT ID FROM Sessions ORDER BY StartDate ASC";
            using IDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                sessionIds.Add(reader.GetString(0));
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
        finally
        {
            CloseConnection();
        }
        return sessionIds;
    }

    public string Login(LoginMessage loginMessage) 
    {
        if (!OpenConnection()) return null; 

        if (loginMessage == null || string.IsNullOrEmpty(loginMessage.Username) || string.IsNullOrEmpty(loginMessage.Password))
        {
            Debug.LogError("Login message is invalid.");
            CloseConnection(); 
            return null;
        }

        string playerId = null; 
        try
        {
            using (IDbCommand usernameCheckCommand = dbConnection.CreateCommand())
            {
                
                usernameCheckCommand.CommandText = "SELECT ID, PasswordID FROM Players WHERE Username = @Username";
                usernameCheckCommand.Parameters.Add(new SqliteParameter("@Username", loginMessage.Username));

               

                using (IDataReader usernameReader = usernameCheckCommand.ExecuteReader())
                {
                    if (usernameReader.Read()) 
                    {
                        string foundPlayerId = usernameReader.GetString(0); 
                        string passwordDataId = usernameReader.GetString(1); 
                        usernameReader.Close(); 

                        using (IDbCommand passwordCheckCommand = dbConnection.CreateCommand())
                        {
                            passwordCheckCommand.CommandText = "SELECT Salt, Password FROM Passwords WHERE ID = @PasswordDataId";
                            passwordCheckCommand.Parameters.Add(new SqliteParameter("@PasswordDataId", passwordDataId));

                            using (IDataReader passwordReader = passwordCheckCommand.ExecuteReader())
                            {
                                if (passwordReader.Read()) 
                                {
                                    string salt = passwordReader.GetString(0);
                                    string storedPasswordHash = passwordReader.GetString(1);
                                    passwordReader.Close(); 

                                    if (storedPasswordHash == Encrypt(loginMessage.Password + salt))
                                    {
                                        // Sikeres bejelentkez�s!
                                        Debug.Log("Bejelentkez�s sikeres.");
                                        playerId = foundPlayerId; 

                                        
                                        using (IDbCommand updateLoginTimeCommand = dbConnection.CreateCommand())
                                        {
                                            updateLoginTimeCommand.CommandText = "UPDATE Players SET SignedIn = @CurrentTime WHERE ID = @PlayerId";
                                            updateLoginTimeCommand.Parameters.Add(new SqliteParameter("@CurrentTime", GetDate()));
                                            updateLoginTimeCommand.Parameters.Add(new SqliteParameter("@PlayerId", playerId));
                                            updateLoginTimeCommand.ExecuteNonQuery();
                                        }
                                    }
                                    else
                                    {
                                        Debug.LogWarning("Hib�s jelsz�.");
                                     
                                    }
                                }
                                else
                                {
                                    Debug.LogError("Password data not found for user, database inconsistency?");
                                }
                            } 
                        } 
                    }
                    else 
                    {
                        Debug.LogWarning("User not found.");
                      
                    }
                } 
            } 
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            playerId = null; 
        }
        finally 
        {
            CloseConnection();
        }

        return playerId; 
    }
    
    public bool SignUp(SignUpMessage signUpMessage) 
    {
        if (signUpMessage == null || string.IsNullOrEmpty(signUpMessage.Username) || string.IsNullOrEmpty(signUpMessage.Password))
        {
            Debug.LogError("Sign up message is invalid.");
            return false;
        }

        if (!OpenConnection()) return false;

        bool success = false; 

        using (IDbTransaction transaction = dbConnection.BeginTransaction()) 
        {
            try
            {

                using (IDbCommand usernameCheckCommand = dbConnection.CreateCommand())
                {
                    usernameCheckCommand.Transaction = transaction; 
                    usernameCheckCommand.CommandText = "SELECT COUNT(*) FROM Players WHERE Username = @Username";
                    usernameCheckCommand.Parameters.Add(new SqliteParameter("@Username", signUpMessage.Username));

                    int userCount = Convert.ToInt32(usernameCheckCommand.ExecuteScalar());
                    if (userCount > 0)
                    {
                        Debug.LogWarning("Username already exists.");
                       
                        CloseConnection(); 
                        return false; 
                    }
                }

               
                Player player = new(signUpMessage.Username, signUpMessage.Password, new PlayerInfo(), null, (Role)signUpMessage.Role);

                using (IDbCommand passwordInsertCommand = dbConnection.CreateCommand())
                {
                    passwordInsertCommand.Transaction = transaction;
                    passwordInsertCommand.CommandText = "INSERT INTO Passwords (ID, Salt, Password) VALUES (@Id, @Salt, @Password)";
                    passwordInsertCommand.Parameters.Add(new SqliteParameter("@Id", player.PasswordData.Id));
                    passwordInsertCommand.Parameters.Add(new SqliteParameter("@Salt", player.PasswordData.Salt));
                    passwordInsertCommand.Parameters.Add(new SqliteParameter("@Password", player.PasswordData.Password));
                    passwordInsertCommand.ExecuteNonQuery();
                }


                using (IDbCommand playerInfoInsertCommand = dbConnection.CreateCommand())
                {
                    playerInfoInsertCommand.Transaction = transaction;
                    playerInfoInsertCommand.CommandText = "INSERT INTO PlayerInfo (ID, Fullname, Gender, Age, DominantHand) VALUES (@Id, @Fullname, @Gender, @Age, @DominantHand)";
                    playerInfoInsertCommand.Parameters.Add(new SqliteParameter("@Id", player.PlayerInfo.Id));
                    playerInfoInsertCommand.Parameters.Add(new SqliteParameter("@Fullname", player.PlayerInfo.Fullname ?? (object)DBNull.Value)); 
                    playerInfoInsertCommand.Parameters.Add(new SqliteParameter("@Gender", player.PlayerInfo.Gender ?? (object)DBNull.Value));
                    playerInfoInsertCommand.Parameters.Add(new SqliteParameter("@Age", player.PlayerInfo.Age));
                    playerInfoInsertCommand.Parameters.Add(new SqliteParameter("@DominantHand", player.PlayerInfo.DominantHand ?? (object)DBNull.Value));
                    playerInfoInsertCommand.ExecuteNonQuery();
                }

   
                using (IDbCommand playerInsertCommand = dbConnection.CreateCommand())
                {
                    playerInsertCommand.Transaction = transaction;
                    playerInsertCommand.CommandText = "INSERT INTO Players (ID, Username, PasswordId, PlayerInfoID, SignedIn, Created, Role) VALUES (@ID, @Username, @PasswordId, @PlayerInfoID, @SignedIn, @Created, @Role)";
                    playerInsertCommand.Parameters.Add(new SqliteParameter("@ID", player.Id));
                    playerInsertCommand.Parameters.Add(new SqliteParameter("@Username", player.Username));
                    playerInsertCommand.Parameters.Add(new SqliteParameter("@PasswordId", player.PasswordData.Id));
                    playerInsertCommand.Parameters.Add(new SqliteParameter("@PlayerInfoID", player.PlayerInfo.Id));
                    playerInsertCommand.Parameters.Add(new SqliteParameter("@SignedIn", player.SignedIn ?? (object)DBNull.Value)); 
                    playerInsertCommand.Parameters.Add(new SqliteParameter("@Created", player.Created));
                    playerInsertCommand.Parameters.Add(new SqliteParameter("@Role", (int)player.Role));
                    playerInsertCommand.ExecuteNonQuery();
                }

                transaction.Commit(); 
                Debug.Log("User signed up successfully.");
                success = true; 
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                transaction.Rollback(); 
                success = false; 
            }
            finally
            {
                CloseConnection(); 
            }
        } 

        return success; 
    }

    public Role GetRole(string playerID)
    {
        if (string.IsNullOrEmpty(playerID))
        {
            Debug.LogError("GetPlayerInfoIdByPlayerId: Invalid playerId.");
            return Role.None;
        }

        if (dbConnection == null || dbConnection.State != ConnectionState.Open) if (!OpenConnection()) return Role.None;

        Role role = Role.None;
        try
        {
            using IDbCommand command = dbConnection.CreateCommand();
            command.CommandText = "SELECT Role FROM Players WHERE ID = @PlayerId";
            command.Parameters.Add(new SqliteParameter("@PlayerId", playerID));
            object result = command.ExecuteScalar();
            if (result != null && result != DBNull.Value) role = (Role)Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
        return role;
    }

    public string GetPlayerInfoIdByPlayerId(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("GetPlayerInfoIdByPlayerId: Invalid playerId.");
            return null;
        }

        if (dbConnection == null || dbConnection.State != ConnectionState.Open) if(!OpenConnection()) return null;

        string playerInfoId = null;

        try
        {
            using IDbCommand command = dbConnection.CreateCommand();
            command.CommandText = "SELECT PlayerInfoID FROM Players WHERE ID = @PlayerId";
            command.Parameters.Add(new SqliteParameter("@PlayerId", playerId));

            object result = command.ExecuteScalar();
            if (result != null && result != DBNull.Value) playerInfoId = result.ToString();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }

        if (string.IsNullOrEmpty(playerInfoId)) Debug.LogWarning($"No PlayerInfoID found for PlayerID: {playerId}");

        return playerInfoId;
    }

    public int GetPlayerAge(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("GetPlayerAge: Invalid playerId.");
            return 0;
        }

        if (dbConnection == null || dbConnection.State != ConnectionState.Open) if (!OpenConnection()) return 0;

        int age = 0;

        try
        {
            string playerInfoId = GetPlayerInfoIdByPlayerId(playerId);

            using IDbCommand command = dbConnection.CreateCommand();
            command.CommandText = "SELECT Age FROM PlayerInfo WHERE ID = @ID LIMIT 1;";
            command.Parameters.Add(new SqliteParameter("@ID", playerInfoId));

            object result = command.ExecuteScalar();
            if (result != null && result != DBNull.Value) age = Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            Debug.LogError($"DatabaseManager.GetPlayerAge error: {ex.Message}");
        }
        finally
        {
            CloseConnection();
        }

        return age;
    }

    public string GetPlayerGender(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("GetPlayerGender: Invalid playerId.");
            return "Male";
        }

        if (dbConnection == null || dbConnection.State != ConnectionState.Open) if (!OpenConnection()) return "Male";

        string gender = "Male";

        try
        {
            string playerInfoId = GetPlayerInfoIdByPlayerId(playerId);

            using IDbCommand command = dbConnection.CreateCommand();
            command.CommandText = "SELECT Gender FROM PlayerInfo WHERE ID = @ID LIMIT 1;";
            command.Parameters.Add(new SqliteParameter("@ID", playerInfoId));

            object result = command.ExecuteScalar();
            if (result != null && result != DBNull.Value) gender = result.ToString();
        }
        catch (Exception ex)
        {
            Debug.LogError($"DatabaseManager.GetPlayerGender error: {ex.Message}");
        }
        finally
        {
            CloseConnection();
        }

        return gender;
    }

    public string GetPlayerDominantHand(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("GetPlayerDominantHand: Invalid playerId.");
            return "Right";
        }

        if (dbConnection == null || dbConnection.State != ConnectionState.Open) if (!OpenConnection()) return "Right";

        string dominantHand = "Right";

        try
        {
            string playerInfoId = GetPlayerInfoIdByPlayerId(playerId);

            using IDbCommand command = dbConnection.CreateCommand();
            command.CommandText = "SELECT DominantHand FROM PlayerInfo WHERE ID = @ID LIMIT 1;";
            command.Parameters.Add(new SqliteParameter("@ID", playerInfoId));

            object result = command.ExecuteScalar();
            if (result != null && result != DBNull.Value) dominantHand = result.ToString();
        }
        catch (Exception ex)
        {
            Debug.LogError($"DatabaseManager.GetPlayerDominantHand error: {ex.Message}");
        }
        finally
        {
            CloseConnection();
        }

        return dominantHand;
    }

    public bool UpdatePlayerInfo(string userId, string fullname, string gender, int age, string dominantHand)
    {
        string playerInfoId = GetPlayerInfoIdByPlayerId(userId);

        if (string.IsNullOrEmpty(playerInfoId))
        {
            Debug.LogError("UpdatePlayerInfo: Invalid playerInfoId.");
            return false;
        }

        if (!OpenConnection()) return false;

        bool success = false;

        try
        {
            using IDbCommand command = dbConnection.CreateCommand();
            command.CommandText = @"UPDATE PlayerInfo SET Fullname = @Fullname, Gender = @Gender, Age = @Age, DominantHand = @DominantHand WHERE ID = @Id";

            command.Parameters.Add(new SqliteParameter("@Fullname", string.IsNullOrEmpty(fullname) ? (object)DBNull.Value : fullname.Trim()));
            command.Parameters.Add(new SqliteParameter("@Gender", string.IsNullOrEmpty(gender) ? (object)DBNull.Value : gender.Trim()));
            command.Parameters.Add(new SqliteParameter("@Age", age));
            command.Parameters.Add(new SqliteParameter("@DominantHand", string.IsNullOrEmpty(dominantHand) ? (object)DBNull.Value : dominantHand.Trim()));
            command.Parameters.Add(new SqliteParameter("@Id", playerInfoId));

            int rowsAffected = command.ExecuteNonQuery();
            success = rowsAffected > 0;

            if (success)
                Debug.Log($"PlayerInfo updated successfully (ID: {playerInfoId}).");
            else
                Debug.LogWarning($"No PlayerInfo found to update with ID: {playerInfoId}");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            success = false;
        }
        finally
        {
            CloseConnection();
        }

        return success;
    }

    public List<Drawing> GetDrawingsForUser(string userId)
    {
        List<Drawing> drawings = new List<Drawing>();
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("Cannot get drawings for null or empty userId.");
            return drawings; 
        }

        if (!OpenConnection()) return drawings;

        try
        {
            using (IDbCommand command = dbConnection.CreateCommand())
            {
                command.CommandText = "SELECT ID, Name, Path, GameType FROM Drawings WHERE PlayerId = @UserId ORDER BY Name ASC"; 
                command.Parameters.Add(new SqliteParameter("@UserId", userId));

                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string id = reader.GetString(0);
                        string name = reader.GetString(1);
                        string path = reader.GetString(2);
                        int gameType = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                    
                        drawings.Add(new Drawing(id, userId, name, path, gameType));
                    }
                    reader.Close(); 
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        
        }
        finally
        {
            CloseConnection();
        }

        Debug.Log($"Found {drawings.Count} drawings for user {userId}");
        return drawings;
    }

    public List<Drawing> GetDrawingsForSession(string sessionId)
    {
        List<Drawing> drawings = new();
        if (string.IsNullOrEmpty(sessionId))
        {
            Debug.LogError("Cannot get drawings for null or empty sessionId.");
            return drawings;
        }
        if (!OpenConnection()) return drawings;
        try
        {
            using IDbCommand command = dbConnection.CreateCommand();
            command.CommandText = "SELECT ID, PlayerID, Name, Path, GameType FROM Drawings WHERE SessionID = @SessionId ORDER BY Name ASC";
            command.Parameters.Add(new SqliteParameter("@SessionId", sessionId));
            using IDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                string id = reader.GetString(0);
                string playerId = reader.GetString(1);
                string name = reader.GetString(2);
                string path = reader.GetString(3);
                int gameType = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
                drawings.Add(new Drawing(id, playerId, name, path, gameType, sessionId));
            }
            reader.Close();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
        finally
        {
            CloseConnection();
        }
        Debug.Log($"Found {drawings.Count} drawings for session {sessionId}");
        return drawings;
    }

    public Drawing GetDrawingById(string drawingId)
    {
        if (string.IsNullOrEmpty(drawingId))
        {
            Debug.LogError("GetDrawingById: Invalid drawingId.");
            return null;
        }
        if (!OpenConnection()) return null;
        Drawing drawing = null;
        try
        {
            using IDbCommand command = dbConnection.CreateCommand();
            command.CommandText = "SELECT PlayerId, Name, Path, GameType, SessionID FROM Drawings WHERE ID = @DrawingId LIMIT 1";
            command.Parameters.Add(new SqliteParameter("@DrawingId", drawingId));
            using IDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                string playerId = reader.GetString(0);
                string name = reader.GetString(1);
                string path = reader.GetString(2);
                int gameType = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                string sessionId = reader.IsDBNull(4) ? null : reader.GetString(4);
                drawing = new Drawing(drawingId, playerId, name, path, gameType, sessionId);
            }
            reader.Close();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            drawing = null;
        }
        finally
        {
            CloseConnection();
        }
        return drawing;
    }

    public bool DeleteDrawing(string drawingId, string filePath)
    {
        if (string.IsNullOrEmpty(drawingId) || string.IsNullOrEmpty(filePath))
        {
            Debug.LogError("DeleteDrawing: Invalid drawingId or filePath.");
            return false;
        }

        bool dbSuccess = false;
        bool fileSuccess = false;


        try
        {
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                Debug.Log($"File deleted successfully: {filePath}");
                fileSuccess = true;
            }
            else
            {
                Debug.LogWarning($"File not found, cannot delete: {filePath}");
                fileSuccess = true; 
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error deleting file {filePath}: {e.Message}");
      
        }


        if (!OpenConnection()) return false;

        try
        {
            using (IDbCommand command = dbConnection.CreateCommand())
            {
                command.CommandText = "DELETE FROM Drawings WHERE ID = @DrawingId";
                command.Parameters.Add(new SqliteParameter("@DrawingId", drawingId));
                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    Debug.Log($"Drawing record deleted successfully from database: ID {drawingId}");
                    dbSuccess = true;
                }
                else
                {
                    Debug.LogWarning($"No drawing record found in database with ID {drawingId} to delete.");
                    dbSuccess = true; 
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            dbSuccess = false;
        }
        finally
        {
            CloseConnection();
        }

        return dbSuccess && fileSuccess; 
    }

}