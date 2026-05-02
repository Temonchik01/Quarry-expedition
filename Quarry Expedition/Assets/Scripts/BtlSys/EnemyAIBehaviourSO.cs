using UnityEngine;

namespace JRPG.Data
{
    public enum AIStrategyType
    {
        /// <summary>Always picks a random available attack. (Boulder)</summary>
        Random,
        /// <summary>Prefers status/utility moves when available. (Golem)</summary>
        StatusFirst,
        /// <summary>Targets the lowest-HP enemy. Future expansion.</summary>
        TargetWeakest
    }

    /// <summary>
    /// Describes which AI decision strategy an enemy uses.
    /// Consumed by EnemyAIStrategyFactory to produce a concrete IEnemyAI.
    /// This keeps enemy configuration purely in the Inspector — no code change
    /// needed to give an enemy a new behaviour.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAIBehaviour", menuName = "JRPG/Enemy AI Behaviour")]
    public sealed class EnemyAIBehaviourSO : ScriptableObject
    {
        [SerializeField] private AIStrategyType _strategyType = AIStrategyType.Random;

        [Tooltip("Probability (0–1) the AI picks a utility/status move when one is available")]
        [SerializeField, Range(0f, 1f)] private float _utilityMoveBias = 0.5f;

        public AIStrategyType StrategyType    => _strategyType;
        public float          UtilityMoveBias => _utilityMoveBias;
    }
}