using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;
using Unity.Physics;

/**
 * Some notes on this system...
 * Had lots of issues with gameobjects and children, since they are actually 
 * independent from each other. This caused issues such as, rotating the 
 * mesh but not the character self (for movement calculation). But shooting uses the 
 * mesh rotation instead of player rotation (which is always the same, since we move 
 * with the camera rotation). Thus, shooting component would be on the mesh entity, 
 * but this then caused issues where I wanted to also get the PhysicsCollider, but 
 * this was on the parent game object, because otherwise the child would only be destroyed.  
 * 
 * Ultimately, this causes a dependency in a compnent on the parent, which is something 
 * that you want to prevent. It was an incorrect design on my part and a good lesson for next time. 
 */

public struct FireAbilityData : IComponentData
{
    public Entity projectilePrefab;
    public float degreesPerSecond;
    public float fireRate;
    public float fireCount;
    public float spreadPercent;

    public bool isFiring;

    internal float cooldown;
}

public struct ProjectileInstigatorData : IComponentData
{
    public Entity instigator;
}

public class FireAbility : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public GameObject projectilePrefab;
    public float degreesPerSecond;
    public float fireRate;
    public float fireCount;
    public float spreadPercent; // How much a fired projectile can spread to the sides. A value of 1 means the projectile flies parallel

    public bool isFiring;


    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new FireAbilityData
        {
            projectilePrefab = conversionSystem.GetPrimaryEntity(projectilePrefab),
            degreesPerSecond = degreesPerSecond,
            fireRate = fireRate,
            spreadPercent = spreadPercent,
            fireCount = fireCount,

            isFiring = isFiring,
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
    uint seed = 1;

    protected override void OnCreate()
    {
        base.OnCreate();
        Barrier = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = Barrier.CreateCommandBuffer().AsParallelWriter();
        var dt = Time.DeltaTime;
        var rand = new Random(seed++);

        Entities
            .ForEach((Entity entity, int entityInQueryIndex, ref FireAbilityData fireData, in LocalToWorld ltw) =>
            {

                if (fireData.isFiring && fireData.cooldown == 0)
                {
                    // Quite an ugly work around, but we need the root collider
                    // I made a mistake where I childed gameobjects for the player
                    // but when designing things in an ECS way, you shouldn't think like that
                    // since entities should not really rely on other entities to work well
                    Entity root = entity;

                    while (HasComponent<Parent>(root) && !HasComponent<PhysicsCollider>(root))
                    {
                        root = GetComponent<Parent>(root).Value;
                    }

                    var projectileCollider = GetComponent<PhysicsCollider>(fireData.projectilePrefab);
                    if (HasComponent<PhysicsCollider>(root))
                    {
                        var parentCollider = GetComponent<PhysicsCollider>(root);
                        projectileCollider.Value.Value.Filter = parentCollider.Value.Value.Filter;
                    }
                    else
                    {
                        projectileCollider.Value.Value.Filter = CollisionFilter.Default;
                        //Debug.LogError("FireAbility is missing a parent with a collider and can't infer collision filter");
                    }

                    for (int i = 0; i < fireData.fireCount; i++)
                        {
                            var instance = ecb.Instantiate(entityInQueryIndex, fireData.projectilePrefab);

                            ecb.SetComponent(entityInQueryIndex, instance, new Rotation { Value = ltw.Rotation });
                            ecb.SetComponent(entityInQueryIndex, instance, new MoveAbilityData
                            {
                                degreesPerSecond = fireData.degreesPerSecond,
                                move = new float2(rand.NextFloat(-fireData.spreadPercent, fireData.spreadPercent), 1),
                                //move = new float2(rand.NextFloat(-0.5f,0.5f), 1),
                                //move = new float2(0, 1),
                            });

                            ecb.SetComponent(entityInQueryIndex, instance, projectileCollider);
                            ecb.AddComponent(entityInQueryIndex, instance, new ProjectileInstigatorData { instigator = entity });
                        }

                    fireData.cooldown = 1f / fireData.fireRate;
                }

                fireData.cooldown = math.max(0, fireData.cooldown - dt);
            })
            .ScheduleParallel();
        
        Barrier.AddJobHandleForProducer(Dependency);
    }
}