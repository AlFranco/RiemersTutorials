// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RenderForm.cs" company="AlFranco">
//   Albert Rodriguez Franco 2013
// </copyright>
// <summary>
//   Riemers Tutorials of DirectX with C#
//   Chapter 1 Terrain
//   SubChapter 5 Rotation and Translation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace RiemersTutorials.DirectX.CSharp.Terrain.RotationAndTranslation
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    using Microsoft.DirectX;
    using Microsoft.DirectX.Direct3D;

    /// <summary>
    /// Form that we'll along these series of chapters
    /// </summary>
    public class RenderForm : Form
    {
        /// <summary>
        ///  In short, a device is a direct link to your graphical adapter. 
        ///  It is an object that gives you direct access to the piece of hardware inside your computer
        /// </summary>
        private Device device;

        /// <summary>
        /// Rotation angle variable for our example
        /// </summary>
        private float angle = 0f;

        /// <summary>
        /// Vertices set as private attribute for refactoring in methods
        /// </summary>
        private CustomVertex.PositionColored[] vertices;

        /// <summary>
        /// The components.
        /// </summary>
        private System.ComponentModel.Container components;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderForm"/> class.
        /// </summary>
        public RenderForm()
        {
            this.InitializeComponent();
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true);
        }

        /// <summary>
        /// The main.
        /// </summary>
        public static void Main()
        {
            using (var ourDxForm = new RenderForm())
            {
                // Initialize the device
                ourDxForm.InitializeDevice();

                // Position the camera
                ourDxForm.CameraPositioning();

                // Declare vertices
                ourDxForm.VertexDeclaration();

                // Run the Form
                Application.Run(ourDxForm);
            }
        }

        /// <summary>
        /// Initializes the device with its presentation parameters
        /// </summary>
        public void InitializeDevice()
        {
            // Presentation Parameters, which we will need to tell the device how to behave
            // Windowed = true => We don't want a fullscreen application
            // SwapEffect = SwapEffect.Discard => Write to the device immediately, do not add extra back buffer that will be presented (= swapped) at runtime
            var presentParams = new PresentParameters { Windowed = true, SwapEffect = SwapEffect.Discard };

            // Creation of the Device:
            // 0 selects the first graphical adapter in your PC
            // Render the graphics using the hardware
            // Bind 'this' window to the device 
            // For now we want all 'vertex processing' to happen on the CPU
            this.device = new Device(0, DeviceType.Hardware, this, CreateFlags.SoftwareVertexProcessing, presentParams);

            // Fix for window resizing for the demo
            this.device.DeviceReset += this.HandleResetEvent;
        }

        /// <summary>
        ///  Control what to draw on the screen
        ///  This method will be called every time something is drawn to the screen
        /// </summary>
        /// <param name="e">
        ///  Paint Event Arguments
        /// </param>
        protected override void OnPaint(PaintEventArgs e)
        {
            // The Clear method will fill the window with a solid color, darkslateblue in our case
            // The ClearFlags indicate what we actually want to clear, in our case the target window
            this.device.Clear(ClearFlags.Target, Color.DarkSlateBlue, 1.0f, 0);

            // Tell the device the we’re going to build the 'scene'
            // The scene is the whole world of objects the device has to display
            this.device.BeginScene();

            // Tell the device what kind of vertex information to expect.
            this.device.VertexFormat = CustomVertex.PositionColored.Format;

            // Rotate the triangle along the Z axis
            // Redefine the point so the (0,0,0) would be in the center of our triangle. 
            // First move (=translate) the triangle a bit to the left and down, and then rotate it. 
            // Multiply your World matrix with a translation matrix
            // this.device.Transform.World = Matrix.Translation(-5, -10 * 1 / 3, 0) * Matrix.RotationZ(this.angle);
            // A bit more complex is the Matrix.RotateAxis, where you first specify your own custom rotation axis, a random rotation axis is generated
            this.device.Transform.World = Matrix.Translation(-5, -10 * 1 / 3, 0)
                                          * Matrix.RotationAxis(
                                              new Vector3(this.angle * 4, this.angle * 2, this.angle * 3), this.angle);

            // This line actually draws the triangle. 
            // The first argument indicates that a list of separate triangles is coming
            this.device.DrawUserPrimitives(PrimitiveType.TriangleList, 1, this.vertices);

            // End of the scene definition
            this.device.EndScene();

            // To actually update our display, we have to Present the updates to the device
            this.device.Present();

            // Force the window to repaint
            this.Invalidate();

            // Rotate the angle on every paint iteration
            this.angle += 0.05f;
        }

        /// <summary>
        /// Dispose method for the Form
        /// </summary>
        /// <param name="disposing">
        /// Dispose components or not
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.components != null)
                {
                    this.components.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Initializes the component
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.Size = new Size(500, 500);
            this.Text = @"DirectX Tutorial";
        }

        /// <summary>
        /// Position the camera
        /// </summary>
        private void CameraPositioning()
        {
            // Tell DirectX where to position the camera and where to look at
            // Tell the device what and how the camera should look at the scene
            // First parameter sets the view angle, 90° in our case
            // Set the view aspect ratio, which is 1 in our case, will be different if our window is a rectangle instead of a square
            // Near clipping plane : any objects closer to the camera than 1f will not be shown
            // Far clipping pane : any object farther than 50f won't be shown 
            this.device.Transform.Projection = Matrix.PerspectiveFovLH(
                (float)Math.PI / 4, (float)this.Width / this.Height, 1f, 50f);

            // Position the camera
            // Define the position we position it 30 units above our (0,0,0) point, the origin
            // Set the target point the camera is looking at. We will be looking at our origin
            // Define which vector will be considered as 'up'
            // this.device.Transform.View = Matrix.LookAtLH(new Vector3(0, 0, 30), new Vector3(0, 0, 0), new Vector3(0, 1, 0));
            // Since the coordinates system is left-handed to see the green corner on lower right we have to position the camera in -Z axis
            this.device.Transform.View = Matrix.LookAtLH(
                new Vector3(0, 0, -30), new Vector3(0, 0, 0), new Vector3(0, 1, 0));


            // We are also required to place some lights to avoid the triangle to be black
            // Disable lighting to avoid this problem for now
            this.device.RenderState.Lighting = false;

            // Avoid the problem of clockwise or counter clock wise define vertices disabling cullmode
            this.device.RenderState.CullMode = Cull.None;
        }

        /// <summary>
        /// Declare vertices
        /// </summary>
        private void VertexDeclaration()
        {
            // Create an array to hold the information for 3 vertices.
            // Change from TransformedColored to PositionColored
            // All you've done here is changed the format from pre-transformed coordinates to 'normal' coordinates
            // PositionColored means the coordinates of the points will be world coordinates and each of the points can have its own color
            this.vertices = new CustomVertex.PositionColored[3];

            // Fill in the position and information for 3 points. 
            // The'f' behind the numbers simply convert the integers to floats, the expected format.
            // If you position the camera on the negative z-axis, the triangle will be defined counter-clockwise relative to the camera and not be drawn
            // Redefine the vertices clockwise to solve the problem (this time clockwise relative to our camera on the negative part of the Z axis) :
            this.vertices[2].Position = new Vector3(0f, 0f, 0f);
            this.vertices[2].Color = Color.Red.ToArgb();
            this.vertices[0].Position = new Vector3(5f, 10f, 0f);
            this.vertices[0].Color = Color.Yellow.ToArgb();
            this.vertices[1].Position = new Vector3(10f, 0f, 0f);
            this.vertices[1].Color = Color.Green.ToArgb();
        }

        /// <summary>
        /// Fire this function when the form is resized
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void HandleResetEvent(object sender, EventArgs e)
        {
            // Position the camera
            this.CameraPositioning();

            // Declare vertices
            this.VertexDeclaration();
        }
    }
}
