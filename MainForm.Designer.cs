namespace Insomnia
{
    partial class MainForm
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
            this.components = new System.ComponentModel.Container();
            this.moveTimer = new System.Windows.Forms.Timer(this.components);
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.notifyMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.enableMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aggressiveMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.notifyMenuStrip.SuspendLayout();
            this.SuspendLayout();
            //
            // moveTimer
            //
            this.moveTimer.Interval = 60000;
            this.moveTimer.Tick += new System.EventHandler(this.OnMoveTimerTick);
            //
            // notifyIcon
            //
            this.notifyIcon.ContextMenuStrip = this.notifyMenuStrip;
            this.notifyIcon.Text = "Insomnia";
            this.notifyIcon.Visible = true;
            //
            // notifyMenuStrip
            //
            this.notifyMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.enableMenuItem,
            this.aggressiveMenuItem,
            this.exitMenuItem});
            this.notifyMenuStrip.Name = "notifyMenuStrip";
            //
            // enableMenuItem
            //
            this.enableMenuItem.CheckOnClick = true;
            this.enableMenuItem.Name = "enableMenuItem";
            this.enableMenuItem.Text = "Enabled";
            this.enableMenuItem.CheckedChanged += new System.EventHandler(this.OnEnableMenuItemCheckedChanged);
            //
            // aggressiveMenuItem
            //
            this.aggressiveMenuItem.CheckOnClick = true;
            this.aggressiveMenuItem.Name = "aggressiveMenuItem";
            this.aggressiveMenuItem.Text = "Aggressive Mode";
            //
            // exitMenuItem
            //
            this.exitMenuItem.Name = "exitMenuItem";
            this.exitMenuItem.Text = "Exit";
            this.exitMenuItem.Click += new System.EventHandler(this.OnExitMenuItemClick);
            //
            // MainForm
            //
            this.Name = "MainForm";
            this.ShowInTaskbar = false;
            this.notifyMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer moveTimer;
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ContextMenuStrip notifyMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem exitMenuItem;
        private System.Windows.Forms.ToolStripMenuItem enableMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aggressiveMenuItem;
    }
}

