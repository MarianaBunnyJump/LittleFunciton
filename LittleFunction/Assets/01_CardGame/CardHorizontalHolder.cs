#region

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

#endregion


//管理Card,把卡片点击等事件的增加的步骤，如交换位置等注册进卡片事件里
public class CardHorizontalHolder : MonoBehaviour
{
    [SerializeField] private Card SelectedCard;
    [SerializeReference] private Card HoveredCard;

    [Tooltip("生成的卡牌数量")] [SerializeField] private int InitCardCount;
    [SerializeField] private int MaxCardCount = 15;
    [SerializeField] private GameObject CardPrefab;

    private RectTransform _rect;
    [SerializeField] private List<Card> Cards;
    [SerializeField] private bool tweenCardReturn = true;
    [SerializeField] private bool isCrossing;
    [SerializeField] private GlobalMgr globalMgr;
    [SerializeField] private float OutNum;
    [SerializeField] private Button Btn_OutCard;
    [SerializeField] private Button Btn_GainCard;


    private void Start()
    {
        var haveCount = GetComponentsInChildren<Card>().Length;
        if (haveCount >= InitCardCount)
        {
            Debug.LogError("初始Card超出上限");
        }

        for (var i = 0; i < InitCardCount - haveCount; i++)
        {
            var slot = Instantiate(CardPrefab, transform);
            slot.name = "Slot" + i;
        }

        _rect = GetComponent<RectTransform>();
        Cards = GetComponentsInChildren<Card>().ToList();

        var cardIndex = 0;
        for (var i = 0; i < Cards.Count; i++)
        {
            Cards[i].PointerEnterEvent.AddListener(PointerEnter);
            Cards[i].PointerExitEvent.AddListener(PointerExit);
            Cards[i].BeginDragEvent.AddListener(BeginDrag);
            Cards[i].EndDragEvent.AddListener(EndDrag);
            Cards[i].name = "Card" + cardIndex;
            //Cards[i].CardNumText.text = cardIndex.ToString();
            Cards[i].CardNumText.gameObject.SetActive(false);
            Cards[i].CardIndex = cardIndex;
            Cards[i].randomCardCount = Random.Range(0, 10);
            cardIndex++;
        }

        //一次性，确保初始的索引值匹配
        StartCoroutine(Frame());

        IEnumerator Frame()
        {
            yield return new WaitForSecondsRealtime(.1f);
            for (var i = 0; i < Cards.Count; i++)
            {
                if (Cards[i].cardVisual != null)
                    Cards[i].cardVisual.KeepIndexSynchro();
            }
        }

        RegisterEvent();
    }

    private void OnDestroy()
    {
        UnRegisterEvent();
    }

    private void RegisterEvent()
    {
        Btn_OutCard.onClick.AddListener(RemoveCard);
        Btn_GainCard.onClick.AddListener(GainCard);
    }

    private void UnRegisterEvent()
    {
        Btn_OutCard.onClick.RemoveListener(RemoveCard);
        Btn_GainCard.onClick.RemoveListener(GainCard);
    }

    private void Update()
    {
        if (SelectedCard == null)
            return;

        if (isCrossing)
            return;

        for (var i = 0; i < Cards.Count; i++)
        {
            if (SelectedCard.transform.position.x > Cards[i].transform.position.x)
            {
                if (SelectedCard.GetCardParentIndex() < Cards[i].GetCardParentIndex())
                {
                    Swap(i);
                    return;
                }
            }
            else if (SelectedCard.transform.position.x < Cards[i].transform.position.x)
            {
                if (SelectedCard.GetCardParentIndex() > Cards[i].GetCardParentIndex())
                {
                    Swap(i);
                    return;
                }
            }
        }
    }

