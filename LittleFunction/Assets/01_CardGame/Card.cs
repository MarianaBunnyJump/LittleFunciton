#region

using System.Collections;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vector3 = UnityEngine.Vector3;

#endregion


//card用于每张卡片实例，管理卡片自身点击拖动悬停等各种状态
public class Card : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler,
    IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Reference")] private Canvas _canvas;
    private Camera _camera;
    private Vector2 _rectSize;
    private VisualCardsHandler _visualHandler;
    [SerializeField] public TMP_Text CardNumText;

    [Header("Movement")] [SerializeField] private float moveSpeedLimit = 50;
    [SerializeField] [ReadOnly] private Vector3 _offset;

    [Header("Selection")] public float selectionUpOffset = 50f;
    [ReadOnly] public bool selected;
    private float _pointerDownTime;
    private float _pointerUpTime;

    [Header("Visual")] [SerializeField] private GameObject cardVisualPrefab;
    [SerializeField] [ReadOnly] public CardVisual cardVisual;

    [Header("States")] [SerializeField] private bool instantiateVisual = true;
    [ReadOnly] public bool isHovering;
    [ReadOnly] public bool isDragging;
    [ReadOnly] public bool _wasDragged;
    [ReadOnly] public int CardIndex = 0;
    [ReadOnly] public int randomCardCount;


    [Header("Events")] [HideInInspector] public UnityEvent<Card> BeginDragEvent;
    [HideInInspector] public UnityEvent<Card> EndDragEvent;
    [HideInInspector] public UnityEvent<Card> PointerEnterEvent;
    [HideInInspector] public UnityEvent<Card> PointerExitEvent;
    [HideInInspector] public UnityEvent<Card> PointerDownEvent;
    [HideInInspector] public UnityEvent<Card, bool> PointerUpEvent;
    [HideInInspector] public UnityEvent<Card, bool> SelectEvent;

    private float _nowTime; //判断卡牌位置逻辑

    private void Start()
    {
        _canvas = GetComponentInParent<Canvas>();
        var rect = GetComponent<RectTransform>();
        _camera = Camera.main;
        //乘上lossyScale很重要！！！
        _rectSize = rect.sizeDelta * rect.lossyScale;

        //避免重复初始化
        if (!instantiateVisual)
        {
            return;
        }

        _visualHandler = FindObjectOfType<VisualCardsHandler>();

        Debug.Log("生成Visual");
        cardVisual = Instantiate(cardVisualPrefab, _visualHandler.transform).GetComponent<CardVisual>();
        cardVisual.Initialize(this);
    }

    //核心计算拖动的逻辑
    private void Update()
    {
        //0.5秒内判断一次位置
        if (Time.deltaTime - _nowTime >= 0.5f)
        {
            _nowTime = Time.time;
            JudgePositionRange();
        }

        //更好的拖动手感原因是写在update里，限制最大移动速度，让卡牌跟随鼠标移动，提升手感
        //手感更好的来源
        if (isDragging)
        {
            //拖动的时候才需要边界限制
            ClampPosition();
            //卡片中心需要移动到的目标位置
            Vector2 targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition) - _offset;
            //直线距离的方向向量
            var direction = (targetPosition - (Vector2)transform.position).normalized;
            //速度向量，方向乘限制后的速度
            var velocity = direction * Mathf.Min(moveSpeedLimit,
                Vector2.Distance(transform.position, targetPosition) / Time.deltaTime);
            // Time.deltaTime 是上一帧到当前帧的时间间隔，向量距离 = 方向速度 * 时间
            transform.Translate(velocity * Time.deltaTime);
        }
    }

    private void ClampPosition()
    {
        Vector2 screenBounds =
            _camera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, _camera.transform.position.z));
        var clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, -screenBounds.x + _rectSize.x / 2,
            screenBounds.x - _rectSize.x / 2);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, -screenBounds.y + _rectSize.y / 2,
            screenBounds.y - _rectSize.y / 2);
        transform.position = new Vector3(clampedPosition.x, clampedPosition.y, 0);
    }

    private void JudgePositionRange()
    {
        Vector2 screenBounds =
            _camera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, _camera.transform.position.z));
        if (transform.position.y < -screenBounds.y + _rectSize.y / 2 ||
            transform.position.y > screenBounds.y - _rectSize.y / 2)
        {
            Debug.LogError("卡片的Y值超出边界");
        }

        if (transform.position.x < -screenBounds.x + _rectSize.x / 2 ||
            transform.position.x > screenBounds.x - _rectSize.x / 2)
        {
            Debug.LogError("卡片的X值超出边界");
        }
    }


    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        BeginDragEvent?.Invoke(this);
        //不记录Z值，只计算XY在世界坐标的偏移量，给接下来的Update里做跟随计算距离用
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        _offset = mousePos - (Vector2)transform.position;

        _canvas.GetComponent<GraphicRaycaster>().enabled = false;
        isDragging = true;
        _wasDragged = true;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EndDragEvent?.Invoke(this);
        _canvas.GetComponent<GraphicRaycaster>().enabled = true;
        isDragging = false;

        //endDrag同时会触发OnPinterUp所以避免避免被识别成长按或者点击事件
        StartCoroutine(WaitFrameEnd());

        IEnumerator WaitFrameEnd()
        {
            yield return new WaitForEndOfFrame();
            _wasDragged = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PointerEnterEvent?.Invoke(this);
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PointerExitEvent?.Invoke(this);
        isHovering = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        _pointerDownTime = Time.time;
        PointerDownEvent?.Invoke(this);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        _pointerUpTime = Time.time;
        //触发**长按**后鼠标抬起事件
        if (_pointerUpTime - _pointerDownTime > .2f)
        {
            PointerUpEvent?.Invoke(this, true);
            return;
        }

        //**拖拽**事件后抬起
        if (_wasDragged)
        {
            return;
        }

        //排除别的选项只剩下**点击**事件了
        selected = !selected;
        PointerUpEvent?.Invoke(this, false);
        SelectEvent?.Invoke(this, selected);

        //transform.up:局部坐标系的 Y 轴正方向在世界空间中的向量
        //Vector3.up: 世界空间的上方移动，卡片旋转后会变成斜着移动，等价为（0，1，0）
        if (selected)
        {
           // Debug.Log($"初始位置{transform.position}");
            var pos = cardVisual.transform.up * selectionUpOffset;
            //使用localPosition!
            transform.localPosition += pos;
//            Debug.Log($"增加的位置{pos}");
        }
        else
        {
            //使用localPosition!
          //  Debug.Log($"高亮的初始位置{transform.position}");
            transform.localPosition = Vector3.zero;
        }
    }

    private void OnDestroy()
    {
        if (cardVisual != null)
            Destroy(cardVisual.gameObject);
    }

    #region public

    public void DeSelect()
    {
        if (selected)
        {
            selected = false;
            transform.localPosition = Vector3.zero;
        }
    }

    public int GetCardParentIndex()
    {
        return transform.parent.CompareTag("Slot") ? transform.parent.GetSiblingIndex() : 0;
    }

    public int GetCardsAmount()
    {
        return transform.parent.CompareTag("Slot") ? transform.parent.parent.childCount - 1 : 0;
    }

    public float NormalizedPosition()
    {
        return transform.parent.CompareTag("Slot")
            ? ((float)GetCardParentIndex()).MyRemap(0, transform.parent.parent.childCount - 1, 0, 1)
            : 0;
    }

    #endregion
}