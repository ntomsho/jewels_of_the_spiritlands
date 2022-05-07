using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] GameObject logo;
    
    [Header("Overlays")]
    public OverlayManager overlayManager;
    [SerializeField] CanvasGroup fadeOutCanvas;
    
    [Header("Options")]
    [SerializeField] Canvas optionsManager;

    [Header("Audio")]
    AudioSource audioManager;
    [SerializeField] AudioClip battleStartSound;

    [Header("Character Selector")]
    public CharacterSelectorCarousel selector;

    [Header("Battle Manager")]
    [SerializeField] GameObject battleManagerPrefab;
    public BattleManager battleManager;

    [Header("Board Manager")]
    [SerializeField] GameObject BoardManagerPrefab;
    public BoardManager boardManager;

    [Header("Positions")]
    public Vector3[] partyPositions;
    public float[] enemyXPositions;
    public float[] enemyYPositions;
    public int[] startingGuard;
    public int[] spiritMod;
    public Sprite[] gemSprites;

    [Header("Game Settings")]
    public int gameDifficulty;
    public int experience;
    public GameObject[] enemyPrefabs;
    // public List<EnemyGroup> enemyGroups;
    
    [System.NonSerialized] public InfoManager infoManager;
    [System.NonSerialized] public CharacterTemplate[] party = new CharacterTemplate[5];    
    bool battleStarting;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        FadeInLogo();

        DontDestroyOnLoad(this.gameObject);
        DontDestroyOnLoad(logo.gameObject);
        DontDestroyOnLoad(overlayManager.gameObject);
        DontDestroyOnLoad(optionsManager.gameObject);
        audioManager = GetComponent<AudioSource>();
    }

    void FadeInLogo()
    {
        var sequence = DOTween.Sequence();
        sequence.Append(logo.GetComponent<SpriteRenderer>().DOFade(1f, 2f));
        sequence.Join(logo.transform.GetChild(0).GetComponent<SpriteRenderer>().DOFade(0.9f, 2f));
        sequence.PrependInterval(2f);
        sequence.OnComplete(() => 
        {
            TweenLogo();
            TweenOptionsButtons();
            StartCoroutine(LoadScene("PartySelectScene"));
        });
    }

    void TweenLogo()
    {
        logo.transform.DOMove(new Vector3(-7.5f, 3.2f, -5f), 1f).SetDelay(1f).SetEase(Ease.InSine);
    }

    void TweenOptionsButtons()
    {
        optionsManager.GetComponent<PauseMenu>().TweenInButtons();
    }

    public void SetParty(CharacterTemplate[] templates)
    {
        for (int ind = 0; ind < templates.Length; ind++)
        {
            party[ind] = CharacterTemplate.Instantiate(templates[ind]);
            party[ind].health = party[ind].characterPrefab.GetComponent<PartyMember>().maxHealth;
        }
    }

    public void StartBattle(int difficultyInt)
    {
        if (battleStarting) return;
        // if (SceneManager.GetActiveScene().name == "BattleScene") return;
        battleStarting = true;
        PlaySound(battleStartSound);
        gameDifficulty = difficultyInt;
        StartFadeOut("BattleScene");
        // fadeOutCanvas.DOFade(1f, 1f).OnComplete(() => {
        //     // StartCoroutine(LoadBattleScene());
        //     StartCoroutine(LoadScene("BattleScene"));
        // });
    }

    void SetupBattleScene()
    {
        GameObject battleObj = Instantiate(battleManagerPrefab, Vector3.zero, Quaternion.identity);
        battleManager = battleObj.GetComponent<BattleManager>();
        GameObject boardObj = Instantiate(BoardManagerPrefab, Vector3.zero, Quaternion.identity);
        boardManager = boardObj.GetComponent<BoardManager>();
        battleManager.boardManager = boardManager;
        boardManager.battleManager = battleManager;
        battleManager.infoCanvas = infoManager;
        battleManager.Setup(party, GetRandomEnemyGroup());
        boardManager.Setup();
    }

    public void Restart()
    {
        // SceneManager.LoadScene("PartySelectScene");
        StartFadeOut("PartySelectScene");
    }

    List<GameObject> GetRandomEnemyGroup()
    {
        List<GameObject> enemyGroup = new List<GameObject>();
        int expValue = (gameDifficulty + 2) * 100;
        while (enemyGroup.Count < 5 && expValue > 0)
        {
            GameObject newEnemy = enemyPrefabs[Random.Range(0,enemyPrefabs.Length)];
            enemyGroup.Add(newEnemy);
            expValue -= newEnemy.GetComponent<Enemy>().difficultyValue;
        }
        return enemyGroup;
    }

    // save this for later build
    // EnemyGroup GetRandomEnemyGroup()
    // {
    //     List<EnemyGroup> eligibleGroups = new List<EnemyGroup>();
    //     int minDifficulty = gameDifficulty * 150;
    //     int maxDifficulty = gameDifficulty * 450;
    //     foreach(EnemyGroup group in enemyGroups)
    //     {
    //         int groupDifficulty = group.DifficultyValue();
    //         if (groupDifficulty >= minDifficulty && groupDifficulty <= maxDifficulty)
    //         {
    //             eligibleGroups.Add(group);
    //         }
    //     }
    //     return eligibleGroups[Random.Range(0, eligibleGroups.Count)];
    // }

    public bool PartySelected()
    {
        foreach(CharacterTemplate pm in party)
        {
            if (pm == null)
            {
                return false;
            }
        }
        return true;
    }

    public void PlaySound(AudioClip clip)
    {
        audioManager.PlayOneShot(clip);
    }

    public void ChangeVolume(float val)
    {
        audioManager.volume = val;
    }

    public void ToggleMute()
    {
        audioManager.mute = !audioManager.mute;
        if (audioManager.isPlaying)
        {
            audioManager.Stop();
        }
    }

    void StartFadeOut(string sceneName)
    {
        fadeOutCanvas.DOFade(1f, 1f).OnComplete(() => {
            StartCoroutine(LoadScene(sceneName));
        });
    }

    IEnumerator LoadScene(string sceneName)
    {
        Debug.Log(sceneName);
        bool battleScene = sceneName == "BattleScene";

        logo.SetActive(!battleScene ? true : false);
        SceneManager.LoadScene(sceneName);

        while (SceneManager.GetActiveScene().name != sceneName)
        {
            yield return null;
        }
        fadeOutCanvas.DOFade(0f, 1f);

        if (battleScene)
        {
            SetupBattleScene();
        }
        else
        {
            //setup party select scene
        }
    }

    // IEnumerator LoadBattleScene()
    // {
    //     logo.SetActive(false);
    //     SceneManager.LoadScene("BattleScene");

    //     while (SceneManager.GetActiveScene().name != "BattleScene")
    //     {
    //         yield return null;
    //     }
    //     SetupBattleScene();
    //     fadeOutCanvas.DOFade(0f, 1f);
    // }
}
