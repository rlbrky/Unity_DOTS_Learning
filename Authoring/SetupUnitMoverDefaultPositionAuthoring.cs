using Unity.Entities;
using UnityEngine;

public class SetupUnitMoverDefaultPositionAuthoring : MonoBehaviour
{
   private class SetupUnitMoverDefaultPositionBaker : Baker<SetupUnitMoverDefaultPositionAuthoring>
   {
      public override void Bake(SetupUnitMoverDefaultPositionAuthoring authoring)
      {
         Entity entity = GetEntity(TransformUsageFlags.Dynamic);
         AddComponent(entity, new SetupUnitMoverDefaultPosition());
      }
   }
}

public struct SetupUnitMoverDefaultPosition : IComponentData
{
    
}