using System.Collections;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ZombieVRPlayModeTests
{
    private const string EvidenceDir = "Docs/evidencias";

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        if (!Directory.Exists(EvidenceDir))
        {
            Directory.CreateDirectory(EvidenceDir);
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Zombie");
        if (asyncLoad != null)
        {
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }
        yield return new WaitForSeconds(0.5f);
    }

    [UnityTest]
    public IEnumerator T1_ScoreboardUI_WorldSpace_Size_And_Updates()
    {
        GameObject scoreboardGO = GameObject.Find("ScoreboardCanvas");
        Assert.IsNotNull(scoreboardGO, "ScoreboardCanvas deve existir na cena.");

        Canvas canvas = scoreboardGO.GetComponent<Canvas>();
        Assert.AreEqual(RenderMode.WorldSpace, canvas.renderMode, "Canvas deve estar em WorldSpace.");

        RectTransform rt = scoreboardGO.GetComponent<RectTransform>();
        float physicalWidth = rt.rect.width * scoreboardGO.transform.lossyScale.x;
        float physicalHeight = rt.rect.height * scoreboardGO.transform.lossyScale.y;

        Assert.GreaterOrEqual(physicalWidth, 0.5f, "Largura física do painel deve ser >= 0.5m.");
        Assert.LessOrEqual(physicalWidth, 4.0f, "Largura física do painel deve ser <= 4.0m.");

        ScoreboardUI scoreboardUI = scoreboardGO.GetComponent<ScoreboardUI>();
        Assert.IsNotNull(scoreboardUI, "Componente ScoreboardUI deve existir no ScoreboardCanvas.");

        int initialScore = GameManager.Instance.Score;
        GameManager.Instance.AddScore(25);
        yield return null;

        Assert.AreEqual(initialScore + 25, GameManager.Instance.Score);
        Assert.IsTrue(scoreboardUI.scoreText.text.Contains((initialScore + 25).ToString()), "Texto do placar deve refletir nova pontuação.");

        if (Camera.main != null)
        {
            Camera.main.transform.LookAt(scoreboardGO.transform.position);
        }
        yield return CaptureInGameScreenshot("screenshot_placar.png");
    }

    [UnityTest]
    public IEnumerator T2_Enemies_Chase_Player_And_Destination_Valid()
    {
        // Inicia o jogo para que o EnemySpawner spawne os inimigos
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            GameManager.Instance.StartGame();
        }
        yield return new WaitForSeconds(1.5f);

        ZombieController[] zombies = Object.FindObjectsByType<ZombieController>();
        Assert.Greater(zombies.Length, 0, "Deve haver inimigos ativos na cena.");

        GameObject player = GameObject.FindWithTag("Player");
        Assert.IsNotNull(player, "Player deve existir na cena.");

        foreach (ZombieController z in zombies)
        {
            NavMeshAgent agent = z.GetComponent<NavMeshAgent>();
            Assert.IsNotNull(agent, "Inimigo deve ter NavMeshAgent.");
            Assert.IsTrue(agent.isOnNavMesh, "NavMeshAgent deve estar sobre a NavMesh.");
        }

        yield return new WaitForSeconds(2.0f);

        foreach (ZombieController z in zombies)
        {
            if (z != null)
            {
                NavMeshAgent agent = z.GetComponent<NavMeshAgent>();
                if (agent != null && agent.isActiveAndEnabled && !agent.isStopped)
                {
                    Assert.IsTrue(agent.hasPath || agent.pathPending, "Agente deve estar calculando ou seguindo caminho.");
                }
            }
        }
    }

    [UnityTest]
    public IEnumerator T3_Bullet_Kills_Enemy_And_Player_Contact_Does_Not_Kill()
    {
        // Inicia o jogo para que os inimigos sejam spawnados
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            GameManager.Instance.StartGame();
        }
        yield return new WaitForSeconds(1.5f);

        ZombieController[] zombies = Object.FindObjectsByType<ZombieController>();
        Assert.Greater(zombies.Length, 0, "Deve haver zumbis na cena.");
        ZombieController targetZombie = zombies[0];

        GameObject player = GameObject.FindWithTag("Player");
        int initialScore = GameManager.Instance.Score;

        // Prova que encostar o PLAYER no zumbi NAO mata o zumbi
        targetZombie.OnTriggerEnter(player.GetComponent<Collider>());
        yield return null;
        Assert.AreEqual(initialScore, GameManager.Instance.Score, "Player encostando no zumbi não deve matar o zumbi nem somar pontos.");

        // Prova que o projetil Bullet mata o zumbi e pontua
        GameObject bulletObj = new GameObject("TestBullet");
        bulletObj.AddComponent<SphereCollider>();
        bulletObj.AddComponent<BulletProjectile>();
        bulletObj.transform.position = targetZombie.transform.position;

        targetZombie.OnTriggerEnter(bulletObj.GetComponent<Collider>());
        yield return new WaitForSeconds(0.3f);

        Assert.Greater(GameManager.Instance.Score, initialScore, "Projétil deve matar o inimigo e aumentar a pontuação.");
        Object.Destroy(bulletObj);
    }

    [UnityTest]
    public IEnumerator T4_Player_Stays_Stationary_And_Headshot_Kills_Enemy()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            GameManager.Instance.StartGame();
        }
        yield return new WaitForSeconds(0.5f);

        GameObject player = GameObject.FindWithTag("Player");
        Assert.IsNotNull(player, "Player deve existir.");
        Vector3 initialPos = player.transform.position;

        PlayerWaypointMover mover = Object.FindAnyObjectByType<PlayerWaypointMover>();
        Assert.IsNotNull(mover, "PlayerWaypointMover deve existir.");

        WaypointController[] waypoints = Object.FindObjectsByType<WaypointController>();
        if (waypoints.Length > 0)
        {
            mover.MoveToWaypoint(waypoints[0]);
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(mover.IsMoving, "Jogador não deve entrar em movimento por waypoint.");
        }

        float moved = Vector3.Distance(new Vector3(player.transform.position.x, 0, player.transform.position.z),
                                       new Vector3(initialPos.x, 0, initialPos.z));
        Assert.Less(moved, 0.05f, "Jogador deve permanecer parado na posição inicial.");

        GameObject enemyBPrefab = Resources.Load<GameObject>("EnemyB");
        GameObject enemyPrefab = enemyBPrefab != null ? enemyBPrefab : Resources.Load<GameObject>("Zombie");
        Assert.IsNotNull(enemyPrefab, "Deve existir prefab de inimigo em Resources.");

        Vector3 spawnPos = initialPos + (Camera.main != null ? Camera.main.transform.forward : Vector3.forward) * 6f;
        spawnPos.y = initialPos.y;
        GameObject enemyGO = Object.Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        yield return null;

        ZombieController enemy = enemyGO.GetComponent<ZombieController>();
        Assert.IsNotNull(enemy, "Inimigo deve possuir ZombieController.");

        int scoreBefore = GameManager.Instance.Score;

        GameObject weaponGO = new GameObject("TestHeadshotWeapon");
        weaponGO.transform.position = (Camera.main != null ? Camera.main.transform.position : initialPos + Vector3.up * 1.6f);
        Bounds b = GetApproxEnemyBounds(enemy);
        Vector3 headPoint = new Vector3(b.center.x, b.max.y - 0.05f, b.center.z);
        weaponGO.transform.LookAt(headPoint);

        WeaponController weapon = weaponGO.AddComponent<WeaponController>();
        weapon.spawnPoint = weaponGO;
        weapon.maxShootDistance = 30f;
        weapon.fireRate = 0.2f;

        float timeout = Time.time + 3.0f;
        while (Time.time < timeout && enemy != null && !enemy.IsDead)
        {
            yield return null;
        }

        Assert.IsTrue(enemy == null || enemy.IsDead, "Mirar na cabeça do inimigo deve atirar e matá-lo.");
        Assert.Greater(GameManager.Instance.Score, scoreBefore, "Matar inimigo deve aumentar a pontuação.");

        if (enemyGO != null) Object.Destroy(enemyGO);
        Object.Destroy(weaponGO);
    }

    private Bounds GetApproxEnemyBounds(ZombieController enemy)
    {
        Collider[] colliders = enemy.GetComponentsInChildren<Collider>();
        bool has = false;
        Bounds bounds = new Bounds(enemy.transform.position, Vector3.zero);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null) continue;
            if (!has) { bounds = colliders[i].bounds; has = true; }
            else bounds.Encapsulate(colliders[i].bounds);
        }
        if (!has)
        {
            Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                if (!has) { bounds = renderers[i].bounds; has = true; }
                else bounds.Encapsulate(renderers[i].bounds);
            }
        }
        if (!has) bounds = new Bounds(enemy.transform.position + Vector3.up, Vector3.one);
        return bounds;
    }

    [UnityTest]
    public IEnumerator T5_New_Enemy_Spawned_With_3DAudio_And_Locomotion()
    {
        GameObject enemyBPrefab = Resources.Load<GameObject>("EnemyB");
        Assert.IsNotNull(enemyBPrefab, "EnemyB.prefab deve existir em Resources.");

        GameObject player = GameObject.FindWithTag("Player");
        Vector3 spawnPos = player != null ? player.transform.position + player.transform.forward * 2.8f : new Vector3(0, 1.7f, 0);
        spawnPos.y = 1.7f;
        Quaternion spawnRot = player != null ? Quaternion.LookRotation(-player.transform.forward) : Quaternion.identity;
        GameObject enemyB = Object.Instantiate(enemyBPrefab, spawnPos, spawnRot);
        yield return null;

        if (Camera.main != null)
        {
            Camera.main.transform.LookAt(enemyB.transform.position + Vector3.up * 0.8f);
        }

        AudioSource audio = enemyB.GetComponent<AudioSource>();
        Assert.IsNotNull(audio, "EnemyB deve possuir AudioSource.");
        Assert.AreEqual(1.0f, audio.spatialBlend, "AudioSource do EnemyB deve ser 3D (spatialBlend == 1.0).");

        ZombieController zc = enemyB.GetComponent<ZombieController>();
        Assert.IsNotNull(zc, "EnemyB deve possuir ZombieController.");
        Assert.AreEqual(25, zc.points, "EnemyB deve valer 25 pontos.");

        NavMeshAgent agent = enemyB.GetComponent<NavMeshAgent>();
        Assert.IsNotNull(agent, "EnemyB deve possuir NavMeshAgent.");

        yield return CaptureInGameScreenshot("screenshot_novo_inimigo.png");

        Object.Destroy(enemyB);
    }

    [UnityTest]
    public IEnumerator T6_GameOver_On_Contact_And_Restart()
    {
        yield return new WaitForSeconds(0.5f);

        // Garante que o jogo esta em estado Playing para que a deteccao de contato funcione
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            GameManager.Instance.StartGame();
            yield return new WaitForSeconds(0.1f);
        }

        GameObject player = GameObject.FindWithTag("Player");
        GameObject enemyBPrefab = Resources.Load<GameObject>("EnemyB");
        GameObject enemy = Object.Instantiate(enemyBPrefab, player.transform.position, Quaternion.identity);

        yield return new WaitForSeconds(0.8f);

        Assert.Less(GameManager.Instance.currentLives, 3, "O contato do inimigo deve tirar uma vida.");

        while (GameManager.Instance.currentLives > 0)
        {
            GameManager.Instance.TakeDamage(1);
        }

        Assert.IsTrue(GameManager.Instance.IsGameOver, "Ao acabar as vidas, o jogo deve terminar.");

        ScoreboardUI scoreboardUI = Object.FindAnyObjectByType<ScoreboardUI>();
        Assert.IsNotNull(scoreboardUI, "ScoreboardUI deve existir.");
        Assert.IsTrue(scoreboardUI.gameOverPanel.activeSelf, "Painel de Game Over deve estar ativo.");

        if (Camera.main != null && scoreboardUI != null)
        {
            Camera.main.transform.LookAt(scoreboardUI.transform.position);
        }

        yield return CaptureInGameScreenshot("screenshot_fim_de_jogo.png");

        Object.Destroy(enemy);
    }

    [UnityTest]
    public IEnumerator T7_Audio_Plays_In_Game_And_Stops_On_GameOver()
    {
        // Inicia o jogo para que a musica toque
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            GameManager.Instance.StartGame();
        }
        yield return new WaitForSeconds(0.5f);

        AudioManager am = AudioManager.Instance;
        Assert.IsNotNull(am, "AudioManager deve existir.");
        Assert.IsNotNull(am.musicSource, "musicSource deve existir.");
        Assert.IsTrue(am.musicSource.isPlaying, "Música de fundo deve estar tocando durante a partida.");

        GameManager.Instance.TriggerGameOver();
        yield return null;

        Assert.IsFalse(am.musicSource.isPlaying, "Música de fundo deve parar ao ocorrer GameOver.");
    }

    [UnityTest]
    public IEnumerator T8_Sweep_60Seconds_ZeroErrors()
    {
        float timer = 0f;
        while (timer < 60f)
        {
            yield return new WaitForSeconds(1.0f);
            timer += 1.0f;
        }

        Assert.IsNotNull(GameManager.Instance, "GameManager deve permanecer integro apos 60 segundos de execucao continua.");
        Debug.Log("[ZombieVRPlayModeTests] Varredura de 60 segundos concluida sem nenhuma excecao ou erro no console!");
    }

    private IEnumerator CaptureInGameScreenshot(string filename)
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        int width = 1920;
        int height = 1080;
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        rt.Create();

        RenderTexture prevTarget = cam.targetTexture;
        cam.targetTexture = rt;

        yield return null;

        RenderTexture prevActive = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        cam.targetTexture = prevTarget;
        RenderTexture.active = prevActive;
        rt.Release();
        Object.Destroy(rt);

        byte[] pngData = tex.EncodeToPNG();
        Object.Destroy(tex);

        string[] dirs = new string[] {
            EvidenceDir,
            Path.Combine(Application.dataPath, "..", "Docs", "evidencias"),
            Path.Combine(Application.dataPath, "..", "..", "Docs", "evidencias")
        };

        foreach (string d in dirs)
        {
            if (!Directory.Exists(d)) Directory.CreateDirectory(d);
            string fullPath = Path.Combine(d, filename);
            File.WriteAllBytes(fullPath, pngData);
            Debug.Log($"[Evidence Capture] {filename} salvo em {fullPath} ({pngData.Length} bytes)");
        }
    }

    [UnityTest]
    public IEnumerator T9_Player_Cannot_Pass_Through_Wall()
    {
        GameObject player = GameObject.FindWithTag("Player");
        Assert.IsNotNull(player, "Player deve existir.");

        player.transform.position = new Vector3(3.2f, 0f, 0f);
        Physics.SyncTransforms();
        for (int i = 0; i < 120; i++)
        {
            PlayerCollision.Move(player.transform, Vector3.right * 0.05f);
        }
        yield return null;

        Assert.Less(player.transform.position.x, 4.75f, "O jogador não pode atravessar a parede em x=5.");
        Assert.Greater(player.transform.position.x, 3.5f, "O jogador deve avançar até encostar na parede.");
    }

    [UnityTest]
    public IEnumerator T10_Player_Cannot_Pass_Through_Crate()
    {
        GameObject player = GameObject.FindWithTag("Player");
        GameObject crate = GameObject.Find("Caixa");
        Assert.IsNotNull(player, "Player deve existir.");
        Assert.IsNotNull(crate, "Caixa deve existir.");

        Vector3 crateCenter = crate.transform.position;
        player.transform.position = new Vector3(crateCenter.x, 0f, crateCenter.z - 2f);
        Physics.SyncTransforms();
        for (int i = 0; i < 120; i++)
        {
            PlayerCollision.Move(player.transform, Vector3.forward * 0.05f);
        }
        yield return null;

        Assert.Less(player.transform.position.z, crateCenter.z - 0.3f, "O jogador não pode atravessar a caixa.");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        yield return new WaitForSeconds(0.2f);
        string rootDir = Path.Combine(Application.dataPath, "..", "..", "Docs", "evidencias");
        if (Directory.Exists(rootDir) && Directory.Exists(EvidenceDir))
        {
            string[] pngFiles = Directory.GetFiles(EvidenceDir, "*.png");
            foreach (string file in pngFiles)
            {
                try
                {
                    string dest = Path.Combine(rootDir, Path.GetFileName(file));
                    File.Copy(file, dest, true);
                }
                catch { }
            }
        }
    }
}

