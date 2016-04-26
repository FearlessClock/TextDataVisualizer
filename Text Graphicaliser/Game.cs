using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.IO;

namespace Text_Graphicaliser
{
    struct Vertex
    {
        public Vector2 position;
        public Vector2 texCoord;
        public Vector4 color;

        public Color Color
        {
            get
            {
                return Color.FromArgb((int)(255 * color.W), (int)(255 * color.X), (int)(255 * color.Y), (int)(255 * color.Z));
            }
            set
            {
                this.color = new Vector4(value.R / 255f, value.G / 255f, value.B / 255f, value.A / 255f);
            }

        }
        static public int SizeInBytes
        {
            get { return Vector2.SizeInBytes * 2 + Vector4.SizeInBytes; }
        }

        public Vertex(Vector2 position, Vector2 texCoord)
        {
            this.position = position;
            this.texCoord = texCoord;
            this.color = new Vector4(1, 1, 1, 1);
        }


    }
    class Game
    {
        public GameWindow window;
        Texture2D texture;
        Texture2D barTex;
        TextWriter textWriter;

        //Start of the vertex buffer
        GraphicsBuffer buffer = new GraphicsBuffer();
        GraphicsBuffer[] CursorBuf;

        StreamReader sr = new StreamReader("Content/text.txt");

        int numberOfThings = 90 - 64;
        int[] letters;
        GraphicsBuffer letterShapes;
        public Game(GameWindow windowInput)
        {
            window = windowInput;

            window.Load += Window_Load;
            window.RenderFrame += Window_RenderFrame;
            window.UpdateFrame += Window_UpdateFrame;
            window.Closing += Window_Closing;
            Camera.SetupCamera(window, 30);
            textWriter = new TextWriter("Alphabet/");

            window.CursorVisible = false;
        }


        private void Window_Load(object sender, EventArgs e)
        {
            texture = ContentPipe.LoadTexture("placeholder.png");
            barTex = ContentPipe.LoadTexture("bar.png");
            letters = new int[numberOfThings];
            for(int i = 0; i < letters.Length; i++)
            {
                letters[i] = 1;
            }
            

            letterShapes = new GraphicsBuffer();
            letterShapes.indexBuffer = new uint[numberOfThings*4];
            for(int i = 0; i < numberOfThings*4; i++)
            {
                letterShapes.indexBuffer[i] = (uint)i;
            }
            letterShapes.vertBuffer = new Vertex[numberOfThings*4];
            for(int i = 0; i < numberOfThings*4; i+=4)
            {
               letterShapes.vertBuffer[i] = new Vertex(new Vector2(0, 0), new Vector2(0, 0));
               letterShapes.vertBuffer[i + 1] = new Vertex(new Vector2(0, 1), new Vector2(0, 1));
               letterShapes.vertBuffer[i + 2] = new Vertex(new Vector2(1, 1), new Vector2(1, 1));
               letterShapes.vertBuffer[i + 3] = new Vertex(new Vector2(1, 0), new Vector2(1, 0));
            }
            letterShapes.VBO = GL.GenBuffer();
            letterShapes.IBO = GL.GenBuffer();
            BufferFill(letterShapes);
        }

        private void BufferFill(GraphicsBuffer buf)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, buf.VBO);
            GL.BufferData<Vertex>(BufferTarget.ArrayBuffer, (IntPtr)(Vertex.SizeInBytes * buf.vertBuffer.Length), buf.vertBuffer, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, buf.IBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(sizeof(uint) * (buf.indexBuffer.Length)), buf.indexBuffer, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }
        int counter = 0;
        private void Window_UpdateFrame(object sender, FrameEventArgs e)
        {
            //32 96
            CursorBuf = Camera.CameraUpdate();
            foreach (GraphicsBuffer b in CursorBuf)
            {
                BufferFill(b);
            }
            string text = sr.ReadLine();
            if (text != null)
            {
                text = text.ToUpper();
                for (int i = 0; i < text.Length; i++)
                {
                    if ((int)text[i] - 65 >= 0 && (int)text[i] - 65 < letters.Length)
                        letters[(int)text[i] - 65]++;
                }
                counter++;
                MoveEverything();
            }
        }
        private void MoveEverything()
        {
            double step = 360/letters.Length;
            for(int i = 0; i < letters.Length; i++)
            {
                float x = (float)Math.Cos(MathHelper.DegreesToRadians(step * i));
                float y = (float)Math.Sin(MathHelper.DegreesToRadians(step * i));
                Vector2 dir = new Vector2(x, y);
                dir *= letters[i]/500;
                letterShapes.vertBuffer[i * 4 + 2].position = dir;
                dir += dir.PerpendicularRight.Normalized();
                letterShapes.vertBuffer[i * 4 + 3].position = dir;
            }
            BufferFill(letterShapes);
        }
        private void Window_RenderFrame(object sender, FrameEventArgs e)
        {
            //Clear screen color
            GL.ClearColor(Color.White);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            //Enable color blending, which allows transparency
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.Texture2D);
            //Blending everything for transparency
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            //Create the projection matrix for the scene
            Camera.MoveCamera();



