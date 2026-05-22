using System;
using TMPro;
using UnityEngine;

public class StatsForever : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI userName_TMP;
    [SerializeField] private UnityEngine.UI.Image gameTematica_IMG;
    [SerializeField] private UnityEngine.UI.Image gameTematicaLogo_IMG;
    [SerializeField] private UnityEngine.UI.Image userAvatar_IMG;
    [SerializeField] private TextMeshProUGUI partidasJugadas_TMP;
    [SerializeField] private TextMeshProUGUI tiempoSobrevivido_TMP;
    [SerializeField] private TextMeshProUGUI respuestasOK_TMP;
    [SerializeField] private TextMeshProUGUI respuestasBAD_TMP;
    [SerializeField] private TextMeshProUGUI totalPreguntas_TMP;
    [SerializeField] private TextMeshProUGUI totalRachas_TMP;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void OnEnable()
    {
        GetStatsPlayer();
    }

    private async void GetStatsPlayer()
    {
        //await GamePlay20SegForever.GetPlayerData(true);
        userName_TMP.text = ManagerGame.gameDataPlayer.player.name;
        int avatar = ManagerGame.gameDataPlayer.player.Avatar;
        userAvatar_IMG.sprite = avatar <= 0 ? ManagerGame.Instance.avatarsList[UnityEngine.Random.Range(1, ManagerGame.Instance.avatarsList.Length)] : ManagerGame.Instance.avatarsList[avatar];
        //gameTematica_IMG.sprite = GamePlay20SegForever.TEMATICA_BANNER;
        //gameTematicaLogo_IMG.sprite = GamePlay20SegForever.TEMATICA_ICON;
        if (GamePlay20SegForever.PLAYERDATAFOREVERSTATS != null)
        {
            partidasJugadas_TMP.text = GamePlay20SegForever.PLAYERDATAFOREVERSTATS.maxGames.ToString("D2");
            
            int totalSeconds = GamePlay20SegForever.PLAYERDATAFOREVERSTATS.maxTime;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            tiempoSobrevivido_TMP.text = minutes.ToString("D2") + ":" + seconds.ToString("D2");

            respuestasOK_TMP.text = GamePlay20SegForever.PLAYERDATAFOREVERSTATS.maxQuestionOk.ToString("D2");
            respuestasBAD_TMP.text = (GamePlay20SegForever.PLAYERDATAFOREVERSTATS.maxQuestion - GamePlay20SegForever.PLAYERDATAFOREVERSTATS.maxQuestionOk).ToString("D2");
            totalPreguntas_TMP.text = GamePlay20SegForever.PLAYERDATAFOREVERSTATS.streak.ToString("D2");
            totalRachas_TMP.text = GamePlay20SegForever.PLAYERDATAFOREVERSTATS.maxQuestion.ToString("D2");
            
        }
    }
}
