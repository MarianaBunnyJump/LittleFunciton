#region

using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#endregion

public class CardVisual : MonoBehaviour
{
    private bool _initialize;
    private Canvas _canvas;
    private Camera _mainCamera;

    [Header("Card")] [ReadOnly] public Card MateCard;
    [ReadOnly] private Transform _cardTransform;
    [ReadOnly] private Vector3 _rotationDelta;
    [ReadOnly] private Vector3 _movementDelta;
    [ReadOnly] private int _savedIndex;

    [Header("References")] public Transform visualShadow;
    [SerializeField] private Transform shakeParent;
    [SerializeField] private Transform tiltParent;
    [SerializeField] private TMP_Text Text;
    [SerializeField] [ReadOnly] private float shadowOffset = 20;
    [SerializeField] [ReadOnly] private Vector2 shadowDistance;
    private Canvas _shadowCanvas;
    private Image _cardImage;

    [Header("Follow Parameters")] [SerializeField]
    private float followSpeed = 30;

    [Header("Rotation Parameters")] [SerializeField]
    private float rotationAmount = 20;

    [SerializeField] private float rotationSpeed = 20;
    [SerializeField] private float autoTiltAmount = 30;

    [SerializeField] [Tooltip("鼠标悬停的偏移程度")]
    private float manualTiltAmount = 20;

    [SerializeField] private float tiltSpeed = 20;

    [Header("Scale Parameters")] [SerializeField]
    private bool scaleAnimations = true;

    [SerializeField] private float scaleOnHover = 1.15f;
    [SerializeField] private float scaleOnSelect = 1.25f;
    [SerializeField] [Tooltip("缩放变化时长")] private float scaleTransition = .15f;
    [SerializeField] private Ease scaleEase = Ease.OutBack;

    [Header("Select Parameters")] [SerializeField]
    private float selectPunchAmount = 20;

    [Header("Hover Parameters")] [SerializeField]
    private float hoverPunchAngle = 5;

    [SerializeField] private float hoverTransition = .15f;

    [Header("Swap Parameters")] [SerializeField]
    private bool swapAnimations = true;

    [SerializeField] private float swapRotationAngle = 30;
    [SerializeField] private float swapTransition = .15f;
    [SerializeField] private int swapVibrato = 5;

    [Header("Curve")] [Tooltip("用于计算手牌整体的弧形程度")] [SerializeField]
    private CurveParameters curve;

    [SerializeField] private int StartArcCardAmount = 5;
    [SerializeField] [ReadOnly] private float _curveYOffset;
    [SerializeField] [ReadOnly] private float _curveRotationOffset;
    private Coroutine _pressCoroutine;

    private void Start()
    {
        shadowDistance = visualShadow.localPosition;
        _mainCamera = Camera.main;
    }


    public void Initialize(Card target, int index = 0)
    {
        MateCard = target;
        if (MateCard == null)
        {
            return;
        }

        _cardTransform = target.transform;
        _canvas = GetComponent<Canvas>();
        _shadowCanvas = visualShadow.GetComponent<Canvas>();

        MateCard.PointerEnterEvent.AddListener(PointerEnter);
        MateCard.PointerExitEvent.AddListener(PointerExit);
        MateCard.BeginDragEvent.AddListener(BeginDrag);
        MateCard.EndDragEvent.AddListener(EndDrag);
        MateCard.PointerDownEvent.AddListener(PointerDown);
        MateCard.PointerUpEvent.AddListener(PointerUp);
        MateCard.SelectEvent.AddListener(Select);

        _initialize = true;
        Text.text = (MateCard.randomCardCount + 1).ToString();
    }

    #region UpdateAni 手牌弧形，跟随平滑移动旋转，

    private void Update()
    {
        if (!_initialize) return;
        SetHandCardsArc();
        SmoothFollow();
        FollowRotation();
        CardTilt();
    }

    //手牌的弧形计算公式
    //拖动过程中交换位置时候的值是要重新计算的,卡片销毁了也需要重新计算
    private void SetHandCardsArc()
    {
        //计算弧形偏移数值
        //*curve.positioningInfluence 用于控制曲线的强度
        //*parentCard.SiblingAmount() 用于手牌数越多，弧形越明显自然
        _curveYOffset = curve.positioning.Evaluate(MateCard.NormalizedPosition()) * curve.positioningInfluence *
                        MateCard.GetCardsAmount();
        _curveYOffset = MateCard.GetCardsAmount() < StartArcCardAmount ? 0 : _curveYOffset;

        _curveRotationOffset = curve.rotation.Evaluate(MateCard.NormalizedPosition());
    }

    //卡牌跟着鼠标指针smooth移动，卡面渲染效果跟着卡牌smooth移动，不是同一个父物体，不能用localPosition
    private void SmoothFollow()
    {
        var verticalOffset = Vector3.up * (MateCard.isDragging ? 0 : _curveYOffset);
        transform.position = Vector3.Lerp(transform.position, _cardTransform.position + verticalOffset,
            followSpeed * Time.deltaTime);
    }

