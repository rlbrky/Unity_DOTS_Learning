using Unity.Entities;
using UnityEngine;

public class HealthbarAuthoring : MonoBehaviour
{
   public GameObject barVisualGO;
   public GameObject healthGO;
   
   private class HealthbarBaker : Baker<HealthbarAuthoring>
   {
      public override void Bake(HealthbarAuthoring authoring)
      {
         Entity entity = GetEntity(TransformUsageFlags.Dynamic);
         AddComponent(entity, new HealthBar
         {
            barVisualEntity = GetEntity(authoring.barVisualGO, TransformUsageFlags.NonUniformScale),
            healthEntity = GetEntity(authoring.healthGO, TransformUsageFlags.Dynamic),
         });
      }
   }
}

public struct HealthBar : IComponentData
{
   public Entity barVisualEntity;
   public Entity healthEntity;
}