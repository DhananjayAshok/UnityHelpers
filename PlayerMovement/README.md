First, set up the [input system](../ProjectSetup/README.md) with Direction and LookDirection Vector3 properties. The following is for a 3rd person controller. 

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
Vector2 inputDirection = input.moveDirection; 
float inputMagnitude = inputDirection.magnitude; // You can accentuate sensitivity to small input using kernel functions like x^2 or e^x here
Vector3 moveDirection = (playerRight * inputDirection.x + playerForward * inputDirection.y).normalized * inputMagnitude;
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
void FixedUpdate()
{
    SetVelocity(newVelocity);
}
```
## Basic Momentum based movement

We can add lingering momentum to make movement more realistic, specifically when jumping through the air

Track the momentum of the player in a Vector3 object, and use it to add to the final velocity. Clamp the magnitude to make sure it doesn't blow up on you. 
```
Vector3 momentum; 
velocity = velocity + (1-dampingFactor) * momentum;
velocity = Vector3.ClampMagnitude(velocity, maximumSpeed); // Clamp the velocity to the maximum speed
momentum = velocity; 
```

## Better Momentum

You could get a breakdown of the current momentum into vertical and horizontal components using [Vector Math](https://github.com/adammyhre/Advanced-Player-Controller/blob/master/Assets/_Project/Scripts/PlayerController/VectorMath.cs), or just set up one variable for each type of momentum
```
Vector3 verticalMomentum;
Vector3 horizontalMomentum;
```
Then incorporate gravity (public variable float) into the vertical momentum (set it to 0 if you are grounded and momentum is downwards)
```
verticalMomentum += directionToGravityCentre * (gravity * Time.deltaTime); // where directionToGravityCentre is Vector3(0, -1, 0) for world and -transform.up for local gravity.
```
Incorporate a state-based horizontal friction:
```
float friction = isGrounded ? groundFriction : airFriction;
horizontalMomentum = Vector3.MoveTowards(horizontalMomentum, Vector3.zero, friction * Time.deltaTime); // MoveTowards makes sure we don't overshoot into negatives
```
Recombine the directions:
```
momentum = horizontalMomentum + verticalMomentum;
```

Do not use damping here; you are essentially handling the damping with friction.  
Note, for all of these, if you want to use world momentum v.s. local momentum you can add:
```
true_momentum = useLocalMomentum ? tr.localToWorldMatrix * momentum : momentum;
```

## Ground Checking
To check for the ground, we send out a downward Raycast from the centre of the player that stops right below their feet and store the result. First we properly set up the layermask in preparation for the cast
```
LayerMask RecalculateSensorLayerMask() {
    int objectLayer = gameObject.layer;
    int layerMask = Physics.AllLayers;

    for (int i = 0; i < 32; i++) {
        if (Physics.GetIgnoreLayerCollision(objectLayer, i)) {
            layerMask &= ~(1 << i);
        }
    }
    
    int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
    layerMask &= ~(1 << ignoreRaycastLayer);
    return layerMask; 
}
RaycastHit hitInfo; // save the hit info
float castLength = 4; // todo: figure out how to set height properly considering step size
Vector3 origin = Vector3.zero; // offset vector for where to throw it from
public void Cast() {
    layerMask = RecalculateSensorLayerMask();
    Vector3 worldOrigin = transform.TransformPoint(origin);
    Vector3 worldDirection = -transform.down;
    Physics.Raycast(worldOrigin, worldDirection, out hitInfo, castLength, layermask, QueryTriggerInteraction.Ignore);
    // if needed Debug.DrawRay(hitInfo.point, hitInfo.normal, Color.red, Time.deltaTime);
}
public bool HasDetectedHit() => hitInfo.collider != null;
public float GetDistance() => hitInfo.distance;
public Vector3 GetNormal() => hitInfo.normal;
public Vector3 GetPosition() => hitInfo.point;
public Collider GetCollider() => hitInfo.collider;
public Transform GetTransform() => hitInfo.transform;
isGrounded = HasDetectedHit();
```

## Jumping
To the start function of your playercontroller, add a listener to the [JumpEvent](../ProjectSetup/README.md) method
```
bool jumpKeyIsPressed;    // Tracks whether the jump key is currently being held down by the player
bool jumpKeyWasPressed;   // Indicates if the jump key was pressed since the last reset, used to detect jump initiation
bool jumpKeyWasLetGo;     // Indicates if the jump key was released since it was last pressed, used to detect when to stop jumping
bool jumpInputIsLocked;   // Prevents jump initiation when true, used to ensure only one jump action per press

void Start() {
    input.EnablePlayerActions();
    input.JumpEvent += HandleJumpKeyInput;
}

void ResetJumpKeys() { // call this at the end of every fixed update
    jumpKeyWasLetGo = false;
    jumpKeyWasPressed = false;
}

void HandleJumpKeyInput(bool isButtonPressed) {
    if (!jumpKeyIsPressed && isButtonPressed) {
        jumpKeyWasPressed = true;
    }

    if (jumpKeyIsPressed && !isButtonPressed) {
        jumpKeyWasLetGo = true;
        jumpInputIsLocked = false;
    }
    
    jumpKeyIsPressed = isButtonPressed;
}
```
This releases a jump lock when we let go of the jump button and allows us to jump again. To add this into the motion, first check whether we are in a jumping state (logic based on grounded + inputs etc.) and if so then override (dont add) the vertical momentum:
```
verticalMomentum = transform.up * jumpSpeed;
momentum = horizontalMomentum + verticalMomentum;
```

## Sliding 

You can check if the ground you are on is too steep using the normal of the ground that you detected in the raycast. If you are grounded and the ground is too steep you are in sliding mode, otherwise just on the ground. For movement we'll want to then add a slide down along the slope, given by the projection of the down direction onto the plane normal. We decompose this into the horizontal and vertical components before adding it in
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
bool IsGroundTooSteep() => Vector3.Angle(GetNormal(), tr.up) > slopeLimit; \\ only meaningful if grounded
Vector3 slopeMomentum = Vector3.ProjectOnPlane(mover.GetGroundNormal(), tr.down).normalized;
Vector3 slopeVertical = VectorMath.ExtractDotVector(slopeMomentum, tr.up);
Vector3 slopeHorizontal = slopeMomentum - slopeVertical;
verticalMomentum += slopeVertical;
horizontalMomentum += slopeHorizontal;
momentum = horizontalMomentum + verticalMomentum;
```
