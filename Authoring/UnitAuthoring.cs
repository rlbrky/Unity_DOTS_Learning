using Unity.Entities;
using UnityEngine;

public class UnitAuthoring : MonoBehaviour
{
    public Faction faction;
    
    public class Baker : Baker<UnitAuthoring>
    {
        public override void Bake(UnitAuthoring unitAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Unit
            {
                faction = unitAuthoring.faction,
            });
        }
    } 
}

public struct Unit : IComponentData
{
    public Faction faction;
}
