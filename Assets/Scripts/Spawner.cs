using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using UnityEngine;
using Unity.Jobs;

public struct SpawnerData : IComponentData
{
    public Entity prefab;
    public float spawnFrequency;
    public float spawnCount;

    internal float cooldown;
}

public class Spawner : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public GameObject prefab;
    public float spawnRate;
    public float spawnCount;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new SpawnerData
        {
            prefab = conversionSystem.GetPrimaryEntity(prefab),
            spawnFrequency = 1f / spawnRate,
            spawnCount = spawnCount,
        });
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(prefab);
    }
}

/**
 * Makes entities respond to when another ProjectileResponder hits them
 */
public class SpawnerSystem : SystemBase
{
    EntityCommandBufferSystem Barrier;
    EntityQuery SpawnerBlockerQuery;
    uint seed = 1;

    protected override void OnCreate()
    {
        Barrier = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        // We need the blocker tags to not spawn ontop of those entities
        var spawnerBlockersData = new NativeArray<float>(SpawnerBlockerQuery.CalculateEntityCount(), Allocator.TempJob);
        var spawnerBlockersRotation = new NativeArray<quaternion>(SpawnerBlockerQuery.CalculateEntityCount(), Allocator.TempJob);
        var fillNativeArraysHandle = Entities
            .WithName("FillSpanwerArrays")
            .WithStoreEntityQueryInField(ref SpawnerBlockerQuery)
            .ForEach((int entityInQueryIndex, in SpawnerBlockerData data, in Rotation rot) =>
            {
                spawnerBlockersData[entityInQueryIndex] = data.degreesFrom;
                spawnerBlockersRotation[entityInQueryIndex] = rot.Value;
            })
            .ScheduleParallel(Dependency);

        var ecb = Barrier.CreateCommandBuffer().AsParallelWriter();
        var dt = Time.DeltaTime;
        var rand = new Unity.Mathematics.Random(seed++);

        var spawnerHandle = Entities
            .WithName("Spawner")
            //.WithReadOnly(spawnerBlockersData)
            //.WithReadOnly(spawnerBlockersRotation)
            .ForEach((int entityInQueryIndex, ref SpawnerData spawner) =>
            {
                if (spawner.cooldown == 0)
                {
                    for (int i = 0; i < spawner.spawnCount; i++)
                    {
                        // Calculate a rotation at which to spawn an enemy
                        var desiredRotation = rand.NextQuaternionRotation();

                        // Retry until location is not near a blocker
                        //var validLocation = true;
                        //do
                        //{
                        //    validLocation = true;

                        //    for(int j = 0; j < spawnerBlockersData.Length; j++)
                        //    {
                        //        var dot = math.dot(desiredRotation, spawnerBlockersRotation[j].Value);
                        //    }
                        //}
                        //while (validLocation);

                        // Spawn the entity 
                        var instance = ecb.Instantiate(entityInQueryIndex, spawner.prefab);

                        ecb.AddComponent(entityInQueryIndex, instance, new Rotation { Value = desiredRotation });
                    }

                    spawner.cooldown = spawner.spawnFrequency;
                }
                else
                {
                    spawner.cooldown = math.max(spawner.cooldown - dt, 0);
                }
            })
            //.WithDisposeOnCompletion(spawnerBlockersData)
            //.WithDisposeOnCompletion(spawnerBlockersRotation)
            .ScheduleParallel(fillNativeArraysHandle);

        Dependency = JobHandle.CombineDependencies(Dependency, spawnerBlockersData.Dispose(spawnerHandle));
        Dependency = JobHandle.CombineDependencies(Dependency, spawnerBlockersRotation.Dispose(spawnerHandle));

        Barrier.AddJobHandleForProducer(Dependency);
    }
}
