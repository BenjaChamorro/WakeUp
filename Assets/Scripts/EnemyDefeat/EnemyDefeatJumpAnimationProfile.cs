using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDefeatJumpAnimationProfile", menuName = "WakeUp/Combat/Enemy Defeat Animation/Jump")]
public class EnemyDefeatJumpAnimationProfile : EnemyDefeatAnimationProfile {
    [SerializeField, Min(0f)] private float jumpHeight = 0.35f;
    [SerializeField, Min(0.05f)] private float durationSeconds = 0.7f;
    [SerializeField, Min(1f)] private float jumpCycles = 1f;

    public override void PlayAnimation(Transform enemyTransform) {
        if (enemyTransform == null) {
            return;
        }

        EnemyDefeatJumpAnimator animator = enemyTransform.GetComponent<EnemyDefeatJumpAnimator>();
        if (animator == null) {
            animator = enemyTransform.gameObject.AddComponent<EnemyDefeatJumpAnimator>();
        }

        animator.Play(jumpHeight, durationSeconds, jumpCycles);
    }
}

public class EnemyDefeatJumpAnimator : MonoBehaviour {
    private Coroutine animationRoutine;

    public void Play(float jumpHeight, float durationSeconds, float jumpCycles) {
        if (animationRoutine != null) {
            StopCoroutine(animationRoutine);
        }

        animationRoutine = StartCoroutine(PlayRoutine(jumpHeight, durationSeconds, jumpCycles));
    }

    private IEnumerator PlayRoutine(float jumpHeight, float durationSeconds, float jumpCycles) {
        Vector3 startLocalPosition = transform.localPosition;
        float elapsedSeconds = 0f;

        while (elapsedSeconds < durationSeconds) {
            float normalizedTime = elapsedSeconds / durationSeconds;
            float jumpOffset = Mathf.Sin(normalizedTime * Mathf.PI * jumpCycles) * jumpHeight;
            transform.localPosition = startLocalPosition + Vector3.up * jumpOffset;

            elapsedSeconds += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = startLocalPosition;
        animationRoutine = null;
    }
}