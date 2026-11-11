using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ScoreboardUI : MonoBehaviour
{
    public float targetDistance = 1.15f;
    public float heightOffset = 0.30f;
    public float rightOffset = 0.55f;
    public float followSpeed = 7.0f;

    public TMP_Text scoreText;
    public TMP_Text highScoreText;
    public TMP_Text killedText;
    public TMP_Text livesText;
    public Image heart1;
    public Image heart2;
    public Image heart3;
    public GameObject startMenuPanel;
    public GameObject playingPanel;
    public GameObject gameOverPanel;
    public TMP_Text gameOverScoreText;

    private Transform cameraTransform;
    private int killedCount;
    private int lastScore;
    private float gameOverTime;
    private bool gameOverActive;
    private bool restarting;
    private Sprite heartSprite;
    private readonly Image[] hearts = new Image[3];

    void Start()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(800, 400);
            rect.localScale = Vector3.one * 0.0008f;
        }

        cameraTransform = Camera.main != null ? Camera.main.transform : null;
        hearts[0] = heart1;
        hearts[1] = heart2;
        hearts[2] = heart3;
        heartSprite = CreateHeartSprite();
        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] != null)
            {
                hearts[i].sprite = heartSprite;
                hearts[i].color = new Color(1f, 0.12f, 0.16f, 1f);
                hearts[i].raycastTarget = false;
            }
        }

        if (startMenuPanel != null) startMenuPanel.SetActive(false);
        if (playingPanel != null) playingPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged += HandleScoreChanged;
            GameManager.Instance.OnGameOver += HandleGameOver;
            GameManager.Instance.OnGameStarted += HandleGameStarted;
            GameManager.Instance.OnLivesChanged += HandleLivesChanged;
            lastScore = GameManager.Instance.Score;
            UpdateTexts(GameManager.Instance.Score, GameManager.Instance.HighScore, GameManager.Instance.currentLives);
            UpdateHearts(GameManager.Instance.currentLives);
            if (!GameManager.Instance.IsPlaying && !GameManager.Instance.IsGameOver)
            {
                GameManager.Instance.StartGame();
            }
        }

        PositionPanel(true);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged -= HandleScoreChanged;
            GameManager.Instance.OnGameOver -= HandleGameOver;
            GameManager.Instance.OnGameStarted -= HandleGameStarted;
            GameManager.Instance.OnLivesChanged -= HandleLivesChanged;
        }
        if (heartSprite != null) Destroy(heartSprite);
        if (heartSprite != null && heartSprite.texture != null) Destroy(heartSprite.texture);
    }

    void Update()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        if (cameraTransform == null) return;

        bool gameOver = GameManager.Instance != null && GameManager.Instance.IsGameOver;
        PositionPanel(false, gameOver);

        // Reinício de segurança: trigger do Cardboard, toque ou clique esquerdo
        // reiniciam após o fim de jogo. O botão por olhar (dwell) continua ativo.
        if (gameOverActive && Time.unscaledTime - gameOverTime > 0.8f && WasRestartPressed())
        {
            gameOverActive = false;
            RestartGame();
        }
    }

    private static bool WasRestartPressed()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;

        Touchscreen touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame) return true;

#if !UNITY_EDITOR
        return Google.XR.Cardboard.Api.IsTriggerPressed;
#else
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && (keyboard.rKey.wasPressedThisFrame ||
                                    keyboard.enterKey.wasPressedThisFrame ||
                                    keyboard.spaceKey.wasPressedThisFrame);
#endif
    }

    private void PositionPanel(bool snap, bool gameOver = false)
    {
        if (cameraTransform == null) return;

        Vector3 forward = cameraTransform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        float distance = gameOver ? 1.5f : targetDistance;
        float side = gameOver ? 0f : rightOffset;
        float height = gameOver ? -0.08f : heightOffset;
        Vector3 desired = cameraTransform.position + forward * distance + right * side + Vector3.up * height;

        Vector3 direction = desired - cameraTransform.position;
        float distanceToPanel = direction.magnitude;
        if (Physics.Raycast(cameraTransform.position, direction.normalized, out RaycastHit hit, distanceToPanel, ~0, QueryTriggerInteraction.Ignore))
        {
            if (!hit.transform.IsChildOf(cameraTransform.parent))
            {
                desired = cameraTransform.position + direction.normalized * Mathf.Max(0.65f, hit.distance - 0.12f);
            }
        }

        Vector3 panelForward = desired - cameraTransform.position;
        panelForward.y = 0f;
        if (panelForward.sqrMagnitude < 0.001f) panelForward = forward;

        float blend = snap ? 1f : 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, blend);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(panelForward), blend);
    }

    private void HandleScoreChanged(int score)
    {
        if (score > lastScore) killedCount++;
        lastScore = score;
        int high = GameManager.Instance != null ? GameManager.Instance.HighScore : score;
        int lives = GameManager.Instance != null ? GameManager.Instance.currentLives : 3;
        UpdateTexts(score, high, lives);
    }

    private void HandleLivesChanged(int lives)
    {
        int score = GameManager.Instance != null ? GameManager.Instance.Score : 0;
        int high = GameManager.Instance != null ? GameManager.Instance.HighScore : 0;
        UpdateTexts(score, high, lives);
        UpdateHearts(lives);
    }

    private void UpdateTexts(int score, int highScore, int lives)
    {
        if (scoreText != null) scoreText.text = $"PONTOS: {score}";
        if (highScoreText != null) highScoreText.text = $"RECORDE: {highScore}";
        if (killedText != null) killedText.text = $"ELIMINADOS: {killedCount}";
        if (livesText != null) livesText.text = $"VIDAS: {lives}";
    }

    private void UpdateHearts(int lives)
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] != null) hearts[i].enabled = i < lives;
        }
    }

    private void HandleGameStarted()
    {
        gameOverActive = false;
        killedCount = 0;
        lastScore = 0;
        if (startMenuPanel != null) startMenuPanel.SetActive(false);
        if (playingPanel != null) playingPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        UpdateTexts(0, GameManager.Instance != null ? GameManager.Instance.HighScore : 0,
            GameManager.Instance != null ? GameManager.Instance.currentLives : 3);
    }

    private void HandleGameOver()
    {
        gameOverActive = true;
        gameOverTime = Time.unscaledTime;
        if (startMenuPanel != null) startMenuPanel.SetActive(false);
        if (playingPanel != null) playingPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (gameOverScoreText != null && GameManager.Instance != null)
        {
            gameOverScoreText.text = $"PONTUAÇÃO FINAL: {GameManager.Instance.Score}\nMELHOR: {GameManager.Instance.HighScore}";
        }
        PositionPanel(true, true);
    }

    public void StartGame()
    {
        if (GameManager.Instance != null) GameManager.Instance.StartGame();
    }

    public void RestartGame()
    {
        if (restarting) return;
        restarting = true;
        if (GameManager.Instance != null) GameManager.Instance.Restart();
        else UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    private Sprite CreateHeartSprite()
    {
        const int size = 48;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Color clear = new Color(1f, 1f, 1f, 0f);
        Color red = Color.white;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = (x - size * 0.5f) / (size * 0.43f);
                float py = (y - size * 0.55f) / (size * 0.43f);
                float heart = Mathf.Pow(px * px + py * py - 1f, 3f) - px * px * py * py * py;
                texture.SetPixel(x, y, heart <= 0f ? red : clear);
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
