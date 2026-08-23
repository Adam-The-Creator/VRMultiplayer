using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Assets.Database.DatabaseManagement;
using Assets.Database.DatabaseManagement.SQLiteDB;

public class AuthManager : MonoBehaviour
{
    [Serializable]
    public class AuthPanel
    {
        [Header("Login Panel Elements")]
        public GameObject panelObject;
        public TMP_InputField usernameInput;
        public TMP_InputField passwordInput;
        public Button loginButton;
        public Button signupButton;
        public TMP_Text feedbackText;

        public virtual void Initialize()
        {
            ClearFeedback();
            usernameInput.text = "";
            usernameInput.placeholder.GetComponent<TMP_Text>().text = "Username";
            passwordInput.text = "";
            passwordInput.placeholder.GetComponent<TMP_Text>().text = "Password";
        }

        public void WriteFeedbackError(string message)
        {
            if (feedbackText != null)
            {
                feedbackText.color = Color.red;
                feedbackText.text = message;
            }
        }

        public void WriteFeedback(string message)
        {
            if (feedbackText != null)
            {
                feedbackText.color = Color.white;
                feedbackText.text = message;
            }
        }

        public void ClearFeedback()
        {
            if (feedbackText != null) feedbackText.text = "";
        }

        public void SetActive(bool visibility = true)
        {
            panelObject.SetActive(visibility);
        }
    }

    [Serializable]
    public class LoginPanel : AuthPanel
    {
        public event Action OnLoginButtonClicked;
        public event Action OnSignupButtonClicked;

        public override void Initialize()
        {
            base.Initialize();
            loginButton.onClick.RemoveAllListeners();
            signupButton.onClick.RemoveAllListeners();

            loginButton.onClick.AddListener(() => { OnLoginButtonClicked?.Invoke(); });
            signupButton.onClick.AddListener(() => { OnSignupButtonClicked?.Invoke(); });
        }

        // Converted to async Task<bool>
        public async Task<bool> LoginAsync()
        {
            string username = usernameInput.text;
            string password = passwordInput.text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Debug.LogWarning("Username or Password field is empty for login.");
                WriteFeedbackError("Username and password are required!");
                return false;
            }

            WriteFeedback("Login in progress...");
            Debug.Log($"Attempting login for user: {username}");

            var loginMessage = new DatabaseManager.LoginMessage { username = username, password = password };

            // Await the asynchronous database call
            string userId = await DatabaseManager.Instance.Login(loginMessage);

            if (!string.IsNullOrEmpty(userId))
            {
                Debug.Log($"Login successful! User ID: {userId}");
                PlayerPrefs.SetString("CurrentUserID", userId);
                PlayerPrefs.SetString("CurrentUsername", usernameInput.text);
                PlayerPrefs.Save();

                WriteFeedback($"Logged in : {PlayerPrefs.GetString("CurrentUsername")}");
                return true;
            }

            Debug.LogWarning("Login failed.");
            WriteFeedbackError("Wrong username or password!");

