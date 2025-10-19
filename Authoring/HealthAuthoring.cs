using Unity.Entities;
using UnityEngine;

public class HealthAuthoring : MonoBehaviour
{
    public int healthAmount;
    public int healthAmountMax;
    
    private class Baker : Baker<HealthAuthoring>
    {
        public override void Bake(HealthAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Health
            {
                healthAmount = authoring.healthAmount,
                healthAmountMax = authoring.healthAmountMax,
                onHealthChanged = true, // Force health bar to update at least once
            });
        }
    }
}

public struct Health : IComponentData
{
    public int healthAmount;
    public int healthAmountMax;
    public bool onHealthChanged;
}