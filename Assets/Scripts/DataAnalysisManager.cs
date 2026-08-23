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
                WriteFeedback("New session created successfully.");
                PlayerPrefs.SetString("CurrentSessionID", newSessionId);
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

        public List<KeyValuePair<string, string>> sessions = new();
        public string selectedSessionID = null;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            // Replaced FileHandler with Application.persistentDataPath
            defaultReportPath = Path.Combine(Application.persistentDataPath, "VRDrawing3D", "Reports");

            OnGenerateReportsButtonClicked = null;
            OnCancelButtonClicked = null;
            OnSessionSelectionChanged = null;
            OnSetAsCurrentButtonClicked = null;

            sessions = await GetSessionsAsync();
            selectedSessionID = sessions.Count > 0 ? sessions[0].Key : null;

            sessionNameDropdown.ClearOptions();
            sessionNameDropdown.AddOptions(sessions.Take(6).ToList().Select(s => s.Value).ToList());
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
            sessionNameDropdown.AddOptions(sessions.Take(6).ToList().Select(s => s.Value).ToList());
            sessionNameDropdown.value = 0;
            sessionNameDropdown.RefreshShownValue();
        }

        public override void Clear()
        {
            base.Clear();
            reportPathInput.text = defaultReportPath;
        }

        public async Task<List<KeyValuePair<string, string>>> GetSessionsAsync()
        {
            List<string> sessionIds = await DatabaseManager.Instance.GetAllSessionIds();
            List<KeyValuePair<string, string>> sessionList = new();
            for (int i = 0; i < sessionIds.Count; ++i)
            {
                string sessionName = await DatabaseManager.Instance.GetSessionNameById(sessionIds[i]);
                sessionList.Add(new KeyValuePair<string, string>(sessionIds[i], sessionName));
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

        // Refactored to DrawingMeta
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

            await RefreshDrawingsContainerAsync();
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

    async void Start()
    {
        await psychologistDashboardPanel.InitializeAsync();
        await newSessionPanel.InitializeAsync();
        await dataAnalysisPanel.InitializeAsync();
        await sessionDrawingsPanel.InitializeAsync();

        bool showBoy = await DatabaseManager.Instance.GetShowBoyForSession(dataAnalysisPanel.selectedSessionID);
        bool showGirl = await DatabaseManager.Instance.GetShowGirlForSession(dataAnalysisPanel.selectedSessionID);

        PlayerPrefs.SetInt("ShowSadChild", Convert.ToInt32(showBoy));
        PlayerPrefs.SetInt("ShowSadGirl", Convert.ToInt32(showGirl));
        PlayerPrefs.Save();

        sessionDrawingsPanel.selectedSessionId = dataAnalysisPanel.selectedSessionID;
        await sessionDrawingsPanel.RefreshDrawingsContainerAsync();

        Role userRole = await AuthManager.GetCurrentUserRoleAsync();
        SetPanelsActive(AuthManager.IsLoggedIn() && userRole == Role.Psychologist);

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
                bool _showBoy = await DatabaseManager.Instance.GetShowBoyForSession(dataAnalysisPanel.selectedSessionID);
                bool _showGirl = await DatabaseManager.Instance.GetShowGirlForSession(dataAnalysisPanel.selectedSessionID);

                PlayerPrefs.SetString("CurrentSessionID", dataAnalysisPanel.selectedSessionID);
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

    public static async Task<string> GetCurrentSessionIDAsync()
    {
        string sessionId = PlayerPrefs.GetString("CurrentSessionID", null);
        if (string.IsNullOrEmpty(sessionId))
        {
            sessionId = await DatabaseManager.Instance.GetLatestSessionId();
            if (!string.IsNullOrEmpty(sessionId))
            {
                PlayerPrefs.SetString("CurrentSessionID", sessionId);
                PlayerPrefs.Save();
            }
        }
        return sessionId;
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