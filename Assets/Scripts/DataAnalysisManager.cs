using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Assets.Database.DatabaseManagement;
using Assets.Database.DatabaseManagement.SQLiteDB;
using DrawingMeta = Assets.Database.DatabaseManagement.SQLiteDB.Drawing;

public class DataAnalysisManager : MonoBehaviour
{
    [Serializable]
    public class PanelBase
    {
        [Header("Panel Object")]
        public GameObject panelObject;
        public TMP_Text feedbackText;

        // Converted to async Task
        public virtual async Task InitializeAsync()
        {
            ClearFeedback();
            await Task.Yield();
        }

        public virtual void Clear()
        {
            ClearFeedback();
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
            if (panelObject != null) panelObject.SetActive(visibility);
        }
    }

    [Serializable]
    public class PsychologistDashboardPanel : PanelBase
    {
        [Header("Dashboard Elements")]
        public TMP_Text activeSessionText;
        public TMP_Text sessionDescriptionText;
        public Button newSessionButton;
        public Button analyzeButton;

        public event Action OnNewSessionButtonClicked;
        public event Action OnAnalyzeButtonClicked;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            activeSessionText.text = await DataAnalysisManager.GetCurrentSessionNameAsync() ?? "No Active Session";
            sessionDescriptionText.text = await DataAnalysisManager.GetCurrentSessionDescriptionAsync() ?? "";

            OnNewSessionButtonClicked = null;
            OnAnalyzeButtonClicked = null;

            newSessionButton.onClick.RemoveAllListeners();
            newSessionButton.onClick.AddListener(() => OnNewSessionButtonClicked?.Invoke());

            analyzeButton.onClick.RemoveAllListeners();
            analyzeButton.onClick.AddListener(() => OnAnalyzeButtonClicked?.Invoke());
        }

