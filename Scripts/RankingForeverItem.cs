using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RankingForeverItem : MonoBehaviour
{
    [SerializeField] private Image backGround_IMG;
    [SerializeField] private Sprite backGroundPlayer;
    [SerializeField] private Sprite backGroundWinner;
    [SerializeField] private Sprite backGroundOther;
    [SerializeField] private Image iconPlayer;
    [SerializeField] private TextMeshProUGUI namePlayer_TMP;
    [SerializeField] private TextMeshProUGUI posPlayer_TMP;
    [SerializeField] private TextMeshProUGUI scorePlayer_TMP;

    public void SetInfo(RankingForeverPlayers itemTMP, bool _isPlayer)
    {
        namePlayer_TMP.text = itemTMP.name;
        posPlayer_TMP.text = itemTMP.position.ToString();
        scorePlayer_TMP.text = itemTMP.streak.ToString();
        ProfileManager.ParseAvatarFecha(itemTMP.avatar, out int avatar, out DateTime fecha, out int fechaInt);
        iconPlayer.sprite = avatar <= 0 ? ManagerGame.Instance.avatarsList[UnityEngine.Random.Range(1, ManagerGame.Instance.avatarsList.Length)] : ManagerGame.Instance.avatarsList[avatar];
        if(_isPlayer)
        {
            backGround_IMG.sprite = backGroundPlayer;
        }
        else if(itemTMP.position < 6)
        {
            backGround_IMG.sprite = backGroundWinner;
        }
    }

}
