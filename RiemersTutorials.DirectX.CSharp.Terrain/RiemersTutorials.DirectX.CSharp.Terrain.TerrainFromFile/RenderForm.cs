// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RenderForm.cs" company="AlFranco">
//   Albert Rodriguez Franco 2013
// </copyright>
// <summary>
//   Riemers Tutorials of DirectX with C#
//   Chapter 1 Terrain
//   SubChapter 7 Terrain creation from file
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace RiemersTutorials.DirectX.CSharp.Terrain.TerrainFromFile
{
    using System;
    using System.Drawing;
    using System.IO;
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
        /// Vertices set as private attribute for refactoring in methods
        /// </summary>
        private CustomVertex.PositionColored[] vertices;

        /// <summary>
        /// In order to reuse vertex positions for complex meshes we use Vertex Buffers
        /// </summary>
        private VertexBuffer vertexBuffer;

        /// <summary>
        /// Indexes corresponding to our vertex buffer
        /// </summary>
        private int[] indices;

        /// <summary>
        /// Index buffer
        /// </summary>
        private IndexBuffer indexBuffer;

        /// <summary>
        /// Sets how many vertices in width the triangle grid will have
        /// </summary>
        private int triangleGridWidth = 64;

        /// <summary>
        /// Sets how many vertices in height the triangle grid will have
        /// </summary>
        private int triangleGridHeight = 64;

        /// <summary>
        /// Array to hold the information of the height on each vertex
        /// </summary>
        private int[,] heightData;

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
                // Set the height data
                ourDxForm.LoadHeightData();

                // Initialize the device
                ourDxForm.InitializeDevice();

                // Position the camera
                ourDxForm.CameraPositioning();

                // Declare vertices
                ourDxForm.VertexDeclaration();

                // Declare indices
                ourDxForm.IndicesDeclaration();

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
            this.device = new Device(0, DeviceType.Hardware, this, CreateFlags.HardwareVertexProcessing, presentParams);

            // Set the device in wireframe mode
            this.device.RenderState.FillMode = FillMode.WireFrame;

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
            this.device.Clear(ClearFlags.Target, Color.Black, 1.0f, 0);

            // Tell the device the we’re going to build the 'scene'
            // The scene is the whole world of objects the device has to display
            this.device.BeginScene();

            // Tell the device what kind of vertex information to expect.
            this.device.VertexFormat = CustomVertex.PositionColored.Format;

            // Set where the vertices are coming from
            this.device.SetStreamSource(0, this.vertexBuffer, 0);

            // Set how those vertices are going to be indexed on the screen
            this.device.Indices = this.indexBuffer;

            device.Transform.World = Matrix.Translation(-this.triangleGridHeight / 2f, -this.triangleGridWidth / 2f, 0);

            // This line actually draws the index primitives
            // The first argument indicates that it has to paint triangles
            // The first zero indicates at which index to start counting in your indexbuffer. 
            // Then you indicate the minimum amount of used indices. We give 0, which will bring no speed optimization. 
            // Then the amount of used vertices and the starting point in our vertexbuffer. 
            // Finally, we have to indicate how many primitives (=triangles) we want to be drawn.
            this.device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, this.triangleGridWidth * this.triangleGridHeight, 0, this.indices.Length / 3);

            // End of the scene definition
            this.device.EndScene();

            // To actually update our display, we have to Present the updates to the device
            this.device.Present();

            // Force the window to repaint
            this.Invalidate();
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
                (float)Math.PI / 4, this.Width / this.Height, 1f, 150f);

            // Position the camera
            // Define the position we position
            // Set the target point the camera is looking at.
            // Define which vector will be considered as 'up'
            this.device.Transform.View = Matrix.LookAtLH(
                new Vector3(0, -40, 50), new Vector3(0, -5, 0), new Vector3(0, 1, 0));

            // We are also required to place some lights to avoid the triangle to be black
            // Disable lighting to avoid this problem for now
            this.device.RenderState.Lighting = false;

            // Avoid the problem of clockwise or counter clock wise define vertices disabling cullmode
            this.device.RenderState.CullMode = Cull.None;
        }

        /// <summary>
        /// Declare vertices, in this case inside a Vertex Buffer
        /// </summary>
        private void VertexDeclaration()
        {
            // Create Vertex Buffer with some parameters
            this.vertexBuffer = new VertexBuffer(
                typeof(CustomVertex.PositionColored),
                this.triangleGridWidth * this.triangleGridHeight,
                this.device,
                Usage.Dynamic | Usage.WriteOnly,
                CustomVertex.PositionColored.Format,
                Pool.Default);

            // Create an array to hold the information for 3 vertices.
            // Change from TransformedColored to PositionColored
            // All you've done here is changed the format from pre-transformed coordinates to 'normal' coordinates
            // PositionColored means the coordinates of the points will be world coordinates and each of the points can have its own color
            this.vertices = new CustomVertex.PositionColored[this.triangleGridWidth * this.triangleGridHeight];

            // Fill in the position and information for 3 points. 
            // The'f' behind the numbers simply convert the integers to floats, the expected format.
            // If you position the camera on the negative z-axis, the triangle will be defined counter-clockwise relative to the camera and not be drawn
            // Redefine the vertices clockwise to solve the problem (this time clockwise relative to our camera on the negative part of the Z axis) :
            for (var x = 0; x < this.triangleGridWidth; x++)
            {
                for (var y = 0; y < this.triangleGridHeight; y++)
                {
                    this.vertices[x + (y * this.triangleGridWidth)].Position = new Vector3(x, y, this.heightData[x, y]);
                    this.vertices[x + (y * this.triangleGridWidth)].Color = Color.White.ToArgb();
                }
            }

            // We set the data of our vertex buffer with the vertices we just created, linking the vertices area with the buffer
            this.vertexBuffer.SetData(this.vertices, 0, LockFlags.None);
        }

        /// <summary>
        /// The indices declaration, this will determine how triangles will be constructed exploring the indices array
        /// </summary>
        private void IndicesDeclaration()
        {
            // Create a new instance of index buffer, assigning how many indices will handle
            // NOTE, if you get errors in the project try changing the typeof(int) to typeof(short) to allocate less space for indices
            this.indexBuffer = new IndexBuffer(
                typeof(int),
                (this.triangleGridWidth - 1) * (this.triangleGridHeight - 1) * 6,
                this.device,
                Usage.WriteOnly,
                Pool.Default);

            // Dimension the array of indices
            this.indices = new int[(this.triangleGridWidth - 1) * (this.triangleGridHeight - 1) * 6];

            // Link indices with vertices, 0 corresponds to vertices[0] and so on, 
            for (var x = 0; x < this.triangleGridWidth - 1; x++)
            {
                for (var y = 0; y < this.triangleGridHeight - 1; y++)
                {
                    // First section lower triangle in the square
                    this.indices[(x + y * (this.triangleGridWidth - 1)) * 6] = (x + 1)
                                                                               + (y + 1) * this.triangleGridWidth;

                    this.indices[(x + y * (this.triangleGridWidth - 1)) * 6 + 1] = (x + 1) + y * this.triangleGridWidth;

                    this.indices[(x + y * (this.triangleGridWidth - 1)) * 6 + 2] = x + y * this.triangleGridWidth;

                    // Second section upper triangle in the square
                    this.indices[(x + y * (this.triangleGridWidth - 1)) * 6 + 3] = (x + 1)
                                                                                   + (y + 1) * this.triangleGridWidth;

                    this.indices[(x + y * (this.triangleGridWidth - 1)) * 6 + 4] = x + y * this.triangleGridWidth;

                    this.indices[(x + y * (this.triangleGridWidth - 1)) * 6 + 5] = x + (y + 1) * this.triangleGridWidth;
                }
            }

            // Link indices with the Index Buffer
            this.indexBuffer.SetData(this.indices, 0, LockFlags.None);
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
            // Reset the WireFrame mode
            this.device.RenderState.FillMode = FillMode.WireFrame;

            // Position the camera
            this.CameraPositioning();

            // Indices declaration
            this.IndicesDeclaration();

            // Declare vertices
            this.VertexDeclaration();
        }

        /// <summary>
        /// Initialized the height information on every vertex
        /// </summary>
        private void LoadHeightData()
        {
            this.heightData = new int[this.triangleGridWidth,this.triangleGridHeight];

            var fileStream = new FileStream("heightdata.raw", FileMode.Open, FileAccess.Read);
            var binaryReader = new BinaryReader(fileStream);

            for (var i = 0; i < this.triangleGridHeight; i++)
            {
                for (var y = 0; y < this.triangleGridWidth; y++)
                {
                    var height = binaryReader.ReadByte() / 50;
                    this.heightData[this.triangleGridWidth - 1 - y, this.triangleGridHeight - 1 - i] = height;
                }
            }

            fileStream.Close();
        }
    }
}
