using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct FireProjectileData : IComponentData
{
    public Entity projectilePrefab;
    public float degreesPerSecond;
    public float fireRate;

    public bool isFiring;

    internal float cooldown;
}

public class FireProjectile : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public GameObject projectilePrefab;
    public float degreesPerSecond;
    public float fireRate;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new FireProjectileData
        {
            projectilePrefab = conversionSystem.GetPrimaryEntity(projectilePrefab),
            degreesPerSecond = degreesPerSecond,
            fireRate = fireRate,
        });
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(projectilePrefab);
    }
}

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public class FireProjectileSystem : SystemBase
{
    EntityCommandBufferSystem Barrier;

    protected override void OnCreate()
    {
        base.OnCreate();
        Barrier = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = Barrier.CreateCommandBuffer().AsParallelWriter();
        var dt = Time.DeltaTime;

        Entities
            .ForEach((int entityInQueryIndex, ref FireProjectileData fireData, in LocalToWorld ltw) =>
            {
                if(fireData.isFiring && fireData.cooldown == 0)
                {
                    var instance = ecb.Instantiate(entityInQueryIndex, fireData.projectilePrefab);
                    
                    ecb.SetComponent(entityInQueryIndex, instance, new Rotation { Value = ltw.Rotation });
                    ecb.SetComponent(entityInQueryIndex, instance, new MoveableState
                    {
                        degreesPerSecond = fireData.degreesPerSecond,
                        move = new float2(0, 1),
                    });

                    fireData.cooldown = 1f / fireData.fireRate;
                }

                fireData.cooldown = math.max(0, fireData.cooldown - dt);
            })
            .ScheduleParallel();

        Barrier.AddJobHandleForProducer(Dependency);
    }
}