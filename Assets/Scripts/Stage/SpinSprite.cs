using UnityEngine;

public class SpinSprite : MonoBehaviour
{
    [SerializeField] private GameObject targetObject;
    [Header("Rotater Settings")]
    private SpriteRenderer spriteRenderer;
    private Quaternion initialPosition;
    [SerializeField] private float rotationSpeed = 1000f;
    [SerializeField] private float rotationTimeDuration = 3f;
    private float restTime;
    private bool isSpinning = false;
    [Header("Dissapear Settings")]
    [SerializeField] private bool disappearWhileSpin = false;
    [SerializeField] private float disappearDelay = 0.5f;
    private float disappearTimer;
    [SerializeField] private float disappearDuration = 1f;
    private float restTime2;
    void Start()
    {
        spriteRenderer = targetObject.GetComponent<SpriteRenderer>();
        initialPosition = targetObject.GetComponent<Transform>().rotation;
        restTime = rotationTimeDuration;
        disappearTimer = disappearDelay;
        restTime2 = disappearDuration;
    }
    void Update()
    {
        if (isSpinning)
        {
            restTime -= Time.deltaTime;
            disappearTimer -= Time.deltaTime;
            restTime2 -= Time.deltaTime;
            if (restTime <= 0f)
            {
                isSpinning = false;
                restTime = rotationTimeDuration;
                disappearTimer = disappearDelay;
                restTime2 = disappearDuration;
                targetObject.transform.rotation = initialPosition;
                if (disappearWhileSpin)
                {
                    targetObject.SetActive(false);
                }
            }
            else
            {
                Spin();
            }
        }
    }
    public void ActivateSpin()
    {
        isSpinning = true;
    }
    void Spin()
    {
        targetObject.transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
        if (disappearWhileSpin)
        {
            Disappear();
        }
    }
    void Disappear()
    {
        if (disappearTimer <= 0f)
        {
            float alpha = Mathf.Lerp(1f, 0f, (disappearDuration - restTime2) / disappearDuration);
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
    }
}
