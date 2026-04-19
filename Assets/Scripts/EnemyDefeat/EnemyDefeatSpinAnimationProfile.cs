using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDefeatSpinAnimationProfile", menuName = "WakeUp/Combat/Enemy Defeat Animation/Spin")]
public class EnemyDefeatSpinAnimationProfile : EnemyDefeatAnimationProfile {
    [SerializeField, Min(0f)] private float rotationsPerSecond = 2f;
    [SerializeField, Min(0f)] private float durationSeconds = 0f;

    public override void PlayAnimation(Transform enemyTransform) {
        if (enemyTransform == null) {
            return;
        }

        EnemyDefeatSpinAnimator animator = enemyTransform.GetComponent<EnemyDefeatSpinAnimator>();
        if (animator == null) {
            animator = enemyTransform.gameObject.AddComponent<EnemyDefeatSpinAnimator>();
        }

        animator.Play(rotationsPerSecond, durationSeconds);
    }
}

public class EnemyDefeatSpinAnimator : MonoBehaviour {
    private float rotationsPerSecond;
    private float durationSeconds;
    private float elapsedSeconds;
    private bool isPlaying;

    public void Play(float newRotationsPerSecond, float newDurationSeconds) {
        rotationsPerSecond = newRotationsPerSecond;
        durationSeconds = newDurationSeconds;
        elapsedSeconds = 0f;
        isPlaying = true;
        enabled = true;
    }

    private void Update() {
        if (!isPlaying) {
            return;
        }

        transform.Rotate(0f, 360f * rotationsPerSecond * Time.deltaTime, 0f, Space.Self);

        if (durationSeconds > 0f) {
            elapsedSeconds += Time.deltaTime;
            if (elapsedSeconds >= durationSeconds) {
                isPlaying = false;
                enabled = false;
            }
        }
    }
}