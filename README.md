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