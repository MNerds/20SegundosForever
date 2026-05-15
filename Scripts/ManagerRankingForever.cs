using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ManagerRankingForever : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private int countItems;
    [SerializeField] private GameObject classicPanel;
    [SerializeField] private GameObject customPanel;
    [SerializeField] private Image customForever_IMG;
    [SerializeField] private Image classicForever_IMG;
    [SerializeField] private TextMeshProUGUI timeClose;
    [SerializeField] private TextMeshProUGUI dateLeaderBoard;
    [SerializeField] private RankingForeverItem item;
    [SerializeField] private Transform panelRanking;
    [SerializeField] private GameObject premios_BTN;
    [SerializeField] private GameObject timer_BTN;
    [SerializeField] private GameObject nextDailyRanking_BTN;
    [SerializeField] private GameObject prevDailyRanking_BTN;
    [SerializeField] private System.DateTime horaCierre;
    [SerializeField] UnityEngine.UI.Toggle dailyToggle;
    private RankingDataForeverResponse rankingForeverList;
    private System.DateTime timeServer;
    private System.DateTime timeCierreServer;
    private System.DateTime currentTime;
    private float _offsetTime = 0;
    private float _daysOffset = 0;
    
    public void OnEnable()
    {
        classicPanel.SetActive(GamePlay20SegForever.IS20SEG);
        customPanel.SetActive(!classicPanel.activeSelf);
        _daysOffset = 0;
        customForever_IMG.overrideSprite = customForever_IMG.overrideSprite = GamePlay20SegForever.TEMATICA_BANNER;
        GetRanking(!dailyToggle.isOn || !GamePlay20SegForever.IS20SEG);        
    }
     
    private void Update()
    {
        _offsetTime += Time.deltaTime;
        currentTime = timeServer.AddSeconds(_offsetTime);
        var rest = timeCierreServer.Subtract(currentTime);
        timeClose.text = rest.Hours.ToString("D2") + ":" + rest.Minutes.ToString("D2") + ":" + rest.Seconds.ToString("D2");
    }

    public async void GetRanking(bool _general)
    {
        CanvasManager.LOG("1-GetRanking()", true);
        GetRanking(_general, 0);
    }
    public async void GetRanking(bool _general, int _days = 0)
    {
        Transicion.Instance.playAnimation(ANIMATIONCLIP.Loading, 500);
        premios_BTN.SetActive(!_general && LoginManager.GetGlobalVarBool(GLOBALVARS.forever_showPremios));
        timer_BTN.SetActive(!_general && false);


        timeServer = await ClockServer.getTimeServer();

        timeCierreServer = new System.DateTime(timeServer.Year, timeServer.Month, timeServer.Day, 23, 59, 00);


        if (_days == 0)
        {
            _daysOffset = 0;
        }
        _daysOffset += _days;

        nextDailyRanking_BTN.SetActive(!(_daysOffset == 0 || _days == 0));
        prevDailyRanking_BTN.SetActive((_daysOffset > -LoginManager.GetGlobalIntVar(GLOBALVARS.ForeverMaxDaysRanking)));
        if (_general)
        {
            _daysOffset = 0;
        }
        else if (_daysOffset <= 0)
        {
            timeServer = timeServer.AddDays(_daysOffset);
        }
        
        _ = GetRankingForever(_general, timeServer);
        dateLeaderBoard.text = timeServer.Day + "-" + timeServer.Month + "-" + timeServer.Year;
        _offsetTime = 0;
    }

    public async Task<bool> GetRankingForever(bool _general, System.DateTime _date)
    {
        CanvasManager.LOG("4-GetRankingForever()", true);

        bool success;
        string value;
        if (_general)
        {
            var _result = await MySqlManager.GetRanking(GamePlay20SegForever.TEMATICA_NAME , countItems);
            success = _result.success;
            value = _result.value;
        }
        else
        {
            var _result = await MySqlManager.GetRankingDaily(GamePlay20SegForever.TEMATICA_NAME, countItems, _date);
            success = _result.success;
            value = _result.value;
        }

        if (success)
        {
            rankingForeverList = JsonUtility.FromJson<RankingDataForeverResponse>(value);
            // Accediendo a los datos
            /*
            foreach (var entry in rankingForeverList.ranking)
            {
                Debug.Log($"User {entry.userId} - Score: {entry.score}");
            }*/
            showRanking();
            return true;
        }
        else
        {
            Debug.Log("ERROR EN LA OBTENCION DE COIS: " + success + " - " + value);
            return false;
        }
    }

    private void showRanking()
    {
        foreach (Transform t in panelRanking)
        {
            Destroy(t.gameObject);
        }

        string myUserId = ManagerGame.gameDataPlayer.player.idPlayer;

        foreach (var itemRanking in rankingForeverList.ranking)
        {
            // Si querés filtrar streak:
            Debug.Log("****R " + itemRanking.name + " - " + itemRanking.streak);
            if (itemRanking.streak <= 0)
                continue;

            RankingForeverItem itemTMP = Instantiate(item, panelRanking);

            bool isMe = myUserId.Equals(itemRanking.userId);

            itemTMP.SetInfo(itemRanking, isMe);
        }

        panelRanking.transform.localPosition = new Vector3(
            panelRanking.transform.localPosition.x,
            0,
            panelRanking.transform.localPosition.z
        );
    }

    public void ShowPremios()
    {
        PopUpManager.Instance.setText(0, true, typeMSJ.msjPremiosForever, true);
    }

    public void NextDay(bool _next)
    {
        GetRanking(false, _next ? 1 : -1);
    }
}




[System.Serializable]
public class RankingForeverPlayers
{
    public int position;
    public string playerId;
    public string userId;
    public string name;
    public int avatar;
    public int streak;
    public int score;
    public int totalCorrect;
    public int totalAnswered;
}
/*
[System.Serializable]
public class RankingForeverPlayers
{
    public string userId;
    public int totalScore;
    public int totalCorrect;
    public int totalAnswered;
    public int maxStreak;
}
*/
[System.Serializable]
public class RankingDataForeverResponse
{
    public string status;
    public string message;
    public List<RankingForeverPlayers> ranking;
    public int userPosition;    
}
