using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using UnityEngine;
using Unity.Burst;
using Unity.Physics.Systems;

[GenerateAuthoringComponent]
public struct ProjectileResponderData : IComponentData
{
    public int life;
    public int damage;
}

/**
 * Makes entities respond to when another ProjectileResponder hits them
 */
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(EndFramePhysicsSystem))]
public class ProjectileResponderSystem : SystemBase
{
    BuildPhysicsWorld buildPhysicsWorldSystem;
    StepPhysicsWorld stepPhysicsWorldSystem;
    EntityQuery projectileResponderGroup;
    EntityCommandBufferSystem barrier;

    protected override void OnCreate()
    {
        buildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        stepPhysicsWorldSystem = World.GetOrCreateSystem<StepPhysicsWorld>();
        projectileResponderGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(ProjectileResponderData)
            }
        });
        barrier = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    //[BurstCompile]
    struct ProjectileResponderJob : ITriggerEventsJob
    {
        public ComponentDataFromEntity<ProjectileResponderData> projectileResponderGroup;
        [ReadOnly] public ComponentDataFromEntity<Parent> parentGroup;
        [ReadOnly] public ComponentDataFromEntity<ProjectileInstigatorData> instigatorGroup;
        //[ReadOnly] public ComponentDataFromEntity<PlayerTag> PlayerTagGroup;
        public EntityCommandBuffer ecb;

        public void Execute(TriggerEvent triggerEvent)
        {
            var entityA = triggerEvent.EntityA;
            var entityB = triggerEvent.EntityB;

            var responderA = projectileResponderGroup[triggerEvent.EntityA];
            var responderB = projectileResponderGroup[triggerEvent.EntityB];

            // Check if we have to do collision response
            if (responderA.life <= 0 || responderB.life <= 0) return;
            if (instigatorGroup.HasComponent(entityA) && instigatorGroup[entityA].instigator == entityB)
            {
                Debug.LogWarning($"Entity A collided with instigator");
                return;
            }
            if (instigatorGroup.HasComponent(entityB) && instigatorGroup[entityB].instigator == entityA)
            {
                Debug.LogWarning($"Entity B collided with instigator");
                return;
            }

            // Adjust the life of both components
            responderA.life -= responderB.damage;
            responderB.life -= responderA.damage;

            projectileResponderGroup[entityA] = responderA;
            projectileResponderGroup[entityB] = responderB;

            Debug.Log($"Collision! " +
                $"A life {responderA.life} dmg {responderA.damage} | " +
                $"B life {responderB.life} dmg {responderB.damage}");

            if (responderA.life <= 0)
            {
                Debug.Log("Entity A died");
                // Since the collision mesh is on the MeshOrientation, we want to get the root
                // This is a design flaw, but it works for this project
                // It has to be on the MeshOrientation since FireAbility uses that rotation to orient 
                // projectiles, but it also requires access to the physics group to know who it belongs to
                // and who to ignore
                var parent = entityA;
                while (parentGroup.HasComponent(parent))
                    parent = parentGroup[parent].Value;
                ecb.DestroyEntity(parent);
            }
            if (responderB.life <= 0)
            {
                Debug.Log("Entity B died");
                var parent = entityB;
                while (parentGroup.HasComponent(parent))
                    parent = parentGroup[parent].Value;
                ecb.DestroyEntity(parent);
            }

        }
    }

    protected override void OnUpdate()
    {
        if (projectileResponderGroup.CalculateEntityCount() == 0)
        {
            return;
        }
        
        Dependency = new ProjectileResponderJob
        {
            projectileResponderGroup = GetComponentDataFromEntity<ProjectileResponderData>(),
            parentGroup = GetComponentDataFromEntity<Parent>(true),
            instigatorGroup = GetComponentDataFromEntity<ProjectileInstigatorData>(true),
            ecb = barrier.CreateCommandBuffer(),
        }.Schedule(stepPhysicsWorldSystem.Simulation,
            ref buildPhysicsWorldSystem.PhysicsWorld, Dependency);

        barrier.AddJobHandleForProducer(Dependency);
    }
}
