using BepInEx;
using BepInEx.Logging;
using CimdyTranslationUtil;
using GlobalSettings;
using HarmonyLib;
using Needleforge;
using Needleforge.Data;
using Needleforge.Makers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using TeamCherry.Localization;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EngineersCrest;

[BepInDependency("org.silksong-modding.i18n")]
[BepInAutoPlugin(id: "io.github.engineerscrest")]
public partial class EngineersCrestPlugin : BaseUnityPlugin
{
    public static new ManualLogSource Logger;
    private static EngineersCrestPlugin instance;
    static Texture2D fallback = new Texture2D(1, 1);
    static Sprite fallbackSprite;
    static Sprite turretSprite;
    public static CrestData engineerCrest;
    public static ToolItem engineerTurret;
    public static Harmony harmony;
    public static GameObject TurretSpawnerPrefab;
    public static AudioClip TurretHitSound;
    static GameObject Prefabs;
    static AudioMixerGroup SoundMixerGroup;
    static AudioMixerGroup MasterMixerGroup;
    public static List<string> TOOL_BLACKLIST = new List<string>()
    {
        "FlintStone",
        "Flea Brew",
        "Lifeblood Syringe",
        "Screw Attack",
        "Rosary Cannon"
    };
    static GameObject LightningRod;
    public static Dictionary<string, Action<ToolItem, Turret>> TOOL_TURRET_CODE_ADDITION = new Dictionary<string, Action<ToolItem, Turret>>()
    {
        { 
            "Lightning Rod", (tool, turret) =>
            {
                GameObject instance = Instantiate(LightningRod);
                instance.transform.position = turret.transform.position;
                float angle = 145;
                if (!turret.facingRight)
                {
                    angle = 180f+(180f-angle);
                }
                angle = Mathf.MoveTowards(angle, 180, 15*(turret.shot-1));
                Vector2 velocity = new Vector2(MathF.Sin(angle * Mathf.Deg2Rad), MathF.Cos(angle * Mathf.Deg2Rad));
                velocity *= 30f;
                Rigidbody2D rb = instance.GetComponent<Rigidbody2D>();
                rb.linearVelocity = velocity;
            }
        }
    };
    static AssetBundle LoadBundleFromStreaming(string bundle)
    {
        string bundlePath = new FileInfo(Path.Combine(Application.streamingAssetsPath, "aa", "StandaloneWindows64", bundle)).FullName;
        return AssetBundle.LoadFromFile(bundlePath);
    }
    protected void LoadLightningRod()
    {
        string searchingFor = "Tool Lightning Rod";
        foreach (AssetBundle i in AssetBundle.GetAllLoadedAssetBundles())
        {
            List<string> matching = i.GetAllAssetNames().Where((i) => Path.GetFileNameWithoutExtension(i.Split('/').Last()) == searchingFor).ToList();
            if (matching.Count > 0)
            {
                LightningRod = i.LoadAsset<GameObject>(matching.First());
            }
        }
    }
    private void Awake()
    {
        instance = this;
        Logger = base.Logger;
        LoadAudio((clip) => TurretHitSound = clip, "EngineersCrest.Assets.turrethit.ogg");

        Prefabs = new GameObject("Prefabs");
        Prefabs.SetActive(false);
        DontDestroyOnLoad(Prefabs);
        TurretSpawnerPrefab = new GameObject("TurretSpawner");
        TurretSpawnerPrefab.AddComponent<TurretSpawner>();
        TurretSpawnerPrefab.transform.SetParent(Prefabs.transform);
        // Put your initialization logic here
        fallback.SetPixel(0, 0, Color.white);
        fallbackSprite = Sprite.Create(fallback, new Rect(0, 0, fallback.width, fallback.height), new Vector2(0.5f, 0.5f), 1);

        Texture2D engineerSprite = LoadTexture("EngineersCrest.Assets.Engineer.png");
        Sprite crestSprite = Sprite.Create(engineerSprite, new(0, 0, engineerSprite.width, engineerSprite.height), new(0.5f, 0.5f), 100f);

        Texture2D enginnerSilhouette = LoadTexture("EngineersCrest.Assets.EngineerSilhouette.png");
        Sprite crestSilhouette = Sprite.Create(enginnerSilhouette, new(0, 0, enginnerSilhouette.width, enginnerSilhouette.height), new(0.5f, 0.5f), 100f);

        Texture2D enginnerGlow = LoadTexture("EngineersCrest.Assets.EngineerGlow.png");
        Sprite crestGlow = Sprite.Create(enginnerGlow, new(0, 0, enginnerGlow.width, enginnerGlow.height), new(0.5f, 0.5f), 100f);

        engineerCrest = NeedleforgePlugin.AddCrest("ENGINEER", 
            new LocalisedString($"Mods.{Id}", "ENGINEER_CREST.NAME"), 
            new LocalisedString($"Mods.{Id}", "ENGINEER_CREST.DESC"), 
        crestSprite, crestSilhouette, crestGlow);

        engineerCrest.BindEvent += (healValue, healAmount, healTime, fsm) =>
        {

        };

        engineerCrest.slots = [
            new() { Type = ToolItemType.Red, Position = new Vector2(0, -0.08f), AttackBinding = AttackToolBinding.Neutral, // 0, Middle Red (Middle)
                NavLeftIndex = 6, NavRightIndex = 5, NavUpIndex = 1, NavDownIndex = 2 },
            new() { Type = ToolItemType.Red, Position = new Vector2(0, 1.52f), AttackBinding = AttackToolBinding.Up, // 1, Upper Red (Middle)
                NavLeftIndex = 3, NavRightIndex = 4, NavUpIndex = 4, NavDownIndex = 0 },
            new() { Type = ToolItemType.Red, Position = new Vector2(0, -1.70f), AttackBinding = AttackToolBinding.Down, // 2, Lower Red (Middle)
                NavLeftIndex = 6, NavRightIndex = 5, NavUpIndex = 0, NavDownIndex = -1 },
            new() { Type = ToolItemType.Blue, Position = new Vector2(-1.47f, 2.21f), // 3, Upper Blue (Left)
                NavLeftIndex = 6, NavRightIndex = 1, NavUpIndex = -1, NavDownIndex = 6 },
            new() { Type = ToolItemType.Yellow, Position = new Vector2(1.47f, 2.21f), // 4, Upper Yellow (Right)
                NavLeftIndex = 1, NavRightIndex = 5, NavUpIndex = -1, NavDownIndex = 5 },
            new() { Type = ToolItemType.Yellow, Position = new Vector2(2.38f, 0.83f), // 5, Lower Yellow (Right)
                NavLeftIndex = 0, NavRightIndex = -1, NavUpIndex = 4, NavDownIndex = 2 },
            new() { Type = ToolItemType.Blue, Position = new Vector2(-2.38f, 0.83f), // 6, Lower Blue (Left)
                NavLeftIndex = -1, NavRightIndex = 0, NavUpIndex = 3, NavDownIndex = 2 }
        ];

        harmony = new Harmony(Id);
        harmony.PatchAll();
        TranslationUtil.SetHarmony(harmony);

        SceneManager.activeSceneChanged += OnSceneChanged;

        Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
    }
    private void OnSceneChanged(Scene prev, Scene next)
    {
        if (next.name == "Menu_Title")
        {
            LoadLightningRod();
            LoadMixers();
        }
    }
    private static void LoadMixers()
    {
        string searchingFor = "Actors";
        string searchingFor2 = "Master";
        foreach (AssetBundle i in AssetBundle.GetAllLoadedAssetBundles())
        {
            List<string> matching = i.GetAllAssetNames().Where((i) => Path.GetFileNameWithoutExtension(i.Split('/').Last()) == searchingFor).ToList();
            if (matching.Count > 0)
            {
                AudioMixerGroup[] groups = i.LoadAssetWithSubAssets<AudioMixerGroup>(matching.First());
                SoundMixerGroup = groups.Where((j) => j.name == searchingFor && j.audioMixer.name == searchingFor).First();
            }
            matching = i.GetAllAssetNames().Where((i) => Path.GetFileNameWithoutExtension(i.Split('/').Last()) == searchingFor2).ToList();
            if (matching.Count > 0)
            {
                AudioMixerGroup[] groups = i.LoadAssetWithSubAssets<AudioMixerGroup>(matching.First());
                MasterMixerGroup = groups.Where((j) => j.name == searchingFor2 && j.audioMixer.name == searchingFor2).First();
            }
        }
    }
    public static Turret CreateTurret(Vector3 position, Vector2 velocity, ToolItem tool)
    {
        GameObject obj = new GameObject();
        obj.transform.position = position;
        obj.layer = LayerMask.NameToLayer("Terrain Detector");
        Turret turret = obj.AddComponent<Turret>();
        turret.SetTool(tool);
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = fallbackSprite;
        renderer.size = new Vector2(1, 1);
        BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
        collider.size = new Vector3(1f, 1f);
        Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
        rb.linearVelocity = velocity;
        rb.sharedMaterial = new PhysicsMaterial2D();
        rb.sharedMaterial.friction = 1f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        obj.transform.localScale = new Vector3(1, 2, 1);

        GameObject hitbox = new GameObject("Hitbox");
        hitbox.layer = LayerMask.NameToLayer("Hero Box");
        hitbox.transform.parent = obj.transform;
        BoxCollider2D turretHitboxCollider = hitbox.AddComponent<BoxCollider2D>();
        turretHitboxCollider.size = new Vector3(1f, 2f);
        turretHitboxCollider.isTrigger = true;

        Rigidbody2D hitboxRb = hitbox.AddComponent<Rigidbody2D>();
        hitboxRb.bodyType = RigidbodyType2D.Kinematic;
        hitboxRb.useFullKinematicContacts = true;
        TurretHitbox turretHitbox = hitbox.AddComponent<TurretHitbox>();
        hitbox.transform.localPosition = Vector3.zero;

        GameObject hitSoundObj = new GameObject("HitSound");
        hitSoundObj.transform.SetParent(obj.transform);
        hitSoundObj.transform.localPosition = Vector3.zero;
        AudioSource hitSound = hitSoundObj.AddComponent<AudioSource>();
        hitSound.clip = TurretHitSound;
        hitSound.spatialBlend = 0.2f;
        hitSound.minDistance = 25;
        hitSound.maxDistance = 50;
        hitSound.dopplerLevel = 0;
        hitSound.outputAudioMixerGroup = MasterMixerGroup;
        hitSound.priority = 100;
        AudioSourcePriority priority = hitSoundObj.AddComponent<AudioSourcePriority>();
        priority.sourceType = AudioSourcePriority.SourceType.Hero;

        return turret;
    }
    public static Texture2D LoadTexture(string name, Texture2D? fallback = null)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        if (!assembly.GetManifestResourceNames().Contains(name))
        {
            if (fallback != null)
            {
                return fallback;
            }
            throw new Exception("No texture with the name of \"" + name + "\" in assembly \"" + assembly.GetName().Name + "\"");
        }
        using (Stream stream = assembly.GetManifestResourceStream(name))
        {
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            Texture2D tex = new Texture2D(1, 1);
            tex.name = name;
            tex.LoadImage(buffer);
            return tex;
        }
    }
    public static void LoadAudio(Action<AudioClip> output, string name)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        if (!assembly.GetManifestResourceNames().Contains(name))
        {
            throw new Exception("No audio with the name of \"" + name + "\" in assembly \"" + assembly.GetName().Name + "\"");
        }
        using (Stream stream = assembly.GetManifestResourceStream(name))
        {
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);

            instance.StartCoroutine(CoLoadAudio(output, name, buffer));
        }
    }
    private static IEnumerator CoLoadAudio(Action<AudioClip> output, string name, byte[] data)
    {
        string path = Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName, "loading.ogg");
        if (File.Exists(path)) File.Delete(path);
        File.WriteAllBytes(path, data);
        UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.OGGVORBIS);
        yield return request.SendWebRequest();
        AudioClip audioClip = DownloadHandlerAudioClip.GetContent(request);
        if (audioClip != null)
        {
            output.Invoke(audioClip);
        }
        if (File.Exists(path)) File.Delete(path);
    }
    private static float[] ConvertByteToFloat(byte[] array)
    {
        float[] floatArr = new float[array.Length / 4];
        for (int i = 0; i < floatArr.Length; i++)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(array, i * 4, 4);
            }
            floatArr[i] = BitConverter.ToSingle(array, i * 4) / 0x80000000;
        }
        return floatArr;
    }
    public static void ThrowToolFrom(ToolItem tool, Vector3 throwPosition, bool throwRight)
    {
        HeroController controller = HeroController.instance;
        Vector3 originalPlayerPos = controller.transform.position;
        controller.transform.position = throwPosition+(controller.transform.position - controller.toolThrowPoint.position);
        bool originallyRight = controller.cState.facingRight;
        controller.cState.facingRight = throwRight;
        Vector3 plrLocalScale = controller.transform.localScale;
        plrLocalScale.x = throwRight ? -1 : 1;
        controller.transform.localScale = plrLocalScale;
        ToolItem.UsageOptions usage = tool.Usage;

        if (usage.ThrowPrefab)
        {
            Vector2 v = Vector2.left;
            float num = 1f;
            float direction = (float)(throwRight ? 0 : 180);
            Vector2 v3 = usage.ThrowOffset;
            v3.y += UnityEngine.Random.Range(-0.1f, 0.1f);
            GameObject gameObject = usage.ThrowPrefab.Spawn(throwPosition);
            if (usage.ScaleToHero)
            {
                Vector3 localScale = gameObject.transform.localScale;
                float num2 = throwRight ? (-num) : num;
                localScale.x = num2 * usage.ThrowPrefab.transform.localScale.x;
                if (usage.FlipScale)
                {
                    localScale.x = -localScale.x;
                }
                gameObject.transform.localScale = localScale;
                if (usage.SetDamageDirection)
                {
                    DamageEnemies component = gameObject.GetComponent<DamageEnemies>();
                    if (component)
                    {
                        component.SetDirection(direction);
                    }
                }
            }
            Vector2 vector = usage.ThrowVelocity.MultiplyElements(new float?(num), null);
            if (vector.magnitude > Mathf.Epsilon)
            {
                Rigidbody2D component2 = gameObject.GetComponent<Rigidbody2D>();
                if (component2)
                {
                    if (!throwRight)
                    {
                        vector.x *= -1f;
                    }
                    if (Mathf.Abs(vector.y) > Mathf.Epsilon)
                    {
                        float magnitude = vector.magnitude;
                        vector = (vector.normalized.DirectionToAngle() + UnityEngine.Random.Range(-2f, 2f)).AngleToDirection() * magnitude;
                    }
                    component2.linearVelocity = vector;
                }
            }
        }
        else
        {
            controller.toolEventTarget.SendEventSafe(usage.FsmEventName);
        }
        tool.OnWasUsed(tool.IsEmpty);
        controller.transform.position = originalPlayerPos;
        controller.cState.facingRight = originallyRight;
        plrLocalScale.x = originallyRight ? -1 : 1;
        controller.transform.localScale = plrLocalScale;
    }
}