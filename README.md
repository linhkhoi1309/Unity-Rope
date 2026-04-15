# Unity Rope

## Overview 
This repository is served as a sandbox to explore all approaches to make 2D ropes in Unity. Feel free to use for educational purposes. Contact me for further clarifications at linhkhoiluong@gmail.com

---

## Hinge Joint 2D

A joint that allows a Rigidbody2D to rotate around a point, like a hinge.


### Collision

- Enable Collision  
  - Allows connected bodies to collide with each other  
  - Usually disabled for stability  

### Connected Body

- Connected Rigidbody 2D  
  - The object this joint is attached to  
  - If null → attaches to the world (fixed point)  
  - Useful for pendulum/swing behavior  

### Anchor Settings

- Anchor (X, Y)  
  - Pivot point on this object (local space)  
  - Defines where rotation happens  
  - Can be outside the sprite  

- Connected Anchor (X, Y)  
  - Pivot point on the connected body (or world)  

- Auto Configure Connected Anchor  
  - Automatically aligns anchors  
  - Disable for manual control  

### Motor

- Use Motor  
  - Enables automatic rotation  

- Motor Speed  
  - Target angular speed (degrees/second)  
  - Can be positive or negative  

- Maximum Motor Force  
  - Max torque applied to reach target speed  
  - Higher value → stronger motor  

### Limits

- Use Limits  
  - Restrict rotation angle  

- Lower Angle  
  - Minimum allowed rotation  

- Upper Angle  
  - Maximum allowed rotation  

### Notes

- Common use cases:
  - Door hinge  
  - Swing  
  - Rotating mechanism  

- Combine:
  - Motor + Limits → controlled rotation  

## Hinge Joint 2D with Ropes / Chains

### First Approach: Segment-Based Rope

#### Setup
1. Create a **Rope** GameObject  
2. Create multiple **segment (node) GameObjects** as children  
3. Add **Hinge Joint 2D** to each segment  
4. For each segment:
   - Connect it to the **previous (upper) segment**
   - Disable **Auto Configure Connected Anchor** (for Segment_0)
5. Attach **Segment_0** to an **Anchor object** (same position)

---

#### Pros
- Easy to set up  
- Little to no coding required  

#### Cons
- Less realistic rope behavior  
- Can stretch and become unstable  
- Collision is acceptable but not very accurate  

---

### Second Approach: Bone-Based Rope (2D Rigging)

#### Setup
1. Rig sprite:
   - Sprite Editor → Skinning Editor  
   - Create Bones → Auto Geometry → Generate  
2. Create a **Rope** GameObject  
3. Add **Sprite Skin** component  
4. For each bone:
   - Add **Hinge Joint 2D**
   - Connect to the **previous (upper) bone**
   - Disable **Auto Configure Connected Anchor** (for Bone_0)
5. Attach **Bone_0** to an **Anchor object** (same position)  
6. Tune parameters:
   - Rigidbody mass  
   - Joint angle limits  

---

#### Pros
- Smoother visual deformation  
- More natural-looking rope  

#### Cons
- Poor / unstable collision handling  
- More complex setup  
- Not recommended for physics-heavy interactions  

---

### Summary

- **Segment-based approach**  
  - Simple, decent physics, easier to control  

- **Bone-based approach**  
  - Better visuals, worse collision, more complex  

- For realistic physics → prefer **Verlet / PBD approach**
## Verlet Integration + Position-based Dynamics with Ropes

### Verlet Integration Overview

#### What is it
- A simple physics method:
  - Uses **positions only**
  - Does **NOT store velocity explicitly**
  - Suitable for **rope, cloth, soft bodies**

#### Core Formula

$$
x_{new} = 2x - x_{prev} + a \cdot dt^2
$$

#### Derivation (Proof)

Using Taylor expansion:

$$
x(t + dt) = x(t) + v(t)dt + \frac{1}{2}a(t)dt^2 + O(dt^3)
$$

$$
x(t - dt) = x(t) - v(t)dt + \frac{1}{2}a(t)dt^2 + O(dt^3)
$$

Add the two equations:

$$
x(t + dt) + x(t - dt) = 2x(t) + a(t)dt^2
$$

Rearrange:

$$
x(t + dt) = 2x(t) - x(t - dt) + a(t)dt^2
$$

#### Key Idea

- Velocity is **implicitly stored**:
  
$$
v \approx \frac{x - x_{prev}}{dt}
$$

- Motion is computed using:
  - Previous movement
  - Acceleration

### PBD (Position-Based Dynamics) Overview

#### Core idea
- Do **not** simulate forces directly
- Instead:
  1. Predict positions
  2. **Project positions to satisfy constraints**

#### Constraint form

$$
C(x) = 0
$$

- Example (rope):

$$
C(p_i, p_{i+1}) = |p_i - p_{i+1}| - L
$$

- Meaning:
  - = 0 → correct length  
  - \> 0 → stretched  
  - < 0 → compressed  

#### How constraints are solved
- Compute constraint error
- Move points to reduce error
- Repeat multiple times (iterative solver)


### Application: Rope

#### Step 1: Initialize rope
- Create multiple rope segments
- Each segment stores:
  - Current position
  - Previous position
- Place segments with fixed spacing

#### Step 2: Simulate motion (Verlet Integration)

$$
x_{new} = 2x - x_{prev} + a \cdot dt^2
$$

- Uses:
  - Previous movement  
  - Acceleration (gravity)  
- No explicit velocity  

#### Step 3: Anchor the rope
- Fix the first segment to a target (e.g. mouse)  
- Defines rope starting point  

#### Step 4: Apply distance constraints

$$
|p_i - p_{i+1}| = segmentLength
$$

- Compute distance error  
- Adjust positions to maintain length  
- Move both points (except anchor)  

#### Step 5: Repeat constraints
- Iterate multiple times per frame  
- More iterations → more rigid rope  

#### Step 6: Handle collisions
- Detect overlaps with colliders  
- Resolve by:
  - Pushing segment out  
  - Reflecting movement (bounce)  

- Rebuild previous position:

$$
x_{prev} = x_{current} - velocity
$$

#### Step 7: Render rope
- Draw segments using LineRenderer  

## References

- [Hinge Joint 2D - Official Unity Tutorial, Unity](https://www.youtube.com/watch?v=l6awvCT29yU)
- [Creating Rope Objects with Physics | Unity Tutorial, Sasquatch B Studios](https://www.youtube.com/watch?v=iGlD3f-5JpA&list=PLfmYNuLHEy-PQ6j6kki9kmM3Z5CayRSI0) 
- [Make a custom rope (with collisions) using VERLET Integration (Unity Tutorial), Sasquatch B Studios](https://www.youtube.com/watch?v=bxG3XP4MVzk&list=PLfmYNuLHEy-PQ6j6kki9kmM3Z5CayRSI0)
- [Verlet Rope in Games, Michael Palmos](https://toqoz.fyi/game-rope.html)
