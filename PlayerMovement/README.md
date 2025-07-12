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

## Jumping
To the start function of your playercontroller, add a listener to the [JumpEvent](../) method,
```
void Start() {
    input.EnablePlayerActions();
    input.JumpEvent += HandleJumpKeyInput;
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