    private void Swap(int index)
    {
        isCrossing = true;

        var selectedParent = SelectedCard.transform.parent;
        var crossedParent = Cards[index].transform.parent;

        //交换父物体
        Cards[index].transform.SetParent(selectedParent);
        SelectedCard.transform.SetParent(crossedParent);
        //改变选中卡片的位置，不能重置手牌的位置，否则不跟手了，拖动时候每帧调用，就会逐个改变位置了
        Cards[index].transform.localPosition =
            Cards[index].selected ? new Vector3(0, Cards[index].selectionUpOffset, 0) : Vector3.zero;

        isCrossing = false;

        if (Cards[index].cardVisual == null)
            return;
        var swapToLeft = Cards[index].GetCardParentIndex() > SelectedCard.GetCardParentIndex();
        Cards[index].cardVisual.PunchRotateAni(swapToLeft ? -1 : 1);

        //Why not just Two Change Cards?
        foreach (var card in Cards)
        {
            card.cardVisual.KeepIndexSynchro();
        }
    }

    private void PointerEnter(Card card)
    {
        HoveredCard = card;
    }

    private void PointerExit(Card card)
    {
        HoveredCard = null;
    }

    private void BeginDrag(Card card)
    {
        SelectedCard = card;
    }

    private void EndDrag(Card card)
    {
        if (SelectedCard == null)
        {
            return;
        }

        SelectedCard.transform.DOLocalMove(
            SelectedCard.selected ? new Vector3(0, SelectedCard.selectionUpOffset, 0) : Vector3.zero,
            tweenCardReturn ? .15f : 0).SetEase(Ease.OutBack);

        //何意味？
        _rect.sizeDelta += Vector2.right;
        _rect.sizeDelta -= Vector2.right;

        SelectedCard = null;
    }

    [Button("全不选")]
    private void ResetAll()
    {
        for (var i = 0; i < Cards.Count; i++)
        {
            Cards[i].DeSelect();
        }
    }

    [Button("发牌")]
    private void AddCard(int count)
    {
        var haveCount = GetComponentsInChildren<Card>().Length;
        if (haveCount > InitCardCount)
        {
            Debug.LogError("初始Card超出上限");
            return;
        }

        for (var i = 0; i < count; i++)
        {
            var thisCount = i + haveCount;
            var slot = Instantiate(CardPrefab, transform);
            slot.name = "Slot" + thisCount;
        }

        Cards.Clear();
        Cards = GetComponentsInChildren<Card>().ToList();

        var cardIndex = 0;
        for (var i = 0; i < Cards.Count; i++)
        {
            //移除重复添加的监听
            Cards[i].PointerEnterEvent.RemoveAllListeners();
            Cards[i].PointerExitEvent.RemoveAllListeners();
            Cards[i].BeginDragEvent.RemoveAllListeners();
            Cards[i].EndDragEvent.RemoveAllListeners();


            Cards[i].PointerEnterEvent.AddListener(PointerEnter);
            Cards[i].PointerExitEvent.AddListener(PointerExit);
            Cards[i].BeginDragEvent.AddListener(BeginDrag);
            Cards[i].EndDragEvent.AddListener(EndDrag);
            Cards[i].name = "Card" + cardIndex;
            //Cards[i].CardNumText.text = cardIndex.ToString();
            Cards[i].CardNumText.gameObject.SetActive(false);
            if (Cards[i].CardIndex == 0)
            {
                Cards[i].randomCardCount = Random.Range(0, 21);
                Cards[i].CardIndex = cardIndex;
            }

            cardIndex++;
        }

        StartCoroutine(Frame());

        IEnumerator Frame()
        {
            yield return new WaitForSecondsRealtime(.1f);
            for (var i = 0; i < Cards.Count; i++)
            {
                if (Cards[i].cardVisual != null)
                {
                    Cards[i].cardVisual.KeepIndexSynchro();
                }
            }
        }
    }

    private void GainCard()
    {
        AddCard(5);
    }

    [Button("出牌")]
    private void RemoveCard()
    {
        for (var i = Cards.Count - 1; i >= 0; i--)
        {
            var card = Cards[i];
            if (card.selected)
            {
                var selectedCard = card;

                selectedCard.transform.DOLocalMoveY(OutNum, .15f).SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        globalMgr.AddCount(10 * selectedCard.CardIndex);

                        if (selectedCard != null && selectedCard.transform.parent != null)
                        {
                            Destroy(selectedCard.transform.parent.gameObject);
                        }

                        Cards.Remove(selectedCard);
                    });
            }
        }
    }
}