    //计算欧拉角左右Z轴倾斜程度
    private void FollowRotation()
    {
        //结合上一个方法，这是跟随时产生的"滞后"位移，让运动更平滑
        var movement = transform.position - _cardTransform.position;
        var movementRotation = Vector3.zero;
        //拖动的时候需求一下lerp值更平滑
        if (MateCard.isDragging)
        {
            _movementDelta = Vector3.Lerp(_movementDelta, movement, 25 * Time.deltaTime);
            movementRotation = _movementDelta * rotationAmount;
        }
        else
        {
            movementRotation = movement * rotationAmount;
        }

        _rotationDelta = Vector3.Lerp(_rotationDelta, movementRotation, rotationSpeed * Time.deltaTime);
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y,
            Mathf.Clamp(_rotationDelta.x, -60, 60));
    }


    private void CardTilt()
    {
        //拖动的卡牌不改变倾斜效果
        _savedIndex = MateCard.isDragging ? _savedIndex : MateCard.GetCardParentIndex();
        //让卡牌自然状态下轻微浮动，悬停时减弱浮动效果,加上手牌数量影响使得每张卡牌浮动效果不同
        var sine = Mathf.Sin(Time.time + _savedIndex) * (MateCard.isHovering ? .2f : 1);
        var cosine = Mathf.Cos(Time.time + _savedIndex) * (MateCard.isHovering ? .2f : 1);

        //鼠标悬停时，卡牌朝向鼠标方向倾斜，用Y轴偏移量调整卡牌效果沿着X轴的旋转值，用X轴偏移量调整卡牌效果沿着Y轴的旋转值
        //注意Y轴正值，沿着X轴的旋转值是负数的
        var offset = transform.position - _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        var tiltX = MateCard.isHovering ? offset.y * -1 * manualTiltAmount : 0;
        var tiltY = MateCard.isHovering ? offset.x * manualTiltAmount : 0;

        //让卡牌形成弧形手牌的旋转，抓在手里的牌Z左右倾斜度不变，
        var tiltZ = MateCard.isDragging
            ? tiltParent.eulerAngles.z
            : _curveRotationOffset * (curve.rotationInfluence * MateCard.GetCardsAmount());

        //将计算出的角度通过lerp平滑赋值
        var lerpX = Mathf.LerpAngle(tiltParent.eulerAngles.x, tiltX + sine * autoTiltAmount,
            tiltSpeed * Time.deltaTime);
        var lerpY = Mathf.LerpAngle(tiltParent.eulerAngles.y, tiltY + cosine * autoTiltAmount,
            tiltSpeed * Time.deltaTime);
        var lerpZ = Mathf.LerpAngle(tiltParent.eulerAngles.z, tiltZ, tiltSpeed / 2 * Time.deltaTime);

        tiltParent.eulerAngles = new Vector3(lerpX, lerpY, lerpZ);
    }

    #endregion

    //选中之后轻微晃动
    private void Select(Card card, bool state)
    {
        DOTween.Kill(2, true);
        float dir = state ? 1 : 0;
        shakeParent.DOPunchPosition(shakeParent.up * selectPunchAmount * dir, scaleTransition);
        shakeParent.DOPunchRotation(Vector3.forward * (hoverPunchAngle / 2), hoverTransition, 20).SetId(2);

        if (scaleAnimations)
            transform.DOScale(scaleOnHover, scaleTransition).SetEase(scaleEase);
    }

    //震荡旋转小动画，增加灵活性
    public void PunchRotateAni(float dir = 1)
    {
        if (!swapAnimations)
            return;

        DOTween.Kill(2, true);
        shakeParent.DOPunchRotation(Vector3.forward * swapRotationAngle * dir, swapTransition, swapVibrato)
            .SetId(3);
    }


    public void KeepIndexSynchro()
    {
        var index = MateCard.transform.parent.GetSiblingIndex();
        transform.SetSiblingIndex(index);
    }

    private void PointerEnter(Card card)
    {
        if (scaleAnimations)
            transform.DOScale(scaleOnHover, scaleTransition).SetEase(scaleEase);

        DOTween.Kill(2, true);
        shakeParent.DOPunchRotation(Vector3.forward * hoverPunchAngle, hoverTransition, 20).SetId(2);
    }

    private void PointerExit(Card card)
    {
        //何意味？为什么要等最后一帧之后？直接用isDrag不一样吗?肉眼貌似看不出区别
        if (!MateCard._wasDragged)
            transform.DOScale(1, scaleTransition).SetEase(scaleEase);
    }

    private void BeginDrag(Card card)
    {
        if (scaleAnimations)
            transform.DOScale(scaleOnSelect, scaleTransition).SetEase(scaleEase);

        _canvas.overrideSorting = true;
    }

    private void EndDrag(Card card)
    {
        _canvas.overrideSorting = false;
        transform.DOScale(1, scaleTransition).SetEase(scaleEase);
    }

    private void PointerDown(Card card)
    {
        if (scaleAnimations)
            transform.DOScale(scaleOnSelect, scaleTransition).SetEase(scaleEase);

        visualShadow.localPosition += -Vector3.up * shadowOffset;
        //何意味？
        _shadowCanvas.overrideSorting = false;
    }

    private void PointerUp(Card card, bool isLongPress)
    {
        if (scaleAnimations)
            transform.DOScale(isLongPress ? scaleOnHover : scaleOnSelect, scaleTransition).SetEase(scaleEase);
        //何意味？
        _canvas.overrideSorting = false;
        visualShadow.localPosition = shadowDistance;
        _shadowCanvas.overrideSorting = true;
    }
}