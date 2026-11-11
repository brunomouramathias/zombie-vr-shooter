using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { StartMenu, Playing, GameOver }
    public GameState State { get; private set; } = GameState.StartMenu;

    public int Score { get; private set; }
    public int HighScore { get; private set; }
    public bool IsGameOver => State == GameState.GameOver;
    public bool IsPlaying => State == GameState.Playing;

    public int maxLives = 3;
    public int currentLives { get; private set; } = 3;

    public event System.Action<int> OnScoreChanged;
    public event System.Action OnGameOver;
    public event System.Action OnGameStarted;
    public event System.Action<int> OnLivesChanged;

    void Awake()
    {
        Instance = this;
        HighScore = PlayerPrefs.GetInt("ZombieVR_HighScore", 0);
        Score = 0;
        currentLives = maxLives;
        State = GameState.StartMenu;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void StartGame()
    {
        if (State == GameState.Playing) return;

        State = GameState.Playing;
        Score = 0;
        currentLives = maxLives;
        Debug.Log("[GameManager] Partida iniciada!");
        OnGameStarted?.Invoke();
        OnScoreChanged?.Invoke(Score);
        OnLivesChanged?.Invoke(currentLives);
    }

    public void AddScore(int points)
    {
        Score += points;
        if (Score > HighScore)
        {
            HighScore = Score;
            PlayerPrefs.SetInt("ZombieVR_HighScore", HighScore);
            PlayerPrefs.Save();
        }

        OnScoreChanged?.Invoke(Score);
    }

    public void TakeDamage(int damage = 1)
    {
        if (State != GameState.Playing) return;

        currentLives -= damage;
        Debug.LogWarning($"[GameManager] Jogador tomou dano! Vidas restantes: {currentLives}/{maxLives}");
        OnLivesChanged?.Invoke(currentLives);

        if (currentLives <= 0)
        {
            TriggerGameOver();
        }
    }

    public void TriggerGameOver()
    {
        if (State == GameState.GameOver) return;

        State = GameState.GameOver;
        Debug.Log("[GameManager] FIM DE JOGO!");
        OnGameOver?.Invoke();
    }

    public void ResetGame()
    {
        Score = 0;
        currentLives = maxLives;
        State = GameState.StartMenu;
        OnScoreChanged?.Invoke(Score);
    }

    public void Restart()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene("Zombie");
    }
}
