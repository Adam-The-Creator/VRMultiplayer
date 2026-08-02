using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DataAnalysisManager : MonoBehaviour
{
    [Serializable] public class PanelBase
    {
        [Header("Panel Object")]
        public GameObject panelObject;
        public TMP_Text feedbackText;

        public virtual void Initialize()
        {
            ClearFeedback();
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

    [Serializable] public class PsychologistDashboardPanel : PanelBase
    {
        [Header("Dashboard Elements")]
        public TMP_Text activeSessionText;
        public TMP_Text sessionDescriptionText;
        public Button newSessionButton;
        public Button analyzeButton;

        public event Action OnNewSessionButtonClicked;
        public event Action OnAnalyzeButtonClicked;

        public override void Initialize()
        {
            base.Initialize();
            activeSessionText.text = DataAnalysisManager.GetCurrentSessionName() ?? "No Active Session";
            sessionDescriptionText.text = DataAnalysisManager.GetCurrentSessionDescription() ?? "";

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

    [Serializable] public class NewSessionPanel : PanelBase
    {
        // TODO: Make this environment independent
        public readonly string defaultDrawingsPath = FileHandler.DrawingsFolder;

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

        public override void Initialize()
        {
            base.Initialize();
            sessionNameInput.text = "";
            sessionNameInput.placeholder.GetComponent<TMP_Text>().text = "Session Name";
            sessionDescriptionInput.text = "";
            sessionDescriptionInput.placeholder.GetComponent<TMP_Text>().text = "Session Description";
            drawingsPath.text = defaultDrawingsPath;
            drawingsPath.placeholder.GetComponent<TMP_Text>().text = defaultDrawingsPath;
            //drawingsPath.interactable = false;
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

        public bool CreateNewSession()
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

            string newSessionId = DatabaseManager.Instance.SaveSession(sessionName, sessionDescription, showBoy, showGirl);
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

    [Serializable] public class DataAnalysisPanel : PanelBase
    {
        // Path to MyDocuments environment independently
        public readonly string defaultReportPath = FileHandler.ReportsFolder;

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

        public override void Initialize()
        {
            base.Initialize();
            
            OnGenerateReportsButtonClicked = null;
            OnCancelButtonClicked = null;
            OnSessionSelectionChanged = null;
            OnSetAsCurrentButtonClicked = null;

            sessions = GetSessions();
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
            //reportPathInput.interactable = false;

            generateReportsButton.onClick.RemoveAllListeners();
            generateReportsButton.onClick.AddListener(() => OnGenerateReportsButtonClicked?.Invoke());

            setAsCurrentButton.onClick.RemoveAllListeners();
            setAsCurrentButton.onClick.AddListener(() => OnSetAsCurrentButtonClicked?.Invoke());

            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(() => OnCancelButtonClicked?.Invoke());
        }

        public void RefreshSessions()
        {
            sessions = GetSessions();
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

        public List<KeyValuePair<string, string>> GetSessions()
        {
            List<string> sessionIds = DatabaseManager.Instance.GetAllSessionIds();
            List<KeyValuePair<string, string>> sessions = new();
            for (int i = 0; i < sessionIds.Count; ++i)
            {
                string sessionName = DatabaseManager.Instance.GetSessionNameById(sessionIds[i]);
                sessions.Add(new KeyValuePair<string, string>(sessionIds[i], sessionName));
            }
            return sessions;
        }
    }

    [Serializable] public class SessionDrawingsPanel : PanelBase
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
        public List<DatabaseManager.Drawing> selectedSessionDrawings = new(); // Drawings for the selected session
        public List<List<string>> pageContents = new(); // List of pages, each page is a list of drawing IDs

        public override void Initialize()
        {
            base.Initialize();

            selectedSessionId = null;
            currentPage = 1;
            totalPages = 1;

            OnPreviousPageButtonClicked = null;
            OnNextPageButtonClicked = null;

            previousPageButton.onClick.RemoveAllListeners();
            previousPageButton.onClick.AddListener(() => {
                RefreshDrawingsContainer();
                currentPage = currentPage > 1 ? currentPage - 1 : 1;
                OnPreviousPageButtonClicked?.Invoke();
            });

            nextPageButton.onClick.RemoveAllListeners();
            nextPageButton.onClick.AddListener(() => {
                RefreshDrawingsContainer();
                currentPage = currentPage < totalPages ? currentPage + 1 : totalPages;
                OnNextPageButtonClicked?.Invoke();
            });

            RefreshDrawingsContainer();
        }

        private List<DatabaseManager.Drawing> GetDrawingsForSession()
        {
            return DatabaseManager.Instance.GetDrawingsForSession(selectedSessionId ?? DataAnalysisManager.GetCurrentSessionID());
        }

        public void RefreshDrawingsContainer()
        {
            ClearDrawingsContainer();
            RefreshPagesForSession();

            if (selectedSessionId != null)
            {
                PageNumberText.text = $"Page: {currentPage} / {totalPages}";
                if (totalPages == 0) return;
                int pageIndex = currentPage - 1;
                foreach (var drawingId in pageContents[Math.Min(Math.Max(pageIndex, 0), pageContents.Count)])
                {
                    GameObject itemObj = GameObject.Instantiate(sessionDrawingItemPrefab, drawingsContainer);
                    var drawingData = selectedSessionDrawings.Find(d => d.Id == drawingId);
                    if (itemObj.TryGetComponent<SessionDrawingButton>(out var sessionDrawingButton)) sessionDrawingButton.Initialize(drawingId);
                }
            }
        }

        private void RefreshPagesForSession()
        {
            selectedSessionDrawings.Clear();
            selectedSessionDrawings = GetDrawingsForSession();
            pageContents.Clear();

            List<string> drawingIDs = selectedSessionDrawings.Select(d => d.Id).ToList();
            if (drawingIDs.Count > 0)
            {
                for (int i = 0; i < drawingIDs.Count; i += itemsPerPage) pageContents.Add(drawingIDs.Skip(i).Take(itemsPerPage).ToList());
            }
            totalPages = pageContents.Count;
            if (currentPage > totalPages) currentPage = 1;
            if (totalPages == 0) currentPage = 0;
        }

        public void ClearDrawingsContainer()
        {
            foreach (Transform drawingItem in drawingsContainer) GameObject.Destroy(drawingItem.gameObject);
        }
    }

    [SerializeField] private PsychologistDashboardPanel psychologistDashboardPanel;
    [SerializeField] private NewSessionPanel newSessionPanel;
    [SerializeField] private DataAnalysisPanel dataAnalysisPanel;
    [SerializeField] private SessionDrawingsPanel sessionDrawingsPanel;

    void Start()
    {
        psychologistDashboardPanel.Initialize();
        newSessionPanel.Initialize();
        dataAnalysisPanel.Initialize();
        sessionDrawingsPanel.Initialize();

        bool showBoy = DatabaseManager.Instance.GetShowBoyForSession(dataAnalysisPanel.selectedSessionID);
        bool showGirl = DatabaseManager.Instance.GetShowGirlForSession(dataAnalysisPanel.selectedSessionID);

        PlayerPrefs.SetInt("ShowSadChild", Convert.ToInt32(showBoy));
        PlayerPrefs.SetInt("ShowSadGirl", Convert.ToInt32(showGirl));
        PlayerPrefs.Save();

        sessionDrawingsPanel.selectedSessionId = dataAnalysisPanel.selectedSessionID;
        sessionDrawingsPanel.RefreshDrawingsContainer();

        SetPanelsActive(AuthManager.IsLoggedIn() && DatabaseManager.Instance.GetRole(AuthManager.GetCurrentUserID()) == DatabaseManager.Role.Psychologist);

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

        newSessionPanel.OnCreateSessionButtonClicked += () =>
        {
            if (newSessionPanel.CreateNewSession())
            {
                dataAnalysisPanel.RefreshSessions();
                SetPanelsActive(true);
                psychologistDashboardPanel.activeSessionText.text = GetCurrentSessionName();
                psychologistDashboardPanel.sessionDescriptionText.text = GetCurrentSessionDescription();
            }
        };
        newSessionPanel.OnCancelSessionButtonClicked += () =>
        {
            newSessionPanel.Clear();
            SetPanelsActive(true);
        };

        dataAnalysisPanel.OnGenerateReportsButtonClicked += () =>
        {
            dataAnalysisPanel.WriteFeedback("Generating reports...");
            DataAnalysis.GenerateReportsForSessionAsync(GetCurrentSessionID(), (success) => {
                Debug.Log($"Report generation finished. Success: {success}");
                if (success) dataAnalysisPanel.WriteFeedback("Reports generated successfully.");
                else dataAnalysisPanel.WriteFeedbackError("Failed to generate some reports.");
            });
        };
        dataAnalysisPanel.OnSetAsCurrentButtonClicked += () =>
        {
            if (!string.IsNullOrEmpty(dataAnalysisPanel.selectedSessionID))
            {
                bool _showBoy = DatabaseManager.Instance.GetShowBoyForSession(dataAnalysisPanel.selectedSessionID);
                bool _showGirl = DatabaseManager.Instance.GetShowGirlForSession(dataAnalysisPanel.selectedSessionID);

                PlayerPrefs.SetString("CurrentSessionID", dataAnalysisPanel.selectedSessionID);
                PlayerPrefs.SetInt("ShowSadChild", Convert.ToInt32(_showBoy));
                PlayerPrefs.SetInt("ShowSadGirl", Convert.ToInt32(_showGirl));
                PlayerPrefs.Save();
                psychologistDashboardPanel.activeSessionText.text = GetCurrentSessionName();
                psychologistDashboardPanel.sessionDescriptionText.text = GetCurrentSessionDescription();
            }
        };
        dataAnalysisPanel.OnSessionSelectionChanged += () =>
        {
            sessionDrawingsPanel.currentPage = 1;
            sessionDrawingsPanel.selectedSessionId = dataAnalysisPanel.selectedSessionID;
            sessionDrawingsPanel.RefreshDrawingsContainer();
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

    public static string GetCurrentSessionID()
    {
        string sessionId = PlayerPrefs.GetString("CurrentSessionID", null);
        if (string.IsNullOrEmpty(sessionId))
        {
            sessionId = DatabaseManager.Instance.GetLatestSessionId();
            if (!string.IsNullOrEmpty(sessionId))
            {
                PlayerPrefs.SetString("CurrentSessionID", sessionId);
                PlayerPrefs.Save();
            }
        }
        return sessionId;
    }

    public static string GetCurrentSessionName()
    {
        string sessionId = GetCurrentSessionID();
        if (string.IsNullOrEmpty(sessionId)) return null;
        return DatabaseManager.Instance.GetSessionNameById(sessionId);
    }

    public static string GetCurrentSessionDescription()
    {
        string sessionId = GetCurrentSessionID();
        if (string.IsNullOrEmpty(sessionId)) return null;
        return DatabaseManager.Instance.GetSessionDescriptionById(sessionId);
    }
}
