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

/**
 * When a spawner is spawning, we don't want to spawn an enemy on the player
 * This tag marks an entity to reconsider spawning
 */ 
[GenerateAuthoringComponent]
public struct SpawnerBlockerData: IComponentData
{
    public float degreesFrom;
}