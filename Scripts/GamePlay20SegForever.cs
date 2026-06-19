using DG.Tweening;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public enum CATEGORYFOREVER
{
    CulturaGeneral,
    VideoJuegos,
    Cine,
    Deportes,
    Series,
    Comics,
    MúsicaArgentina,
    MúsicaInternacional,
    Tecnología,
    Fútbol,
    CulturaPopArgentina,
}

public class GamePlay20SegForever : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI pregunta;
    [SerializeField] private TextMeshProUGUI preguntaCount;
    [SerializeField] private CanvasGroup respuestasPanel;
    [SerializeField] private PanelStatusGame statusPanel;
    [SerializeField] private Respuestas[] respuestasButton;
    [SerializeField] private GameObject panelMenu;
    [SerializeField] private GameObject panelGameplay;
    [SerializeField] private GameObject panelLeaderBoard;

    [SerializeField] private VideoPlayer playerIntroForever;
    [SerializeField] private VideoClip clipIntroForever;

    [SerializeField] private VideoPlayer preview;
    [SerializeField] private VideoPlayer preview2;
    [SerializeField] private Image preview3Custom;
    private bool alternatePreview = true;
    [SerializeField] private CategoryForever[] categories;

    [SerializeField] private GameObject rewardCoinsButon;
    [SerializeField] private Button rewardButon;

    private QuestionsForeverResponse preguntas20SegForeverList;
    private QuestionsForeverResponse preguntas20SegForeverListTMP;
    private int indexPregunta;

    [Tooltip("Numero de preguntas ok en el nivel actual")]
    [SerializeField] private int indexPreguntaOk;

    [Tooltip("Numero de preguntas ok en la partida actual")]
    [SerializeField] private int indexPreguntaTotalOk;

    [Tooltip("Numero de preguntas que debe responder bien para pasar al siguiente nivel")]
    [SerializeField] private int maxLevelQuestionFinal = 10;
    [SerializeField] private int maxLevelQuestion = 10;

    [Tooltip("Niveles maximos alcanzables")]
    [SerializeField] private int maxLevel = 3;

    [Tooltip("Numero de preguntas descargada en cada consulta")]
    [SerializeField] private int maxQuestionRequest = 20;

    [SerializeField] private int segundosRespOk = 8;
    [SerializeField] private int segundosRespBad = 5;

    [SerializeField] private int minQuestionToCaptcha = 10;
    [SerializeField] private int nexQuestionToCaptcha = 0;

    [Header("Anti repeticion de preguntas")]
    [Tooltip("Cantidad minima de preguntas utiles que deben quedar despues de filtrar repetidas. Si queda por debajo, vuelve a pedir.")]
    [SerializeField] private int minQuestionsAfterFilter = 10;

    [Tooltip("Intentos maximos pidiendo preguntas nuevas antes de resetear la lista local de IDs usados.")]
    [SerializeField] private int maxAttemptsBeforeResetUsedIds = 5;

    private readonly HashSet<string> usedQuestionIds = new HashSet<string>();
    private readonly List<QuestionForeverData> bufferedQuestions = new List<QuestionForeverData>();

    private int _level = 1;
    private string _category = "";
    public static GamePlay20SegForever Instance;

    public static string TEMATICA_ID;
    public static string TEMATICA_NAME;
    public static string TEMATICA_DESC;
    public static PriceEvent[] TEMATICA_PREMIOS;
    public static Sprite TEMATICA_ICON;
    public static Sprite TEMATICA_BANNER;
    public static bool IS20SEG;
    [SerializeField] private Image iconGameForever;

    private Coroutine _coroutineGamePlay;
    private bool isPlaying = false;
    private System.DateTime startPartida;
    private int score = 100;
    private int rewardMax = 6;
    private int rewardTotal;
    private int partidasTotales;
    private CoinsResponse coinsRewards;

    public static System.TimeSpan TIMEPARTIDA;
    public static int TOTALQUESTION;
    public static int TOTALQUESTIONOK;
    public static int HIGSCORE;
    public static bool IsHIGSCORE;

    public static GameStatsForever PLAYERDATAFOREVERSTATS;

    [SerializeField] private int partidasTotalesShowReward = 2;
    [SerializeField] private int partidasTotalesShowInterstitial = 2;
    private int _partidasTotalesShowInterstitialCurrent;

    private void Awake()
    {
        Instance = this;
        minQuestionToCaptcha = LoginManager.GetGlobalIntVar(GLOBALVARS.ForeverMinQuestionForCaptcha);
        //maxQuestionRequest = 10;
    }

    public async void PlayGame()
    {
        await Task.Delay(UIAnimator.defaultDurationAwait);
        if (!ManagerGame.Instance.UseTicket())
        {
            return;
        }

        //maxQuestionRequest = 10;
        maxLevelQuestion = maxLevelQuestionFinal - 1;
        minQuestionsAfterFilter = Mathf.Max(1, maxLevelQuestion);

        usedQuestionIds.Clear();
        bufferedQuestions.Clear();

        indexPregunta = 0;
        indexPreguntaTotalOk = indexPreguntaOk = 0;
        _level = 1;
        StopGamePlay(false);
        _coroutineGamePlay = StartCoroutine(startGame());
        Timer.Instance.setTimer(ManagerGame.timeRespuesta, false, false);
        ManagerGame.setStatus(STATUSGAME.FOREVER);
        Transicion.Instance.playAnimation(ANIMATIONCLIP.Loading, 1000);
        panelMenu.SetActive(false);
        panelGameplay.SetActive(!panelMenu.activeSelf);
        startPartida = System.DateTime.Now;
        partidasTotales++;
    }

    public void SetForeverTematica(bool _leaderboard = false)
    {
        iconGameForever.overrideSprite = TEMATICA_ICON;
        if (!_leaderboard)
        {
            panelMenu.SetActive(true);
        }
    }

    [ContextMenu("Stop")]
    private void StopGamePlayTest()
    {
        AnalyticsManager.StartEvent(AnalyticsEventName.Forever, AnalyticsEventParameter.ForeverGamePlay, AnalyticsEventParameterValue.Abandonar);
        OnTimerEndAsync(true);
    }

    private void StopGamePlay(bool _saveGame = true)
    {
        if (_coroutineGamePlay != null)
        {
            Timer.Instance.StopTimer();
            StopCoroutine(_coroutineGamePlay);
        }

        _coroutineGamePlay = null;
        isPlaying = false;

        if (partidasTotales == partidasTotalesShowReward)
        {
            // rewardButon.interactable = true;
        }

        if (_saveGame)
        {
            IsHIGSCORE = false;
            if (TOTALQUESTIONOK > HIGSCORE)
            {
                HIGSCORE = TOTALQUESTIONOK;
                IsHIGSCORE = true;
            }
            _ = SetPartida(TOTALQUESTION, TOTALQUESTIONOK, (TIMEPARTIDA = System.DateTime.Now.Subtract(startPartida)).TotalSeconds, score);
        }
    }

    public async void ShowLeaderBoard()
    {
        await Task.Delay(UIAnimator.defaultDurationAwait);
        panelLeaderBoard.SetActive(true);
        panelMenu.SetActive(false);
        LoadReward();
        AnalyticsManager.StartEvent(AnalyticsEventName.Forever, AnalyticsEventParameter.Screen, AnalyticsEventParameterValue.ShowLeaderBoard);
    }

    public void BuyTickets()
    {
    }

    private void SetPreguntaIndex(int _value)
    {
        preguntaCount.text = (_value).ToString();
    }

    private void OnEnable()
    {
        if (LoginManager.GetGlobalVarBool(GLOBALVARS.maintenanceForever))
        {
            ActivateGameObject.Instance.Mantenimiento();
            return;
        }
        AnalyticsManager.StartEvent(AnalyticsEventName.Game, AnalyticsEventParameter.Screen, AnalyticsEventParameterValue.ForeverHome);
        Timer.Instance.StopTimer();
        PlayIntro();
        Timer.OnTimerEnd += OnTimerEndAsync;
        ActivateGameObject.OnLocalBackPress += backToMenu;
        RewardedAds.Instance.LoadAd(true);
        panelMenu.SetActive(false);
        _ = GetPlayerData();
        ShowTutorial();
    }

    public async void ShowReward()
    {
        await Task.Delay(UIAnimator.defaultDurationAwait);
        RewardedAds.Instance.ShowAd();
        partidasTotales = 0;
        rewardTotal++;
    }

    private async void PlayIntro()
    {
        playerIntroForever.GetComponent<CanvasGroup>().alpha = 1.0f;
        playerIntroForever.gameObject.SetActive(true);
        playerIntroForever.clip = clipIntroForever;
        playerIntroForever.Play();

        while (!playerIntroForever.isPlaying)
        {
            await Task.Delay(100);
        }

        while (gameObject.activeSelf && playerIntroForever.isPlaying)
        {
            await Task.Delay(10);
        }

        playerIntroForever.GetComponent<CanvasGroup>().DOFade(0, 0.5f).OnComplete(() =>
        {
            playerIntroForever.gameObject.SetActive(false);
        });
    }

    private void setPreview(CategoryForever _category)
    {
        VideoPlayer _preview, _preview2;

        if (alternatePreview)
        {
            _preview = preview;
            _preview2 = preview2;
        }
        else
        {
            _preview = preview2;
            _preview2 = preview;
        }

        alternatePreview = !alternatePreview;
        _preview2.clip = _category.categoryClip;

        _preview.GetComponent<RawImage>().DOFade(0, 0.5f);
        _preview2.GetComponent<RawImage>().DOFade(1, 0.5f);
    }

    private void OnDisable()
    {
        Timer.OnTimerEnd -= OnTimerEndAsync;
        ActivateGameObject.OnLocalBackPress -= backToMenu;
        StopGamePlay(isPlaying);
    }

    private IEnumerator startGame()
    {
        AnalyticsManager.StartEvent(AnalyticsEventName.Forever, AnalyticsEventParameter.ForeverGamePlay, AnalyticsEventParameterValue.Iniciar);

        LoadReward();
        InterstitialAds.Instance.LoadAd();
        isPlaying = true;
        TOTALQUESTIONOK = TOTALQUESTION = 0;
        SetPreguntaIndex(0);

        var task = LoadQuestionBlock(_level, true);
        yield return new WaitUntil(() => task.IsCompleted);

        nexQuestionToCaptcha = minQuestionToCaptcha + Random.Range(1, minQuestionToCaptcha);
        if (Transicion.Instance.IsActive())
        {
            Transicion.Instance.playAnimation(ANIMATIONCLIP.FadeOut, 10);
        }

        if (task.Result)
        {
            Timer.Instance.setTimer(ManagerGame.timeRespuesta, true, true);

            while (true)
            {
                Debug.Log("Bloque de Preguntas cargadas correctamente");

                if (TOTALQUESTION > minQuestionToCaptcha && nexQuestionToCaptcha == TOTALQUESTION)
                {
                    Debug.Log($"ENTRO AL CAPTCHA {minQuestionToCaptcha} {nexQuestionToCaptcha} {TOTALQUESTION}");
                    nexQuestionToCaptcha = TOTALQUESTION + Random.Range(minQuestionToCaptcha, minQuestionToCaptcha * 2);

                    Timer.PAUSE_TIMER = true;
                    bool captchaResult = false;

                    var taskCaptcha = LoginManager.Instance.GetCaptchaAsync(false);
                    yield return new WaitUntil(() => taskCaptcha.IsCompleted);
                    captchaResult = taskCaptcha.Result;

                    Debug.Log("Resultado del captcha: " + captchaResult);

                    if (!captchaResult)
                    {
                        AnalyticsManager.StartEvent(AnalyticsEventName.Forever, AnalyticsEventParameter.ForeverGamePlay, AnalyticsEventParameterValue.DescalificadoCaptcha);
                        OnTimerEndAsync(true);
                        yield break;
                    }
                    else
                    {
                        Timer.PAUSE_TIMER = false;
                    }
                }

                if (preguntas20SegForeverList == null || preguntas20SegForeverList.questions == null || preguntas20SegForeverList.questions.Count == 0)
                {
                    var reloadTask = LoadQuestionBlock(_level, false);
                    yield return new WaitUntil(() => reloadTask.IsCompleted);

                    if (!reloadTask.Result)
                    {
                        Debug.LogError("[Forever] No se pudieron cargar preguntas.");
                        OnTimerEndAsync(true);
                        yield break;
                    }
                }

                if (indexPregunta >= preguntas20SegForeverList.questions.Count)
                {
                    int nextLevel = _level;
                    var reloadTask = LoadQuestionBlock(nextLevel, false);
                    yield return new WaitUntil(() => reloadTask.IsCompleted);

                    if (!reloadTask.Result)
                    {
                        Debug.LogError("[Forever] No se pudieron recargar preguntas.");
                        OnTimerEndAsync(true);
                        yield break;
                    }
                }

                QuestionForeverData currentQuestion = preguntaActual();

                if (setPregunta(currentQuestion))
                {
                    MarkQuestionAsUsed(currentQuestion);

                    yield return new WaitWhile(() => !Respuestas.isSelectResp);

                    bool result;
                    TOTALQUESTION++;

                    if (result = currentQuestion.showResultForever())
                    {
                        Timer.Instance.addTimer(segundosRespOk);
                        indexPreguntaOk++;
                        indexPreguntaTotalOk++;
                        TOTALQUESTIONOK++;
                    }
                    else
                    {
#if UNITY_EDITOR
                        Timer.Instance.addTimer(0);
#endif
                        Timer.Instance.addTimer(-segundosRespBad);
                    }
                    _ = setRespuesta(currentQuestion._id, result);

                    yield return new WaitForSeconds(.75f);

                    currentQuestion.resetRespuestas();
                    indexPregunta++;
                    SetPreguntaIndex(indexPreguntaTotalOk);

                    Debug.Log("*****q " + (indexPregunta - 2) + "==" + preguntas20SegForeverList.questions.Count);
                }
                else
                {
                    indexPregunta++;
                }

                if (indexPreguntaOk >= maxLevelQuestion || indexPregunta >= preguntas20SegForeverList.questions.Count - 2)
                {
                    bool levelUp = indexPreguntaOk >= maxLevelQuestion;

                    if (levelUp)
                    {
                        if (maxLevelQuestion == 8)
                        {
                            maxLevelQuestion -= 2;
                        }
                        else if (maxLevelQuestion == 6)
                        {
                            maxLevelQuestion -= 1;
                        }
                        else if (maxLevelQuestion == 5)
                        {
                            maxLevelQuestion--;
                        }
                    }

                    int nextLevel = levelUp ? ++_level : _level;
                    indexPreguntaOk = 0;
                    minQuestionsAfterFilter = Mathf.Max(1, maxLevelQuestion);

                    var reloadTask = LoadQuestionBlock(nextLevel, false);
                    yield return new WaitUntil(() => reloadTask.IsCompleted);

                    if (!reloadTask.Result)
                    {
                        Debug.LogError("[Forever] No se pudieron cargar mas preguntas.");
                        OnTimerEndAsync(true);
                        yield break;
                    }
                }
            }
        }
        else
        {
            Debug.LogError("Falló la carga de preguntas");
        }
    }

    private async Task<bool> LoadQuestionBlock(int level, bool transition = true)
    {
        bufferedQuestions.Clear();

        int attempts = 0;
        bool usedIdsWereReset = false;
        int minRequired = Mathf.Max(1, minQuestionsAfterFilter);

        while (bufferedQuestions.Count < minRequired)
        {
            attempts++;

            if (attempts > maxAttemptsBeforeResetUsedIds)
            {
                if (!usedIdsWereReset)
                {
                    Debug.LogWarning($"[Forever] No se juntaron suficientes preguntas nuevas despues de {maxAttemptsBeforeResetUsedIds} intentos. Se resetean IDs usados y se vuelve a intentar.");
                    usedQuestionIds.Clear();
                    bufferedQuestions.Clear();
                    attempts = 1;
                    usedIdsWereReset = true;
                }
                else
                {
                    Debug.LogWarning("[Forever] Incluso despues de resetear IDs no se llego al minimo. Se continua con las preguntas disponibles.");
                    break;
                }
            }

            bool success = await GetQuestions(level, transition && attempts == 1);

            if (!success)
            {
                return false;
            }

            AddOnlyNewQuestionsToBuffer(preguntas20SegForeverListTMP);

            Debug.Log($"[Forever] Intento {attempts}. Preguntas utiles acumuladas: {bufferedQuestions.Count}/{minRequired}. IDs usados: {usedQuestionIds.Count}");

            await Task.Yield();
        }

        if (bufferedQuestions.Count == 0)
        {
            return false;
        }

        preguntas20SegForeverList = new QuestionsForeverResponse
        {
            status = preguntas20SegForeverListTMP != null ? preguntas20SegForeverListTMP.status : string.Empty,
            message = preguntas20SegForeverListTMP != null ? preguntas20SegForeverListTMP.message : string.Empty,
            questions = new List<QuestionForeverData>(bufferedQuestions)
        };

        indexPregunta = 0;
        lastIdQuestion = null;
        bufferedQuestions.Clear();

        return true;
    }

    private void AddOnlyNewQuestionsToBuffer(QuestionsForeverResponse response)
    {
        if (response == null || response.questions == null)
        {
            return;
        }

        foreach (var question in response.questions)
        {
            if (question == null || string.IsNullOrEmpty(question._id))
            {
                continue;
            }

            if (usedQuestionIds.Contains(question._id))
            {
                continue;
            }

            if (bufferedQuestions.Any(q => q != null && q._id == question._id))
            {
                continue;
            }

            bufferedQuestions.Add(question);
        }

        bufferedQuestions.Sort((a, b) => a.shortValue.CompareTo(b.shortValue));
    }

    private void MarkQuestionAsUsed(QuestionForeverData question)
    {
        if (question == null || string.IsNullOrEmpty(question._id))
        {
            return;
        }

        usedQuestionIds.Add(question._id);
    }

    public async Task<bool> setRespuesta(string _idRespuesta, bool _isCorrect)
    {
        var _result = await MySqlManager.SetQuestions20SegForever(_idRespuesta, _isCorrect, 100, 100);

        if (_result.success)
        {
            Debug.Log("OBTENEMOS LAS PREGUNTAS: " + _result.success + " - " + _result.value + "****\n");
            return true;
        }
        else
        {
            Debug.Log("ERROR EN LA OBTENCION DE COIS: " + _result.success + " - " + _result.value);

            if (await PopUpManager.Instance.ShowKickPopUpAsync())
            {
                OnTimerEndAsync(true);
            }
            return false;
        }
    }

    public void OnTimerEndAsync()
    {
        AnalyticsManager.StartEvent(AnalyticsEventName.Forever, AnalyticsEventParameter.ForeverGamePlay, AnalyticsEventParameterValue.Perder);
        if (gameObject.activeInHierarchy)
        {
            OnTimerEndAsync(true);
        }
    }

    public void OnTimerEndAsync(bool _showPopUp = true)
    {
        StopGamePlay();
        if (_showPopUp)
        {
            showPopUpEndGame();
        }
    }

    public async void showPopUpEndGame()
    {
        Debug.Log(ManagerGame.gameDataPlayer.evento);
        if (ManagerGame.gameDataPlayer.evento == null)
        {
            ManagerGame.gameDataPlayer.evento = new EventoData();
        }

        PopUpManager.Instance.setText("", "" + ManagerGame.gameDataPlayer.evento.puntuacionObtenidaEvento,
                                      typePOPUP.OK, 0, false, typeMSJ.msjEndForever, "", "", 100, false, false);
        while (PopUpManager.result == 0)
        {
            await Task.Delay(10);
        }
        backToMenu(true);

        if (preguntas20SegForeverList != null && preguntas20SegForeverList.questions != null && preguntas20SegForeverList.questions.Count > 0)
        {
            preguntaActual().resetRespuestas();
        }
    }

    public QuestionForeverData preguntaActual()
    {
        Debug.Log("*****R" + preguntas20SegForeverList.questions.Count + " - " + indexPregunta);
        return preguntas20SegForeverList.questions[indexPregunta];
    }

    string lastIdQuestion;

    public bool setPregunta(QuestionForeverData _pregunta)
    {
        if (_pregunta == null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(lastIdQuestion) && _pregunta._id.Equals(lastIdQuestion))
        {
            return false;
        }

        lastIdQuestion = _pregunta._id;
        ManagerGame.setStatus(STATUSGAME.WAITING_ASNWER_PLAYER);

        pregunta.text = _pregunta.question;

        int offset = 0;
        if (IS20SEG)
        {
            var cat = GetCategory(_pregunta.category);

            if (cat != null)
            {
                Debug.Log("Encontrado: " + cat.category);
                setPreview(cat);
            }
            else
            {
                Debug.Log("No se encontró la categoría");
            }
        }
        else
        {
            preview3Custom.overrideSprite = TEMATICA_BANNER;
        }
        preview3Custom.transform.parent.gameObject.SetActive(!IS20SEG);
        if (_pregunta.options == null || _pregunta.options.Count == 0)
        {
            Debug.LogWarning("[Forever] Pregunta sin opciones: " + _pregunta._id);
            return false;
        }

        _pregunta.options.Sort((a, b) => a.sortValue.CompareTo(b.sortValue));
        _pregunta.respuestasButton.Clear();

        for (int i = 0; i < _pregunta.options.Count && i + offset < respuestasButton.Length; i++)
        {
            if (_pregunta.options[i].correct)
            {
                _pregunta.idRespuestaCorrecta = _pregunta.options[i]._id;
            }
            respuestasButton[i + offset].setRespuesta(_pregunta.options[i]);
            _pregunta.respuestasButton.Add(respuestasButton[i + offset]);
        }
        return true;
    }

    private CategoryForever GetCategory(string input)
    {
        Debug.Log("****f Buscando " + input);

        if (!System.Enum.TryParse<CATEGORYFOREVER>(input, true, out var categoriaEnum))
        {
            return null;
        }

        Debug.Log("****f Encontro " + categoriaEnum);

        var result = categories.FirstOrDefault(c => c.category == categoriaEnum);

        if (result == null)
        {
            Debug.LogWarning("No se encontro clip para categoria: " + categoriaEnum);
            return null;
        }

        Debug.Log("****f Encontro " + result.categoryClip);
        return result;
    }

    public async Task<bool> GetQuestions(int _level, bool _transition = true)
    {
        if (_level > maxLevel)
        {
            _level = maxLevel;
            _level = Random.Range(0, 10) < 5 ? maxLevel : maxLevel - 1;
        }

        //maxQuestionRequest = 10;

        var _result = await MySqlManager.GetQuestions20SegForeverFilter(_level, maxQuestionRequest, _category, null, TEMATICA_NAME, true, _transition);
        Debug.Log("****ME TRAJO\n" + _result.value);

        if (_result.success)
        {
            preguntas20SegForeverListTMP = JsonUtility.FromJson<QuestionsForeverResponse>(_result.value);

            if (preguntas20SegForeverListTMP == null)
            {
                Debug.LogWarning("[Forever] No se pudo parsear QuestionsForeverResponse.");
                return true;
            }

            if (preguntas20SegForeverListTMP.questions == null)
            {
                preguntas20SegForeverListTMP.questions = new List<QuestionForeverData>();
            }

            preguntas20SegForeverListTMP.questions.Sort((a, b) => a.shortValue.CompareTo(b.shortValue));

            Debug.Log("20FOrever OBTENEMOS LAS PREGUNTAS: " + _result.success + " - " + _result.value + "****\n" + preguntas20SegForeverListTMP.questions.Count);
            return true;
        }
        else
        {
            Debug.Log("20FOrever ERROR EN LA OBTENCION DE PREGUNTAS: " + _result.success + " - " + _result.value);
            if (await PopUpManager.Instance.ShowKickPopUpAsync())
            {
                OnTimerEndAsync(true);
            }
            return false;
        }
    }

    public async Task<bool> GetPlayerData(bool _transicion = false)
    {
        var _result = await MySqlManager.GetPlayerDataForever(_transicion);

        if (_result.success)
        {
            PlayerStatsGeneral response = JsonConvert.DeserializeObject<PlayerStatsGeneral>(_result.value);
            PLAYERDATAFOREVERSTATS = response.data;
            if (PLAYERDATAFOREVERSTATS != null)
            {
                HIGSCORE = PLAYERDATAFOREVERSTATS.streak;
                Debug.Log("*****h" + PLAYERDATAFOREVERSTATS.streak + " - " + PLAYERDATAFOREVERSTATS.maxQuestionOk + " - " + _result.value);
            }
            _ = GetCoinsRewards();
            return true;
        }
        else
        {
            Debug.Log("ERROR EN LA OBTENCION DE DATA PLAYER: " + _result.success + " - " + _result.value);
            _ = GetCoinsRewards();
            return false;
        }
    }

    public async Task<bool> SetPartida(int _respuestasTotales, int _respuestasOk, double _time, int _score)
    {
        var _result = await MySqlManager.SetPartidaForever(TEMATICA_ID, _respuestasTotales, _respuestasOk, (int)_time, _score);

        if (_result.success)
        {
            Debug.Log("PARTIDA GUARDADA: " + _result.success + " - " + _result.value);
            return true;
        }
        else
        {
            Debug.Log("PARTIDA NO GUARDADA: " + _result.success + " - " + _result.value);
            return false;
        }
    }

    public async Task<bool> GetCoinsRewards()
    {
        while (!TematicasLoader.loadCompleteTheme)
        {
            await Task.Delay(100);
        }
        await AsignRewardsAsync();
        var _result = await MySqlManager.GetCoinsRewards();

        if (_result.success)
        {
            coinsRewards = JsonUtility.FromJson<CoinsResponse>(_result.value);

            Debug.Log("OBTENEMOS LAS COINS REWARDS: " + _result.success + " - " + _result.value + " - count: " + coinsRewards.rewardsCoins + "\n");
            rewardCoinsButon.SetActive(coinsRewards.rewardsCoins > 0);
            return true;
        }
        else
        {
            Debug.Log("ERROR EN LA OBTENCION DE COINS REWARDS: " + _result.success + " - " + _result.value);
            return false;
        }
    }
    public async Task AsignRewardsAsync()
    {
        var timeServer = await ClockServer.getTimeServer();
        var _result = await MySqlManager.GetRankingDaily(TEMATICA_NAME, 10, timeServer.AddDays(-1));
        if (_result.success)
        {
            var rankingForeverList = JsonConvert.DeserializeObject<RankingDataForeverResponse>(_result.value).ranking;
            RankingForeverPlayers player = rankingForeverList
            .FirstOrDefault(r => r.playerId == ManagerGame.gameDataPlayer.player.idPlayer);

            if (player != null)
            {
                if (player.position < TEMATICA_PREMIOS.Length)
                {
                    Debug.Log("[AsignRewards] coins" + TEMATICA_PREMIOS[player.position].NameNormal + " - " + TEMATICA_PREMIOS[player.position].coins);

                    _result = await MySqlManager.setRewardsCoins(TEMATICA_ID, TEMATICA_PREMIOS[player.position - 1].coins, false);
                    if (_result.success)
                    {
                        Debug.Log("[AsignRewards] Se pudieron asignar las RewrdsCoins " + TEMATICA_PREMIOS[player.position].coins);

                    }
                    else
                    {
                        Debug.Log("[AsignRewards] No se pudieron asignar las RewrdsCoins " + TEMATICA_PREMIOS[player.position].coins);

                    }
                }else 
                {
                    Debug.Log("[AsignRewards] No tiene premio, posicion: " + player.position +" < "+ TEMATICA_PREMIOS.Length);

                }
            }
            else
            {
                Debug.Log("[AsignRewards] No se encontró el jugador en el ranking." + timeServer);
            }

            
        }
    }
    public async Task<bool> ClainCoinsRewards()
    {
        var _result = await MySqlManager.ClainCoinsRewards();

        if (_result.success)
        {
            Debug.Log("Agregamos coins : " + _result.success + " - " + _result.value + " - count: ");
            CoinsResponse response = JsonUtility.FromJson<CoinsResponse>(_result.value);
             
            ManagerGame.gameDataPlayer.player.Coins = response.coins.ToString();
            rewardCoinsButon.SetActive(false);
            return true;
        }
        else
        {
            Debug.Log("ERROR AGREGAR COINS: " + _result.success + " - " + _result.value);
            return false;
        }
    }

    private void backToMenu()
    {
        backToMenu(false);
    }

    private async void backToMenu(bool _showInstertitial, bool _showPopUp = true)
    {
        if (isPlaying && _showPopUp)
        {
            PopUpManager.Instance.setText(LocalizationManager.GetText(LocalizedKeys20Segundos.Atencion),
                LocalizationManager.GetText(LocalizedKeys20Segundos.Abandonar_Forever), typePOPUP.QUESTION, 5, true, typeMSJ.msj,
                LocalizationManager.GetText(LocalizedKeys20Segundos.Si), LocalizationManager.GetText(LocalizedKeys20Segundos.No));

            while (PopUpManager.Instance.isVisible())
            {
                await Task.Delay(10);
            }
            Debug.Log("*****P " + PopUpManager.result);
            if (PopUpManager.result != 1)
            {
                ActivateGameObject.LOCALBACK = true;
                return;
            }
            OnTimerEndAsync(false);
            _showInstertitial = true;
        }

        if (gameObject.activeInHierarchy)
        {
            panelGameplay.SetActive(false);
            panelLeaderBoard.SetActive(false);
            panelMenu.SetActive(true);
        }

        if (_showInstertitial)
        {
            if (_partidasTotalesShowInterstitialCurrent++ % partidasTotalesShowInterstitial == 0)
            {
                if (gameObject.activeInHierarchy)
                {
                    InterstitialAds.Instance.ShowAd();
                }
            }
        }
        SetRewardButton();
    }

    private void SetRewardButton()
    {
        rewardButon.interactable = RewardedAds.isReadyRewards && PowerUp_Item.TICKETS_RESTANTES > 0;
    }

    private void LoadReward()
    {
        if (!RewardedAds.isReadyRewards)
        {
            RewardedAds.Instance.LoadAd();
        }
    }

    public void ShowEventsList()
    {
        PopUpManager.Instance.setText("", "", typePOPUP.OK, 0, false, typeMSJ.msjEventosForever, "", "", 100, false, false);

    }

    public void ShowPlayerStats()
    {
        ShowPlayerStatsAsync();
        LoadReward();
    }

    public async void ShowPlayerStatsAsync()
    {
        await Task.Delay(UIAnimator.defaultDurationAwait);
        Transicion.Instance.playAnimation(ANIMATIONCLIP.Loading, 500);
        await GetPlayerData();
        PopUpManager.Instance.setText("", "", typePOPUP.OK, 0, false, typeMSJ.msjStatsPlayerForever, "", "", 100, false, false);
        Transicion.Instance.playAnimation(ANIMATIONCLIP.FadeOut);
        AnalyticsManager.StartEvent(AnalyticsEventName.Forever, AnalyticsEventParameter.Screen, AnalyticsEventParameterValue.ShowStats);
    }

    public void ShowTutorial(bool _force = false)
    {
        Debug.Log("****t" + LogIn_Register.loadIntgDate(KEY_PLAYERPREF.SHOW_TUTORIAL_FOREVER + "_" + ManagerGame.gameDataPlayer.player.name, 1));
        if (LogIn_Register.loadIntgDate(KEY_PLAYERPREF.SHOW_TUTORIAL_FOREVER, 1) || _force)
        {
            PopUpManager.Instance.setText(0, false, typeMSJ.tutorialForever, false, "", "", false);
            LogIn_Register.saveIntDate(KEY_PLAYERPREF.SHOW_TUTORIAL_FOREVER, 0);
        }
    }

    public void ShowStoreTicket()
    {
        IAP_Store.AUTO_SELECT_ITEM = CuponType.ticket;
        ManagerGame.Instance.showStoreAsync();
        LoadReward();
    }

    public void ShowRewardsPopUp()
    {
        PopUpManager.Instance.setText("", "", typePOPUP.QUESTION, 0, true, typeMSJ.msjRewardsCoinsForever, "SI", "NO", coinsRewards.rewardsCoins, false, false);
        RewardsCoinsRewardsPopUp();
    }
    
    public void RewardsCoinsRewardsPopUp()
    {
        _ = ClainCoinsRewards();
    }
}

