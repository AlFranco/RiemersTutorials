// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RenderForm.cs" company="AlFranco">
//   Albert Rodriguez Franco 2013
// </copyright>
// <summary>
//   Riemers Tutorials of DirectX with C#
//   Chapter 1 Terrain
//   SubChapter 14 Sunrise over your terrain
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace RiemersTutorials.DirectX.CSharp.Terrain.MeshLighting
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Windows.Forms;

    using Microsoft.DirectX;
    using Microsoft.DirectX.Direct3D;
    using Microsoft.DirectX.DirectInput;

    /// <summary>
    /// Form that we'll along these series of chapters
    /// </summary>
    public class RenderForm : Form
    {
        /// <summary>
        ///  In short, a device is a direct link to your graphical adapter. 
        ///  It is an object that gives you direct access to the piece of hardware inside your computer
        /// </summary>
        private Microsoft.DirectX.Direct3D.Device graphicsDevice;

        /// <summary>
        /// The device pointing to our keyboard
        /// </summary>
        private Microsoft.DirectX.DirectInput.Device keyboardDevice;

        /// <summary>
        /// Our angle of rotation obtained from the keyboard
        /// </summary>
        private float angle;

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
        private short[] indices;

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
        /// Minimum height in our image
        /// </summary>
        private int minimumHeight = 255;

        /// <summary>
        /// Maximum height in our image
        /// </summary>
        private int maximumHeight = 0;

        /// <summary>
        /// Mesh for our terrain
        /// </summary>
        private Mesh meshTerrain;

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

                // Create our mesh
                ourDxForm.CreateMesh();

                // Initialize the keyboard
                ourDxForm.InitializeKeyboard();

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
            // AutoDepthStencilFormat = DepthFormat.D16 => Here you create a Z-buffer with a precision of 16 bits. 
            // What this means in short: Every distance is presented between 0 and 1, with 0 being the near plane (1f in our case) and 1 being the far plane (250f in our case). 
            // With 16 bits, you have 2^16 = 65536 possible distances between them
            // EnableAutoDepthStencil = true => Enable Z-Buffer
            var presentParams = new PresentParameters
                                    {
                                        Windowed = true,
                                        SwapEffect = SwapEffect.Discard,
                                        AutoDepthStencilFormat = DepthFormat.D16,
                                        EnableAutoDepthStencil = true
                                    };

            // Creation of the Device:
            // 0 selects the first graphical adapter in your PC
            // Render the graphics using the hardware
            // Bind 'this' window to the device 
            // For now we want all 'vertex processing' to happen on the CPU
            this.graphicsDevice = new Microsoft.DirectX.Direct3D.Device(
                0,
                Microsoft.DirectX.Direct3D.DeviceType.Hardware,
                this,
                CreateFlags.HardwareVertexProcessing,
                presentParams);

            // Fix for window resizing for the demo
            this.graphicsDevice.DeviceReset += this.HandleResetEvent;
        }

        /// <summary>
        /// Initialize the keyboard device
        /// </summary>
        public void InitializeKeyboard()
        {
            // The first line allocates the system's default keyboard to your variable keyb. 
            this.keyboardDevice = new Microsoft.DirectX.DirectInput.Device(SystemGuid.Keyboard);

            // Then you set some flags that adds default keyboard behavior to keyb. For example, if your window loses focus, your keyboard won't be attached to it any longer. 
            this.keyboardDevice.SetCooperativeLevel(
                this,
                CooperativeLevelFlags.Background | CooperativeLevelFlags.NonExclusive);

            // Don't forget to acquire your keyboard and to call this method from your Main method:
            this.keyboardDevice.Acquire();
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
            // The ClearFlags indicate what we actually want to clear, in our case the target window and the Z-Buffer
            // We need to fill our ZDepth buffer with zeroes. 
            // When we would draw a triangle, it would be further away from the viewer than what was previously defined in the Z-buffer, so our triangle is discarded. 
            // So in fact, we have to first fill our buffer with ones.
            this.graphicsDevice.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);


            // Tell the device the we’re going to build the 'scene'
            // The scene is the whole world of objects the device has to display
            this.graphicsDevice.BeginScene();

            // Of course we still need our translation and rotation
            this.graphicsDevice.Transform.World = Matrix.Translation(-this.triangleGridHeight / 2, -this.triangleGridWidth / 2, 0) * Matrix.RotationZ(this.angle);

            // Most Meshes are divided in subsets. 
            // For example, a house might have 4 simple subsets: a roof, the walls, the door and the window. In our mesh, there actually is only one subset
            var numSubSets = this.meshTerrain.GetAttributeTable().Length;
            
            for (var i = 0; i < numSubSets; ++i)
            {
                this.meshTerrain.DrawSubset(i);
            }

            // End of the scene definition
            this.graphicsDevice.EndScene();

            // To actually update our display, we have to Present the updates to the device
            this.graphicsDevice.Present();

            // Force the window to repaint
            this.Invalidate();

            // Read the keyboard
            this.ReadKeyboard();
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
            // Far clipping pane : any object farther than 250f won't be shown 
            this.graphicsDevice.Transform.Projection = Matrix.PerspectiveFovLH(
                (float)Math.PI / 4,
                this.Width / this.Height,
                1f,
                250f);

            // Position the camera
            // Define the position we position
            // Set the target point the camera is looking at.
            // Define which vector will be considered as 'up'
            this.graphicsDevice.Transform.View = Matrix.LookAtLH(
                new Vector3(80, 0, 120),
                new Vector3(-20, 0, 0),
                new Vector3(0, 0, 1));


            // Avoid the problem of clockwise or counter clock wise define vertices disabling cullmode
            this.graphicsDevice.RenderState.CullMode = Cull.None;

            // Tell the system that now we are using lights
            this.graphicsDevice.RenderState.Lighting = true;

            // Start defining your lights. They start at index 0, and we’ll only be defining one
            // The simplest case, a directional light. Imagine this as the sunlight: the light will travel in one particular direction
            this.graphicsDevice.Lights[0].Type = LightType.Directional;

            // The diffuse color is simply the color of the light
            this.graphicsDevice.Lights[0].Diffuse = Color.White;

            // Define the direction your light shines
            this.graphicsDevice.Lights[0].Direction = new Vector3(0.5f, 0, 1f);

            // Enable the light
            this.graphicsDevice.Lights[0].Enabled = true;
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
                this.graphicsDevice,
                Usage.Dynamic | Usage.WriteOnly,
                CustomVertex.PositionColored.Format,
                Pool.Default);

            // Create an array to hold the information for 3 vertices.
            // Change from TransformedColored to PositionColored
            // All you've done here is changed the format from pre-transformed coordinates to 'normal' coordinates
            // PositionColored means the coordinates of the points will be world coordinates and each of the points can have its own color
            this.vertices = new CustomVertex.PositionColored[this.triangleGridWidth * this.triangleGridHeight];

            // Fill in the position and information for 3 points. 
            // If you position the camera on the negative z-axis, the triangle will be defined counter-clockwise relative to the camera and not be drawn
            // Define four areas that will have different colours depending on the height
            // At the bottom we have blue lakes, then the green trees, the brown mountain and finally snow topped peaks.
            // Redefine the vertices clockwise to solve the problem (this time clockwise relative to our camera on the negative part of the Z axis) :
            for (var x = 0; x < this.triangleGridWidth; x++)
            {
                for (var y = 0; y < this.triangleGridHeight; y++)
                {
                    this.vertices[x + (y * this.triangleGridWidth)].Position = new Vector3(x, y, this.heightData[x, y]);

                    if (this.heightData[x, y] < this.minimumHeight + (this.maximumHeight - this.minimumHeight) / 4)
                    {
                        this.vertices[x + y * this.triangleGridWidth].Color = Color.Blue.ToArgb();
                    }
                    else if (this.heightData[x, y]
                             < this.minimumHeight + (this.maximumHeight - this.minimumHeight) * 2 / 4)
                    {
                        this.vertices[x + y * this.triangleGridWidth].Color = Color.Green.ToArgb();
                    }
                    else if (this.heightData[x, y]
                             < this.minimumHeight + (this.maximumHeight - this.minimumHeight) * 3 / 4)
                    {
                        this.vertices[x + y * this.triangleGridWidth].Color = Color.Brown.ToArgb();
                    }
                    else
                    {
                        this.vertices[x + y * this.triangleGridWidth].Color = Color.White.ToArgb();
                    }
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
                typeof(short),
                (this.triangleGridWidth - 1) * (this.triangleGridHeight - 1) * 6,
                this.graphicsDevice,
                Usage.WriteOnly,
                Pool.Default);

            // Dimension the array of indices
            this.indices = new short[(this.triangleGridWidth - 1) * (this.triangleGridHeight - 1) * 6];

            // Link indices with vertices, 0 corresponds to vertices[0] and so on, 
            for (var x = 0; x < this.triangleGridWidth - 1; x++)
            {
                for (var y = 0; y < this.triangleGridHeight - 1; y++)
                {
                    // First section lower triangle in the square
                    this.indices[(x + y * (this.triangleGridWidth - 1)) * 6] =
                        (short)((x + 1) + (y + 1) * this.triangleGridWidth);

                    this.indices[(x + y * (this.triangleGridWidth - 1)) * 6 + 1] =
                        (short)((x + 1) + y * this.triangleGridWidth);

                    this.indices[(x + y * (this.triangleGridWidth - 1)) * 6 + 2] =
                        (short)(x + y * this.triangleGridWidth);

                    // Second section upper triangle in the square
                    this.indices[(x + y * (this.triangleGridWidth - 1)) * 6 + 3] =
                        (short)((x + 1) + (y + 1) * this.triangleGridWidth);

                    this.indices[(x + y * (this.triangleGridWidth - 1)) * 6 + 4] =
                        (short)(x + y * this.triangleGridWidth);

                    this.indices[(x + y * (this.triangleGridWidth - 1)) * 6 + 5] =
                        (short)(x + (y + 1) * this.triangleGridWidth);
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
            // this.graphicsDevice.RenderState.FillMode = FillMode.WireFrame;

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
            var fileStream = new FileStream("heightmap.bmp", FileMode.Open, FileAccess.Read);
            var binaryReader = new BinaryReader(fileStream);

            // Scroll to the byte that indicates the offset to the actual pixeldata. To do this, simply read 10 bytes to position our reader at byte 11, the first offset byte.
            for (var i = 0; i < 10; i++)
            {
                binaryReader.ReadByte();
            }

            // The following 4 bytes represent the offset. 
            // Since every byte can only represent a value between 0 and 255, the first byte has to be multiplied by 1, the second by 256, the next by 256*256 and so on
            int offset = binaryReader.ReadByte();
            offset += binaryReader.ReadByte() * 256;
            offset += binaryReader.ReadByte() * 256 * 256;
            offset += binaryReader.ReadByte() * 256 * 256 * 256;

            // Next we scroll further another 4 bytes to byte 19, where we find the WIDTH and the HEIGHT of the image
            for (var i = 0; i < 4; i++)
            {
                binaryReader.ReadByte();
            }

            this.triangleGridWidth = binaryReader.ReadByte();
            this.triangleGridWidth += binaryReader.ReadByte() * 256;
            this.triangleGridWidth += binaryReader.ReadByte() * 256 * 256;
            this.triangleGridWidth += binaryReader.ReadByte() * 256 * 256 * 256;

            this.triangleGridHeight = binaryReader.ReadByte();
            this.triangleGridHeight += binaryReader.ReadByte() * 256;
            this.triangleGridHeight += binaryReader.ReadByte() * 256 * 256;
            this.triangleGridHeight += binaryReader.ReadByte() * 256 * 256 * 256;

            // Now we can initialise our heightData array and scroll further to the pixeldata:
            this.heightData = new int[this.triangleGridWidth, this.triangleGridHeight];

            for (var i = 0; i < (offset - 26); i++)
            {
                binaryReader.ReadByte();
            }

            // Read until the end the bytes corresponding to the color of each pixel
            for (var i = 0; i < this.triangleGridHeight; i++)
            {
                for (var y = 0; y < this.triangleGridWidth; y++)
                {
                    // We are going to store the sum of the 3 colors as the height for a pixel. Divide to normalize
                    int height = binaryReader.ReadByte();
                    height += binaryReader.ReadByte();
                    height += binaryReader.ReadByte();
                    height /= 8;

                    this.heightData[this.triangleGridWidth - 1 - y, this.triangleGridHeight - 1 - i] = height;

                    if (height < this.minimumHeight)
                    {
                        this.minimumHeight = height;
                    }
                    if (height > this.maximumHeight)
                    {
                        this.maximumHeight = height;
                    }
                }
            }
        }

        /// <summary>
        /// Reads the keyboard and places the angle for rotation
        /// </summary>
        private void ReadKeyboard()
        {
            KeyboardState keys = this.keyboardDevice.GetCurrentKeyboardState();

            if (keys[Key.Delete])
            {
                this.angle += 0.03f;
            }

            if (keys[Key.Next])
            {
                this.angle -= 0.03f;
            }
        }

        /// <summary>
        /// Creates the mesh
        /// </summary>
        private void CreateMesh()
        {
            // The first parameter reflects the number of triangles to draw, 
            // Followed by the number of vertices in our VertexBuffer. 
            // We let the Managed environment manage our memory, 
            // Define the format of our vertices
            // Pass our device
            this.meshTerrain = new Mesh(
                (this.triangleGridWidth - 1) * (this.triangleGridHeight - 1) * 2,
                this.triangleGridWidth * this.triangleGridHeight,
                MeshFlags.Managed,
                CustomVertex.PositionColored.Format,
                this.graphicsDevice);

            // Set our vertex and index information
            this.meshTerrain.SetVertexBufferData(this.vertices, LockFlags.None);
            this.meshTerrain.SetIndexBufferData(this.indices, LockFlags.None);

            // DirectX will always be able to place the vertices in a more efficient order. 
            // This can be done for you by the OptimizeInPlace method, but this requires an AdjecencyInformation array to be passed to it. 
            // From this array, DirectX can read how close every vertex lies to its neighbors. 
            // This information might be needed to optimize your mesh, for example to discard a vertex if it’s very close to one of its neighbors.
            // First you define an array to hold the information. Since our terrain only uses triangles and every triangle has no more than 3 neighboring vertices, this will be a safe upper limit
            var adjac = new int[this.meshTerrain.NumberFaces * 3];

            // Generate the Adjacency information, where we indicate that every 2 vertices, with a difference of less than 0.5f between them, can be treated as one vertex
            this.meshTerrain.GenerateAdjacency(0.5f, adjac);

            // Call the OptimizeInPlace method, where we pass our Adjacency information and define how to optimize
            // You can optimize for compatibility, for datasize and more. We define it to optimize for cache memory, so we will have less cache misses and thus our program will be executed faster 
            // Note: cache is the fast memory in the CPU, for every cache miss, data has to be transferred from the slower RAM to the cache
            this.meshTerrain.OptimizeInPlace(MeshFlags.OptimizeVertexCache, adjac);

            // DirectX can automatically calculate the normal vectors in the vertices of our Mesh. 
            // First, we are going to replace our Mesh with a Mesh that supports the PositionNormalColored vertexformat.
            this.meshTerrain = this.meshTerrain.Clone(this.meshTerrain.Options.Value, CustomVertex.PositionNormalColored.Format, this.graphicsDevice);
            
            // Compute the normals of our mesh
            this.meshTerrain.ComputeNormals();
        }
    }
}
