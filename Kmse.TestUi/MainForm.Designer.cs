namespace Kmse.TestUi
{
    partial class frmMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            this.statusMain = new System.Windows.Forms.StatusStrip();
            this.lblFramesPerSecond = new System.Windows.Forms.ToolStripStatusLabel();
            this.mnuMain = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadCartridgeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.quitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.debugToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loggingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.spriteTileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.spritesDebugToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tileMemoryDebugToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.lblCartridgeName = new System.Windows.Forms.ToolStripLabel();
            this.btnStart = new System.Windows.Forms.ToolStripButton();
            this.btnPause = new System.Windows.Forms.ToolStripButton();
            this.btnStop = new System.Windows.Forms.ToolStripButton();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.picMain = new System.Windows.Forms.PictureBox();
            this.picSprites = new System.Windows.Forms.PictureBox();
            this.txtDebugLog = new System.Windows.Forms.TextBox();
            this.picTileMemory = new System.Windows.Forms.PictureBox();
            this.diagOpenCartridge = new System.Windows.Forms.OpenFileDialog();
            this.statusMain.SuspendLayout();
            this.mnuMain.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picMain)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picSprites)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picTileMemory)).BeginInit();
            this.SuspendLayout();
            // 
            // statusMain
            // 
            this.statusMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblFramesPerSecond});
            this.statusMain.Location = new System.Drawing.Point(0, 988);
            this.statusMain.Name = "statusMain";
            this.statusMain.Size = new System.Drawing.Size(1567, 22);
            this.statusMain.TabIndex = 0;
            // 
            // lblFramesPerSecond
            // 
            this.lblFramesPerSecond.Name = "lblFramesPerSecond";
            this.lblFramesPerSecond.Size = new System.Drawing.Size(75, 17);
            this.lblFramesPerSecond.Text = "Not Running";
            // 
            // mnuMain
            // 
            this.mnuMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.debugToolStripMenuItem});
            this.mnuMain.Location = new System.Drawing.Point(0, 0);
            this.mnuMain.Name = "mnuMain";
            this.mnuMain.Size = new System.Drawing.Size(1567, 24);
            this.mnuMain.TabIndex = 1;
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadCartridgeToolStripMenuItem,
            this.quitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // loadCartridgeToolStripMenuItem
            // 
            this.loadCartridgeToolStripMenuItem.Name = "loadCartridgeToolStripMenuItem";
            this.loadCartridgeToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.loadCartridgeToolStripMenuItem.Text = "Load Cartridge";
            this.loadCartridgeToolStripMenuItem.Click += new System.EventHandler(this.loadCartridgeToolStripMenuItem_Click);
            // 
            // quitToolStripMenuItem
            // 
            this.quitToolStripMenuItem.Name = "quitToolStripMenuItem";
            this.quitToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.quitToolStripMenuItem.Text = "Quit";
            // 
            // debugToolStripMenuItem
            // 
            this.debugToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loggingToolStripMenuItem,
            this.spriteTileToolStripMenuItem});
            this.debugToolStripMenuItem.Name = "debugToolStripMenuItem";
            this.debugToolStripMenuItem.Size = new System.Drawing.Size(54, 20);
            this.debugToolStripMenuItem.Text = "Debug";
            // 
            // loggingToolStripMenuItem
            // 
            this.loggingToolStripMenuItem.Name = "loggingToolStripMenuItem";
            this.loggingToolStripMenuItem.Size = new System.Drawing.Size(127, 22);
            this.loggingToolStripMenuItem.Text = "Logging";
            // 
            // spriteTileToolStripMenuItem
            // 
            this.spriteTileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.spritesDebugToolStripMenuItem,
            this.tileMemoryDebugToolStripMenuItem});
            this.spriteTileToolStripMenuItem.Name = "spriteTileToolStripMenuItem";
            this.spriteTileToolStripMenuItem.Size = new System.Drawing.Size(127, 22);
            this.spriteTileToolStripMenuItem.Text = "Sprite/Tile";
            // 
            // spritesDebugToolStripMenuItem
            // 
            this.spritesDebugToolStripMenuItem.Checked = true;
            this.spritesDebugToolStripMenuItem.CheckOnClick = true;
            this.spritesDebugToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.spritesDebugToolStripMenuItem.Name = "spritesDebugToolStripMenuItem";
            this.spritesDebugToolStripMenuItem.Size = new System.Drawing.Size(140, 22);
            this.spritesDebugToolStripMenuItem.Text = "Sprites";
            // 
            // tileMemoryDebugToolStripMenuItem
            // 
            this.tileMemoryDebugToolStripMenuItem.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.tileMemoryDebugToolStripMenuItem.Checked = true;
            this.tileMemoryDebugToolStripMenuItem.CheckOnClick = true;
            this.tileMemoryDebugToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tileMemoryDebugToolStripMenuItem.Name = "tileMemoryDebugToolStripMenuItem";
            this.tileMemoryDebugToolStripMenuItem.Size = new System.Drawing.Size(140, 22);
            this.tileMemoryDebugToolStripMenuItem.Text = "Tile Memory";
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblCartridgeName,
            this.btnStart,
            this.btnPause,
            this.btnStop});
            this.toolStrip1.Location = new System.Drawing.Point(0, 24);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1567, 25);
            this.toolStrip1.TabIndex = 2;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // lblCartridgeName
            // 
            this.lblCartridgeName.Name = "lblCartridgeName";
            this.lblCartridgeName.Size = new System.Drawing.Size(117, 22);
            this.lblCartridgeName.Text = "No Cartridge Loaded";
            // 
            // btnStart
            // 
            this.btnStart.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.btnStart.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnStart.Enabled = false;
            this.btnStart.Image = ((System.Drawing.Image)(resources.GetObject("btnStart.Image")));
            this.btnStart.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(35, 22);
            this.btnStart.Text = "Start";
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnPause
            // 
            this.btnPause.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.btnPause.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnPause.Enabled = false;
            this.btnPause.Image = ((System.Drawing.Image)(resources.GetObject("btnPause.Image")));
            this.btnPause.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnPause.Name = "btnPause";
            this.btnPause.Size = new System.Drawing.Size(42, 22);
            this.btnPause.Text = "Pause";
            this.btnPause.Click += new System.EventHandler(this.btnPause_Click);
            // 
            // btnStop
            // 
            this.btnStop.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.btnStop.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnStop.Enabled = false;
            this.btnStop.Image = ((System.Drawing.Image)(resources.GetObject("btnStop.Image")));
            this.btnStop.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(35, 22);
            this.btnStop.Text = "Stop";
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 49);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.picMain);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.picSprites);
            this.splitContainer1.Panel2.Controls.Add(this.txtDebugLog);
            this.splitContainer1.Panel2.Controls.Add(this.picTileMemory);
            this.splitContainer1.Size = new System.Drawing.Size(1567, 939);
            this.splitContainer1.SplitterDistance = 1046;
            this.splitContainer1.TabIndex = 4;
            // 
            // picMain
            // 
            this.picMain.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.picMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picMain.Location = new System.Drawing.Point(0, 0);
            this.picMain.Name = "picMain";
            this.picMain.Size = new System.Drawing.Size(1046, 939);
            this.picMain.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picMain.TabIndex = 0;
            this.picMain.TabStop = false;
            this.picMain.Paint += new System.Windows.Forms.PaintEventHandler(this.picMain_Paint);
            // 
            // picSprites
            // 
            this.picSprites.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.picSprites.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picSprites.Location = new System.Drawing.Point(0, 384);
            this.picSprites.Name = "picSprites";
            this.picSprites.Size = new System.Drawing.Size(517, 386);
            this.picSprites.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picSprites.TabIndex = 2;
            this.picSprites.TabStop = false;
            this.picSprites.Paint += new System.Windows.Forms.PaintEventHandler(this.picSprites_Paint);
            // 
            // txtDebugLog
            // 
            this.txtDebugLog.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.txtDebugLog.Location = new System.Drawing.Point(0, 770);
            this.txtDebugLog.Multiline = true;
            this.txtDebugLog.Name = "txtDebugLog";
            this.txtDebugLog.ReadOnly = true;
            this.txtDebugLog.Size = new System.Drawing.Size(517, 169);
            this.txtDebugLog.TabIndex = 1;
            this.txtDebugLog.TabStop = false;
            // 
            // picTileMemory
            // 
            this.picTileMemory.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.picTileMemory.Dock = System.Windows.Forms.DockStyle.Top;
            this.picTileMemory.Location = new System.Drawing.Point(0, 0);
            this.picTileMemory.Name = "picTileMemory";
            this.picTileMemory.Size = new System.Drawing.Size(517, 384);
            this.picTileMemory.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picTileMemory.TabIndex = 0;
            this.picTileMemory.TabStop = false;
            this.picTileMemory.Paint += new System.Windows.Forms.PaintEventHandler(this.picTileMemory_Paint);
            // 
            // diagOpenCartridge
            // 
            this.diagOpenCartridge.FileName = "*.sms";
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1567, 1010);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.statusMain);
            this.Controls.Add(this.mnuMain);
            this.KeyPreview = true;
            this.MainMenuStrip = this.mnuMain;
            this.Name = "frmMain";
            this.Text = "KMSE - Sega Master System MkII Emulator (Test UI)";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.frmMain_KeyDown);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.frmMain_KeyPress);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.frmMain_KeyUp);
            this.statusMain.ResumeLayout(false);
            this.statusMain.PerformLayout();
            this.mnuMain.ResumeLayout(false);
            this.mnuMain.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picMain)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picSprites)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picTileMemory)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private StatusStrip statusMain;
        private MenuStrip mnuMain;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem loadCartridgeToolStripMenuItem;
        private ToolStripMenuItem quitToolStripMenuItem;
        private ToolStrip toolStrip1;
        private ToolStripLabel lblCartridgeName;
        private ToolStripButton btnPause;
        private ToolStripButton btnStart;
        private ToolStripButton btnStop;
        private SplitContainer splitContainer1;
        private PictureBox picMain;
        private PictureBox picSprites;
        private TextBox txtDebugLog;
        private PictureBox picTileMemory;
        private ToolStripMenuItem debugToolStripMenuItem;
        private ToolStripMenuItem loggingToolStripMenuItem;
        private ToolStripMenuItem spriteTileToolStripMenuItem;
        private ToolStripMenuItem spritesDebugToolStripMenuItem;
        private ToolStripMenuItem tileMemoryDebugToolStripMenuItem;
        private OpenFileDialog diagOpenCartridge;
        private ToolStripStatusLabel lblFramesPerSecond;
    }
}