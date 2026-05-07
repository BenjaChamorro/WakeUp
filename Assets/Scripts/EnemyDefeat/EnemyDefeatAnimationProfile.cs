using UnityEngine;

public abstract class EnemyDefeatAnimationProfile : ScriptableObject {
    public abstract void PlayAnimation(Transform enemyTransform);
}