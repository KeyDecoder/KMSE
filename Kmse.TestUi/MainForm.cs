using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Security;
using Autofac;
using Kmse.Core;
using Kmse.Core.IO.Controllers;
using Timer = System.Windows.Forms.Timer;

namespace Kmse.TestUi
{
    public partial class frmMain : Form
    {
        private readonly ILifetimeScope _scope;
        private IMasterSystemConsole _console;
        private IControllerPort _controllers;
        private ILifetimeScope _emulationScope;

        delegate void LogMessageCallback(string text);
        private string _currentCartridgeFilename;
        private Thread _emulatorThread;
        private CancellationTokenSource _emulationCancellationTokenSource;

        private Bitmap _mainFrameBitmap;
        private Bitmap _debugSpritesBitmap;
        private Bitmap _debugTileMemoryBitmap;

        private BufferedGraphics _mainFrameBufferedGraphics;
        private BufferedGraphics _debugSpritesBufferedGraphics;
        private BufferedGraphics _debugTileMemoryBufferedGraphics;

        private SemaphoreSlim _mainDisplayUpdateSemaphore;
        private SemaphoreSlim _debugSpritesDisplayUpdateSemaphore;
        private SemaphoreSlim _debugTileMemoryDisplayUpdateSemaphore;

        private readonly Timer _fpsTimer;
        private Stopwatch _fpsStopWatch;
        private int _frameCount;

        public frmMain(ILifetimeScope scope)
        {
            _scope = scope;
            InitializeComponent();

            _mainFrameBitmap = new Bitmap(256, 192, PixelFormat.Format32bppArgb);
            _debugSpritesBitmap = new Bitmap(256, 192, PixelFormat.Format32bppArgb);
            _debugTileMemoryBitmap = new Bitmap(256, 192, PixelFormat.Format32bppArgb);

            using (var graphics = CreateGraphics())
            {
                _mainFrameBufferedGraphics = BufferedGraphicsManager.Current.Allocate(graphics, new Rectangle(0, 0, picMain.Width, picMain.Height));
                _debugSpritesBufferedGraphics = BufferedGraphicsManager.Current.Allocate(graphics, new Rectangle(0, 0, picSprites.Width, picSprites.Height));
                _debugTileMemoryBufferedGraphics = BufferedGraphicsManager.Current.Allocate(graphics, new Rectangle(0, 0, picTileMemory.Width, picTileMemory.Height));
            }

            _mainDisplayUpdateSemaphore = new SemaphoreSlim(1);
            _debugSpritesDisplayUpdateSemaphore = new SemaphoreSlim(1);
            _debugTileMemoryDisplayUpdateSemaphore = new SemaphoreSlim(1);

            _fpsTimer = new Timer();
            _fpsTimer.Interval = 1000;
            _fpsStopWatch = new Stopwatch();
            _fpsTimer.Tick += UpdateFramesPerSecond;
        }

        private void WriteToDebugLogControl(string text)
        {
            if (txtDebugLog.InvokeRequired)
            {
                var callback = new LogMessageCallback(WriteToDebugLogControl);
                try
                {
                    Invoke(callback, text);
                }
                catch (ObjectDisposedException)
                {
                }

                return;
            }

            try
            {
                txtDebugLog.AppendText($"{text}{Environment.NewLine}");
            }
            catch (ObjectDisposedException)
            {
            }
        }

        public void LogDebug(string message)
        {
            // TODO: Check if debug enabled
            //WriteToDebugLogControl(message);
        }

        public void LogError(string message)
        {
            WriteToDebugLogControl(message);
        }

        public void LogInformation(string message)
        {
            WriteToDebugLogControl(message);
        }

        public void LogInstruction(string message)
        {
            // Disable this for now to avoid overloading the UI
            //WriteToDebugLogControl(message);
        }

        public void LogMemoryOperation(string message)
        {
            // Disable this for now to avoid overloading the UI
            //WriteToDebugLogControl(message);
        }

        public void DrawMainFrame(ReadOnlySpan<byte> frame)
        {
            WriteFrameToBitMap(frame, _mainFrameBitmap, _mainDisplayUpdateSemaphore);
            picMain.Invalidate();
            // If this is too slow, we can force an update by calling picMain.Update (this needs to be invoked on the UI thread though)
        }

