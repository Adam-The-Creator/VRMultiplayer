using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Music Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float volume = 0.5f;
    [SerializeField] private bool playOnStart = false;

    private List<AudioClip> musicTracks = new List<AudioClip>();
    private List<string> musicTrackNames = new List<string>();
    private int currentTrackIndex = -1;
    private bool isPlaying = false;
    private bool isPaused = false;
    private bool isLoadingMusic = false;
    private string musicFolderPath;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.volume = volume;
        }

        musicFolderPath = Path.Combine(Application.streamingAssetsPath, "Music");
    }

    void Start()
    {
        StartCoroutine(LoadMusicFromStreamingAssets());
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Login" || scene.name.Contains("Login"))
        {
            StopMusic();
            Debug.Log("MusicManager: Music stopped because Login scene was loaded");
        }
    }

    void Update()
    {
        if (isPlaying && !audioSource.isPlaying && audioSource.loop)
        {
        }
    }

    
    private IEnumerator LoadMusicFromStreamingAssets()
    {
        isLoadingMusic = true;
        musicTracks.Clear();
        musicTrackNames.Clear();

        Debug.Log($"MusicManager: Looking for music in: {musicFolderPath}");
        Debug.Log($"MusicManager: StreamingAssets path: {Application.streamingAssetsPath}");
        Debug.Log($"MusicManager: Platform: {Application.platform}");

        if (!Directory.Exists(musicFolderPath))
        {
            Debug.LogError($"MusicManager: Music folder does NOT exist at: {musicFolderPath}");
            Debug.LogError($"MusicManager: Please create the folder: Assets/StreamingAssets/Music/");
            isLoadingMusic = false;
            yield break;
        }

        Debug.Log($"MusicManager: Music folder EXISTS at: {musicFolderPath}");

        List<string> audioFiles = new List<string>();
        try
        {
            string[] allFiles = Directory.GetFiles(musicFolderPath);
            Debug.Log($"MusicManager: Found {allFiles.Length} total file(s) in directory");
            
            foreach (string file in allFiles)
            {
                string fileName = Path.GetFileName(file);
                string ext = Path.GetExtension(file).ToLower();
                Debug.Log($"MusicManager: Checking file: {fileName} (extension: {ext})");
                
                if (ext == ".meta")
                {
                    Debug.Log($"MusicManager: Skipping .meta file: {fileName}");
                    continue;
                }
                
                if (ext == ".mp3" || ext == ".wav" || ext == ".ogg")
                {
                    audioFiles.Add(file);
                    Debug.Log($"MusicManager: Added audio file: {fileName}");
                }
                else
                {
                    Debug.LogWarning($"MusicManager: Unsupported file type: {fileName} (extension: {ext})");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MusicManager: Error reading directory: {e.Message}");
            Debug.LogError($"MusicManager: Stack trace: {e.StackTrace}");
            isLoadingMusic = false;
            yield break;
        }

        if (audioFiles.Count == 0)
        {
            Debug.LogError($"MusicManager: No audio files found in {musicFolderPath}");
            Debug.LogError($"MusicManager: Supported formats: .mp3, .wav, .ogg");
            Debug.LogError($"MusicManager: Please add audio files to: Assets/StreamingAssets/Music/");
            isLoadingMusic = false;
            yield break;
        }

        Debug.Log($"MusicManager: Found {audioFiles.Count} audio file(s) to load");

        foreach (string filePath in audioFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string fileExtension = Path.GetExtension(filePath).ToLower();
            
            Debug.Log($"MusicManager: Attempting to load: {fileName} ({fileExtension}) from {filePath}");

            AudioType audioType = AudioType.UNKNOWN;
            if (fileExtension == ".mp3")
                audioType = AudioType.MPEG;
            else if (fileExtension == ".wav")
                audioType = AudioType.WAV;
            else if (fileExtension == ".ogg")
                audioType = AudioType.OGGVORBIS;

            string url;
            #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                url = "file:///" + filePath.Replace("\\", "/");
            #elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
                url = "file://" + filePath;
            #elif UNITY_ANDROID
                url = filePath;
            #elif UNITY_IOS
                url = "file://" + filePath;
            #else
                url = "file:///" + filePath.Replace("\\", "/");
            #endif

            Debug.Log($"MusicManager: Loading from URL: {url} (AudioType: {audioType})");

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
            {
                if (www == null)
                {
                    Debug.LogError($"MusicManager: Failed to create UnityWebRequest for {fileName}");
                    continue;
                }

                DownloadHandlerAudioClip downloadHandler = new DownloadHandlerAudioClip(url, audioType);
                if (downloadHandler != null)
                {
                    downloadHandler.streamAudio = true;
                    www.downloadHandler = downloadHandler;
                }

                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    if (clip != null)
                    {
                        clip.name = fileName;
                        musicTracks.Add(clip);
                        musicTrackNames.Add(fileName);
                        Debug.Log($"MusicManager: ✓ Successfully loaded {fileName} (length: {clip.length:F2}s, channels: {clip.channels}, frequency: {clip.frequency}Hz)");
                    }
                    else
                    {
                        Debug.LogError($"MusicManager: ✗ AudioClip is null for {fileName} even though request succeeded");
                    }
                }
                else
                {
                    Debug.LogError($"MusicManager: ✗ Failed to load {fileName}");
                    Debug.LogError($"MusicManager: Error: {www.error}");
                    Debug.LogError($"MusicManager: Result: {www.result}");
                    Debug.LogError($"MusicManager: Response code: {www.responseCode}");
                }
            }

            yield return null;
        }

        isLoadingMusic = false;
        Debug.Log($"MusicManager: ===== Finished loading {musicTracks.Count} music track(s) out of {audioFiles.Count} file(s) =====");

        if (musicTracks.Count == 0)
        {
            Debug.LogError("MusicManager: No tracks were successfully loaded! Check the errors above.");
        }

        if (playOnStart && musicTracks.Count > 0)
        {
            PlayMusic(1); 
        }
    }

    public List<string> GetMusicTrackNames()
    {
        List<string> trackNames = new List<string>();
        trackNames.Add("None"); 
        
        trackNames.AddRange(musicTrackNames);
        
        return trackNames;
    }

    public bool IsLoadingMusic()
    {
        return isLoadingMusic;
    }

    public void PlayMusic(int trackIndex)
    {
        if (trackIndex == 0)
        {
            StopMusic();
            currentTrackIndex = -1;
            return;
        }

        int actualIndex = trackIndex - 1;

        if (actualIndex < 0 || actualIndex >= musicTracks.Count)
        {
            Debug.LogWarning($"MusicManager: Invalid track index {trackIndex}");
            return;
        }

        AudioClip clipToPlay = musicTracks[actualIndex];
        if (clipToPlay == null)
        {
            Debug.LogWarning($"MusicManager: AudioClip at index {actualIndex} is null");
            return;
        }

        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        audioSource.clip = clipToPlay;
        audioSource.Play();
        currentTrackIndex = trackIndex;
        isPlaying = true;
        isPaused = false;

        Debug.Log($"MusicManager: Now playing {clipToPlay.name}");
    }

    public void StopMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            isPlaying = false;
            isPaused = false;
            Debug.Log("MusicManager: Music stopped");
        }
    }

    public void PauseMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
            isPaused = true;
            Debug.Log("MusicManager: Music paused");
        }
    }

    public void ResumeMusic()
    {
        if (audioSource != null && audioSource.clip != null)
        {
            if (isPaused)
            {
                audioSource.UnPause();
                isPaused = false;
                isPlaying = true;
                Debug.Log("MusicManager: Music resumed");
            }
            else if (!audioSource.isPlaying)
            {
                audioSource.Play();
                isPlaying = true;
                isPaused = false;
                Debug.Log("MusicManager: Music started");
            }
        }
    }

    public bool IsPaused()
    {
        return isPaused;
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }

    public float GetVolume()
    {
        return volume;
    }

    public int GetCurrentTrackIndex()
    {
        return currentTrackIndex;
    }

    public bool IsPlaying()
    {
        return isPlaying && audioSource != null && audioSource.isPlaying;
    }

    public void ReloadMusic()
    {
        StartCoroutine(LoadMusicFromStreamingAssets());
    }

    public int GetTrackCount()
    {
        return musicTracks.Count;
    }

    public void PrintDebugInfo()
    {
        Debug.Log("=== MusicManager Debug Info ===");
        Debug.Log($"StreamingAssets Path: {Application.streamingAssetsPath}");
        Debug.Log($"Music Folder Path: {musicFolderPath}");
        Debug.Log($"Directory Exists: {Directory.Exists(musicFolderPath)}");
        Debug.Log($"Is Loading: {isLoadingMusic}");
        Debug.Log($"Loaded Tracks: {musicTracks.Count}");
        Debug.Log($"Track Names: {string.Join(", ", musicTrackNames)}");
        Debug.Log($"Current Track Index: {currentTrackIndex}");
        Debug.Log($"Is Playing: {isPlaying}");
        
        if (Directory.Exists(musicFolderPath))
        {
            try
            {
                string[] files = Directory.GetFiles(musicFolderPath);
                Debug.Log($"Files in directory: {files.Length}");
                foreach (string file in files)
                {
                    Debug.Log($"  - {Path.GetFileName(file)}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error listing files: {e.Message}");
            }
        }
        Debug.Log("=== End Debug Info ===");
    }
}

