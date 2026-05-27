using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TematicaItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameTematica;
    [SerializeField] private TextMeshProUGUI desctematica;
    [SerializeField] private Image iconTematica;
    [SerializeField] private Image bannerTematica;
    [SerializeField] private Button playButton;
    [SerializeField] private Button leaderBoardButton;


    public string _id;
    private string _name;
    private string _descripcion;
    private PriceEvent[] _premios;
    private string _iconUrl;
    private string _bannerUrl;
    private string _status;
    private bool _is20Seg;

    public static TematicaItem TEMATICA_ITEM;

    public async void SetTematica(
        string id,
        string name,
        string descripcion,
        PriceEvent[] premios,
    string icon,
        string banner,
        string status,
        bool is20Seg, 
        bool _setGame
    )
    {
        _id = id;
        _name = name;
        _descripcion = descripcion;
        _premios = premios;
        _iconUrl = icon;
        _bannerUrl = banner;
        _status = status;
        _is20Seg = is20Seg;

        Debug.Log($"Temática cargada: {name} - {id}");
        iconTematica.sprite = await PopUpManager.GetRemoteTexture(_iconUrl);
        bannerTematica.sprite = await PopUpManager.GetRemoteTexture(_bannerUrl);

        if (playButton)
        {
            playButton.onClick.AddListener(() =>
            {
                SelectForever();
            });
            leaderBoardButton.onClick.AddListener(() =>
               {
                   ShowLeaderBoard(_id);
               });
        }
        if(_setGame)
        {
            SelectForever();
        }
    }

    private void ShowLeaderBoard(string id)
    {
        throw new NotImplementedException();
    }
    public void SetBannerAlpha(float alpha)
    {
        if (iconTematica == null) return;

        Color c = iconTematica.color;
        c.a = alpha;
        iconTematica.color = c;
    }
    public void SelectForever(bool _leaderBoard = false)
    {
        GamePlay20SegForever.TEMATICA_ID = _id;
        GamePlay20SegForever.TEMATICA_NAME = _name;
        GamePlay20SegForever.TEMATICA_DESC = _descripcion;
        GamePlay20SegForever.TEMATICA_PREMIOS = _premios;
        GamePlay20SegForever.TEMATICA_ICON= iconTematica.sprite;
        GamePlay20SegForever.TEMATICA_BANNER = bannerTematica.sprite;
        GamePlay20SegForever.IS20SEG= _is20Seg;
        GamePlay20SegForever.Instance.SetForeverTematica(_leaderBoard);
    }


}