            //Enable all the different arrays
            GL.EnableClientState(ArrayCap.ColorArray);
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.TextureCoordArray);


            textWriter.WriteToScreen(new Vector2(0, 30), counter.ToString(), window.Width, 20);

            //Bind the texture that will be used
            GL.BindTexture(TextureTarget.Texture2D, texture.ID);

            ////Load the vert and index buffers
            //GL.BindBuffer(BufferTarget.ArrayBuffer, CursorBuf[0].VBO);
            //GL.VertexPointer(2, VertexPointerType.Float, Vertex.SizeInBytes, (IntPtr)0);
            //GL.TexCoordPointer(2, TexCoordPointerType.Float, Vertex.SizeInBytes, (IntPtr)(Vector2.SizeInBytes));
            //GL.ColorPointer(4, ColorPointerType.Float, Vertex.SizeInBytes, (IntPtr)(Vector2.SizeInBytes * 2));
            //GL.BindBuffer(BufferTarget.ElementArrayBuffer, CursorBuf[0].IBO);

            ////Load the translation matrix into the modelView matrix
            //Matrix4 mat = Matrix4.CreateTranslation(0, 0, 0);
            //GL.MatrixMode(MatrixMode.Modelview);                //Load the modelview matrix, last in the chain of view matrices
            //GL.LoadMatrix(ref mat);
            //mat = Matrix4.CreateScale(1, 1, 0);                 //Create a scale matrix
            //GL.MultMatrix(ref mat);                              //Multiply the scale matrix with the modelview matrix
            //GL.DrawElements(PrimitiveType.Quads, CursorBuf[0].indexBuffer.Length, DrawElementsType.UnsignedInt, 0);

            //GL.BindBuffer(BufferTarget.ArrayBuffer, buffer.VBO);
            //GL.VertexPointer(2, VertexPointerType.Float, Vertex.SizeInBytes, (IntPtr)0);
            //GL.TexCoordPointer(2, TexCoordPointerType.Float, Vertex.SizeInBytes, (IntPtr)(Vector2.SizeInBytes));
            //GL.ColorPointer(4, ColorPointerType.Float, Vertex.SizeInBytes, (IntPtr)(Vector2.SizeInBytes * 2));
            //GL.BindBuffer(BufferTarget.ElementArrayBuffer, buffer.IBO);
            ////Create a scale matrux
            //Matrix4 mat;
            //for (int i = 0; i < letters.Length; i++)
            //{
            //    GL.MatrixMode(MatrixMode.Modelview);
            //    GL.LoadIdentity();
            //    mat = Matrix4.CreateTranslation(400, 300, 0);
            //    GL.MultMatrix(ref mat);
            //    mat = Matrix4.CreateRotationZ(i * 360 / letters.Length);   //Create a translation matrix
            //                                                               //Load the modelview matrix, last in the chain of view matrices
            //    GL.MultMatrix(ref mat);                             //Load the translation matrix into the modelView matrix
            //    mat = Matrix4.CreateTranslation(letters[i] / 10, 0, 0);
            //    GL.MultMatrix(ref mat);
            //    mat = Matrix4.CreateScale(30, 30, 0);                 //Create a scale matrix
            //    GL.MultMatrix(ref mat);                              //Multiply the scale matrix with the modelview matrix
            //    GL.DrawElements(PrimitiveType.Quads, buffer.indexBuffer.Length, DrawElementsType.UnsignedInt, 0);
            //}

            GL.BindBuffer(BufferTarget.ArrayBuffer, letterShapes.VBO);
            GL.VertexPointer(2, VertexPointerType.Float, Vertex.SizeInBytes, (IntPtr)0);
            GL.TexCoordPointer(2, TexCoordPointerType.Float, Vertex.SizeInBytes, (IntPtr)(Vector2.SizeInBytes));
            GL.ColorPointer(4, ColorPointerType.Float, Vertex.SizeInBytes, (IntPtr)(Vector2.SizeInBytes * 2));
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, letterShapes.IBO);
            GL.BindTexture(TextureTarget.Texture2D, barTex.ID);

            
            GL.LoadIdentity();
            Matrix4 mat = Matrix4.CreateTranslation(400, 300, 0);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref mat);                     //Load the translation matrix into the modelView matrix
            mat = Matrix4.CreateScale(20, 20, 0);                 //Create a scale matrix
            GL.MultMatrix(ref mat);                              //Multiply the scale matrix with the modelview matrix
            GL.DrawElements(PrimitiveType.Quads, letterShapes.indexBuffer.Length, DrawElementsType.UnsignedInt, 0);

            for (int i = 0; i < letters.Length; i++)
            {
                Vector2 vec = new Vector2(letterShapes.vertBuffer[i * 4 + 3].position.X*20 + 400, 
                                            letterShapes.vertBuffer[i * 4 + 3].position.Y*20 + 300);
                //vec *= 2;
                textWriter.WriteToScreen(vec, ((char)(i + 65)).ToString(), 30, 20);
            }
            //Flush everything 
            GL.Flush();
            //Write the new buffer to the screen
            window.SwapBuffers();
        }
    }
}
