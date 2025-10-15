using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class UnitSelectionManager : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mouseWorldPosition = MouseWorldPosition.Instance.GetPosition();

            //Allocators are insanely important, you should be careful when using them.
            //Creating a persistent allocator requires you to destroy it so be careful when using that or you will cause memory leaks.
            //Temp allocator is disposed automatically at the end of frame.
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            EntityQuery entityQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<UnitMover>().Build(entityManager);
            
            NativeArray<Entity> entities = entityQuery.ToEntityArray(Allocator.Temp);
            
            NativeArray<UnitMover> unitMoverArray = entityQuery.ToComponentDataArray<UnitMover>(Allocator.Temp);
            for (int i = 0; i < unitMoverArray.Length; i++)
            {
                UnitMover unitMover = unitMoverArray[i];
                unitMover.targetPosition = mouseWorldPosition;
                // The above code works on the copy we made because we are working with structs so in order to actually change the data inside our component we need to find and change it like this.
                //entityManager.SetComponentData(entities[i], unitMover);
                //Another way to achieve the component data save would be
                unitMoverArray[i] = unitMover;
            }
            entityQuery.CopyFromComponentDataArray(unitMoverArray); //This will update all the data that matches the entity query we wrote above.
        }
    }
}
