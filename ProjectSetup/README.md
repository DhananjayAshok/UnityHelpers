## Input System:

1. First, on your player, add a PlayerInput component.
2. Then hit create actions and set up the actions as you see fit. Each of these actions (e.g. Move) will become an implementable interface in your management script. Generate a C# script file for this, and name it X (Avoid names like PlayerInput, this will cause clash)
3. Create a ScriptableObject script Y (input manager) and at the top make it implement the interface above with:
```
public class Y : ScriptableObject, X.IPlayerActions
```
Making it implement the interface means implementing the On[Something] methods found in the C# script you generated: e.g.
```
public X playerInputInterface;
public void EnableActions() // call this to enable actions, maybe on a start
{
    if (playerInputInterface == null)
    {
        playerInputInterface = new X();
    }
    playerInputInterface.Player.SetCallbacks(this);
    playerInputInterface.Enable();
}
public void OnMove(UnityEngine.InputSystem.InputAction.CallbackContext context)
{
    // Something
}
```
4. Define every sparsely occuring input (e.g. jumping) with an event that can add subscribers with event += MethodName;
Events allow you to trigger all methods that are subscribed to that event with event.Invoke(), with the input parameters of subscribing methods matching the event input declaration. For example the following line creates a MoveEvent event for which subscribers must take in a Vector2
```
using UnityEngine.InputSystem; // for InputAction and InputActionPhase
public event UnityAction<Vector2> JumpEvent = delegate { };

public void OnJump(InputAction.CallbackContext context) {
        switch (context.phase) {
            case InputActionPhase.Started:
                Jump.Invoke(true);
                break;
            case InputActionPhase.Canceled:
                Jump.Invoke(false);
                break;
        }
    }

// Elsewhere in the code:
    Y input; 
    void Start() {
            input.EnableActions();
            input.Jump += HandleJumpKeyInput;
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
5. Define every value you may want to constantly access as a property that has a getter function that will read from the input info when called on. For example for Move we may want to store the direction we are moving in and access it as needed with:
```
public Vector3 Direction => playerInputInterface.Player.Move.ReadValue<Vector2>(); // Returns a Vector3 even though the action in the PlayerInput object gives a Vector2
// Equivalent to:
public Vector3 Direction
{
    get
    {
        return playerInputInterface.Player.Move.ReadValue<Vector2>();
    }
}
```
See [git amend](https://www.youtube.com/watch?v=z5zShkCR0mg&ab_channel=git-amend) for a good reference
