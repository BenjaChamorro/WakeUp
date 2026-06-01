using UnityEngine;

public class NPCSpriteController : MonoBehaviour
{
    [SerializeField] private string horizontalParam = "Horizontal";
    [SerializeField] private string verticalParam = "Vertical";
    [SerializeField] private string talkingParam = "Estado";
    [SerializeField] private string walkingParam = "IsWalking";
    [SerializeField] private bool flipXAnimations = false;

    private Animator animator;
    private Vector2 direction = Vector2.zero;
    private bool isTalking = false;
    private bool isWalking = false;

    private bool hasHorizontalParam;
    private bool hasVerticalParam;
    private bool hasTalkingParam;
    private bool hasWalkingParam;

    void Awake()
    {
        animator = GetComponent<Animator>();
        hasHorizontalParam = HasParameter(horizontalParam, AnimatorControllerParameterType.Float);
        hasVerticalParam = HasParameter(verticalParam, AnimatorControllerParameterType.Float);
        hasTalkingParam = HasParameter(talkingParam, AnimatorControllerParameterType.Bool);
        hasWalkingParam = HasParameter(walkingParam, AnimatorControllerParameterType.Bool);
    }

    void Update()
    {
        if (animator == null) return;

        if (hasHorizontalParam)
            animator.SetFloat(horizontalParam, flipXAnimations ? -direction.x : direction.x);

        if (hasVerticalParam)
            animator.SetFloat(verticalParam, direction.y);

        if (hasTalkingParam)
            animator.SetBool(talkingParam, isTalking);

        if (hasWalkingParam)
            animator.SetBool(walkingParam, isWalking);
    }

    // Called by movement/AI systems to update facing/movement direction.
    public void SetDirection(Vector2 dir)
    {
        direction = dir.sqrMagnitude > 1f ? dir.normalized : dir;
        isWalking = direction.sqrMagnitude > 0.0001f;
    }

    // Convenience: set direction based on a velocity vector (normalizes if moving).
    public void SetDirectionFromVelocity(Vector2 velocity)
    {
        if (velocity.sqrMagnitude < 0.0001f)
        {
            direction = Vector2.zero;
            isWalking = false;
        }
        else
        {
            direction = velocity.normalized;
            isWalking = true;
        }
    }

    // Called by dialog systems to mark the NPC as talking or not.
    public void SetTalking(bool talking)
    {
        isTalking = talking;
    }

    private bool HasParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (animator == null || string.IsNullOrEmpty(parameterName))
            return false;

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == parameterType && parameters[i].name == parameterName)
                return true;
        }

        return false;
    }
}