        public void DrawTileMemoryDebugFrame(ReadOnlySpan<byte> frame)
        {
            if (!IsTileMemoryDebugDisplayEnabled())
            {
                return;
            }

            WriteFrameToBitMap(frame, _debugTileMemoryBitmap, _debugTileMemoryDisplayUpdateSemaphore);
            picTileMemory.Invalidate();
        }

        public void DrawSpriteDebugFrame(ReadOnlySpan<byte> frame)
        {
            if (!IsSpriteDebugDisplayEnabled())
            {
                return;
            }

            WriteFrameToBitMap(frame, _debugSpritesBitmap, _debugSpritesDisplayUpdateSemaphore);
            picSprites.Invalidate();
        }

        public bool IsTileMemoryDebugDisplayEnabled()
        {
            return tileMemoryDebugToolStripMenuItem.Checked;
        }

        public bool IsSpriteDebugDisplayEnabled()
        {
            return spritesDebugToolStripMenuItem.Checked;
        }

        private void WriteFrameToBitMap(ReadOnlySpan<byte> frame, Bitmap bitmap, SemaphoreSlim semaphore)
        {
            if (!semaphore.Wait(10))
            {
                // Drop frame if taking too long
                return;
            }

            try
            {
                // Lock the bitmap so we can update it
                var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                var bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);

                var ptr = bmpData.Scan0;
                unsafe
                {
                    var dataAsSpan = new Span<byte>(ptr.ToPointer(), frame.Length);
                    frame.CopyTo(dataAsSpan);
                }

                bitmap.UnlockBits(bmpData);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private void loadCartridgeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (diagOpenCartridge.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            try
            {
                _currentCartridgeFilename = diagOpenCartridge.FileName;
                lblCartridgeName.Text = $@"Current Cartridge: {Path.GetFileNameWithoutExtension(diagOpenCartridge.SafeFileName)}";
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                btnPause.Enabled = false;
                btnPause.Text = "Pause";

                // If currently running, then stop current emulation and clear
                if (_console != null)
                {
                    StopEmulation();
                }
            }
            catch (SecurityException ex)
            {
                MessageBox.Show($@"Failed to load due to security error: {ex.Message}");
            }
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            if (_console != null)
            {
                StopEmulation();
            }

            btnStop.Enabled = true;
            btnPause.Enabled = true;
            btnPause.Text = "Pause";
            btnStart.Enabled = false;
            this.ActiveControl = picMain;
            await StartEmulation();
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            if (_console.IsPaused())
            {
                _fpsTimer.Start();
                _fpsStopWatch.Restart();
                _console.Unpause();
                btnPause.Text = "Pause";
            }
            else
            {
                _fpsTimer.Stop();
                _fpsStopWatch.Stop();
                _console.Pause();
                btnPause.Text = "Resume";
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            btnStop.Enabled = false;
            btnPause.Enabled = false;
            btnStart.Enabled = true;
            StopEmulation();
        }


        private async Task StartEmulation()
        {
            _emulationCancellationTokenSource = new CancellationTokenSource();

            _emulationScope = _scope.BeginLifetimeScope();
            _console = _emulationScope.Resolve<IMasterSystemConsole>();
            _controllers = _emulationScope.Resolve<IControllerPort>();

            var status = await _console.LoadCartridge(_currentCartridgeFilename, _emulationCancellationTokenSource.Token);
            if (!status)
            {
                WriteToDebugLogControl("Failed to load cartridge");
                return;
            }

            _emulationCancellationTokenSource.Token.Register(() => _console?.PowerOff());

            _emulatorThread = new Thread(RunEmulation)
            {
                Name = "RunEmulation"
            };
            _emulatorThread.Start();
            _fpsTimer.Start();
            _fpsStopWatch.Restart();
        }

        private void StopEmulation()
        {
            _fpsTimer.Stop();
            _fpsStopWatch.Stop();
            _emulationCancellationTokenSource?.Cancel();
            _emulatorThread.Join(TimeSpan.FromSeconds(1));
            WriteToDebugLogControl("Emulation stopped");
        }

        private void RunEmulation()
        {
            _console.PowerOn();
            _console.Run();

            // Powered off, clean up
            _emulationScope.Dispose();
            _emulationScope = null;
            _emulationCancellationTokenSource = null;
            _console = null;
            _controllers = null;
        }

        private void picMain_Paint(object sender, PaintEventArgs e)
        {
            if (!_mainDisplayUpdateSemaphore.Wait(10))
            {
                // Drop frame if taking too long
                return;
            }

            try
            {
                var graphics = _mainFrameBufferedGraphics.Graphics;
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.DrawImage(_mainFrameBitmap, 0, 0, picMain.Width, picMain.Height);
                _mainFrameBufferedGraphics.Render(e.Graphics);

                Interlocked.Increment(ref _frameCount);
            }
            finally
            {
                _mainDisplayUpdateSemaphore.Release();
            }

        }

        private void picTileMemory_Paint(object sender, PaintEventArgs e)
        {
            if (!_debugTileMemoryDisplayUpdateSemaphore.Wait(10))
            {
                // Drop frame if taking too long
                return;
            }

            try
            {
                var graphics = _debugTileMemoryBufferedGraphics.Graphics;
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.DrawImage(_debugTileMemoryBitmap, 0, 0, picTileMemory.Width, picTileMemory.Height);
                _debugTileMemoryBufferedGraphics.Render(e.Graphics);
            }
            finally
            {
                _debugTileMemoryDisplayUpdateSemaphore.Release();
            }
        }

        private void picSprites_Paint(object sender, PaintEventArgs e)
        {
            if (!_debugSpritesDisplayUpdateSemaphore.Wait(10))
            {
                // Drop frame if taking too long
                return;
            }

            try
            {
                var graphics = _debugSpritesBufferedGraphics.Graphics;
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.DrawImage(_debugSpritesBitmap, 0, 0, picSprites.Width, picSprites.Height);
                _debugSpritesBufferedGraphics.Render(e.Graphics);
            }
            finally
            {
                _debugSpritesDisplayUpdateSemaphore.Release();
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_emulationCancellationTokenSource is { IsCancellationRequested: false })
            {
                _emulationCancellationTokenSource?.Cancel();
            }
        }

        private void UpdateFramesPerSecond(object sender, EventArgs e)
        {
            _fpsStopWatch.Stop();
            var timeSinceLastUpdate = _fpsStopWatch.ElapsedMilliseconds;
            var currentFrameCount = Interlocked.Exchange(ref _frameCount, 0);
            _fpsStopWatch.Restart();

            var framesPerSecond = currentFrameCount / Convert.ToDouble(timeSinceLastUpdate) * 1000;
            lblFramesPerSecond.Text = $@"{framesPerSecond:0.00} fps";
        }

        private void frmMain_KeyDown(object sender, KeyEventArgs e)
        {
            UpdateController(e, true);
        }
        private void frmMain_KeyUp(object sender, KeyEventArgs e)
        {
            UpdateController(e, false);
        }

        private void frmMain_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (_console == null)
            {
                return;
            }

            switch (e.KeyChar)
            {
                case 'p':
                case 'P': _console.TriggerPauseButton(); break;
                default:
                    e.Handled = false;
                    return;
            }
            e.Handled = true;
        }

        private void UpdateController(KeyEventArgs e, bool pressed)
        {
            if (_controllers == null)
            {
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.Up: _controllers.ChangeInputAControlState(ControllerInputStatus.Up, pressed); break;
                case Keys.Down: _controllers.ChangeInputAControlState(ControllerInputStatus.Down, pressed); break;
                case Keys.Left: _controllers.ChangeInputAControlState(ControllerInputStatus.Left, pressed); break;
                case Keys.Right: _controllers.ChangeInputAControlState(ControllerInputStatus.Right, pressed); break;
                case Keys.Z: _controllers.ChangeInputAControlState(ControllerInputStatus.LeftButton, pressed); break;
                case Keys.X: _controllers.ChangeInputAControlState(ControllerInputStatus.RightButton, pressed); break;
                case Keys.R: _controllers.ChangeResetButtonState(pressed); break;
                default:
                    e.Handled = false;
                    return;
            }

            e.Handled = true;
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            this.ActiveControl = picMain;
        }
    }
}