        public override void Clear()
        {
            base.Clear();
            activeSessionText.text = "No Active Session";
            sessionDescriptionText.text = "";
        }
    }

    [Serializable]
    public class NewSessionPanel : PanelBase
    {
        public string defaultDrawingsPath;

        [Header("New Session Elements")]
        public TMP_InputField sessionNameInput;
        public TMP_InputField sessionDescriptionInput;
        public TMP_InputField drawingsPath;
        public Toggle showBoyToggle;
        public Toggle showGirlToggle;
        public Button createSessionButton;
        public Button cancelButton;

        public event Action OnCreateSessionButtonClicked;
        public event Action OnCancelSessionButtonClicked;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            // Replaced FileHandler with Application.persistentDataPath
            defaultDrawingsPath = Path.Combine(Application.persistentDataPath, "VRDrawing3D", "Drawings");

            sessionNameInput.text = "";
            sessionNameInput.placeholder.GetComponent<TMP_Text>().text = "Session Name";
            sessionDescriptionInput.text = "";
            sessionDescriptionInput.placeholder.GetComponent<TMP_Text>().text = "Session Description";
            drawingsPath.text = defaultDrawingsPath;
            drawingsPath.placeholder.GetComponent<TMP_Text>().text = defaultDrawingsPath;

            showBoyToggle.isOn = true;
            showGirlToggle.isOn = true;

            OnCreateSessionButtonClicked = null;
            OnCancelSessionButtonClicked = null;

            createSessionButton.onClick.RemoveAllListeners();
            createSessionButton.onClick.AddListener(() => OnCreateSessionButtonClicked?.Invoke());

            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(() => OnCancelSessionButtonClicked?.Invoke());
        }

        public override void Clear()
        {
            base.Clear();
            sessionNameInput.text = "";
            sessionDescriptionInput.text = "";
            drawingsPath.text = defaultDrawingsPath;
            showBoyToggle.isOn = true;
            showGirlToggle.isOn = true;
        }

        public async Task<bool> CreateNewSessionAsync()
        {
            string sessionName = sessionNameInput.text.Trim();
            string sessionDescription = sessionDescriptionInput.text.Trim();
            bool showBoy = showBoyToggle.isOn;
            bool showGirl = showGirlToggle.isOn;

            if (string.IsNullOrEmpty(sessionName))
            {
                WriteFeedbackError("Session name cannot be empty.");
                return false;
            }

            string newSessionId = await DatabaseManager.Instance.SaveSession(sessionName, sessionDescription, showBoy, showGirl);
            if (!string.IsNullOrEmpty(newSessionId))
            {
                // The server makes a new session the active one, so there is nothing to store locally.
                WriteFeedback("New session created successfully.");
                PlayerPrefs.SetInt("ShowSadChild", Convert.ToInt32(showBoy));
                PlayerPrefs.SetInt("ShowSadGirl", Convert.ToInt32(showGirl));
                PlayerPrefs.Save();
            }
            else
            {
                WriteFeedbackError("Failed to create new session.");
                return false;
            }
            return true;
        }
    }

    [Serializable]
    public class DataAnalysisPanel : PanelBase
    {
        public string defaultReportPath;

        [Header("New Session Elements")]
        public TMP_Dropdown sessionNameDropdown;
        public TMP_InputField reportPathInput;
        public Button generateReportsButton;
        public Button setAsCurrentButton;
        public Button cancelButton;

        public event Action OnGenerateReportsButtonClicked;
        public event Action OnSetAsCurrentButtonClicked;
        public event Action OnCancelButtonClicked;
        public event Action OnSessionSelectionChanged;

        private const int MaxDropdownSessions = 6;

        public List<KeyValuePair<string, string>> sessions = new();
        public string selectedSessionID = null;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            defaultReportPath = Path.Combine(Application.persistentDataPath, "VRDrawing3D", "Reports");

            OnGenerateReportsButtonClicked = null;
            OnCancelButtonClicked = null;
            OnSessionSelectionChanged = null;
            OnSetAsCurrentButtonClicked = null;

            sessions = await GetSessionsAsync();
            selectedSessionID = sessions.Count > 0 ? sessions[0].Key : null;

            sessionNameDropdown.ClearOptions();
            sessionNameDropdown.AddOptions(sessions.Select(s => s.Value).ToList());
            sessionNameDropdown.value = 0;
            sessionNameDropdown.onValueChanged.RemoveAllListeners();
            sessionNameDropdown.onValueChanged.AddListener((index) => {
                selectedSessionID = sessions[index].Key;
                OnSessionSelectionChanged?.Invoke();
            });
            sessionNameDropdown.RefreshShownValue();

            reportPathInput.text = defaultReportPath;
            reportPathInput.placeholder.GetComponent<TMP_Text>().text = defaultReportPath;

            generateReportsButton.onClick.RemoveAllListeners();
            generateReportsButton.onClick.AddListener(() => OnGenerateReportsButtonClicked?.Invoke());

            setAsCurrentButton.onClick.RemoveAllListeners();
            setAsCurrentButton.onClick.AddListener(() => OnSetAsCurrentButtonClicked?.Invoke());

            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(() => OnCancelButtonClicked?.Invoke());
        }

        public async Task RefreshSessionsAsync()
        {
            sessions = await GetSessionsAsync();
            selectedSessionID = sessions.Count > 0 ? sessions[0].Key : null;
            sessionNameDropdown.ClearOptions();
            sessionNameDropdown.AddOptions(sessions.Select(s => s.Value).ToList());
            sessionNameDropdown.value = 0;
            sessionNameDropdown.RefreshShownValue();
        }

        public override void Clear()
        {
            base.Clear();
            reportPathInput.text = defaultReportPath;
        }

        // Returns the newest sessions first. The server lists them oldest first, and only the newest ones fit into
        // the dropdown, so only their names are requested.
        public async Task<List<KeyValuePair<string, string>>> GetSessionsAsync()
        {
            List<string> sessionIds = await DatabaseManager.Instance.GetAllSessionIds();
            List<KeyValuePair<string, string>> sessionList = new();
            foreach (string sessionId in Enumerable.Reverse(sessionIds).Take(MaxDropdownSessions))
            {
                string sessionName = await DatabaseManager.Instance.GetSessionNameById(sessionId);
                sessionList.Add(new KeyValuePair<string, string>(sessionId, sessionName ?? sessionId));
            }
            return sessionList;
        }
    }

    [Serializable]
    public class SessionDrawingsPanel : PanelBase
    {
        [Header("Session Drawings Elements")]
        public TMP_Text PageNumberText;
        public Button previousPageButton;
        public Button nextPageButton;
        public Transform drawingsContainer;
        public GameObject sessionDrawingItemPrefab;

        public event Action OnPreviousPageButtonClicked;
        public event Action OnNextPageButtonClicked;

        public int currentPage = 1;
        public int totalPages = 1;
        public readonly int itemsPerPage = 7;
        public string selectedSessionId = null;

        public List<DrawingMeta> selectedSessionDrawings = new();
        public List<List<string>> pageContents = new();

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            selectedSessionId = null;
            currentPage = 1;
            totalPages = 1;

            OnPreviousPageButtonClicked = null;
            OnNextPageButtonClicked = null;

            previousPageButton.onClick.RemoveAllListeners();
            previousPageButton.onClick.AddListener(async () => {
                currentPage = currentPage > 1 ? currentPage - 1 : 1;
                await RefreshDrawingsContainerAsync();
                OnPreviousPageButtonClicked?.Invoke();
            });

            nextPageButton.onClick.RemoveAllListeners();
            nextPageButton.onClick.AddListener(async () => {
                currentPage = currentPage < totalPages ? currentPage + 1 : totalPages;
                await RefreshDrawingsContainerAsync();
                OnNextPageButtonClicked?.Invoke();
            });

            // No session is selected yet (the manager sets it next), so there is nothing to fetch here.
            ClearDrawingsContainer();
        }

        private async Task<List<DrawingMeta>> GetDrawingsForSessionAsync()
        {
            string sessionIdToFetch = selectedSessionId ?? await DataAnalysisManager.GetCurrentSessionIDAsync();
            return await DatabaseManager.Instance.GetDrawingsForSession(sessionIdToFetch);
        }

        public async Task RefreshDrawingsContainerAsync()
        {
            ClearDrawingsContainer();
            await RefreshPagesForSessionAsync();

            if (selectedSessionId != null)
            {
                PageNumberText.text = $"Page: {currentPage} / {totalPages}";
                if (totalPages == 0) return;

                int pageIndex = currentPage - 1;
                foreach (var drawingId in pageContents[Math.Min(Math.Max(pageIndex, 0), pageContents.Count - 1)])
                {
                    GameObject itemObj = Instantiate(sessionDrawingItemPrefab, drawingsContainer);
                    var drawingData = selectedSessionDrawings.Find(d => d.id == drawingId);

                    if (itemObj.TryGetComponent<SessionDrawingButton>(out var sessionDrawingButton))
                    {
                        sessionDrawingButton.Initialize(drawingId); // Note: Update SessionDrawingButton to take ID if not already
                    }
                }
            }
        }

        private async Task RefreshPagesForSessionAsync()
        {
            selectedSessionDrawings.Clear();
            selectedSessionDrawings = await GetDrawingsForSessionAsync();
            pageContents.Clear();

            List<string> drawingIDs = selectedSessionDrawings.Select(d => d.id).ToList();
            if (drawingIDs.Count > 0)
            {
                for (int i = 0; i < drawingIDs.Count; i += itemsPerPage)
                {
                    pageContents.Add(drawingIDs.Skip(i).Take(itemsPerPage).ToList());
                }
            }

            totalPages = pageContents.Count;
            if (currentPage > totalPages) currentPage = 1;
            if (totalPages == 0) currentPage = 0;
        }

        public void ClearDrawingsContainer()
        {
            foreach (Transform drawingItem in drawingsContainer) Destroy(drawingItem.gameObject);
        }
    }

    [SerializeField] private PsychologistDashboardPanel psychologistDashboardPanel;
    [SerializeField] private NewSessionPanel newSessionPanel;
    [SerializeField] private DataAnalysisPanel dataAnalysisPanel;
    [SerializeField] private SessionDrawingsPanel sessionDrawingsPanel;

    // Loads everything the panels show. It needs a valid login token, so the LobbyManager calls it after the login
    // (or at scene start when the player is already logged in) instead of this component loading in Start.
    public async Task InitializeForUserAsync(bool isPsychologist)
    {
        if (isPsychologist)
        {
            // The panels' InitializeAsync reset their events, so the events are subscribed again afterwards.
            await psychologistDashboardPanel.InitializeAsync();
            await newSessionPanel.InitializeAsync();
            await dataAnalysisPanel.InitializeAsync();
            await sessionDrawingsPanel.InitializeAsync();
            SubscribeEvents();
        }

        // Every role needs this: the game scenes show the sad child / sad girl models based on it.
        await ApplySessionDisplayPrefsAsync(await GetCurrentSessionIDAsync());

        if (isPsychologist)
        {
            sessionDrawingsPanel.selectedSessionId = dataAnalysisPanel.selectedSessionID;
            await sessionDrawingsPanel.RefreshDrawingsContainerAsync();
        }

        // The login may have been lost (token expired) while the data was loading.
        SetPanelsActive(isPsychologist && AuthManager.IsLoggedIn());
    }

    // Keeps the previous values when the session cannot be read, instead of hiding both models.
    private static async Task ApplySessionDisplayPrefsAsync(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId)) return;

        // A "not found" answer from the server deserializes into a Session without an id.
        Session session = await DatabaseManager.Instance.GetSessionById(sessionId);
        if (session == null || string.IsNullOrEmpty(session.id)) return;

        PlayerPrefs.SetInt("ShowSadChild", Convert.ToInt32(session.showBoy));
        PlayerPrefs.SetInt("ShowSadGirl", Convert.ToInt32(session.showGirl));
        PlayerPrefs.Save();
    }

    private void SubscribeEvents()
    {
        psychologistDashboardPanel.OnNewSessionButtonClicked += () =>
        {
            newSessionPanel.SetActive(true);
            dataAnalysisPanel.SetActive(false);
            sessionDrawingsPanel.SetActive(false);
        };

        psychologistDashboardPanel.OnAnalyzeButtonClicked += () =>
        {
            newSessionPanel.SetActive(false);
            dataAnalysisPanel.SetActive(true);
            sessionDrawingsPanel.SetActive(true);
        };

        newSessionPanel.OnCreateSessionButtonClicked += async () =>
        {
            if (await newSessionPanel.CreateNewSessionAsync())
            {
                await dataAnalysisPanel.RefreshSessionsAsync();

                // The dropdown now selects the new session, so the drawings list has to follow it.
                sessionDrawingsPanel.currentPage = 1;
                sessionDrawingsPanel.selectedSessionId = dataAnalysisPanel.selectedSessionID;
                await sessionDrawingsPanel.RefreshDrawingsContainerAsync();

                SetPanelsActive(true);
                psychologistDashboardPanel.activeSessionText.text = await GetCurrentSessionNameAsync();
                psychologistDashboardPanel.sessionDescriptionText.text = await GetCurrentSessionDescriptionAsync();
            }
        };

        newSessionPanel.OnCancelSessionButtonClicked += () =>
        {
            newSessionPanel.Clear();
            SetPanelsActive(true);
        };

        dataAnalysisPanel.OnGenerateReportsButtonClicked += async () =>
        {
            dataAnalysisPanel.WriteFeedback("Generating reports...");
            string currentSessionId = await GetCurrentSessionIDAsync();

            // Assuming DataAnalysis class handles report generation externally
            DataAnalysis.GenerateReportsForSessionAsync(currentSessionId, (success) => {
                Debug.Log($"Report generation finished. Success: {success}");
                if (success) dataAnalysisPanel.WriteFeedback("Reports generated successfully.");
                else dataAnalysisPanel.WriteFeedbackError("Failed to generate some reports.");
            });
        };

        dataAnalysisPanel.OnSetAsCurrentButtonClicked += async () =>
        {
            if (!string.IsNullOrEmpty(dataAnalysisPanel.selectedSessionID))
            {
                // The active session is stored on the server, so that it is the same on every PC.
                if (!await DatabaseManager.Instance.ActivateSession(dataAnalysisPanel.selectedSessionID))
                {
                    dataAnalysisPanel.WriteFeedbackError("Failed to set the session as current.");
                    return;
                }
                dataAnalysisPanel.ClearFeedback();

                bool _showBoy = false;
                bool _showGirl = false;

                if (!string.IsNullOrEmpty(dataAnalysisPanel.selectedSessionID))
                {
                    _showBoy = await DatabaseManager.Instance.GetShowBoyForSession(dataAnalysisPanel.selectedSessionID);
                    _showGirl = await DatabaseManager.Instance.GetShowGirlForSession(dataAnalysisPanel.selectedSessionID);
                }

                PlayerPrefs.SetInt("ShowSadChild", Convert.ToInt32(_showBoy));
                PlayerPrefs.SetInt("ShowSadGirl", Convert.ToInt32(_showGirl));
                PlayerPrefs.Save();

                psychologistDashboardPanel.activeSessionText.text = await GetCurrentSessionNameAsync();
                psychologistDashboardPanel.sessionDescriptionText.text = await GetCurrentSessionDescriptionAsync();
            }
        };

        dataAnalysisPanel.OnSessionSelectionChanged += async () =>
        {
            sessionDrawingsPanel.currentPage = 1;
            sessionDrawingsPanel.selectedSessionId = dataAnalysisPanel.selectedSessionID;
            await sessionDrawingsPanel.RefreshDrawingsContainerAsync();
        };

        dataAnalysisPanel.OnCancelButtonClicked += () =>
        {
            dataAnalysisPanel.Clear();
            SetPanelsActive(true);
        };
    }

    public void SetPanelsActive(bool dashboardActive = true)
    {
        psychologistDashboardPanel.SetActive(dashboardActive);
        newSessionPanel.SetActive(false);
        dataAnalysisPanel.SetActive(false);
        sessionDrawingsPanel.SetActive(false);
    }

    // The active session of the server (its default session while no other one is active). It is not stored on this PC,
    // so a logout or another PC never changes it.
    public static async Task<string> GetCurrentSessionIDAsync()
    {
        return await DatabaseManager.Instance.GetActiveSessionId();
    }

    public static async Task<string> GetCurrentSessionNameAsync()
    {
        string sessionId = await GetCurrentSessionIDAsync();
        if (string.IsNullOrEmpty(sessionId)) return null;
        return await DatabaseManager.Instance.GetSessionNameById(sessionId);
    }

    public static async Task<string> GetCurrentSessionDescriptionAsync()
    {
        string sessionId = await GetCurrentSessionIDAsync();
        if (string.IsNullOrEmpty(sessionId)) return null;
        return await DatabaseManager.Instance.GetSessionDescriptionById(sessionId);
    }
}