            return false;
        }
    }

    [Serializable]
    public class SignupPanel : AuthPanel
    {
        public TMP_Dropdown roleDropdown;
        public event Action OnLoginButtonClicked;
        public event Action OnSignUpButtonClicked;

        // Changed to use the Role enum from SQLiteDB namespace
        private Role selectedRole = Role.Player;

        public override void Initialize()
        {
            base.Initialize();

            loginButton.onClick.RemoveAllListeners();
            signupButton.onClick.RemoveAllListeners();

            signupButton.onClick.AddListener(() => { OnSignUpButtonClicked?.Invoke(); });
            loginButton.onClick.AddListener(() => { OnLoginButtonClicked?.Invoke(); });

            selectedRole = Role.Player;
            roleDropdown.ClearOptions();
            roleDropdown.AddOptions(new List<string> { "Player", "Psychologist" });
            roleDropdown.value = (int)selectedRole;
            roleDropdown.RefreshShownValue();
            roleDropdown.onValueChanged.RemoveAllListeners();
            roleDropdown.onValueChanged.AddListener((int index) =>
            {
                selectedRole = (Role)index;
                Debug.Log($"SignupPanel: Selected role changed to {selectedRole}");
            });
        }

        // Converted to async Task<bool>
        public async Task<bool> SignupAsync()
        {
            string username = usernameInput.text;
            string password = passwordInput.text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Debug.LogWarning("AuthManager: Username or Password field is empty for sign up.");
                WriteFeedbackError("Username and password are required!");
                return false;
            }

            WriteFeedback("Sign up in progress...");
            Debug.Log($"AuthManager: Attempting sign up for user: {username}");

            var signUpMessage = new DatabaseManager.SignUpMessage { username = username, password = password, role = (int)selectedRole };

            // Await the asynchronous database call
            bool success = await DatabaseManager.Instance.SignUp(signUpMessage);

            if (success)
            {
                Debug.Log("AuthManager: Sign up successful!");
                WriteFeedback("Successful registration!");
                if (usernameInput != null) usernameInput.text = "";
                if (passwordInput != null) passwordInput.text = "";
                return true;
            }

            Debug.LogWarning("AuthManager: Sign up failed (e.g., username taken).");
            WriteFeedbackError("Sign up failed! Username may be taken.");

            return false;
        }
    }

    [Header("Authentication Panels")]
    [SerializeField] private LoginPanel loginPanel;
    [SerializeField] private SignupPanel signupPanel;

    public event Action OnLogIn;
    public event Action OnLogout;
    public event Action OnSignUp;

    void Start()
    {
        loginPanel.Initialize();
        signupPanel.Initialize();

        // Convert the lambda to an async void to await the login process
        loginPanel.OnLoginButtonClicked += async () =>
        {
            // Call the async login method
            if (await loginPanel.LoginAsync())
            {
                loginPanel.SetActive(false);
                signupPanel.SetActive(false);
                OnLogIn?.Invoke();
            }
        };

        loginPanel.OnSignupButtonClicked += () =>
        {
            loginPanel.SetActive(false);
            signupPanel.SetActive(true);
            signupPanel.Initialize();
        };

        signupPanel.OnLoginButtonClicked += () =>
        {
            signupPanel.SetActive(false);
            loginPanel.SetActive(true);
            loginPanel.Initialize();
        };

        // Convert the lambda to an async void to await the signup process
        signupPanel.OnSignUpButtonClicked += async () =>
        {
            // Call the async signup method
            if (await signupPanel.SignupAsync())
            {
                signupPanel.SetActive(false);
                loginPanel.SetActive(true);
                loginPanel.Initialize();
                OnSignUp?.Invoke();
            }
        };

        if (IsLoggedIn())
        {
            loginPanel.SetActive(false);
            signupPanel.SetActive(false);
        }
        else
        {
            loginPanel.SetActive(true);
            signupPanel.SetActive(false);
        }
    }

    public void Logout()
    {
        if (!IsLoggedIn())
        {
            Debug.LogWarning("AuthManager: No user is currently logged in.");
            return;
        }
        Debug.Log($"AuthManager: Logging out user: {GetCurrentUsername()}");

        PlayerPrefs.DeleteKey("CurrentUserID");
        PlayerPrefs.DeleteKey("CurrentUsername");
        PlayerPrefs.DeleteKey("DrawingToLoad");
        PlayerPrefs.DeleteKey("LoadMode");
        PlayerPrefs.Save();

        loginPanel.Initialize();
        signupPanel.Initialize();
        loginPanel.SetActive(true);
        signupPanel.SetActive(false);

        OnLogout?.Invoke();
    }

    public static bool IsLoggedIn()
    {
        return PlayerPrefs.HasKey("CurrentUserID") && !string.IsNullOrEmpty(PlayerPrefs.GetString("CurrentUserID", null));
    }

    public static string GetCurrentUsername()
    {
        return PlayerPrefs.GetString("CurrentUsername", null);
    }

    public static string GetCurrentUserID()
    {
        return PlayerPrefs.GetString("CurrentUserID", null);
    }

    // Since GetRole is async in DatabaseManager, you either need to make this async,
    // or retrieve the role synchronously if absolutely necessary. 
    // Here is the async version:
    public static async Task<Role> GetCurrentUserRoleAsync()
    {
        string userId = GetCurrentUserID();
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogWarning("AuthManager: No user is currently logged in.");
            return Role.None;
        }

        Role role = await DatabaseManager.Instance.GetRole(userId);
        Debug.Log($"AuthManager: Current user role is {role}");
        return role;
    }

    public void InitializeEvents()
    {
        OnLogIn = null;
        OnLogout = null;
        OnSignUp = null;
    }
}