[System.Serializable]
public class Respuesta20SegForever
{
    public string _id;
    public string text;
    public bool correct;
    public int sortValue = Random.Range(0, 1000);
}

[System.Serializable]
public class Pregunta20SegForever : Pregunta
{
    public string _id;
    public string question;
    public List<Respuesta20SegForever> options;

    public int score;
    public int level;
    public int time;
    public int category;
    public int order;
    public string createdAt;
    public string updatedAt;
    public int __v;
    public int shortValue = Random.Range(0, 100000);
}

[System.Serializable]
public class Preguntas20SegForeverList
{
    public List<Pregunta20SegForever> pregutasForever;
}

[System.Serializable]
public class CategoryForever
{
    public CATEGORYFOREVER category;
    public VideoClip categoryClip;
}

[System.Serializable]
public class GameStatsForever2
{
    public int scoreTotal;
    public int maxQuestionOk;
    public int maxQuestion;
    public int streak;
    public int maxGames;
    public int timePlayed;
}

[System.Serializable]
public class PlayerStatsGeneral
{
    public string status;
    public string message;
    public GameStatsForever data;
}

[System.Serializable]
public class GameStatsForever
{
    public int streak;
    public int scoreTotal;
    public int maxTime;
    public int maxGames;
    public int maxQuestionOk;
    public int maxQuestion;
}

