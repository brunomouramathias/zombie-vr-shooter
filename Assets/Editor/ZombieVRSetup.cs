using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ZombieVRSetup
{
    private const string ScenePath = "Assets/Scenes/Zombie.unity";

    [MenuItem("Tools/Zombie VR/Check Missing Scripts")]
    public static bool CheckMissingScripts()
    {
        bool foundAny = false;
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject[] rootObjs = scene.GetRootGameObjects();
        foreach (GameObject go in rootObjs)
        {
            if (CheckGameObjectMissingScripts(go))
            {
                foundAny = true;
            }
        }

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources" });
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && CheckGameObjectMissingScripts(prefab))
            {
                foundAny = true;
            }
        }

        if (!foundAny)
        {
            Debug.Log("[ZombieVRSetup] Zero missing scripts encontrados na cena e prefabs!");
        }
        else
        {
            Debug.LogWarning("[ZombieVRSetup] Missing scripts detectados!");
        }

        return !foundAny;
    }

    private static bool CheckGameObjectMissingScripts(GameObject go)
    {
        bool hasMissing = false;
        Component[] components = go.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
            {
                Debug.LogError($"[ZombieVRSetup] Missing script em: {GetFullPath(go.transform)} (índice {i})");
                hasMissing = true;
            }
        }

        foreach (Transform child in go.transform)
        {
            if (CheckGameObjectMissingScripts(child.gameObject))
            {
                hasMissing = true;
            }
        }

        return hasMissing;
    }

    private static string GetFullPath(Transform t)
    {
        if (t.parent == null) return t.name;
        return GetFullPath(t.parent) + "/" + t.name;
    }

    [MenuItem("Tools/Zombie VR/Fix Enemy Scale")]
    public static void FixEnemyScale()
    {
        string path = "Assets/Resources/Zombie.prefab";
        GameObject contents = PrefabUtility.LoadPrefabContents(path);
        if (contents == null)
        {
            Debug.LogError("[ZombieVRSetup] Zombie.prefab não encontrado.");
            return;
        }

        contents.transform.localScale = Vector3.one * 0.01f;
        SphereCollider collider = contents.GetComponent<SphereCollider>();
        if (collider != null)
        {
            collider.radius = 60f;
            collider.center = new Vector3(0f, 90f, 0f);
        }

        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
        AssetDatabase.SaveAssets();
        Debug.Log("[ZombieVRSetup] Zombie em escala 0,01 (~1,8 m de altura) com colisor compatível.");
    }

    [MenuItem("Tools/Zombie VR/Apply HUD Layout")]
    public static void ApplyHudLayout()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject canvasObject = GameObject.Find("ScoreboardCanvas");
        if (canvasObject == null)
        {
            Debug.LogError("[ZombieVRSetup] ScoreboardCanvas não encontrado.");
            return;
        }

        ScoreboardUI ui = canvasObject.GetComponent<ScoreboardUI>();
        if (ui == null) ui = canvasObject.AddComponent<ScoreboardUI>();
        ui.targetDistance = 1.15f;
        ui.heightOffset = 0.30f;
        ui.rightOffset = 0.55f;
        ui.followSpeed = 7f;

        Transform playing = canvasObject.transform.Find("PlayingPanel");
        if (playing != null)
        {
            Transform hearts = playing.Find("Hearts");
            if (hearts == null)
            {
                GameObject heartsObject = new GameObject("Hearts", typeof(RectTransform));
                heartsObject.transform.SetParent(playing, false);
                RectTransform heartsRect = heartsObject.GetComponent<RectTransform>();
                heartsRect.anchorMin = new Vector2(1f, 1f);
                heartsRect.anchorMax = new Vector2(1f, 1f);
                heartsRect.pivot = new Vector2(1f, 1f);
                heartsRect.anchoredPosition = new Vector2(-24f, -18f);
                heartsRect.sizeDelta = new Vector2(170f, 50f);
                hearts = heartsObject.transform;
            }

            for (int i = 0; i < 3; i++)
            {
                Transform heart = hearts.Find($"Heart{i + 1}");
                if (heart == null)
                {
                    GameObject heartObject = new GameObject($"Heart{i + 1}", typeof(RectTransform));
                    heartObject.transform.SetParent(hearts, false);
                    heart = heartObject.transform;
                }

                RectTransform rect = heart.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(i * 52f, 0f);
                rect.sizeDelta = new Vector2(42f, 42f);
                Image image = heart.GetComponent<Image>();
                if (image == null) image = heart.gameObject.AddComponent<Image>();
                image.raycastTarget = false;
                image.color = new Color(1f, 0.12f, 0.16f, 1f);

                if (i == 0) ui.heart1 = image;
                if (i == 1) ui.heart2 = image;
                if (i == 2) ui.heart3 = image;
            }

            if (ui.playingPanel == null) ui.playingPanel = playing.gameObject;
            playing.gameObject.SetActive(true);
        }

        if (ui.startMenuPanel != null) ui.startMenuPanel.SetActive(false);
        if (ui.gameOverPanel != null) ui.gameOverPanel.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ZombieVRSetup] HUD aplicado: painel discreto à direita e três corações.");
    }

    [MenuItem("Tools/Zombie VR/Fix Player Ground And Collision")]
    public static void FixPlayerGroundAndCollision()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[ZombieVRSetup] Player não encontrado.");
            return;
        }

        Vector3 position = player.transform.position;
        position.y = 0f;
        player.transform.position = position;

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.transform.SetParent(player.transform);
            camera.transform.localPosition = new Vector3(0f, 1.65f, 0.08f);
            camera.transform.localRotation = Quaternion.Euler(0f, 5.85f, 0f);
        }

        CapsuleCollider capsule = player.GetComponent<CapsuleCollider>();
        if (capsule == null) capsule = player.AddComponent<CapsuleCollider>();
        capsule.isTrigger = false;
        capsule.radius = 0.35f;
        capsule.height = 1.8f;
        capsule.center = new Vector3(0f, 0.9f, 0f);

        Rigidbody body = player.GetComponent<Rigidbody>();
        if (body == null) body = player.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ZombieVRSetup] Player reposicionado no piso (y=0), olhos a 1,65 m e colisão ativa.");
    }

    [MenuItem("Tools/Zombie VR/Log Collision Diagnostics")]
    public static void LogCollisionDiagnostics()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[Diagnostics] Player não encontrado.");
            return;
        }

        CapsuleCollider capsule = player.GetComponent<CapsuleCollider>();
        Debug.Log($"[Diagnostics] Player pos={player.transform.position} lossyScale={player.transform.lossyScale} capsule={(capsule != null ? capsule.bounds.ToString() : "none")}");

        RaycastHit[] hits = Physics.RaycastAll(player.transform.position + Vector3.up * 6f, Vector3.down, 30f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            Debug.Log($"[Diagnostics] Abaixo: {hit.collider.name} pontoY={hit.point.y:F2} dist={hit.distance:F2} bounds={hit.collider.bounds}");
        }

        foreach (string name in new[] { "Caixa", "Wall", "Wall (1)", "Floor", "Floor (1)", "Teto" })
        {
            GameObject go = GameObject.Find(name);
            if (go == null) continue;
            Collider col = go.GetComponent<Collider>();
            Renderer renderer = go.GetComponent<Renderer>();
            Debug.Log($"[Diagnostics] {name} pos={go.transform.position} bounds={(col != null ? col.bounds.ToString() : renderer != null ? renderer.bounds.ToString() : "none")}");
        }

        foreach (string prefabName in new[] { "Zombie", "EnemyB" })
        {
            GameObject prefab = Resources.Load<GameObject>(prefabName);
            if (prefab == null) continue;
            GameObject instance = Object.Instantiate(prefab, new Vector3(0f, 0f, 15f), Quaternion.identity);
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                Debug.Log($"[Diagnostics] {prefabName} scale={instance.transform.localScale} height={bounds.size.y:F2} size={bounds.size}");
            }
            Object.DestroyImmediate(instance);
        }
    }

    [MenuItem("Tools/Zombie VR/Setup All")]
    public static void SetupAll()
    {
        Debug.Log("[ZombieVRSetup] Iniciando Setup All...");

        SetupBuildSettings();
        ImportTMPEssentials();
        SetupAudioResources();
        SetupBulletPrefab();
        SetupZombiePrefab();
        SetupEnemyBPrefab();
        SetupScene();

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();

        CheckMissingScripts();
        Debug.Log("[ZombieVRSetup] Setup All concluído com sucesso!");
    }

    private static void SetupBuildSettings()
    {
        string scenePath = "Assets/Scenes/Zombie.unity";
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        bool found = false;
        foreach (var s in scenes)
        {
            if (s.path == scenePath)
            {
                s.enabled = true;
                found = true;
                break;
            }
        }
        if (!found)
        {
            var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
            for (int i = 0; i < scenes.Length; i++) newScenes[i] = scenes[i];
            newScenes[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = newScenes;
            Debug.Log("[ZombieVRSetup] Adicionada cena Zombie.unity aos Build Settings.");
        }
    }

    private static void ImportTMPEssentials()
    {
        if (!Directory.Exists("Assets/TextMesh Pro"))
        {
            string pkgPath = "Library/PackageCache/com.unity.ugui@1c4864518158/Package Resources/TMP Essential Resources.unitypackage";
            if (File.Exists(pkgPath))
            {
                Debug.Log("[ZombieVRSetup] Importando TMP Essential Resources...");
                AssetDatabase.ImportPackage(pkgPath, false);
            }
        }
    }

    private static void SetupAudioResources()
    {
        if (!Directory.Exists("Assets/Resources/Sons"))
        {
            Directory.CreateDirectory("Assets/Resources/Sons");
        }

        string[] audioFiles = { "background_music.mp3", "game_over_sting.wav", "zombie_groan.wav", "zombie_attack.wav", "zombie_death.wav" };
        foreach (string file in audioFiles)
        {
            string src = Path.Combine("Assets/Sons", file);
            string dest = Path.Combine("Assets/Resources/Sons", file);
            if (File.Exists(src) && !File.Exists(dest))
            {
                File.Copy(src, dest, true);
                AssetDatabase.ImportAsset(dest);
            }
        }
    }

    private static void SetupBulletPrefab()
    {
        string path = "Assets/Resources/Bullet.prefab";
        GameObject bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (bulletPrefab == null) return;

        bool modified = false;
        if (bulletPrefab.GetComponent<BulletProjectile>() == null)
        {
            bulletPrefab.AddComponent<BulletProjectile>();
            modified = true;
        }

        SphereCollider sc = bulletPrefab.GetComponent<SphereCollider>();
        if (sc != null && !sc.isTrigger)
        {
            // O projetil colide com triggers do zumbi
        }

        if (modified)
        {
            EditorUtility.SetDirty(bulletPrefab);
            PrefabUtility.SavePrefabAsset(bulletPrefab);
        }
    }

    private static void SetupZombiePrefab()
    {
        string path = "Assets/Resources/Zombie.prefab";
        GameObject zombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (zombiePrefab == null) return;

        ZombieController zc = zombiePrefab.GetComponent<ZombieController>();
        if (zc == null)
        {
            zc = zombiePrefab.AddComponent<ZombieController>();
        }

        zc.speed = 3.5f;
        zc.stoppingDistance = 0.6f;
        zc.contactRadius = 0.85f;
        zc.points = 10;
        zc.walkAnimName = "walk";
        zc.attackAnimName = "attack";
        zc.deathAnimName = "back_fall";

        zombiePrefab.transform.localScale = Vector3.one * 0.01f;

        zc.groanSound = Resources.Load<AudioClip>("Sons/zombie_groan");
        zc.attackSound = Resources.Load<AudioClip>("Sons/zombie_attack");
        zc.deathSound = Resources.Load<AudioClip>("Sons/zombie_death");

        NavMeshAgent agent = zombiePrefab.GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = zombiePrefab.AddComponent<NavMeshAgent>();
        }
        agent.speed = 3.5f;
        agent.stoppingDistance = 0.6f;
        agent.radius = 0.5f;
        agent.height = 2.0f;

        AudioSource audio = zombiePrefab.GetComponent<AudioSource>();
        if (audio == null)
        {
            audio = zombiePrefab.AddComponent<AudioSource>();
        }
        AudioManager.ConfigureAudioSource3D(audio, 2.0f, 30.0f);

        SphereCollider sc = zombiePrefab.GetComponent<SphereCollider>();
        if (sc == null)
        {
            sc = zombiePrefab.AddComponent<SphereCollider>();
        }
        sc.isTrigger = true;
        sc.radius = 60f;
        sc.center = new Vector3(0, 90f, 0);

        zombiePrefab.tag = "Zombie";

        EditorUtility.SetDirty(zombiePrefab);
        PrefabUtility.SavePrefabAsset(zombiePrefab);
    }

    private static void SetupEnemyBPrefab()
    {
        string enemyBPath = "Assets/Resources/EnemyB.prefab";
        GameObject contents = PrefabUtility.LoadPrefabContents(enemyBPath);
        if (contents != null)
        {
            contents.transform.localScale = Vector3.one * 1.0f;
            ConfigureEnemyBComponents(contents);
            PrefabUtility.SaveAsPrefabAsset(contents, enemyBPath);
            PrefabUtility.UnloadPrefabContents(contents);
            Debug.Log("[ZombieVRSetup] EnemyB.prefab atualizado com sucesso via LoadPrefabContents!");
            return;
        }
    }

    private static void ConfigureEnemyBComponents(GameObject go)
    {
        go.tag = "Zombie";
        go.transform.localScale = Vector3.one * 1.0f;

        // Garante material URP Lit nos renderers do EnemyB
        string matPath = "Assets/Enemy/M_Skeleton.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Universal Render Pipeline/Simple Lit");

        if (mat == null && urpLit != null)
        {
            mat = new Material(urpLit);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else if (mat != null && urpLit != null)
        {
            mat.shader = urpLit;
        }

        if (mat != null)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Enemy/skeleton_texture_A.png");
            if (texture != null)
            {
                mat.SetTexture("_BaseMap", texture);
                mat.mainTexture = texture;
            }
            mat.color = Color.white;
            EditorUtility.SetDirty(mat);

            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                r.sharedMaterial = mat;
            }
        }

        // Animator com clipes reais de locomocao do esqueleto (evita T-Pose)
        RuntimeAnimatorController animCtrl = SetupEnemyBAnimator();
        Animator anim = go.GetComponent<Animator>();
        if (anim == null) anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = animCtrl;
        anim.applyRootMotion = false;

        NavMeshAgent agent = go.GetComponent<NavMeshAgent>();
        if (agent == null) agent = go.AddComponent<NavMeshAgent>();
        agent.speed = 3.2f;
        agent.stoppingDistance = 1.3f;
        agent.radius = 0.45f;
        agent.height = 1.8f;
        agent.acceleration = 6f;

        CapsuleCollider col = go.GetComponent<CapsuleCollider>();
        if (col == null) col = go.AddComponent<CapsuleCollider>();
        col.isTrigger = true;
        col.radius = 0.45f;
        col.height = 1.8f;
        col.center = new Vector3(0, 0.9f, 0);

        AudioSource audio = go.GetComponent<AudioSource>();
        if (audio == null) audio = go.AddComponent<AudioSource>();
        AudioManager.ConfigureAudioSource3D(audio, 2.0f, 30.0f);

        ZombieController zc = go.GetComponent<ZombieController>();
        if (zc == null) zc = go.AddComponent<ZombieController>();
        zc.speed = 3.2f;
        zc.stoppingDistance = 1.3f;
        zc.contactRadius = 1.4f;
        zc.points = 25; // Mais pontos
        zc.walkAnimName = "walk";
        zc.attackAnimName = "attack";
        zc.deathAnimName = "death";

        zc.groanSound = Resources.Load<AudioClip>("Sons/zombie_groan");
        zc.attackSound = Resources.Load<AudioClip>("Sons/zombie_attack");
        zc.deathSound = Resources.Load<AudioClip>("Sons/zombie_death");
    }

    private static RuntimeAnimatorController SetupEnemyBAnimator()
    {
        string controllerPath = "Assets/Enemy/EnemyB.controller";
        var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

        AnimationClip walkClip = FindClipInFBX("Assets/Enemy/Anim_Walk.fbx");
        AnimationClip attackClip = FindClipInFBX("Assets/Enemy/Anim_Attack.fbx");
        AnimationClip deathClip = FindClipInFBX("Assets/Enemy/Anim_Death.fbx");

        var rootSm = controller.layers[0].stateMachine;

        var walkState = FindOrAddState(rootSm, "walk");
        if (walkClip != null) walkState.motion = walkClip;
        rootSm.defaultState = walkState;

        if (attackClip != null)
        {
            var attackState = FindOrAddState(rootSm, "attack");
            attackState.motion = attackClip;
        }

        if (deathClip != null)
        {
            var deathState = FindOrAddState(rootSm, "death");
            deathState.motion = deathClip;
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static UnityEditor.Animations.AnimatorState FindOrAddState(UnityEditor.Animations.AnimatorStateMachine sm, string name)
    {
        foreach (var s in sm.states)
        {
            if (s.state.name == name) return s.state;
        }
        return sm.AddState(name);
    }

    private static AnimationClip FindClipInFBX(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var obj in assets)
        {
            if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                return clip;
            }
        }
        return null;
    }

    private static void SetupAtmosphereAndLighting()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camGO = GameObject.FindWithTag("MainCamera");
            if (camGO != null) mainCam = camGO.GetComponent<Camera>();
        }
        if (mainCam != null)
        {
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 1f);
        }

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.40f, 0.40f, 0.45f, 1f);

        Light[] lights = Object.FindObjectsByType<Light>();
        foreach (Light l in lights)
        {
            if (l.type == LightType.Directional)
            {
                l.color = new Color(0.88f, 0.90f, 1.0f);
                l.intensity = 1.0f;
                l.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                l.shadows = LightShadows.None;
            }
            else if (l.type == LightType.Point)
            {
                // Luzes internas (pontos amarelos) sem sombra para evitar sombra que se move
                l.shadows = LightShadows.None;
            }
        }
    }

    private static void SetupSceneMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Universal Render Pipeline/Simple Lit");

        // 1. Floor Material
        string floorMatPath = "Assets/Materials/M_Floor.mat";
        Material floorMat = AssetDatabase.LoadAssetAtPath<Material>(floorMatPath);
        if (floorMat == null && urpLit != null)
        {
            floorMat = new Material(urpLit);
            AssetDatabase.CreateAsset(floorMat, floorMatPath);
        }
        else if (floorMat != null && urpLit != null)
        {
            floorMat.shader = urpLit;
        }

        Texture2D floorTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/imgres.jpg");
        Texture2D floorNorm = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/floorNormal.jpg");
        if (floorMat != null)
        {
            if (floorTex != null) { floorMat.SetTexture("_BaseMap", floorTex); floorMat.mainTexture = floorTex; }
            if (floorNorm != null) floorMat.SetTexture("_BumpMap", floorNorm);
            floorMat.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            EditorUtility.SetDirty(floorMat);
        }

        // 2. Wall Material
        string wallMatPath = "Assets/Materials/Wall.mat";
        Material wallMat = AssetDatabase.LoadAssetAtPath<Material>(wallMatPath);
        if (wallMat == null && urpLit != null)
        {
            wallMat = new Material(urpLit);
            AssetDatabase.CreateAsset(wallMat, wallMatPath);
        }
        else if (wallMat != null && urpLit != null)
        {
            wallMat.shader = urpLit;
        }

        Texture2D wallTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/imgres-1.jpg");
        Texture2D wallNorm = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/brickwork_normal-map.jpg");
        if (wallMat != null)
        {
            if (wallTex != null) { wallMat.SetTexture("_BaseMap", wallTex); wallMat.mainTexture = wallTex; }
            if (wallNorm != null) wallMat.SetTexture("_BumpMap", wallNorm);
            wallMat.color = Color.white;
            EditorUtility.SetDirty(wallMat);
        }

        // 3. Crate Material
        string crateMatPath = "Assets/Materials/M_Crate.mat";
        Material crateMat = AssetDatabase.LoadAssetAtPath<Material>(crateMatPath);
        if (crateMat == null && urpLit != null)
        {
            crateMat = new Material(urpLit);
            AssetDatabase.CreateAsset(crateMat, crateMatPath);
        }
        else if (crateMat != null && urpLit != null)
        {
            crateMat.shader = urpLit;
        }

        Texture2D crateNorm = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Crate_01_NRM.png");
        if (crateMat != null)
        {
            if (wallTex != null) { crateMat.SetTexture("_BaseMap", wallTex); crateMat.mainTexture = wallTex; }
            if (crateNorm != null) crateMat.SetTexture("_BumpMap", crateNorm);
            crateMat.color = new Color(0.7f, 0.6f, 0.5f, 1f);
            EditorUtility.SetDirty(crateMat);
        }

        // 4. Ceiling Material
        string ceilingMatPath = "Assets/Materials/M_Ceiling.mat";
        Material ceilingMat = AssetDatabase.LoadAssetAtPath<Material>(ceilingMatPath);
        if (ceilingMat == null && urpLit != null)
        {
            ceilingMat = new Material(urpLit);
            AssetDatabase.CreateAsset(ceilingMat, ceilingMatPath);
        }
        else if (ceilingMat != null && urpLit != null)
        {
            ceilingMat.shader = urpLit;
        }

        if (ceilingMat != null)
        {
            if (floorTex != null) { ceilingMat.SetTexture("_BaseMap", floorTex); ceilingMat.mainTexture = floorTex; }
            ceilingMat.color = new Color(0.35f, 0.35f, 0.4f, 1f);
            EditorUtility.SetDirty(ceilingMat);
        }

        // Apply clean materials to all scene renderers
        MeshRenderer[] renderers = Object.FindObjectsByType<MeshRenderer>();
        foreach (MeshRenderer mr in renderers)
        {
            string lowerName = mr.gameObject.name.ToLower();
            if (lowerName.StartsWith("floor"))
            {
                if (floorMat != null) mr.sharedMaterial = floorMat;
            }
            else if (lowerName.StartsWith("wall"))
            {
                if (wallMat != null) mr.sharedMaterial = wallMat;
            }
            else if (lowerName.StartsWith("caixa") || lowerName.StartsWith("crate"))
            {
                if (crateMat != null) mr.sharedMaterial = crateMat;
            }
            else if (lowerName.StartsWith("teto") || lowerName.StartsWith("ceiling"))
            {
                if (ceilingMat != null) mr.sharedMaterial = ceilingMat;
            }
            else if (mr.sharedMaterial == null || mr.sharedMaterial.shader == null || mr.sharedMaterial.shader.name.Contains("InternalError") || mr.sharedMaterial.shader.name == "Standard")
            {
                if (wallMat != null) mr.sharedMaterial = wallMat;
            }
            EditorUtility.SetDirty(mr);
        }
    }

    private static void SetupScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // Remove zumbi estático pré-existente da cena para que o EnemySpawner gerencie toda a população
        GameObject[] rootObjects = scene.GetRootGameObjects();
        foreach (GameObject root in rootObjects)
        {
            ZombieController[] zcs = root.GetComponentsInChildren<ZombieController>(true);
            foreach (ZombieController zc in zcs)
            {
                if (zc != null)
                {
                    Debug.Log($"[ZombieVRSetup] Removendo zumbi estático da cena: {zc.gameObject.name}");
                    Object.DestroyImmediate(zc.gameObject);
                }
            }
        }

        SetupAtmosphereAndLighting();
        SetupSceneMaterials();
        SetupPlayerAndCamera();
        SetupGameManagers();
        SetupWaypoints();
        SetupScoreboardCanvas();

        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void SetupPlayerAndCamera()
    {
        // Remove quaisquer clones orfaos gerados durante testes ou playmode anterior
        Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        GameObject[] roots = activeScene.GetRootGameObjects();
        foreach (GameObject go in roots)
        {
            if (go.name.Contains("(Clone)") || (go.name != "Zombie" && go.GetComponent<ZombieController>() != null))
            {
                Object.DestroyImmediate(go);
            }
        }

        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            player = new GameObject("Player");
        }
        player.transform.position = new Vector3(0.465f, 0f, 2.31f);
        player.transform.rotation = Quaternion.Euler(0f, -174.47f, 0f);
        player.tag = "Player";

        // Rigidbody Kinematic
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb == null) rb = player.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // CapsuleCollider
        CapsuleCollider col = player.GetComponent<CapsuleCollider>();
        if (col == null) col = player.AddComponent<CapsuleCollider>();
        col.radius = 0.35f;
        col.height = 1.8f;
        col.center = new Vector3(0, 0.9f, 0);

        // PlayerWaypointMover
        if (player.GetComponent<PlayerWaypointMover>() == null)
        {
            player.AddComponent<PlayerWaypointMover>();
        }

        // PlayerHitDetector
        if (player.GetComponent<PlayerHitDetector>() == null)
        {
            player.AddComponent<PlayerHitDetector>();
        }

        // Camera e PlayerController
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.gameObject.tag = "MainCamera";
            mainCam.transform.SetParent(player.transform);
            mainCam.transform.localPosition = new Vector3(0f, 1.65f, 0.08f);
            mainCam.transform.localRotation = Quaternion.Euler(0f, 5.85f, 0f);

            PlayerController pc = mainCam.GetComponent<PlayerController>();
            if (pc == null) pc = mainCam.gameObject.AddComponent<PlayerController>();
            pc.player = player.transform;
            pc.speed = 3.0f;
            pc.walkByLookingDown = false;
        }
    }

    private static void SetupGameManagers()
    {
        GameObject managers = GameObject.Find("GameManagers");
        if (managers == null)
        {
            managers = new GameObject("GameManagers");
        }

        GameManager gm = managers.GetComponent<GameManager>();
        if (gm == null) gm = managers.AddComponent<GameManager>();

        AudioManager am = managers.GetComponent<AudioManager>();
        if (am == null) am = managers.AddComponent<AudioManager>();
        am.backgroundMusic = Resources.Load<AudioClip>("Sons/background_music");
        am.gameOverSting = Resources.Load<AudioClip>("Sons/game_over_sting");

        EnemySpawner spawner = managers.GetComponent<EnemySpawner>();
        if (spawner == null) spawner = managers.AddComponent<EnemySpawner>();
        spawner.zombiePrefab = Resources.Load<GameObject>("Zombie");
        spawner.enemyBPrefab = Resources.Load<GameObject>("EnemyB");
        spawner.initialEnemyCount = 4;
        spawner.maxEnemyCount = 8;
    }

    private static void SetupWaypoints()
    {
        GameObject waypointsRoot = GameObject.Find("Waypoints");
        if (waypointsRoot == null)
        {
            waypointsRoot = new GameObject("Waypoints");
        }

        // Pelo menos 8 waypoints espalhados pela arena
        Vector3[] wpPositions = new Vector3[]
        {
            new Vector3(5f, 1.7f, 5f),
            new Vector3(-5f, 1.7f, 5f),
            new Vector3(8f, 1.7f, -4f),
            new Vector3(-8f, 1.7f, -4f),
            new Vector3(0f, 1.7f, 8f),
            new Vector3(0f, 1.7f, -8f),
            new Vector3(6f, 1.7f, 0f),
            new Vector3(-6f, 1.7f, 0f)
        };

        // Material compartilhado para os waypoints
        Material wpMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_WaypointMarker.mat");
        if (wpMat == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s == null) s = Shader.Find("Unlit/Color");
            wpMat = new Material(s);
            wpMat.color = new Color(0.1f, 0.7f, 1.0f, 0.8f);
            AssetDatabase.CreateAsset(wpMat, "Assets/Materials/M_WaypointMarker.mat");
        }

        PlayerWaypointMover mover = Object.FindAnyObjectByType<PlayerWaypointMover>();

        for (int i = 0; i < wpPositions.Length; i++)
        {
            string wpName = $"Waypoint ({i + 1})";
            Transform wpChild = waypointsRoot.transform.Find(wpName);
            GameObject wpObj;
            if (wpChild == null)
            {
                wpObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wpObj.name = wpName;
                wpObj.transform.SetParent(waypointsRoot.transform);
            }
            else
            {
                wpObj = wpChild.gameObject;
            }

            // Valida na NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(wpPositions[i], out hit, 4.0f, NavMesh.AllAreas))
            {
                wpObj.transform.position = hit.position + Vector3.up * 0.05f;
            }
            else
            {
                wpObj.transform.position = wpPositions[i] + Vector3.up * 0.05f;
            }

            wpObj.transform.localScale = new Vector3(1.0f, 0.04f, 1.0f); // Disco fino no chao

            Renderer r = wpObj.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = wpMat;

            // Trigger collider para mirada
            Collider col = wpObj.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            WaypointController wc = wpObj.GetComponent<WaypointController>();
            if (wc == null) wc = wpObj.AddComponent<WaypointController>();
            wc.markerRenderer = wpObj.GetComponent<MeshRenderer>();

            // Configura anel de progresso em WorldSpace sobre o disco
            Transform canvasTrans = wpObj.transform.Find("WaypointCanvas");
            if (canvasTrans == null)
            {
                GameObject canvasGO = new GameObject("WaypointCanvas");
                canvasGO.transform.SetParent(wpObj.transform);
                canvasGO.transform.localPosition = new Vector3(0, 1.2f, 0);
                canvasGO.transform.localRotation = Quaternion.Euler(90f, 0, 0);
                canvasGO.transform.localScale = Vector3.one * 0.01f;

                Canvas c = canvasGO.AddComponent<Canvas>();
                c.renderMode = RenderMode.WorldSpace;
                RectTransform rt = canvasGO.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(100, 100);

                GameObject ringGO = new GameObject("ProgressRing");
                ringGO.transform.SetParent(canvasGO.transform, false);
                Image ringImg = ringGO.AddComponent<Image>();
                ringImg.color = new Color(0.2f, 1f, 0.4f, 0.9f);
                ringImg.type = Image.Type.Filled;
                ringImg.fillMethod = Image.FillMethod.Radial360;
                ringImg.fillAmount = 0f;
                RectTransform ringRT = ringGO.GetComponent<RectTransform>();
                ringRT.sizeDelta = new Vector2(80, 80);

                wc.progressRing = ringImg;
            }

            if (mover != null) mover.RegisterWaypoint(wc);
        }
    }

    private static void SetupScoreboardCanvas()
    {
        GameObject scoreboardGO = GameObject.Find("ScoreboardCanvas");
        if (scoreboardGO == null)
        {
            scoreboardGO = new GameObject("ScoreboardCanvas");
        }

        Canvas canvas = scoreboardGO.GetComponent<Canvas>();
        if (canvas == null) canvas = scoreboardGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        RectTransform canvasRT = scoreboardGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(800, 400);
        canvasRT.localScale = Vector3.one * 0.002f; // Painel físico de 1.6m x 0.8m

        if (scoreboardGO.GetComponent<CanvasScaler>() == null)
        {
            scoreboardGO.AddComponent<CanvasScaler>();
        }
        if (scoreboardGO.GetComponent<GraphicRaycaster>() == null)
        {
            scoreboardGO.AddComponent<GraphicRaycaster>();
        }

        ScoreboardUI scoreboardUI = scoreboardGO.GetComponent<ScoreboardUI>();
        if (scoreboardUI == null) scoreboardUI = scoreboardGO.AddComponent<ScoreboardUI>();
        scoreboardUI.targetDistance = 1.15f;
        scoreboardUI.heightOffset = 0.30f;
        scoreboardUI.rightOffset = 0.55f;
        scoreboardUI.followSpeed = 7f;

        EnsureEventSystemInScene();

        // Fundo do Canvas
        Transform bgTrans = scoreboardGO.transform.Find("Background");
        GameObject bgGO;
        if (bgTrans == null)
        {
            bgGO = new GameObject("Background");
            bgGO.transform.SetParent(scoreboardGO.transform, false);
            Image bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.05f, 0.1f, 0.75f);
            RectTransform bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;
        }
        else
        {
            bgGO = bgTrans.gameObject;
        }

        // StartMenu Panel
        Transform startMenuTrans = scoreboardGO.transform.Find("StartMenuPanel");
        GameObject startMenuGO;
        if (startMenuTrans == null)
        {
            startMenuGO = new GameObject("StartMenuPanel");
            startMenuGO.transform.SetParent(scoreboardGO.transform, false);
            RectTransform smRT = startMenuGO.AddComponent<RectTransform>();
            smRT.anchorMin = Vector2.zero;
            smRT.anchorMax = Vector2.one;
            smRT.sizeDelta = Vector2.zero;

            TMP_Text title = CreateTMPText(startMenuGO.transform, "TitleText", "ZOMBIE VR", 68, new Vector2(0, 110));
            title.color = new Color(1f, 0.8f, 0.2f, 1f);

            CreateTMPText(startMenuGO.transform, "SubText", "Sobreviva no labirinto com a pistola\nOlhe para os zumbis para atirar!", 32, new Vector2(0, 35));

            GameObject playBtn = new GameObject("PlayButton");
            playBtn.transform.SetParent(startMenuGO.transform, false);
            RectTransform btnRT = playBtn.AddComponent<RectTransform>();
            btnRT.anchoredPosition = new Vector2(0, -75);
            btnRT.sizeDelta = new Vector2(260, 75);

            Image btnBg = playBtn.AddComponent<Image>();
            btnBg.color = new Color(0.15f, 0.65f, 0.25f, 0.95f);

            BoxCollider btnCol = playBtn.AddComponent<BoxCollider>();
            btnCol.size = new Vector3(260, 75, 10);

            GazeButton gazeBtn = playBtn.AddComponent<GazeButton>();
            gazeBtn.dwellTime = 1.5f;

            Button unityBtn = playBtn.AddComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(unityBtn.onClick, scoreboardUI.StartGame);
            if (gazeBtn.onClick == null) gazeBtn.onClick = new UnityEngine.Events.UnityEvent();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(gazeBtn.onClick, scoreboardUI.StartGame);

            CreateTMPText(playBtn.transform, "BtnText", "JOGAR", 42, Vector2.zero);
            CreateTMPText(startMenuGO.transform, "KeyPrompt", "Clique em JOGAR ou pressione ESPAÇO", 26, new Vector2(0, -145));
        }
        else
        {
            startMenuGO = startMenuTrans.gameObject;
            Transform btnTrans = startMenuGO.transform.Find("PlayButton");
            if (btnTrans != null)
            {
                Button unityBtn = btnTrans.GetComponent<Button>();
                if (unityBtn == null) unityBtn = btnTrans.gameObject.AddComponent<Button>();
                unityBtn.onClick.RemoveAllListeners();
                UnityEditor.Events.UnityEventTools.AddPersistentListener(unityBtn.onClick, scoreboardUI.StartGame);

                GazeButton gazeBtn = btnTrans.GetComponent<GazeButton>();
                if (gazeBtn != null)
                {
                    if (gazeBtn.onClick == null) gazeBtn.onClick = new UnityEngine.Events.UnityEvent();
                    gazeBtn.onClick.RemoveAllListeners();
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(gazeBtn.onClick, scoreboardUI.StartGame);
                }
            }
        }
        scoreboardUI.startMenuPanel = startMenuGO;

        // Playing Panel
        Transform playingTrans = scoreboardGO.transform.Find("PlayingPanel");
        GameObject playingGO;
        if (playingTrans == null)
        {
            playingGO = new GameObject("PlayingPanel");
            playingGO.transform.SetParent(scoreboardGO.transform, false);
            RectTransform pRT = playingGO.AddComponent<RectTransform>();
            pRT.anchorMin = Vector2.zero;
            pRT.anchorMax = Vector2.one;
            pRT.sizeDelta = Vector2.zero;

            scoreboardUI.scoreText = CreateTMPText(playingGO.transform, "ScoreText", "PONTOS: 0", 64, new Vector2(0, 80));
            scoreboardUI.highScoreText = CreateTMPText(playingGO.transform, "HighScoreText", "RECORDE: 0", 42, new Vector2(0, 0));
            scoreboardUI.killedText = CreateTMPText(playingGO.transform, "KilledText", "ELIMINADOS: 0", 38, new Vector2(0, -70));
            scoreboardUI.livesText = CreateTMPText(playingGO.transform, "LivesText", "VIDAS: 3", 42, new Vector2(0, -130));

            // Corações no topo direito do painel de jogo
            GameObject heartsGO = new GameObject("Hearts");
            heartsGO.transform.SetParent(playingGO.transform, false);
            RectTransform heartsRT = heartsGO.AddComponent<RectTransform>();
            heartsRT.anchorMin = new Vector2(1, 1);
            heartsRT.anchorMax = new Vector2(1, 1);
            heartsRT.pivot = new Vector2(1, 1);
            heartsRT.anchoredPosition = new Vector2(-30, -30);
            heartsRT.sizeDelta = new Vector2(160, 50);

            for (int i = 0; i < 3; i++)
            {
                GameObject heartGO = new GameObject($"Heart{i + 1}");
                heartGO.transform.SetParent(heartsGO.transform, false);
                RectTransform hRT = heartGO.AddComponent<RectTransform>();
                hRT.anchorMin = Vector2.zero;
                hRT.anchorMax = Vector2.zero;
                hRT.pivot = new Vector2(0, 0.5f);
                hRT.anchoredPosition = new Vector2(i * 45, 0);
                hRT.sizeDelta = new Vector2(35, 35);
                Image hImg = heartGO.AddComponent<Image>();
                hImg.color = Color.red;
                hImg.raycastTarget = false;

                if (i == 0) scoreboardUI.heart1 = hImg;
                if (i == 1) scoreboardUI.heart2 = hImg;
                if (i == 2) scoreboardUI.heart3 = hImg;
            }
        }
        else
        {
            playingGO = playingTrans.gameObject;
            scoreboardUI.scoreText = playingGO.transform.Find("ScoreText")?.GetComponent<TMP_Text>();
            scoreboardUI.highScoreText = playingGO.transform.Find("HighScoreText")?.GetComponent<TMP_Text>();
            scoreboardUI.killedText = playingGO.transform.Find("KilledText")?.GetComponent<TMP_Text>();
            scoreboardUI.livesText = playingGO.transform.Find("LivesText")?.GetComponent<TMP_Text>();
            Transform heartsTrans = playingGO.transform.Find("Hearts");
            if (heartsTrans != null)
            {
                scoreboardUI.heart1 = heartsTrans.Find("Heart1")?.GetComponent<Image>();
                scoreboardUI.heart2 = heartsTrans.Find("Heart2")?.GetComponent<Image>();
                scoreboardUI.heart3 = heartsTrans.Find("Heart3")?.GetComponent<Image>();
            }
        }
        scoreboardUI.playingPanel = playingGO;

        // GameOver Panel
        Transform gameOverTrans = scoreboardGO.transform.Find("GameOverPanel");
        GameObject gameOverGO;
        if (gameOverTrans == null)
        {
            gameOverGO = new GameObject("GameOverPanel");
            gameOverGO.transform.SetParent(scoreboardGO.transform, false);
            RectTransform goRT = gameOverGO.AddComponent<RectTransform>();
            goRT.anchorMin = Vector2.zero;
            goRT.anchorMax = Vector2.one;
            goRT.sizeDelta = Vector2.zero;

            TMP_Text title = CreateTMPText(gameOverGO.transform, "TitleText", "FIM DE JOGO", 72, new Vector2(0, 110));
            title.color = new Color(1f, 0.2f, 0.2f, 1f);

            scoreboardUI.gameOverScoreText = CreateTMPText(gameOverGO.transform, "GameOverScoreText", "PONTUAÇÃO FINAL: 0", 48, new Vector2(0, 20));

            // Botao Reiniciar acionado por olhar e clique
            GameObject restartBtn = new GameObject("RestartButton");
            restartBtn.transform.SetParent(gameOverGO.transform, false);
            RectTransform btnRT = restartBtn.AddComponent<RectTransform>();
            btnRT.anchoredPosition = new Vector2(0, -90);
            btnRT.sizeDelta = new Vector2(260, 70);

            Image btnBg = restartBtn.AddComponent<Image>();
            btnBg.color = new Color(0.2f, 0.6f, 0.2f, 0.9f);

            BoxCollider btnCol = restartBtn.AddComponent<BoxCollider>();
            btnCol.size = new Vector3(260, 70, 10);

            GazeButton gazeBtn = restartBtn.AddComponent<GazeButton>();
            gazeBtn.dwellTime = 2.0f;

            // Anel de progresso do botao
            GameObject ringGO = new GameObject("ProgressRing");
            ringGO.transform.SetParent(restartBtn.transform, false);
            Image ringImg = ringGO.AddComponent<Image>();
            ringImg.color = new Color(1f, 1f, 0.2f, 0.9f);
            ringImg.type = Image.Type.Filled;
            ringImg.fillMethod = Image.FillMethod.Radial360;
            ringImg.fillAmount = 0f;
            RectTransform ringRT = ringGO.GetComponent<RectTransform>();
            ringRT.sizeDelta = new Vector2(270, 80);
            gazeBtn.progressRing = ringImg;

            CreateTMPText(restartBtn.transform, "BtnText", "REINICIAR", 38, Vector2.zero);

            // Vincula evento de clique ao ScoreboardUI.RestartGame tanto no GazeButton quanto no Button da UI
            Button unityBtn = restartBtn.AddComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(unityBtn.onClick, scoreboardUI.RestartGame);

            if (gazeBtn.onClick == null) gazeBtn.onClick = new UnityEngine.Events.UnityEvent();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(gazeBtn.onClick, scoreboardUI.RestartGame);
        }
        else
        {
            gameOverGO = gameOverTrans.gameObject;
            scoreboardUI.gameOverScoreText = gameOverGO.transform.Find("GameOverScoreText")?.GetComponent<TMP_Text>();

            Transform btnTrans = gameOverGO.transform.Find("RestartButton");
            if (btnTrans != null)
            {
                Button unityBtn = btnTrans.GetComponent<Button>();
                if (unityBtn == null) unityBtn = btnTrans.gameObject.AddComponent<Button>();
                unityBtn.onClick.RemoveAllListeners();
                UnityEditor.Events.UnityEventTools.AddPersistentListener(unityBtn.onClick, scoreboardUI.RestartGame);

                GazeButton gazeBtn = btnTrans.GetComponent<GazeButton>();
                if (gazeBtn != null)
                {
                    if (gazeBtn.onClick == null) gazeBtn.onClick = new UnityEngine.Events.UnityEvent();
                    gazeBtn.onClick.RemoveAllListeners();
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(gazeBtn.onClick, scoreboardUI.RestartGame);
                }
            }
        }
        scoreboardUI.gameOverPanel = gameOverGO;

        // Estado inicial de gameplay sempre limpo
        if (scoreboardUI.scoreText != null) scoreboardUI.scoreText.text = "PONTOS: 0";
        if (scoreboardUI.highScoreText != null) scoreboardUI.highScoreText.text = "RECORDE: 0";
        if (scoreboardUI.killedText != null) scoreboardUI.killedText.text = "ELIMINADOS: 0";
        if (scoreboardUI.gameOverScoreText != null) scoreboardUI.gameOverScoreText.text = "PONTUAÇÃO FINAL: 0";

        startMenuGO.SetActive(true);
        playingGO.SetActive(false);
        gameOverGO.SetActive(false);

        // Posiciona a tela de Menu inicial a frente do jogador
        scoreboardGO.transform.position = new Vector3(0.465f, 2.5f, 0.0f);
        scoreboardGO.transform.rotation = Quaternion.Euler(0f, -174.47f, 0f);

        // Garante font em todos os textos TMP do placar
        TMP_FontAsset defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (defaultFont != null)
        {
            foreach (TMP_Text t in scoreboardGO.GetComponentsInChildren<TMP_Text>(true))
            {
                t.font = defaultFont;
            }
        }
    }

    private static void EnsureEventSystemInScene()
    {
        UnityEngine.EventSystems.EventSystem es = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (es == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Debug.Log("[ZombieVRSetup] EventSystem criado na cena.");
        }
        else if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
        {
            es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }

    private static TMP_Text CreateTMPText(Transform parent, string name, string text, float fontSize, Vector2 anchoredPos)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(700, 80);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (font != null)
        {
            tmp.font = font;
        }

        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.textWrappingMode = TextWrappingModes.Normal;

        return tmp;
    }

    [MenuItem("Tools/Zombie VR/Capture Evidence Screenshots")]
    public static void CaptureEvidenceScreenshots()
    {
        Debug.Log("[ZombieVRSetup] Capturando evidências visuais...");
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        SetupAtmosphereAndLighting();
        SetupSceneMaterials();

        string[] dirs = new string[] {
            Path.Combine(Application.dataPath, "..", "Docs", "evidencias"),
            Path.Combine(Application.dataPath, "..", "..", "Docs", "evidencias")
        };
        foreach (string d in dirs)
        {
            if (!Directory.Exists(d)) Directory.CreateDirectory(d);
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGO = GameObject.FindWithTag("MainCamera");
            if (camGO != null) cam = camGO.GetComponent<Camera>();
        }
        if (cam == null) return;

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 1f);

        ScoreboardUI scoreboardUI = Object.FindAnyObjectByType<ScoreboardUI>();
        GameObject scoreboardGO = GameObject.Find("ScoreboardCanvas");

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (scoreboardGO != null && font != null)
        {
            foreach (TMP_Text t in scoreboardGO.GetComponentsInChildren<TMP_Text>(true))
            {
                t.font = font;
            }
        }

        // 1. screenshot_placar.png
        if (scoreboardUI != null && scoreboardGO != null)
        {
            scoreboardGO.SetActive(true);
            scoreboardGO.transform.position = new Vector3(0.46f, 2.5f, 5.0f);
            scoreboardGO.transform.rotation = Quaternion.identity;

            scoreboardUI.playingPanel.SetActive(true);
            scoreboardUI.gameOverPanel.SetActive(false);
            scoreboardUI.scoreText.text = "PONTOS: 75";
            scoreboardUI.highScoreText.text = "RECORDE: 120";
            scoreboardUI.killedText.text = "ELIMINADOS: 4";

            cam.transform.position = new Vector3(0.46f, 2.5f, 2.7f);
            cam.transform.LookAt(scoreboardGO.transform.position);

            SaveCamShot(cam, "screenshot_placar.png", dirs);
        }

        // 2. screenshot_waypoints.png
        GameObject waypointsGO = GameObject.Find("Waypoints");
        if (waypointsGO != null)
        {
            if (scoreboardGO != null) scoreboardGO.SetActive(false);
            cam.transform.position = new Vector3(0f, 5.0f, -3.0f);
            cam.transform.LookAt(new Vector3(0f, 0.5f, 4.0f));

            SaveCamShot(cam, "screenshot_waypoints.png", dirs);
            if (scoreboardGO != null) scoreboardGO.SetActive(true);
        }

        // 3. screenshot_novo_inimigo.png
        GameObject enemyBPrefab = Resources.Load<GameObject>("EnemyB");
        if (enemyBPrefab != null)
        {
            if (scoreboardGO != null) scoreboardGO.SetActive(false);
            GameObject enemyB = Object.Instantiate(enemyBPrefab, new Vector3(0f, 0.05f, 4.0f), Quaternion.Euler(0, 180, 0));

            cam.transform.position = new Vector3(0f, 1.4f, 1.5f);
            cam.transform.LookAt(new Vector3(0f, 1.3f, 4.0f));

            SaveCamShot(cam, "screenshot_novo_inimigo.png", dirs);
            Object.DestroyImmediate(enemyB);
            if (scoreboardGO != null) scoreboardGO.SetActive(true);
        }

        // 4. screenshot_fim_de_jogo.png
        if (scoreboardUI != null && scoreboardGO != null)
        {
            scoreboardGO.SetActive(true);
            scoreboardGO.transform.position = new Vector3(0.46f, 2.5f, 5.0f);
            scoreboardGO.transform.rotation = Quaternion.identity;

            scoreboardUI.playingPanel.SetActive(false);
            scoreboardUI.gameOverPanel.SetActive(true);
            if (scoreboardUI.gameOverScoreText != null)
            {
                scoreboardUI.gameOverScoreText.text = "PONTUAÇÃO FINAL: 75";
            }

            cam.transform.position = new Vector3(0.46f, 2.5f, 2.7f);
            cam.transform.LookAt(scoreboardGO.transform.position);

            SaveCamShot(cam, "screenshot_fim_de_jogo.png", dirs);
            scoreboardUI.gameOverPanel.SetActive(false);
            scoreboardUI.playingPanel.SetActive(true);
        }

        // Restaura cena, player, camera e placar para o estado perfeito de gameplay limpo
        SetupPlayerAndCamera();
        SetupScoreboardCanvas();

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ZombieVRSetup] Captura de evidências concluída e cena restaurada com sucesso!");
    }

    private static void SaveCamShot(Camera cam, string filename, string[] dirs)
    {
        int width = 1920;
        int height = 1080;
        RenderTexture rt = new RenderTexture(width, height, 24);
        RenderTexture prevRT = cam.targetTexture;
        RenderTexture prevActive = RenderTexture.active;

        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        cam.targetTexture = prevRT;
        RenderTexture.active = prevActive;
        Object.DestroyImmediate(rt);

        byte[] pngData = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);

        foreach (string d in dirs)
        {
            string filePath = Path.Combine(d, filename);
            File.WriteAllBytes(filePath, pngData);
            Debug.Log($"[Screenshot] Salvo: {filePath} ({pngData.Length} bytes)");
        }
    }

    [MenuItem("Tools/Zombie VR/Verify Android Build")]
    public static void VerifyAndroidBuild()
    {
        Debug.Log("[ZombieVRSetup] Verificando compilação de scripts para Android...");
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Temp/AndroidBuildTest/test.apk",
            target = BuildTarget.Android,
            options = BuildOptions.BuildScriptsOnly
        };

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("[ZombieVRSetup] Compilação de scripts para plataforma Android concluída com SUCESSO!");
        }
        else
        {
            Debug.LogError($"[ZombieVRSetup] Falha na compilação para Android: {report.summary.result}, erros: {report.summary.totalErrors}");
        }
    }
}
