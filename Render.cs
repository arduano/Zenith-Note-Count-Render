using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using BMEngine;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace NoteCountRender
{
    public class Render : IPluginRender
    {
        #region PreviewConvert
        BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }
        #endregion

        public string Name => "Note Counter";

        public string Description => "Generate note counts and other midi statistics";

        public bool Initialized { get; set; } = false;

        public System.Windows.Media.ImageSource PreviewImage { get; set; }

        public bool ManualNoteDelete => true;

        public int NoteCollectorOffset => 0;

        public double LastMidiTimePerTick { get; set; } = 500000 / 96.0;

        public MidiInfo CurrentMidi { get; set; }

        public double NoteScreenTime => 1 / LastMidiTimePerTick * (1000000.0 / renderSettings.fps);

        public long LastNoteCount { get; set; } = 0;

        public System.Windows.Controls.Control SettingsControl { get; set; }

        RenderSettings renderSettings;
        Settings settings;

        GLTextEngine textEngine;
        public void Dispose()
        {
            textEngine.Dispose();
            Initialized = false;
            Console.WriteLine("Disposed of FlatRender");
        }

        int fontSize = 40;
        string font = "Arial";
        public void Init()
        {
            textEngine = new GLTextEngine();
            if (settings.fontName != font || settings.fontSize != fontSize)
            {
                font = settings.fontName;
                fontSize = settings.fontSize;
            }
            textEngine.SetFont(font, fontSize);
            noteCount = 0;
            nps = 0;
            frames = 0;
            notesHit = new LinkedList<long>();
            Initialized = true;
            Console.WriteLine("Initialised FlatRender");
        }

        public Render(RenderSettings settings)
        {
            this.renderSettings = settings;
            this.settings = new Settings();
            SettingsControl = new SettingsCtrl(this.settings);
            PreviewImage = BitmapToImageSource(Properties.Resources.preview);
        }

        long noteCount = 0;
        double nps = 0;
        int frames = 0;
        long currentNotes = 0;
        long polyphony = 0;

        LinkedList<long> notesHit = new LinkedList<long>();

        public void RenderFrame(FastList<Note> notes, double midiTime, int finalCompositeBuff)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, finalCompositeBuff);

            GL.Viewport(0, 0, renderSettings.width, renderSettings.height);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Clear(ClearBufferMask.DepthBufferBit);

            if (settings.fontName != font || settings.fontSize != fontSize)
            {
                font = settings.fontName;
                fontSize = settings.fontSize;
                textEngine.SetFont(font, fontSize);
            }
            if (!renderSettings.paused)
            {
                polyphony = 0;
                currentNotes = 0;
                long nc = 0;
                lock (notes)
                    foreach (Note n in notes)
                    {
                        nc++;
                        if (n.start < midiTime)
                        {
                            if (n.end > midiTime || !n.hasEnded)
                            {
                                polyphony++;
                            }
                            else if (n.meta != null)
                            {
                                n.delete = true;
                            }
                            if (n.meta == null)
                            {
                                currentNotes++;
                                noteCount++;
                                n.meta = true;
                            }
                        }
                        if (n.start > midiTime) break;
                    }
                LastNoteCount = nc;
                notesHit.AddLast(currentNotes);
                while (notesHit.Count > renderSettings.fps / 4) notesHit.RemoveFirst();
                nps = notesHit.Sum() * 4;
            }

            double tempo = LastMidiTimePerTick * CurrentMidi.division;
            tempo = (1 / tempo) * 1000000 * 60;

            int seconds = frames / renderSettings.fps;
            TimeSpan time = new TimeSpan(0, 0, seconds);
            if (!renderSettings.paused) frames++;

            string text = settings.text;
            text = text.Replace("{bpm}", (Math.Round(tempo * 10) / 10).ToString());
            text = text.Replace("{nc}", noteCount.ToString("#,##0"));
            text = text.Replace("{tn}", CurrentMidi.noteCount.ToString("#,##0"));
            text = text.Replace("{ppq}", CurrentMidi.division.ToString());
            text = text.Replace("{nps}", nps.ToString("#,##0"));
            text = text.Replace("{plph}", polyphony.ToString("#,##0"));
            //text = text.Replace("{sus}", (polyphony / nps * 100).ToString("#,##0.0"));
            text = text.Replace("{seconds}", ((double)frames / renderSettings.fps).ToString("#,##0.0"));
            text = text.Replace("{time}", time.ToString("mm\\:ss"));
            text = text.Replace("{ticks}", ((int)midiTime).ToString("#,##0"));

            if (settings.textAlignment == Alignments.TopLeft)
            {
                var size = textEngine.GetBoundBox(text);
                Matrix4 transform = Matrix4.Identity;
                transform = Matrix4.Mult(transform, Matrix4.CreateScale(1.0f / 1920.0f, -1.0f / 1080.0f, 1.0f));
                transform = Matrix4.Mult(transform, Matrix4.CreateTranslation(-1, 1, 0));
                transform = Matrix4.Mult(transform, Matrix4.CreateRotationZ(0));

                textEngine.Render(text, transform, Color4.White);
            }
            else if (settings.textAlignment == Alignments.TopRight)
            {
                float offset = 0;
                string[] lines = text.Split('\n');
                foreach (var line in lines)
                {
                    var size = textEngine.GetBoundBox(line);
                    Matrix4 transform = Matrix4.Identity;
                    transform = Matrix4.Mult(transform, Matrix4.CreateTranslation(-size.Width, offset, 0));
                    transform = Matrix4.Mult(transform, Matrix4.CreateScale(1.0f / renderSettings.width, -1.0f / renderSettings.height, 1.0f));
                    transform = Matrix4.Mult(transform, Matrix4.CreateTranslation(1, 1, 0));
                    transform = Matrix4.Mult(transform, Matrix4.CreateRotationZ(0));
                    offset += size.Height;
                    textEngine.Render(line, transform, Color4.White);
                }
            }
            else if (settings.textAlignment == Alignments.BottomLeft)
            {
                float offset = 0;
                string[] lines = text.Split('\n');
                foreach (var line in lines.Reverse())
                {
                    var size = textEngine.GetBoundBox(line);
                    Matrix4 transform = Matrix4.Identity;
                    transform = Matrix4.Mult(transform, Matrix4.CreateTranslation(0, offset - size.Height, 0));
                    transform = Matrix4.Mult(transform, Matrix4.CreateScale(1.0f / 1920.0f, -1.0f / 1080.0f, 1.0f));
                    transform = Matrix4.Mult(transform, Matrix4.CreateTranslation(-1, -1, 0));
                    transform = Matrix4.Mult(transform, Matrix4.CreateRotationZ(0));
                    offset -= size.Height;
                    textEngine.Render(line, transform, Color4.White);
                }
            }
            else if (settings.textAlignment == Alignments.BottomRight)
            {
                float offset = 0;
                string[] lines = text.Split('\n');
                foreach (var line in lines.Reverse())
                {
                    var size = textEngine.GetBoundBox(line);
                    Matrix4 transform = Matrix4.Identity;
                    transform = Matrix4.Mult(transform, Matrix4.CreateTranslation(-size.Width, offset - size.Height, 0));
                    transform = Matrix4.Mult(transform, Matrix4.CreateScale(1.0f / 1920.0f, -1.0f / 1080.0f, 1.0f));
                    transform = Matrix4.Mult(transform, Matrix4.CreateTranslation(1, -1, 0));
                    transform = Matrix4.Mult(transform, Matrix4.CreateRotationZ(0));
                    offset -= size.Height;
                    textEngine.Render(line, transform, Color4.White);
                }
            }
            else if (settings.textAlignment == Alignments.TopSpread)
            {
                float offset = 0;
                string[] lines = text.Split('\n');
                float dist = 1.0f / (lines.Length + 1);
                int p = 1;
                foreach (var line in lines.Reverse())
                {
                    var size = textEngine.GetBoundBox(line);
                    Matrix4 transform = Matrix4.Identity;
                    transform = Matrix4.Mult(transform, Matrix4.CreateTranslation(-size.Width / 2, 0, 0));
                    transform = Matrix4.Mult(transform, Matrix4.CreateScale(1.0f / 1920.0f, -1.0f / 1080.0f, 1.0f));
                    transform = Matrix4.Mult(transform, Matrix4.CreateTranslation((dist * p++) * 2 - 1, 1, 0));
                    transform = Matrix4.Mult(transform, Matrix4.CreateRotationZ(0));
                    offset -= size.Height;
                    textEngine.Render(line, transform, Color4.White);
                }
            }
            else if (settings.textAlignment == Alignments.BottomSpread)
            {
                float offset = 0;
                string[] lines = text.Split('\n');
                float dist = 1.0f / (lines.Length + 1);
                int p = 1;
                foreach (var line in lines.Reverse())
                {
                    var size = textEngine.GetBoundBox(line);
                    Matrix4 transform = Matrix4.Identity;
                    transform = Matrix4.Mult(transform, Matrix4.CreateTranslation(-size.Width / 2, -size.Height, 0));
                    transform = Matrix4.Mult(transform, Matrix4.CreateScale(1.0f / 1920.0f, -1.0f / 1080.0f, 1.0f));
                    transform = Matrix4.Mult(transform, Matrix4.CreateTranslation((dist * p++) * 2 - 1, -1, 0));
                    transform = Matrix4.Mult(transform, Matrix4.CreateRotationZ(0));
                    offset -= size.Height;
                    textEngine.Render(line, transform, Color4.White);
                }
            }

            //offset += size.Height;

            //text = "asdfsfdb\n34kh5bk234jhhjbgfvd";
            //size = textEngine.GetBoundBox(text);
            //transform = Matrix4.Identity;
            //transform = Matrix4.Mult(transform, Matrix4.CreateTranslation(-renderSettings.width / 2, -renderSettings.height / 2 + offset, 0));
            //transform = Matrix4.Mult(transform, Matrix4.CreateRotationZ(0));
            //transform = Matrix4.Mult(transform, Matrix4.CreateScale(1.0f / 1920.0f * 2, -1.0f / 1080.0f * 2, 1.0f));

            //textEngine.Render(text, transform, Color4.White);
        }

        public void SetTrackColors(NoteColor[][] trakcs)
        {

        }
    }
}