[System.Serializable]
public class CoinRewardsData
{
    public string userId;
    public int coins;
    public int coinsReward;
    public int totalPotential;
}

[System.Serializable]
public class CoinsClaimResponse
{
    public string message;
    public string userId;
    public int coinsClamed;
    public int newCoinsTotal;
    public int coinsReward;
}


/*using DG.Tweening;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;


public enum CATEGORYFOREVER
{
CulturaGeneral,
VideoJuegos,
Cine,
Deportes,
Series,
Comics,
MúsicaArgentina,
MúsicaInternacional,
Tecnología,
Fútbol,
CulturaPopArgentina,
}

public class GamePlay20SegForever : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private TextMeshProUGUI pregunta;
    [SerializeField] private TextMeshProUGUI preguntaCount;
    [SerializeField] private CanvasGroup respuestasPanel;
    [SerializeField] private PanelStatusGame statusPanel;
    [SerializeField] private Respuestas[] respuestasButton;
    [SerializeField] private GameObject panelMenu;
    [SerializeField] private GameObject panelGameplay;
    [SerializeField] private GameObject panelLeaderBoard;

    [SerializeField] private VideoPlayer playerIntroForever;
    [SerializeField] private VideoClip clipIntroForever;

    [SerializeField] private VideoPlayer preview;
    [SerializeField] private VideoPlayer preview2;
    private bool alternatePreview = true;
    [SerializeField] private CategoryForever[] categories;

    [SerializeField] private GameObject rewardCoinsButon;
    [SerializeField] private Button rewardButon;
    
    private QuestionsForeverResponse preguntas20SegForeverList;
    private QuestionsForeverResponse preguntas20SegForeverListTMP;
    private int indexPregunta;
    [Tooltip("Numero de preguntas ok en el nivel actual")]
    [SerializeField] private int indexPreguntaOk;
    [Tooltip("Numero de preguntas ok en la partida actual")]
    [SerializeField] private int indexPreguntaTotalOk;
    [Tooltip("Numero de preguntas que debe responder bien para pasar al siguiente nivel")]
    [SerializeField] private int maxLevelQuestionFinal = 10;
    [SerializeField] private int maxLevelQuestion = 10;
    [Tooltip("Niveles maximos alcanzables")]
    [SerializeField] private int maxLevel = 3;
    [Tooltip("Numero de preguntas descargada en cada consulta")]
    [SerializeField] private int maxQuestionRequest;
    [SerializeField] private int segundosRespOk = 8;
    [SerializeField] private int segundosRespBad = 5;

    [SerializeField] private int minQuestionToCaptcha = 10;
    [SerializeField] private int nexQuestionToCaptcha = 0;
    //[SerializeField] private int minQuestionToCaptcha = 10;

    private int _level = 1;
    private int _category = 1;
    public static GamePlay20SegForever Instance;

    public static string TEMATICA_ID;
    public static string TEMATICA_NAME;
    public static string TEMATICA_DESC;
    public static Sprite TEMATICA_ICON;
    public static Sprite TEMATICA_BANNER;
    public static bool IS20SEG;
    [SerializeField] private Image iconGameForever;



    private Coroutine _coroutineGamePlay;
    private bool reloadPreguntas = false;
    private bool isPlaying = false;
    private float timePartida;
    private System.DateTime startPartida;
    private int score = 100;
    private int rewardMax = 6;
    private int rewardTotal;
    private int partidasTotales;
    private CoinsResponse coinsRewards;

    public static System.TimeSpan TIMEPARTIDA;
    public static int TOTALQUESTION;
    public static int TOTALQUESTIONOK;
    public static int HIGSCORE;
    public static bool IsHIGSCORE;
    
    public static GameStatsForever PLAYERDATAFOREVERSTATS;

    [SerializeField] private int partidasTotalesShowReward = 2;    
    [SerializeField] private int partidasTotalesShowInterstitial = 2;    
    private int _partidasTotalesShowInterstitialCurrent;

    private void Awake()
    {
        Instance = this;
        minQuestionToCaptcha = LoginManager.GetGlobalIntVar(GLOBALVARS.ForeverMinQuestionForCaptcha);
    }
    public async void PlayGame()
    {

        await Task.Delay(UIAnimator.defaultDurationAwait);
        if (!ManagerGame.Instance.UseTicket())
        {
            return;
        }

        //OnApplicationFocus(true);
        maxLevelQuestion = maxLevelQuestionFinal -1;
        indexPregunta = 0;
        indexPreguntaTotalOk = indexPreguntaOk = 0;
        _level = 1;
        StopGamePlay(false);
        _coroutineGamePlay = StartCoroutine(startGame());
        Timer.Instance.setTimer(ManagerGame.timeRespuesta, false, false);
        ManagerGame.setStatus(STATUSGAME.FOREVER);
        Transicion.Instance.playAnimation(ANIMATIONCLIP.Loading, 1000);
        panelMenu.SetActive(false);
        panelGameplay.SetActive(!panelMenu.activeSelf);
        startPartida = System.DateTime.Now;
        partidasTotales++;        
    }

    public void SetForeverTematica(bool _leaderboard = false)
    {
        iconGameForever.overrideSprite = TEMATICA_ICON;
        if (!_leaderboard)
        {
            panelMenu.SetActive(true);
        }
    }

    [ContextMenu("Stop")]
    private void StopGamePlayTest()
    {
        AnalyticsManager.StartEvent(AnalyticsEventName.Forever, AnalyticsEventParameter.ForeverGamePlay, AnalyticsEventParameterValue.Abandonar);
        OnTimerEndAsync(true);
    }

    private void StopGamePlay(bool _saveGame = true)
    {
        if (_coroutineGamePlay != null)
        {
            Timer.Instance.StopTimer();
            StopCoroutine(_coroutineGamePlay);
        }
        
        _coroutineGamePlay = null;
        isPlaying = false;
        
        if(partidasTotales == partidasTotalesShowReward)
        {
          //  rewardButon.interactable = true;
        }

        if (_saveGame)
        {
            IsHIGSCORE = false;
            if (TOTALQUESTIONOK > HIGSCORE)
            {
                HIGSCORE = TOTALQUESTIONOK;
                IsHIGSCORE = true;
            }
            _ = SetPartida(TOTALQUESTION, TOTALQUESTIONOK, (TIMEPARTIDA = System.DateTime.Now.Subtract(startPartida)).TotalSeconds, score);
        }

    }
    public async void ShowLeaderBoard()
    {
        await Task.Delay(UIAnimator.defaultDurationAwait);
        panelLeaderBoard.SetActive(true);
        panelMenu.SetActive(false);
        LoadReward();
        AnalyticsManager.StartEvent(AnalyticsEventName.Forever, AnalyticsEventParameter.Screen, AnalyticsEventParameterValue.ShowLeaderBoard);
    }
    public void BuyTickets()
    {

    }
    private void SetPreguntaIndex(int _value)
    {
        preguntaCount.text = (_value).ToString();
    }
    private void OnEnable()
    {
        if (LoginManager.GetGlobalVarBool(GLOBALVARS.maintenanceForever))
        {
            ActivateGameObject.Instance.Mantenimiento();
            return;
        }
        AnalyticsManager.StartEvent(AnalyticsEventName.Game, AnalyticsEventParameter.Screen, AnalyticsEventParameterValue.ForeverHome);
        Timer.Instance.StopTimer();
        PlayIntro();
        Timer.OnTimerEnd += OnTimerEndAsync;
        ActivateGameObject.OnLocalBackPress += backToMenu;
        RewardedAds.Instance.LoadAd(true);
        panelMenu.SetActive(false);
        _ = GetPlayerData();
        ShowTutorial();
    }
    public async void ShowReward()
    {
        await Task.Delay(UIAnimator.defaultDurationAwait);
        RewardedAds.Instance.ShowAd();
        //rewardButon.interactable = false;
        partidasTotales = 0;
        rewardTotal++;
    }
    private async void PlayIntro()
    {
        playerIntroForever.GetComponent<CanvasGroup>().alpha = 1.0f;
        playerIntroForever.gameObject.SetActive(true);
        playerIntroForever.clip = clipIntroForever;
        playerIntroForever.Play();
        
        while (!playerIntroForever.isPlaying)
        {
            await Task.Delay(100);
        }

            while (gameObject.activeSelf && playerIntroForever.isPlaying)
        {
            await Task.Delay(10);
        }

        playerIntroForever.GetComponent<CanvasGroup>().DOFade(0, 0.5f).OnComplete(() =>
        {
        playerIntroForever.gameObject.SetActive(false);
        });
        
        //Transicion.Instance.playAnimation(ANIMATIONCLIP.Loading, 500);
        //Transicion.Instance.playAnimation(ANIMATIONCLIP.FadeOut);
    }
    private void setPreview(CategoryForever _category)
    {
        VideoPlayer _preview, _preview2;

        if(alternatePreview)
        {
            _preview = preview;
            _preview2 = preview2;
        }
        else
        {
            _preview = preview2;
            _preview2 = preview;
        }

        alternatePreview = !alternatePreview;
        _preview2.clip = _category.categoryClip;

        _preview.GetComponent<RawImage>().DOFade(0 , 0.5f);
        _preview2.GetComponent<RawImage>().DOFade(1, 0.5f);
    }
    private void OnDisable()
    {
        Timer.OnTimerEnd -= OnTimerEndAsync;
        ActivateGameObject.OnLocalBackPress -= backToMenu;
        StopGamePlay(isPlaying);
    }
    private IEnumerator startGame()
    {
        AnalyticsManager.StartEvent(AnalyticsEventName.Forever, AnalyticsEventParameter.ForeverGamePlay, AnalyticsEventParameterValue.Iniciar);
        
        LoadReward();
        InterstitialAds.Instance.LoadAd();
        isPlaying = true;
        TOTALQUESTIONOK = TOTALQUESTION = 0;
        SetPreguntaIndex(0);
        var task = GetQuestions(_level);
        yield return new WaitUntil(() => task.IsCompleted);
        nexQuestionToCaptcha = minQuestionToCaptcha + Random.Range(1, minQuestionToCaptcha);
        if (Transicion.Instance.IsActive())
        {
            Transicion.Instance.playAnimation(ANIMATIONCLIP.FadeOut, 10);
        }
        //reloadPreguntas = true;
        if (task.Result)
        {
            Timer.Instance.setTimer(ManagerGame.timeRespuesta, true, true);
            preguntas20SegForeverList = preguntas20SegForeverListTMP;

            while (true)
            {
                Debug.Log("Bloque de Preguntas cargadas correctamente");

                if (TOTALQUESTION > minQuestionToCaptcha && nexQuestionToCaptcha == TOTALQUESTION)
                {
                    Debug.Log($"ENTRO AL CAPTCHA {minQuestionToCaptcha} {nexQuestionToCaptcha} {TOTALQUESTION}");
                    nexQuestionToCaptcha = TOTALQUESTION + Random.Range(minQuestionToCaptcha, minQuestionToCaptcha * 2);

                    Timer.PAUSE_TIMER = true;
                    bool captchaResult = false;

                    // Ejecutar la tarea async
                    var taskCaptcha = LoginManager.Instance.GetCaptchaAsync(false);

                    // Esperar hasta que se complete
                    yield return new WaitUntil(() => taskCaptcha.IsCompleted);

                    // Obtener el resultado
                    captchaResult = taskCaptcha.Result;

                    Debug.Log("Resultado del captcha: " + captchaResult);

                    if (!captchaResult)
                    {
                        AnalyticsManager.StartEvent(AnalyticsEventName.Forever, AnalyticsEventParameter.ForeverGamePlay, AnalyticsEventParameterValue.DescalificadoCaptcha);
                        OnTimerEndAsync(true);
                        yield break;
                    }
                    else
                    {
                        Timer.PAUSE_TIMER = false;
                    }
                }
                if (setPregunta(preguntaActual()))
                {
                    yield return new WaitWhile(() => !Respuestas.isSelectResp);

                    bool result;
                    TOTALQUESTION++;

                    if (result = preguntaActual().showResultForever())
                    {
                        Timer.Instance.addTimer(segundosRespOk);
                        indexPreguntaOk++;
                        indexPreguntaTotalOk++;
                        TOTALQUESTIONOK++;
                    }
                    else
                    {
#if UNITY_EDITOR
                        Timer.Instance.addTimer(0);
#endif
                        Timer.Instance.addTimer(-segundosRespBad);
                    }
                    _ = setRespuesta(preguntaActual()._id, result);

                    yield return new WaitForSeconds(.75f);

                    preguntaActual().resetRespuestas();

                    if (reloadPreguntas)
                    {
                        preguntas20SegForeverList = preguntas20SegForeverListTMP;
                        indexPregunta = 0;
                        reloadPreguntas = false;
                    }
                    else
                    {
                        indexPregunta++;
                    }
                    SetPreguntaIndex(indexPreguntaTotalOk);

                    Debug.Log("*****q " + (indexPregunta - 2) + "==" + preguntas20SegForeverList.questions.Count);
                }
                else
                {
                    indexPregunta++;
                }
                if (indexPreguntaOk >= maxLevelQuestion || (indexPregunta) == preguntas20SegForeverList.questions.Count - 2)
                {
                    if (indexPreguntaOk >= maxLevelQuestion)
                    {
                        if (maxLevelQuestion == 8)
                        {
                            maxLevelQuestion -= 2;
                        }
                        else if (maxLevelQuestion == 6)
                        {
                            maxLevelQuestion -= 1;
                        }
                        else if (maxLevelQuestion == 5)
                        {
                            maxLevelQuestion--;
                        }
                    }
                    _ = GetQuestions(indexPreguntaOk >= maxLevelQuestion ? ++_level : _level, false);
                    indexPreguntaOk = 0;
                    reloadPreguntas = true;
                }
            }
        }
        else
        {
            Debug.LogError("Falló la carga de preguntas");
        }
    }
    public async Task<bool> setRespuesta(string _idRespuesta, bool _isCorrect)
    {
        var _result = await MySqlManager.SetQuestions20SegForever(_idRespuesta, _isCorrect, 100,100 );

        if (_result.success)
        {
            Debug.Log("OBTENEMOS LAS PREGUNTAS: " + _result.success + " - " + _result.value + "****\n");
            return true;
        }
        else
        {
            Debug.Log("ERROR EN LA OBTENCION DE COIS: " + _result.success + " - " + _result.value);

            if (await PopUpManager.Instance.ShowKickPopUpAsync())
            {
                OnTimerEndAsync(true);
            }
            return false;
        }
    }
    public void OnTimerEndAsync()
    {
        AnalyticsManager.StartEvent(AnalyticsEventName.Forever, AnalyticsEventParameter.ForeverGamePlay, AnalyticsEventParameterValue.Perder);
        if (gameObject.activeInHierarchy)
        {
            OnTimerEndAsync(true);
        }
    }
    public void OnTimerEndAsync(bool _showPopUp = true)
    {
        StopGamePlay();
        if (_showPopUp)
        {
            showPopUpEndGame();
        }
    }
    public async void showPopUpEndGame()
    {
        //PopUpManager.Instance.setText("Game Over", "Partida finalizada", typePOPUP.OK, 0);
        Debug.Log(ManagerGame.gameDataPlayer.evento);
        if(ManagerGame.gameDataPlayer.evento == null)
        {
            ManagerGame.gameDataPlayer.evento = new EventoData();
        }

        PopUpManager.Instance.setText(  "", "" + ManagerGame.gameDataPlayer.evento.puntuacionObtenidaEvento,
                                        typePOPUP.OK,0, false, typeMSJ.msjEndForever, "", "", 100, false, false);
        while (PopUpManager.result == 0)
        {
            await Task.Delay(10);            
        }
        backToMenu(true);
        preguntaActual().resetRespuestas();

    }
    public QuestionForeverData preguntaActual()
    {
        Debug.Log("*****R" + preguntas20SegForeverList.questions.Count + " - " + indexPregunta); 
        return preguntas20SegForeverList.questions[indexPregunta];
    }
    string lastIdQuestion;
    public bool setPregunta(QuestionForeverData _pregunta)
    {
        if(_pregunta._id.Equals(lastIdQuestion))
        {
            return false;
        }
        lastIdQuestion = _pregunta._id;
        ManagerGame.setStatus(STATUSGAME.WAITING_ASNWER_PLAYER);

        pregunta.text = _pregunta.question;

        int offset = 0;// gameDataPlayer.evento.respuestasOk.Count == 4 ? 0 : 4; //BUG
        offset = 0;
        var cat = GetCategory(_pregunta.category);

        if (cat != null)
        {
            Debug.Log("Encontrado: " + cat.category);
            setPreview(cat);
            
        }
        else
        {
            Debug.Log("No se encontró la categoría");
        }


        //setPreview(categories[_pregunta.category-1]);//REVERT Parsear las categorias para que sean las mismas
        //setPreview(categories[Random.Range(0, categories.Length)]);
        //setPreview(categories[Random.Range(0, 2)]);

        //_ = _pregunta.options.OrderBy(x => Random.value).ToList();

        _pregunta.options.Sort((a, b) => a.sortValue.CompareTo(b.sortValue));


        for (int i = 0; i < _pregunta.options.Count; i++)
        {
            if(_pregunta.options[i].correct)
            {
                _pregunta.idRespuestaCorrecta = _pregunta.options[i]._id;
            }
            respuestasButton[i + offset].setRespuesta(_pregunta.options[i]);
            _pregunta.respuestasButton.Add(respuestasButton[i + offset]);
        }
        return true;
    }

    private CategoryForever GetCategory(string input)
    {
        // 1. Convertir string → enum
        if (!System.Enum.TryParse<CATEGORYFOREVER>(input, true, out var categoriaEnum))
            return null;
        Debug.Log("****f Encontro " + categoriaEnum);
        // 2. Buscar en el array
        var result = categories.FirstOrDefault(c => c.category == categoriaEnum);
        Debug.Log("****f Encontro " + result.categoryClip);
        return result;
    }
    public async Task<bool> GetQuestions(int _level, bool _transition = true)
    {
        if (_level > maxLevel)
        {
            _level = maxLevel;
            _level = Random.Range(0, 10) < 5 ? maxLevel : maxLevel - 1;
        }        
        var _result = await MySqlManager.GetQuestions20SegForeverFilter(_level, maxQuestionRequest,_category, null, TEMATICA_NAME, true, _transition);
        Debug.Log("****ME TRAJO\n" + _result.value);


        if (_result.success)
        {
           //_result.value = "{ \"pregutasForever\":" + _result.value + "}";

            //#if UNITY_EDITOR            
            //          value = "{ \"pregutasForever\":" + (Random.Range(0,10) > 5 ? list12 : list1) + "}";
            //        preguntas20SegForeverListTMP = JsonUtility.FromJson<Preguntas20SegForeverList>(value);
            //#else
        preguntas20SegForeverListTMP = JsonUtility.FromJson<QuestionsForeverResponse>(_result.value); 

//#endif
            if (preguntas20SegForeverListTMP != null)
            {
                if(preguntas20SegForeverListTMP.questions != null &&
                    preguntas20SegForeverListTMP.questions.Count < 10)
                {
                    OnTimerEndAsync(true);
                    return true;
                }
            }
            preguntas20SegForeverListTMP.questions.Sort((a, b) => a.shortValue.CompareTo(b.shortValue));
            Debug.Log("20FOrever OBTENEMOS LAS PREGUNTAS: " + _result.success + " - " + _result.value + "****\n" + preguntas20SegForeverListTMP.questions.Count + " - " + preguntas20SegForeverListTMP.questions[0].question);
            return true;

        }
        else
        {
            Debug.Log("20FOrever ERROR EN LA OBTENCION DE PREGUNTAS: " + _result.success + " - " + _result.value);
            if (await PopUpManager.Instance.ShowKickPopUpAsync())
            {
                OnTimerEndAsync(true);
            }
            return false;
        }
    }
    public async Task<bool> GetPlayerData(bool _transicion = false)
    {        
        var _result = await MySqlManager.GetPlayerDataForever(_transicion);

        if (_result.success)
        {
            PlayerStatsGeneral response = JsonConvert.DeserializeObject<PlayerStatsGeneral>(_result.value);
            PLAYERDATAFOREVERSTATS = response.data;
            if (PLAYERDATAFOREVERSTATS != null)
            {
                HIGSCORE = PLAYERDATAFOREVERSTATS.streak;
                Debug.Log("*****h" + PLAYERDATAFOREVERSTATS.streak + " - " + PLAYERDATAFOREVERSTATS.maxQuestionOk + " - " + _result.value);

            }
            _ = GetCoinsRewards();
            return true;
        }
        else
        {
            Debug.Log("ERROR EN LA OBTENCION DE DATA PLAYER: " + _result.success + " - " + _result.value);
            _ = GetCoinsRewards();
            return false;
        }
        
    }
    public async Task<bool> SetPartida(int _respuestasTotales, int _respuestasOk, double _time, int _score)
    {        
        var _result = await MySqlManager.SetPartidaForever(TEMATICA_ID, _respuestasTotales, _respuestasOk, (int)_time, _score);

        if (_result.success)
        {
            Debug.Log("PARTIDA GUARDADA: " + _result.success + " - " + _result.value);

            return true;
        }
        else
        {
            Debug.Log("PARTIDA NO GUARDADA: " + _result.success + " - " + _result.value);

            return false;
        }
    } 
    public async Task<bool> GetCoinsRewards()
    {        
        var _result = await MySqlManager.GetCoinsRewards();

        if (_result.success)
        {
            coinsRewards = JsonUtility.FromJson<CoinsResponse>(_result.value);
   
            Debug.Log("OBTENEMOS LAS COINS REWARDS: " + _result.success + " - " + _result.value + " - count: " + coinsRewards.rewardsCoins +"\n");
            rewardCoinsButon.SetActive(coinsRewards.rewardsCoins > 0);
            return true;
        }
        else
        {
            Debug.Log("ERROR EN LA OBTENCION DE COINS REWARDS: " + _result.success + " - " + _result.value);
            return false;
        }
    }
    public async Task<bool> ClainCoinsRewards()
    {
        var _result = await MySqlManager.ClainCoinsRewards();

        if (_result.success)
        {
            Debug.Log("Agregamos coins : " + _result.success + " - " + _result.value + " - count: ");
            CoinsResponse response = JsonUtility.FromJson<CoinsResponse>(_result.value);

            ManagerGame.gameDataPlayer.player.Coins = (int.Parse(ManagerGame.gameDataPlayer.player.coins) + response.rewardsCoins).ToString();
            rewardCoinsButon.SetActive(false);
            return true;
        }
        else
        {
            Debug.Log("ERROR AGREGAR COINS: " + _result.success + " - " + _result.value);
            return false;
        }
    }
    private void backToMenu()
    {
        backToMenu(false);
    }
    private async void backToMenu(bool _showInstertitial, bool _showPopUp = true)
    {
        if(isPlaying && _showPopUp)
        {
            PopUpManager.Instance.setText("Atención", "Perderas el Ticket si abandonas la partida. ¿Salir?", typePOPUP.QUESTION, 5, true, typeMSJ.msj, "SI", "NO");//salir del juego

            while (PopUpManager.Instance.isVisible())
            {
                await Task.Delay(10);
            }
            Debug.Log("*****P " + PopUpManager.result);
            if (PopUpManager.result != 1)
            {
                ActivateGameObject.LOCALBACK = true;
                return;
            }
            OnTimerEndAsync(false);
            _showInstertitial = true;
        }

        if (gameObject.activeInHierarchy)
        {
            panelGameplay.SetActive(false);
            panelLeaderBoard.SetActive(false);
            panelMenu.SetActive(true);            
        }
        //await Task.Delay(10);

        if (_showInstertitial)
        {
            if (_partidasTotalesShowInterstitialCurrent++ % partidasTotalesShowInterstitial == 0)
            {
                if (gameObject.activeInHierarchy)
                {
                    InterstitialAds.Instance.ShowAd();
                }
            }
        }
        SetRewardButton();
    }
    private void SetRewardButton()
    {
        
        rewardButon.interactable = RewardedAds.isReadyRewards && PowerUp_Item.TICKETS_RESTANTES > 0;

    }
    private void LoadReward()
    {
        if (!RewardedAds.isReadyRewards)
        {
            RewardedAds.Instance.LoadAd();
        }
    }
    public void ShowPlayerStats()
    {
        ShowPlayerStatsAsync();
        LoadReward();
    }
    public async void ShowPlayerStatsAsync()
    {
        await Task.Delay(UIAnimator.defaultDurationAwait);
        Transicion.Instance.playAnimation(ANIMATIONCLIP.Loading, 500);
        await GetPlayerData();
        //PopUpManager.Instance.setText("", "" + ManagerGame.gameDataPlayer.evento.puntuacionObtenidaEvento, typePOPUP.OK, 0, false, typeMSJ.msjStatsPlayerForever, "", "", 100, false, false);
        PopUpManager.Instance.setText("", "", typePOPUP.OK, 0, false, typeMSJ.msjStatsPlayerForever, "", "", 100, false, false);//REVERT ESTABA COMO ARRIBA
        Transicion.Instance.playAnimation(ANIMATIONCLIP.FadeOut);
        AnalyticsManager.StartEvent(AnalyticsEventName.Forever, AnalyticsEventParameter.Screen, AnalyticsEventParameterValue.ShowStats);

    }
    public void ShowTutorial(bool _force = false)
    {
        Debug.Log("****t" + LogIn_Register.loadIntgDate(KEY_PLAYERPREF.SHOW_TUTORIAL_FOREVER + "_" + ManagerGame.gameDataPlayer.player.name, 1));
        if (LogIn_Register.loadIntgDate(KEY_PLAYERPREF.SHOW_TUTORIAL_FOREVER, 1) || _force)
        {
            PopUpManager.Instance.setText(0, false, typeMSJ.tutorialForever, false, "", "", false);
            LogIn_Register.saveIntDate(KEY_PLAYERPREF.SHOW_TUTORIAL_FOREVER, 0);
        }
    }
    public void ShowStoreTicket()
    {
        IAP_Store.AUTO_SELECT_ITEM = CuponType.ticket;
        ManagerGame.Instance.showStoreAsync();
        LoadReward();
    }
    public void ShowRewardsPopUp()
    {
        PopUpManager.Instance.setText("", "", typePOPUP.QUESTION, 0, true, typeMSJ.msjRewardsCoinsForever, "SI", "NO", coinsRewards.rewardsCoins, false, false);//salir del juego
        RewardsCoinsRewardsPopUp();
    }
    public void RewardsCoinsRewardsPopUp()
    {
        _ = ClainCoinsRewards();
    
    }



}

[System.Serializable]
public class Respuesta20SegForever
{
    public string _id;
    public string text;
    public bool correct;
    public int sortValue = Random.Range(0, 1000);
}

[System.Serializable]
public class Pregunta20SegForever : Pregunta
{
    public string _id;
    public string question;
    public List<Respuesta20SegForever> options;
    //public List<Respuestas> respuestasButton = new List<Respuestas>();

    public int score;
    public int level;
    public int time;
    public int category;
    public int order;
    public string createdAt;
    public string updatedAt;
    public int __v;
    public int shortValue = Random.Range(0,100000);
}


[System.Serializable]
public class Preguntas20SegForeverList
{
    public List<Pregunta20SegForever> pregutasForever;
}



[System.Serializable]
public class CategoryForever
{
    public CATEGORYFOREVER category;
    public VideoClip categoryClip;
}

/////////////////////////////////////PLAYER DATA
[System.Serializable]
public class GameStatsForever2
{
    public int scoreTotal;
    public int maxQuestionOk;
    public int maxQuestion;
    public int streak;
    public int maxGames;
    public int timePlayed;
}




[System.Serializable]
public class PlayerStatsGeneral
{
    public string status;
    public string message;
    public GameStatsForever data;
}

[System.Serializable]
public class GameStatsForever
{
    public int streak;
    public int scoreTotal;
    public int maxTime;
    public int maxGames;
    public int maxQuestionOk;
    public int maxQuestion;
}

/////////////////////////////////////////////COINS REWARDS
///
[System.Serializable]
public class CoinRewardsData
{
    public string userId;
    public int coins;
    public int coinsReward;
    public int totalPotential;
}

[System.Serializable]
public class CoinsClaimResponse
{
    public string message;
    public string userId;
    public int coinsClamed;
    public int newCoinsTotal;
    public int coinsReward;
}

*/