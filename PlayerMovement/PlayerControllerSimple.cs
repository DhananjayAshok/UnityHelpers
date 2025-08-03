using Unity.VisualScripting;
using UnityEngine;
using System; 

public enum MovementState
{
    Still,
    Walking,
    Sprinting,
    Sliding,
    Focusing,
    Dodging
}

public enum GroundState
{
    Grounded,
    Jumping,
    Falling, 
    FallingInjured
}


public class PlayerController : MonoBehaviour
{
    // give a rigidBody and coller to the player
    public Transform cameraTransform;
    public bool cameraRelativeMovement = true; // If true, the player will move relative to the camera's orientation


    // Player movement settings
    public float walkSpeed = 5f; // Speed of the player movement
    public float sprintSpeed = 7.5f; // Speed of the player when sprinting
    public float focusSpeed = 2.5f; // Speed of the player when guarding
    public float slideSpeed = 7f; // Speed of the player when sliding
    public float jumpSpeed = 10f; // Speed of the player when jumping
    public float jumpHorizontalSpeedDecay = 0.02f; // Resistance applied to horizontal velocity when jumping
    public float gravity = 20f; // Gravity applied to the player
    public float dodgeSpeed = 13f; // Speed of the player when dodging
    public float dodgeTime = 0.15f; // Duration of the dodge action

    // Spherecast settings
    public float sphereCastRadius = 0.01f; // Radius of the sphere cast for ground detection
    public float sphereCastCenterOffset = 0f; // Offset from the center of the player for the sphere cast based on the player's height

    // Lag step
    [Range(0, 50)]
    public int walkLagFrames = 10; // Number of frames to continue motion after input stops
    [Range(0, 50)]
    public int sprintLagFrames = 20;
    [Range(0, 50)]
    public int focusLagFrames = 0;
    [Range(0f, 1f)]
    public float walkLagDampening = 0.5f; // Factor to reduce velocity each frame during lag
    [Range(0f, 1f)]
    public float sprintLagDampening = 0.1f;
    [Range(0f, 1f)]
    public float focusLagDampening = 0.2f;
    public float forceDampCutMag = 0.1f; // Magnitude below which the force is considered negligible

    private int nLagFramesRemaining; 
    private bool isLagging = false;
    private float dodgeTimer; // Time when the dodge started
    private float dodgeEndTime; // Time when the dodge should end
    MovementState preDodgeState; // Store the state before dodging
    private Vector3 dodgeDirection = Vector3.zero; // Direction of the dodge
    private float jumpTimer; // Timer for the jump action
    private float jumpEndTime; // Time when the jump should end
    private Vector3 horizontalJump = Vector3.zero; // Direction of the jump, if needed
    private Vector3 verticalJump = Vector3.zero;

    public MovementState movementState = MovementState.Still; // Current movement state of the player
    public GroundState groundState = GroundState.Grounded; // Current ground state of the player
    public InputManager input;

