using SharpDX;
using System.Windows.Input;
namespace MST_Heightmap_Generator_GUI
{
    /// <summary>
    /// A camera that can move freely in space with constant speed.
    /// Controling with arrow keys or WASD and mouse
    /// </summary>
    class FreeCamera : Camera
    {
        /// <summary>
        /// creates a new camera and sets a projection matrix up
        /// </summary>
        /// <param name="aspectRatio">Aspect ratio, defined as view space width divided by height. 
        ///                          To match aspect ratio of the viewport, the property AspectRatio.</param>
        /// <param name="fov">Field of view in the y direction, in radians.</param>
        /// <param name="nearPlane">Distance to the near view plane.</param>
        /// <param name="farPlane">Distance to the far view plane.</param>
        public FreeCamera(float aspectRatio, float fov = 1.309f, float nearPlane = 0.1f, float farPlane = 5000.0f) : 
                            base(aspectRatio, fov, nearPlane, farPlane)
        {
        }

        /// <summary>
        /// Speed in forward and backwards direction - pressing arrow up or w
        /// </summary>
        public float ForwardSpeed
        {
            get { return forwardSpeed; }
            set { forwardSpeed = value; }
        }


        /// <summary>
        /// Speed for side movements - pressing left/right arrow or a/d
        /// </summary>
        public float SideSpeed
        {
            get { return sideSpeed; }
            set { sideSpeed = value; }
        }

        /// <summary>
        /// Speed of the camera rotation - using the mouse
        /// </summary>
        public float RotationSpeed
        {
            get { return rotationSpeed; }
            set { rotationSpeed = value; }
        }

        // movement factors variables
        protected float rotationSpeed = 0.01f;
        protected float forwardSpeed = 0.08f;
        protected float sideSpeed = 0.08f;

        // some intern controlling variables
        protected double phi = 0.0f;
        protected double theta = 0.0f;
        protected double lastMouseX = 0; // last x position of the mouse
        protected double lastMouseY = 0; // last y position of the mouse

        private int forwardMovementCommand = 0;
        private int sideMovementCommand = 0;
        private int rotationXCommand = 0;
        private int rotationYCommand = 0;

        /// <summary>
        /// Updates the Camera 
        /// </summary>
        public override void Update(float passedTimeSinceLastFrame)
        {
            // mouse movement
            UpdateThetaPhiFromMouse(passedTimeSinceLastFrame);

            // resulting view direction
            viewDirection = new Vector3((float)(System.Math.Cos(phi) * System.Math.Sin(theta)),
                                        (float)(System.Math.Cos(theta)),
                                        (float)(System.Math.Sin(phi) * System.Math.Sin(theta)));
            // up vector - by rotation 90°
            double theta2 = theta + (float)System.Math.PI / 2.0f;
            Vector3 upVec = new Vector3((float)(System.Math.Cos(phi) * System.Math.Sin(theta2)),
                                        (float)(System.Math.Cos(theta2)),
                                        (float)(System.Math.Sin(phi) * System.Math.Sin(theta2)));
            // compute side
            Vector3 sideVec = Vector3.Cross(upVec, viewDirection);



            // forward movement
            Position += forwardMovementCommand * forwardSpeed * viewDirection;

            // side movement
            Position += sideMovementCommand * sideSpeed * sideVec;

            UpdateThetaPhiFromMouse(passedTimeSinceLastFrame);

            // compute view matrix
            viewMatrix = Matrix.LookAtLH(Position, Position + viewDirection, upVec);
        }

        /// <summary>
        /// intern helper to update view angles by mouse
        /// </summary>
        protected void UpdateThetaPhiFromMouse(float passedTimeSinceLastFrame)
        {
            // mouse movement
            double deltaX = (Mouse.GetPosition(null).X + rotationXCommand * 100) - lastMouseX;
            double deltaY = (Mouse.GetPosition(null).Y + rotationYCommand * 100) - lastMouseY;
            phi += deltaX * rotationSpeed;
            theta -= deltaY * rotationSpeed;
            lastMouseX = Mouse.GetPosition(null).X;
            lastMouseY = Mouse.GetPosition(null).Y;
        }
    }
}
