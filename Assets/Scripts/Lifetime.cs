using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[GenerateAuthoringComponent]
public struct LifetimeData : IComponentData
{
    public float lifetime;
}

public class LifetimeSystem : SystemBase
{
    EntityCommandBufferSystem Barrier;

    protected override void OnCreate()
    {
        base.OnCreate();
        Barrier = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = Barrier.CreateCommandBuffer().AsParallelWriter();
        var dt = Time.DeltaTime;

        Entities
            .ForEach((int entityInQueryIndex, Entity entity, ref LifetimeData life) =>
            {
                life.lifetime -= dt;
                if(life.lifetime < 0f)
                {
                    ecb.DestroyEntity(entityInQueryIndex, entity);
                }
            })
            .ScheduleParallel();

        Barrier.AddJobHandleForProducer(Dependency);
    }
}