    Vector3 previousVelocityInput = Vector3.zero;
    Vector3 currentVelocityInput = Vector3.zero;
    private Rigidbody rb;
    private Collider col;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        rb.freezeRotation = true;
        rb.useGravity = false;
        input.sprintEvent += HandleSprint;
        input.dodgeEvent += HandleDodge;
        input.JumpEvent += HandleJump;
        ResetLagFrames();
    }
    void ResetLagFrames() {
        isLagging = false;
        if (movementState == MovementState.Walking)
        {
            nLagFramesRemaining = walkLagFrames;
        }
        else if (movementState == MovementState.Sprinting)
        {
            nLagFramesRemaining = sprintLagFrames;
        }
        else if (movementState == MovementState.Focusing)
        {
            nLagFramesRemaining = focusLagFrames;
        }
        else
        {
            nLagFramesRemaining = walkLagFrames; // No lag frames for other states
        }
    }

    void CheckForGround()
    {
        Vector3 sphereCastOrigin = transform.position - transform.up * sphereCastCenterOffset;
        RaycastHit hit;
        // do the sphere cast to check if the player is grounded
        // Create a layermask that only includes the ground layer
        LayerMask groundLayer = LayerMask.GetMask("Ground");
        // no query trigger interaction
        Physics.Raycast(sphereCastOrigin, -transform.up, out hit, sphereCastRadius + 0.1f, groundLayer, QueryTriggerInteraction.Ignore);
        // Debug the hit
        if (hit.collider != null)
        {
            if (groundState != GroundState.Jumping)
            {
                groundState = GroundState.Grounded; // If the sphere cast hits something, the player is grounded
            }
        }
        else
        {
            if ( groundState != GroundState.Jumping)
            {
                groundState = GroundState.Falling;
            }
        }
    }

    float getLagDampening() {
        if (movementState == MovementState.Walking || movementState == MovementState.Still)
        {
            return walkLagDampening;
        }
        else if (movementState == MovementState.Sprinting)
        {
            return sprintLagDampening;
        }
        else if (movementState == MovementState.Focusing)
        {
            return focusLagDampening;
        }
        else
        {
            return 1f; // Full dampening for other states
        }
    }
    void HandleSprint()
    {
        if (movementState != MovementState.Sprinting)
        {
            if (groundState != GroundState.FallingInjured)
            {
                movementState = MovementState.Sprinting;
            }
        }
        else
        {
            movementState = MovementState.Walking;
        }
    }

    void HandleDodge()
    {
        if (groundState != GroundState.Grounded) {
            return; // Prevent dodging while in the air
        }
        if (movementState == MovementState.Dodging)
        {
            return; // Prevent starting a new dodge if already dodging
        }
        else
        {
            preDodgeState = movementState; // Store the current state before dodging
            movementState = MovementState.Dodging; // Start dodging
            dodgeDirection = currentVelocityInput.normalized; // Use the current input direction for dodging
            if (dodgeDirection == Vector3.zero) {
                dodgeDirection = -transform.forward; // Default to backward if no input
            }
            dodgeTimer = Time.time;
            dodgeEndTime = dodgeTimer + dodgeTime; // Set the end time for the dodge
        }
    }

    void HandleJump()
    {
        if (groundState == GroundState.Grounded)
        {
            SetMoveDirection();
            horizontalJump = GetCurrentSpeed() * currentVelocityInput;
            if (horizontalJump.magnitude < forceDampCutMag)
            {
                horizontalJump = Vector3.zero; // If the jump velocity is too small, set it to zero
            }
            groundState = GroundState.Jumping; // Set the ground state to jumping
            verticalJump = transform.up * jumpSpeed;
            SetVelocity(verticalJump + horizontalJump);
        }
    }

    void EndDodge() {
        dodgeTimer = 0f; // Reset dodge timer
        dodgeEndTime = 0f;
        dodgeDirection = Vector3.zero; // Reset dodge direction
        movementState = preDodgeState; // Return to the previous state Will need to change this if we want to dodge into a state like attacked or sliding
    }

    public void SetMoveDirection() {
        Vector3 playerRight;
        Vector3 playerForward;
        if (cameraRelativeMovement)
        {
            playerRight = Vector3.ProjectOnPlane(cameraTransform.right, transform.up).normalized;
            playerForward = Vector3.ProjectOnPlane(cameraTransform.forward, transform.up).normalized;
        }
        else
        {
            playerRight = transform.right.normalized; // it may already be normalized idk
            playerForward = transform.forward.normalized;
        }
        
        Vector2 inputDirection = input.moveDirection; 
        float inputMagnitude = inputDirection.magnitude;
        // inputMagnitude = (float) Math.Pow(inputMagnitude, 2);
        Vector3 moveDirection = (playerRight * inputDirection.x + playerForward * inputDirection.y).normalized * inputMagnitude;
        currentVelocityInput = moveDirection;
    }

    float GetCurrentSpeed() {
        // can modify to include states
        switch (movementState)
        {
            case MovementState.Walking:
                return walkSpeed;
            case MovementState.Sprinting:
                return sprintSpeed;
            case MovementState.Focusing:
                return focusSpeed;
            case MovementState.Sliding:
                return slideSpeed;
            case MovementState.Dodging:
                return dodgeSpeed;
            default:
                return walkSpeed;
        }
    }

    public void SetVelocity(Vector3 velocity) {
        rb.linearVelocity = velocity;
    } 

    // Update is called once per frame
    void FixedUpdate()
    {
        CheckForGround();
        if (groundState == GroundState.Grounded)
        {
            SetMoveDirection();
            if (movementState == MovementState.Dodging)
            {
                dodgeTimer += Time.fixedDeltaTime;
                if (dodgeTimer >= dodgeEndTime)
                {
                    EndDodge(); // End the dodge if the time is up
                    SetVelocity(Vector3.zero);
                    previousVelocityInput = Vector3.zero;
                }
                else
                {
                    // Apply dodge velocity in the dodge direction
                    Vector3 dodgeVelocity = dodgeDirection * dodgeSpeed;
                    SetVelocity(dodgeVelocity);
                    previousVelocityInput = dodgeDirection; // This technically doesn't matter
                }
            }
            else
            {

                Vector3 newVelocity = currentVelocityInput * GetCurrentSpeed();
                if (currentVelocityInput == Vector3.zero && previousVelocityInput != Vector3.zero && nLagFramesRemaining > 0 && movementState != MovementState.Focusing)
                {
                    isLagging = true;
                    Vector3 lagVelocity = GetCurrentSpeed() * previousVelocityInput * (1 - getLagDampening());
                    if (lagVelocity.magnitude < forceDampCutMag)
                    {
                        lagVelocity = Vector3.zero; // If the lag velocity is too small, set it to zero
                        nLagFramesRemaining = 0; // Reset lag frames if the velocity is negligible
                        previousVelocityInput = Vector3.zero; // Reset previous input velocity
                        movementState = MovementState.Still;
                    }
                    else
                    {
                        nLagFramesRemaining--;
                        previousVelocityInput = previousVelocityInput * (1 - getLagDampening()); // Reduce previous input velocity by dampening factor
                    }
                    SetVelocity(lagVelocity);
                }
                else
                {
                    if (newVelocity == Vector3.zero)
                    {
                        movementState = MovementState.Still;
                    }
                    else
                    {
                        if (movementState != MovementState.Sprinting)
                        {
                            movementState = MovementState.Walking;
                        }
                    }
                    ResetLagFrames();
                    SetVelocity(newVelocity);
                    previousVelocityInput = currentVelocityInput;
                }
            }
        }
        else if(groundState == GroundState.Jumping || groundState == GroundState.Falling)
        {
            horizontalJump = horizontalJump * (1 - jumpHorizontalSpeedDecay); // Decay horizontal jump speed
            verticalJump -= Vector3.up * gravity * Time.fixedDeltaTime; // Apply gravity to vertical jump speed
            if (verticalJump.y <= 0) {
                groundState = GroundState.Falling; // If the vertical speed is negative, the player is falling
            }
            SetVelocity(horizontalJump + verticalJump); // Set the velocity to the jump speed
        }
    }
}
