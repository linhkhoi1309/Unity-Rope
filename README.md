# Unity-Rope

## Hinge Joint 2D

- Enable Collision: should rigid bodies connected with this joint collide
- Connected Rigid Body (Rigidbody 2D):
    + if unassigned, the object will act as a swing when colliding with other objects.

- Auto Configure Connected Anchor 
- Anchor (X, Y): the point that the sprite will rotate around relative to the sprite in local space, doesn't need to be within bounds of the sprite
- Connected Anchor (X, Y)
- Use Motor
    + Motor Speed (degree/second, can be negative): target angular speed that the motor try to reach
    + Maximum Motor Force: the maximum force the motor can use to achieve the desired motor target speed
- Use Limits
- Angle Limits:
    + Lower Angle
    + Upper Angle

## Hinge Joint 2D with Ropes / Chains

### First approach:

1. Create a Rope game object 
2. Create a list of node or segment game objects and place it inside the Rope 
3. Assign Hinge Joint 2D component for each segment object.
4. Assign the connected rigidbody (in Hinge Joint 2D component) for each its upper segment consecutively (turn off Auto Configure Connected Anchor for the Segment_0)
5. Segment_0 assign with an Anchor object (Anchor object is a Segment object has the same position with the Segment_0)

#### Pros:
- Easy to setup
- No code or almost no code
#### Cons:
- Rope physics effect is not as good as second and Verlet Intergration approach
- When stretching out too much, weird effects might happen.
- Collision detection is acceptable but not as good as Verlet Integration approach

### Second approach:
1. Rigging: Sprite Editor -> Skinning Editor -> Create Bones -> Auto Geometry -> Generate For Selected
2. Create a Rope game object 
3. Assign Sprite Skin component -> Create bones -> Assign Hinge Joint 2D component for each children bone object -> Assign the connected rigidbody (in Hinge Joint 2D component) for each its upper bone consecutively (turn off Auto Configure Connected Anchor for the Bone_0) -> Bone_0 assign with an Anchor object (Anchor object is like a Bone object has the same position with the Bone_0)
4. Tuning for better effect (Rigidbody's mass, Hinge Joint 2D's Angle limits) 

#### Pros:
- Smoother rope physics effects
- No code or almost no code
#### Cons:
- Weird collision detection -> Not recommended to use
- Harder to setup & configure

## Hinge Joint 2D with Verlet Integration

## References

- [Hinge Joint 2D - Official Unity Tutorial, Unity](https://www.youtube.com/watch?v=l6awvCT29yU)
- [Creating Rope Objects with Physics | Unity Tutorial, Sasquatch B Studios](https://www.youtube.com/watch?v=iGlD3f-5JpA&list=PLfmYNuLHEy-PQ6j6kki9kmM3Z5CayRSI0) 
- [Make a custom rope (with collisions) using VERLET Integration (Unity Tutorial), Sasquatch B Studios](https://www.youtube.com/watch?v=bxG3XP4MVzk&list=PLfmYNuLHEy-PQ6j6kki9kmM3Z5CayRSI0)
