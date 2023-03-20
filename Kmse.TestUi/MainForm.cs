using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Security;
using Autofac;
using Kmse.Core;
using Kmse.Core.IO.Controllers;
using Kmse.Core.Z80;
using Kmse.Core.Z80.Model;
using Kmse.Core.Z80.Registers.General;
using Kmse.Core.Z80.Registers.SpecialPurpose;
using Timer = System.Windows.Forms.Timer;

namespace Kmse.TestUi
{
    public partial class frmMain : Form
    {
        private readonly ILifetimeScope _scope;
        private IMasterSystemConsole _console;
        private IControllerPort _controllers;
        private IZ80Cpu _cpu;
        private IZ80FlagsManager _flags;
        private IZ80ProgramCounter _pc;
        private ILifetimeScope _emulationScope;

        delegate void LogMessageCallback(string text);
        private string _currentCartridgeFilename;
        private Thread _emulatorThread;
        private CancellationTokenSource _emulationCancellationTokenSource;

        private readonly Bitmap _mainFrameBitmap;
        private readonly Bitmap _debugSpritesBitmap;
        private readonly Bitmap _debugTileMemoryBitmap;

        private readonly BufferedGraphics _mainFrameBufferedGraphics;
        private readonly BufferedGraphics _debugSpritesBufferedGraphics;
        private readonly BufferedGraphics _debugTileMemoryBufferedGraphics;

        private readonly SemaphoreSlim _mainDisplayUpdateSemaphore;
        private readonly SemaphoreSlim _debugSpritesDisplayUpdateSemaphore;
        private readonly SemaphoreSlim _debugTileMemoryDisplayUpdateSemaphore;

        private readonly Timer _fpsTimer = new();
        private readonly Stopwatch _fpsStopWatch;
        private int _frameCount;

        private TextWriter _instructionLogWriter;
        private TextWriter _memoryAccessWriter;

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

            _fpsTimer.Interval = 1000;
            _fpsStopWatch = new Stopwatch();
            _fpsTimer.Tick += UpdateFramesPerSecond;
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
            if (IsLogInstructionsToFileEnabled())
            {
                _instructionLogWriter ??= new StreamWriter($@"instructions-{Path.GetFileNameWithoutExtension(_currentCartridgeFilename)}-{DateTime.Now.ToString("ddMMyyyyhhmm")}.txt");
                _instructionLogWriter.WriteLine($"{message} {GetRegistersAsString()} {GetFlagsAsString()}");
            }
        }

        public void LogMemoryOperation(string message)
        {
            // Disable this for now to avoid overloading the UI
            //WriteToDebugLogControl(message);

            if (IsLogMemoryAccessToFileEnabled())
            {
                _memoryAccessWriter ??= new StreamWriter($@"memoryaccess-{Path.GetFileNameWithoutExtension(_currentCartridgeFilename)}-{DateTime.Now.ToString("ddMMyyyyhhmm")}.txt");
                _memoryAccessWriter.WriteLine($"{_pc.Value:X4}: {message} {GetRegistersAsString()} {GetFlagsAsString()}");
            }
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

        public bool IsLogInstructionsToFileEnabled()
        {
            return mnuItemLogInstructionToFile.Checked;
        }

        public bool IsLogMemoryAccessToFileEnabled()
        {
            return mnuItemLogMemoryAccessToFile.Checked;
        }

        private string GetRegistersAsString()
        {
            var status = _cpu.GetStatus();
            return $"AF:{status.Af.Word:X4} BC:{status.Bc.Word:X4} DE:{status.De.Word:X4} HL:{status.Hl.Word:X4} IX:{status.Ix.Word:X4} IY:{status.Iy.Word:X4} PC: {status.Pc:X4} SP:{status.StackPointer:X4}";
        }

        private string GetFlagsAsString()
        {
            // ReSharper disable once UseStringInterpolation
            return string.Format("({0}{1}{2}{3}{4}{5}{6}{7})",
                _flags.IsFlagSet(Z80StatusFlags.SignS) ? "S" : "-",
                _flags.IsFlagSet(Z80StatusFlags.ZeroZ) ? "Z" : "-",
                _flags.IsFlagSet(Z80StatusFlags.NotUsedX5) ? "X5" : "-",
                _flags.IsFlagSet(Z80StatusFlags.HalfCarryH) ? "H" : "-",
                _flags.IsFlagSet(Z80StatusFlags.NotUsedX3) ? "X3" : "-",
                _flags.IsFlagSet(Z80StatusFlags.ParityOverflowPV) ? "P" : "-",
                _flags.IsFlagSet(Z80StatusFlags.AddSubtractN) ? "N" : "-",
                _flags.IsFlagSet(Z80StatusFlags.CarryC) ? "C" : "-");
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

        private static void WriteFrameToBitMap(ReadOnlySpan<byte> frame, Bitmap bitmap, SemaphoreSlim semaphore)
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
            _cpu = _emulationScope.Resolve<IZ80Cpu>();
            _flags = _emulationScope.Resolve<IZ80FlagsManager>();
            _pc = _emulationScope.Resolve<IZ80ProgramCounter>();

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

            _emulatorThread?.Join(TimeSpan.FromSeconds(1));

            _instructionLogWriter?.Flush();
            _memoryAccessWriter?.Flush();
            _instructionLogWriter?.Dispose();
            _memoryAccessWriter?.Dispose();
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

        private async void frmMain_Load(object sender, EventArgs e)
        {
            this.ActiveControl = picMain;

            // DEBUG code
            spritesDebugToolStripMenuItem.Checked = false;
            tileMemoryDebugToolStripMenuItem.Checked = true;
            picSprites.Visible = spritesDebugToolStripMenuItem.Checked;
            picTileMemory.Visible = tileMemoryDebugToolStripMenuItem.Checked;
            //_currentCartridgeFilename = @"C:\development\smsemulator\California Games (USA, Europe).sms";
            _currentCartridgeFilename = @"C:\development\smsemulator\bios13.sms";
            //_currentCartridgeFilename = @"C:\development\smsemulator\Sonic The Hedgehog 2 (Europe).sms";
            lblCartridgeName.Text = $@"Current Cartridge: {Path.GetFileNameWithoutExtension(_currentCartridgeFilename)}";
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            btnPause.Enabled = true;
            btnPause.Text = "Pause";
            await StartEmulation();
        }

        private void spritesDebugToolStripMenuItem_Click(object sender, EventArgs e)
        {
            picSprites.Visible = spritesDebugToolStripMenuItem.Checked;
        }

        private void tileMemoryDebugToolStripMenuItem_Click(object sender, EventArgs e)
        {
            picTileMemory.Visible = tileMemoryDebugToolStripMenuItem.Checked;
        }
    }
}