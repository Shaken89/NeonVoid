using UnityEngine;
using System;
using System.IO;

/// <summary>
/// Manages game saves, settings, and persistent data.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [System.Serializable]
    private class SaveData
    {
        public int highScore;
        public float musicVolume = 0.5f;
        public float sfxVolume = 0.7f;
        public int totalGamesPlayed;
        public int totalKills;
        public float totalPlayTime;
        public DateTime lastPlayed;
    }

    private const string SAVE_FILE_NAME = "savegame.json";
    private SaveData currentSave;
    private string savePath;

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSaveSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region Initialization

    private void InitializeSaveSystem()
    {
        savePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        LoadGame();
    }

    #endregion

    #region Save/Load

    public void SaveGame()
    {
        try
        {
            currentSave.lastPlayed = DateTime.Now;
            string json = JsonUtility.ToJson(currentSave, true);
            File.WriteAllText(savePath, json);
            Debug.Log($"Game saved to: {savePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game: {e.Message}");
        }
    }

    public void LoadGame()
    {
        try
        {
            if (File.Exists(savePath))
            {
                string json = File.ReadAllText(savePath);
                currentSave = JsonUtility.FromJson<SaveData>(json);
                Debug.Log("Game loaded successfully!");
            }
            else
            {
                currentSave = new SaveData();
                Debug.Log("No save file found. Creating new save data.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game: {e.Message}");
            currentSave = new SaveData();
        }
    }

    public void DeleteSave()
    {
        try
        {
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
                currentSave = new SaveData();
                Debug.Log("Save file deleted.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete save: {e.Message}");
        }
    }

    #endregion

    #region High Score

    public void SaveHighScore(int score)
    {
        if (score > currentSave.highScore)
        {
            currentSave.highScore = score;
            SaveGame();
        }
    }

    public int GetHighScore()
    {
        return currentSave.highScore;
    }

    #endregion

    #region Settings

    public void SaveSettings(float music, float sfx)
    {
        currentSave.musicVolume = Mathf.Clamp01(music);
        currentSave.sfxVolume = Mathf.Clamp01(sfx);
        SaveGame();
    }

    public float GetMusicVolume()
    {
        return currentSave.musicVolume;
    }

    public float GetSFXVolume()
    {
        return currentSave.sfxVolume;
    }

    #endregion

    #region Statistics

    public void IncrementGamesPlayed()
    {
        currentSave.totalGamesPlayed++;
        SaveGame();
    }

    public void AddKills(int kills)
    {
        currentSave.totalKills += kills;
        SaveGame();
    }

    public void AddPlayTime(float time)
    {
        currentSave.totalPlayTime += time;
        SaveGame();
    }

    public int GetTotalGamesPlayed() => currentSave.totalGamesPlayed;
    public int GetTotalKills() => currentSave.totalKills;
    public float GetTotalPlayTime() => currentSave.totalPlayTime;
    public DateTime GetLastPlayed() => currentSave.lastPlayed;

    #endregion

    #region Application Events

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
            SaveGame();
    }

    #endregion
}
