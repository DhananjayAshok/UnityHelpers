First, set up the (ProjectSetup/README.md)[input system] with Direction and LookDirection Vector3 properties. The following is for a 3rd person controller. 

## Basic Player Movement
Calculating movement direction is easy if you just want to use the player transform:
```
Vector3 playerRight = transform.right.normalized; // it may already be normalized idk
Vector3 playerForward = transform.forward.normalized;
```
If we want to move based on the direction the camera is facing (mostly what we'll want), we must project the input movement along the player's transform.up to cut out any local y-axis movement:
```
Vector3 playerRight = Vector3.ProjectOnPlane(cameraTransform.right, transform.up).normalized;
Vector3 playerForward = Vector3.ProjectOnPlane(cameraTransform.forward, transform.up).normalized;
```
We can then use the input system (input) direction property to determine the final direction and give a velocity based on the state we are in (for example grounded v.s. not grounded)
```
Vector3 moveDirection = (playerRight * input.Direction.x + playerForward * input.Direction.y).normalized; // norm might not be needed
Vector3 velocity = isGrounded? moveDirection * groundedSpeed : moveDirection * airSpeed;
```
With a rigidbody based character controller, we can create motion using:
```
Rigidbody rb;
void Setup() {
    rb = GetComponent<Rigidbody>();
    rb.freezeRotation = true;
    rb.useGravity = false;
}
public void SetVelocity(Vector3 velocity) => rb.linearVelocity = velocity;

SetVelocity(velocity); 
```
## Basic Momentum based movement

We can add lingering momentum to make movement more realistic, specifically when jumping through the air (if we can't move much in mid-air)

Track the momentum of the player in a Vector3 object, and use it to add to the final velocity. 
```
velocity = velocity + dampingFactor * momentum;
momentum = velocity; 
```

## Better Momentum

First, get a breakdown of the current momentum into vertical and horizontal components using (https://github.com/adammyhre/Advanced-Player-Controller/blob/master/Assets/_Project/Scripts/PlayerController/VectorMath.cs)[Vector Math]
```
/// <summary>
/// Extracts and returns the component of a vector that is in the direction of a given vector.
/// </summary>
/// <param name="vector">The vector from which to extract the component.</param>
/// <param name="direction">The direction vector to extract along.</param>
/// <returns>The component of the vector in the direction of the given vector.</returns>
public static Vector3 ExtractDotVector(Vector3 vector, Vector3 direction) {
    direction.Normalize();
    return direction * Vector3.Dot(vector, direction);
}
Vector3 verticalMomentum = VectorMath.ExtractDotVector(momentum, tr.up);
Vector3 horizontalMomentum = momentum - verticalMomentum;
```
Then incorporate gravity (public variable float) into the vertical momentum (set it to 0 if you are grounded and the momentum is downwards)
```
verticalMomentum += directionToGravityCentre * (gravity * Time.deltaTime); // where directionToGravityCentre is Vector3(0, -1, 0) for world and -transform.up for local gravity.
```
Incorporate a state-based horizontal friction:
```
float friction = isGrounded ? groundFriction : airFriction;
horizontalMomentum = Vector3.MoveTowards(horizontalMomentum, Vector3.zero, friction * Time.deltaTime); \\ MoveTowards makes sure we don't overshoot into negatives
```
Recombine the directions:
```
momentum = horizontalMomentum + verticalMomentum;

```
Note, for all of these, if you want to use world momentum v.s. local momentum you can add:
```
true_momentum = useLocalMomentum ? tr.localToWorldMatrix * momentum : momentum;
```
