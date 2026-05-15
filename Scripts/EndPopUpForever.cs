using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndPopUpForever : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timePartida;
    [SerializeField] private TextMeshProUGUI totalQuestion;
    [SerializeField] private TextMeshProUGUI totalQuestionOk;
    [SerializeField] private TextMeshProUGUI higScore;
    [SerializeField] private Image bannerCustomForever;
    [SerializeField] private UnityEngine.UI.Image backGround;
    [SerializeField] private GameObject newHighScore;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnEnable()
    {
        bannerCustomForever.sprite = GamePlay20SegForever.TEMATICA_BANNER;
        timePartida.text = GamePlay20SegForever.TIMEPARTIDA.Minutes.ToString("D2") + ":" + GamePlay20SegForever.TIMEPARTIDA.Seconds.ToString("D2");
        totalQuestion.text = GamePlay20SegForever.TOTALQUESTION.ToString("D2");
        totalQuestionOk.text = GamePlay20SegForever.TOTALQUESTIONOK.ToString("D2");
        higScore.text = GamePlay20SegForever.HIGSCORE.ToString("D2");
        Debug.Log(GamePlay20SegForever.TOTALQUESTIONOK + " - " + GamePlay20SegForever.HIGSCORE);
        newHighScore.SetActive(GamePlay20SegForever.IsHIGSCORE);
        if(newHighScore.activeSelf)
        {
            higScore.text = GamePlay20SegForever.TOTALQUESTIONOK.ToString("D2");
        }
    }

}
