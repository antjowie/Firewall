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
    BuildPhysicsWorld BuildPhysicsWorldSystem;
    StepPhysicsWorld StepPhysicsWorldSystem;
    EntityQuery ProjectileResponderGroup;
    EntityCommandBufferSystem Barrier;

    protected override void OnCreate()
    {
        BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        StepPhysicsWorldSystem = World.GetOrCreateSystem<StepPhysicsWorld>();
        ProjectileResponderGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(ProjectileResponderData)
            }
        });
        Barrier = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    [BurstCompile]
    struct ProjectileResponderJob : ITriggerEventsJob
    {
        public ComponentDataFromEntity<ProjectileResponderData> ProjectileResponderGroup;
        [ReadOnly] public ComponentDataFromEntity<Parent> ParentGroup;
        //[ReadOnly] public ComponentDataFromEntity<PlayerTag> PlayerTagGroup;
        public EntityCommandBuffer Ecb;

        public void Execute(TriggerEvent triggerEvent)
        {
            var entityA = ProjectileResponderGroup[triggerEvent.EntityA];
            var entityB = ProjectileResponderGroup[triggerEvent.EntityB];

            // Adjust the life of both components
            entityA.life -= entityB.damage;
            entityB.life -= entityA.damage;

            ProjectileResponderGroup[triggerEvent.EntityA] = entityA;
            ProjectileResponderGroup[triggerEvent.EntityB] = entityB;

            if (entityA.life <= 0)
            {
                // Since the collision mesh is on the MeshOrientation, we want to get the root
                // This is a design flaw, but it works for this project
                // It has to be on the MeshOrientation since FireAbility uses that rotation to orient 
                // projectiles, but it also requires access to the physics group to know who it belongs to
                // and who to ignore
                var parent = triggerEvent.EntityA;
                while (ParentGroup.HasComponent(parent))
                    parent = ParentGroup[parent].Value;
                Ecb.DestroyEntity(parent);
            }
            if (entityB.life <= 0)
            {
                var parent = triggerEvent.EntityB;
                while (ParentGroup.HasComponent(parent))
                    parent = ParentGroup[parent].Value;
                Ecb.DestroyEntity(parent);
            }

        }
    }

    protected override void OnUpdate()
    {
        if (ProjectileResponderGroup.CalculateEntityCount() == 0)
        {
            return;
        }

        Dependency = new ProjectileResponderJob
        {
            ProjectileResponderGroup = GetComponentDataFromEntity<ProjectileResponderData>(),
            ParentGroup = GetComponentDataFromEntity<Parent>(true),
            Ecb = Barrier.CreateCommandBuffer(),
        }.Schedule(StepPhysicsWorldSystem.Simulation,
            ref BuildPhysicsWorldSystem.PhysicsWorld, Dependency);

        Barrier.AddJobHandleForProducer(Dependency);
    }
}
