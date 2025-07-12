## Setup

Start by creating the following object hierarchy
```
playerObject (add playercontroller and rigidbody here)
└─── Model Root
    └─── Player model
└─── Camera Root
    └─── Camera Controls (Camera Controller here)
         └─── Camera Target (Marks ideal spot where you want camera to be relative to player)
              └─── Camera
```
## Simple Camera
First, set up the variables to store the current X and Y angles of the camera:
```
float currentXAngle;
float currentYAngle;
// in awake
currentXAngle = transform.localRotation.eulerAngles.x;
currentYAngle = transform.localRotation.eulerAngles.y;
```
In the update method, get the input Vector2 from the [Input Manager Interface](../ProjectSetup/README.md) LookDirection property, shift the expected angles and then update the values
```
// in Update()
currentXAngle += input.LookDirection.x * cameraSpeed * Time.deltaTime;
currentYAngle += horizontalInput * cameraSpeed * Time.deltaTime; // make this -= to invert
tr.localRotation = Quaternion.Euler(currentXAngle, currentYAngle, 0);
```
You can have an optional smoothing of speed, with clipping on the vertical angles:
```        
[Range(1f, 50f)] public float cameraSmoothingFactor = 25f;
    horizontalInput = Mathf.Lerp(0, input.LookDirection.x, Time.deltaTime * cameraSmoothingFactor);
    verticalInput = Mathf.Lerp(0, input.LookDirection.y, Time.deltaTime * cameraSmoothingFactor);
    
    currentXAngle += verticalInput * cameraSpeed * Time.deltaTime;
    currentYAngle += horizontalInput * cameraSpeed * Time.deltaTime;
    
    currentXAngle = Mathf.Clamp(currentXAngle, -upperVerticalLimit, lowerVerticalLimit);
```
