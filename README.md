## Concept
> Heavily inspired from Super Stardust (PS3, A top down arena game made with Unity's DOTS)
Survive as long as possible against the hordes of bugs trying to eliminate you.

## Features
* Movement around a spherical surface with the ability to dash
* 2 different weapons that can be upgraded by killing monsters
* Different enemies (that may either shoot you, move towards you and can be damaged only by a certain weapon)

## Risks
~~I'll likely utilize the Physics system for overlap checks, but I'm not sure how much of the physics is implemented for DOTS.~~ There seems to be [plenty of info](https://docs.unity3d.com/Packages/com.unity.physics@0.6/manual/getting_started.html).

## Tasks
* [X] Level
* [X] Moveable entity
* [X] Shooting
* [] Enemy
* [] Damage
* [] Spawner
### Stretch
* [] Enemy movement behavior
* [] Enemy shooting behavior
* [] Weapon upgrades
* [] ?Setup build?

## Notes/Research
* I usually used ConvertToEntity but that is [discouraged](https://docs.unity3d.com/Packages/com.unity.entities@0.16/manual/conversion.html#scene-conversion)?. I'm not sure what the alternatives are. From [talks](https://www.youtube.com/watch?v=BNMrevfB6Q0) and the page self variables should be instantiated using a mix of `GameObjectConversionSystem.GetPrimaryEntity`, `GameObjectConversionSystem.ConvertGameObjectHierarchy` and `GameObjectConversionSystem.CreateAdditionalEntity`. It's something I may want to consider for the future.