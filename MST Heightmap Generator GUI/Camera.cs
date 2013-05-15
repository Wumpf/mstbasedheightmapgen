using SharpDX;
using System.Windows.Input;

namespace MST_Heightmap_Generator_GUI
{
    /// <summary>
    /// Abstract class for cameras
    /// you can easily make your own camera by inheriting this class
    /// </summary>
    class Camera
    {
        // matrices
        private Matrix projectionMatrix;
        private Matrix viewMatrix = Matrix.Identity;
        // vectors
        private Vector3 viewDirection = new Vector3(0, 0, 1);
        private Vector3 position = new Vector3(0, 1, 0);
        
        // projection stuff
        public float AspectRatio
        {
            get { return aspectRatio; }
            set { aspectRatio = value; RebuildProjectionMatrix(); }
        }
        private float aspectRatio;
        private float fov;
        private float nearPlane;
        private float farPlane;

        // movement factors variables
        private float rotationSpeed = 0.01f;
        private float forwardSpeed = 0.8f;
        private float speedUpFactor = 10.0f;
        private float sideSpeed = 0.8f;
		protected float upSpeed = 0.7f;

        // some intern controlling variables
        private double phi = 0.0f;
        private double theta = -MathUtil.Pi/2;
        private double lastMouseX = 0; // last x position of the mouse
        private double lastMouseY = 0; // last y position of the mouse


        /// <summary>
        /// creates a new camera and sets a projection matrix up
        /// </summary>
        /// <param name="aspectRatio">Aspect ratio, defined as view space width divided by height. 
        ///                          To match aspect ratio of the viewport, the property AspectRatio.</param>
        /// <param name="fov">Field of view in the y direction, in radians.</param>
        /// <param name="nearPlane">Distance to the near view plane.</param>
        /// <param name="farPlane">Distance to the far view plane.</param>
        public Camera(float aspectRatio, float fov, float nearPlane, float farPlane)
        { 
            this.aspectRatio = aspectRatio;
            this.fov = fov;
            this.nearPlane = nearPlane;
            this.farPlane = farPlane;
            RebuildProjectionMatrix();
        }

        /// <summary>
        /// The projection matrix for this camera
        /// </summary>
        public Matrix ProjectionMatrix
        {
            get { return projectionMatrix; }
        }

        /// <summary>
        /// The view matrix for this camera
        /// </summary>
        public Matrix ViewMatrix
        {
            get { return viewMatrix; }
        }

        /// <summary>
        /// Current position of the camera
        /// </summary>
        public Vector3 Position
        {
            get { return position; }
            set { position = value; } 
        }

        /// <summary>
        /// Current view-direction of the camera
        /// </summary>
        public Vector3 Direction
        {
            get { return viewDirection; }
        }

        /// <summary>
        /// Intern function for recreating the projection matrix.
        /// Capsuling the Matrix.Create... makes it easy to exchange the type of projection
        /// </summary>
        private void RebuildProjectionMatrix()
        {
            projectionMatrix = Matrix.PerspectiveFovLH(fov, aspectRatio, nearPlane, farPlane);
        }

        /// <summary>
        /// Updates the Camera 
        /// </summary>
        public void Update(float passedTimeSinceLastFrame)
        {
            // mouse movement
            UpdateThetaPhiFromMouse(passedTimeSinceLastFrame);

            // resulting view direction
            viewDirection = new Vector3((float)(System.Math.Cos(phi) * System.Math.Sin(theta)),
                                        (float)(System.Math.Cos(theta)),
                                        (float)(System.Math.Sin(phi) * System.Math.Sin(theta)));
            // up vector - by rotation 90°
            float theta2 = (float)theta + (float)System.Math.PI / 2.0f;
            Vector3 upVec = new Vector3((float)(System.Math.Cos(phi) * System.Math.Sin(theta2)),
                                        (float)(System.Math.Cos(theta2)),
                                        (float)(System.Math.Sin(phi) * System.Math.Sin(theta2)));
            // compute side
            Vector3 sideVec = Vector3.Cross(upVec, viewDirection);

            float speedUp = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ? speedUpFactor : 1.0f;

            // forward movement
            float forward = (Keyboard.IsKeyDown(Key.W) ? 1.0f : 0.0f) - (Keyboard.IsKeyDown(Key.S) ? 1.0f : 0.0f);
            Position += forward * forwardSpeed * viewDirection * speedUp;

            // side movement
            float side = (Keyboard.IsKeyDown(Key.D) ? 1.0f : 0.0f) - (Keyboard.IsKeyDown(Key.A) ? 1.0f : 0.0f);
            Position += side * sideSpeed * sideVec * speedUp;

            // upward movement
            float up = Keyboard.IsKeyDown(Key.Space) ? 1.0f : 0.0f;
            Position += up * upSpeed * upVec;

            // compute view matrix
            viewMatrix = Matrix.LookAtLH(Position, Position + viewDirection, upVec);
        }

        /// <summary>
        /// intern helper to update view angles by mouse
        /// </summary>
        protected void UpdateThetaPhiFromMouse(float passedTimeSinceLastFrame)
        {
            if (Mouse.RightButton == MouseButtonState.Pressed)
            {
                // mouse movement
                double deltaX = Mouse.GetPosition(null).X - lastMouseX;
                double deltaY = Mouse.GetPosition(null).Y - lastMouseY;
                phi -= deltaX * rotationSpeed;
                theta -= deltaY * rotationSpeed;
            }
            else
            {
                theta += (Keyboard.IsKeyDown(Key.Up) ? rotationSpeed * 10 : 0.0f);
                theta -= (Keyboard.IsKeyDown(Key.Down) ? rotationSpeed * 10 : 0.0f);
                phi -= (Keyboard.IsKeyDown(Key.Right) ? rotationSpeed * 10 : 0.0f);
                phi += (Keyboard.IsKeyDown(Key.Left) ? rotationSpeed * 10 : 0.0f);
            }


            lastMouseX = Mouse.GetPosition(null).X;
            lastMouseY = Mouse.GetPosition(null).Y;
        }

    }
}
