This section contains set up scripts


# Input System:

1. First, on your player, add a PlayerInput component.
2. Then hit create actions and set up the actions as you see fit. Each of these actions (e.g. Move) will become an implementable interface in your management script. Generate a C# script file for this, and name it X (Avoid names like PlayerInput, this will cause clash)
3. Create a ScriptableObject script Y (input manager) and at the top make it implement the interface above with:
```
public class Y : ScriptableObject, X.IPlayerActions
```
Making it implement the interface means implementing the On[Something] methods found in the C# script you generated: e.g.
```
public X playerInputInterface;
public void OnEnable()
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
4. Define every value that you want to be readable by other classes as properties that read their values from events when called.
Events allow you to trigger all methods that are subscribed to that event with event.Invoke(), with the input parameters of subscribing methods matching the event input declaration. For example the following line creates a MoveEvent event for which subscribers must take in a Vector2
```
public event UnityAction<Vector2> MoveEvent = delegate { };
```
