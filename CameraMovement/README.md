## Setup

Start by creating the following object hierarchy
```
playerObject (add playermovement and cameramovement here)
└───Player Root (add rigidbody and colider here)
    └─── Model Root
        └─── Model (animator here)
└─── Camera Root
    └─── Targets (centered at root)
         └─── Walk Target (positioned at the desired offset)
         └─── Run Target
    └─── Camera Holder
         └─── Camera
```
## Simple Camera
First, set up the variables for the speed, the clipping on rotation for vertical, the offset by which we'll look at the player, etc
```
        [Header("Camera Settings")]
        public float cameraSpeed = 100f;
        public float xLowLimit = -20;
        public float xHighLimit = 20;
        public float heightOffset = 1.5f;
        public float lookEpsilon = 0.05f;
        public float cameraSmoothTime = 2f;
```
Then, add references to the tranforms 
```
        [Header("Camera Target References")]
        public Transform playerTransform; // Player Root
        public Transform cameraTransform; // Camera Holder
        public Transform targetTransform; // Targets
        public Transform walkTarget;  // Walk Target
        public Transform sprintTarget; // Run Target

        private Transform currentTarget;
        private Transform followTarget; // Can either be player or something you want to look at e.g. locked in enemy
        private float realXLowLimit; //  To handle -ve to 360 angle conversion
```
Then, in Awake, set up the required references and convert the angle to 360. We also make functions to clamp the angle and change the target
```
        void Awake()
        {
            if (xLowLimit < 0) {
                realXLowLimit = 360 + xLowLimit;
            }
            currentTarget = walkTarget;
            followTarget = playerTransform;
        }

        float ClampXAngle(float angle) {
            if (angle > realXLowLimit) { // whatever to 360
                return angle; 
            }
            else if (angle < xHighLimit) { // 0 to whatever
                return angle;
            }
            // need to clamp. First find the closest limit
            float distToLow = Mathf.Abs(angle - realXLowLimit);
            float distToHigh = Mathf.Abs(angle - xHighLimit);
            if (distToLow < distToHigh) {
                return realXLowLimit;
            } else {
                return xHighLimit;
            }
        }

        public void Sprint() {
            currentTarget = sprintTarget;
        }

        public void Walk() {
            currentTarget = walkTarget;
        }
```

Make a function to update the targets rotation based on the input from the player
```
// Assumes inputManager has public Vector2 lookDirection => playerActionInput.Player.Look.ReadValue<Vector2>();
        void UpdateTargets() {
            float newXAngle = targetTransform.localEulerAngles.x - inputManager.lookDirection.y * cameraSpeed * Time.deltaTime;
            float newYAngle = targetTransform.localEulerAngles.y + inputManager.lookDirection.x * cameraSpeed * Time.deltaTime;
            if (inputManager.lookDirection.y != 0)
            {
                newXAngle = ClampXAngle(newXAngle);
            }
            targetTransform.localRotation = Quaternion.Euler(newXAngle, newYAngle, 0);
            targetTransform.position = playerTransform.position;
        }
```

Finally, in the *same* Update function as the playerMovement (so FixedUpdate) to avoid jitter, rotate the camera target and smoothly move the camera towards it

```
 void FixedUpdate()
 {
     UpdateTargets();
     cameraTransform.position = Vector3.Lerp(cameraTransform.position, currentTarget.position, Time.deltaTime * 10f);
     // smoothly look at the follow target
     Vector3 lookTarget = followTarget.position + Vector3.up * heightOffset;
     Quaternion lookQuaternion = Quaternion.LookRotation(lookTarget - cameraTransform.position);
     if (Quaternion.Angle(cameraTransform.rotation, lookQuaternion) > lookEpsilon)
     {
         cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, lookQuaternion, cameraSmoothTime * Time.deltaTime * 10f);
     }
 }
```

To avoid collisions, you can cast a sphere between the camera position and its intended target, register collisions with the environment, and then set a virtual target that stops before the colliding object
