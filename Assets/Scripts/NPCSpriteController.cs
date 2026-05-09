using UnityEngine;

public class NPCSpriteController : MonoBehaviour
{
    [SerializeField] private string horizontalParam = "Horizontal";
    [SerializeField] private string verticalParam = "Vertical";
    [SerializeField] private string talkingParam = "Estado";

    private Animator animator;
    private Vector2 direction = Vector2.zero;
    private bool isTalking = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (animator == null) return;

        animator.SetFloat(horizontalParam, direction.x);
        animator.SetFloat(verticalParam, direction.y);
        animator.SetBool(talkingParam, isTalking);
    }

    // Called by movement/AI systems to update facing/movement direction.
    public void SetDirection(Vector2 dir)
    {
        direction = dir.sqrMagnitude > 1f ? dir.normalized : dir;
    }

    // Convenience: set direction based on a velocity vector (normalizes if moving).
    public void SetDirectionFromVelocity(Vector2 velocity)
    {
        if (velocity.sqrMagnitude < 0.0001f) direction = Vector2.zero;
        else direction = velocity.normalized;
    }

    // Called by dialog systems to mark the NPC as talking or not.
    public void SetTalking(bool talking)
    {
        isTalking = talking;
    }
}
