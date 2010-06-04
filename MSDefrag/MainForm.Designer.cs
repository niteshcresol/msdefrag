namespace MSDefrag
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.progressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.progressBarText = new System.Windows.Forms.ToolStripStatusLabel();
            this.progressBarStatistics = new System.Windows.Forms.ToolStripStatusLabel();
            this.LogMessageLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolButtonStartDefrag = new System.Windows.Forms.ToolStripButton();
            this.toolButtonStartSimulation = new System.Windows.Forms.ToolStripButton();
            this.toolButtonStopDefrag = new System.Windows.Forms.ToolStripButton();
            this.diskBitmap = new MSDefrag.DiskBitmap();
            this.statusStrip1.SuspendLayout();
            this.toolStripContainer1.BottomToolStripPanel.SuspendLayout();
            this.toolStripContainer1.ContentPanel.SuspendLayout();
            this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
            this.toolStripContainer1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.diskBitmap)).BeginInit();
            this.SuspendLayout();
            // 
            // statusStrip1
            // 
            this.statusStrip1.BackColor = System.Drawing.SystemColors.Control;
            this.statusStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.statusStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Visible;
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.progressBar,
            this.progressBarText,
            this.progressBarStatistics,
            this.LogMessageLabel});
            this.statusStrip1.Location = new System.Drawing.Point(0, 0);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(830, 22);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(49, 17);
            this.toolStripStatusLabel1.Text = "Progress";
            // 
            // progressBar
            // 
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(200, 16);
            this.progressBar.Step = 1;
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            // 
            // progressBarText
            // 
            this.progressBarText.Name = "progressBarText";
            this.progressBarText.Size = new System.Drawing.Size(27, 17);
            this.progressBarText.Text = "0 %";
            // 
            // progressBarStatistics
            // 
            this.progressBarStatistics.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.None;
            this.progressBarStatistics.Name = "progressBarStatistics";
            this.progressBarStatistics.Size = new System.Drawing.Size(0, 17);
            this.progressBarStatistics.Text = "Frame skip: 0";
            // 
            // LogMessageLabel
            // 
            this.LogMessageLabel.Name = "LogMessageLabel";
            this.LogMessageLabel.Size = new System.Drawing.Size(0, 17);
            // 
            // toolStripContainer1
            // 
            // 
            // toolStripContainer1.BottomToolStripPanel
            // 
            this.toolStripContainer1.BottomToolStripPanel.Controls.Add(this.statusStrip1);
            // 
            // toolStripContainer1.ContentPanel
            // 
            this.toolStripContainer1.ContentPanel.BackColor = System.Drawing.SystemColors.Control;
            this.toolStripContainer1.ContentPanel.Controls.Add(this.diskBitmap);
            this.toolStripContainer1.ContentPanel.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(830, 577);
            this.toolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
            this.toolStripContainer1.Margin = new System.Windows.Forms.Padding(0);
            this.toolStripContainer1.Name = "toolStripContainer1";
            this.toolStripContainer1.Size = new System.Drawing.Size(830, 630);
            this.toolStripContainer1.TabIndex = 1;
            this.toolStripContainer1.Text = "toolStripContainer1";
            // 
            // toolStripContainer1.TopToolStripPanel
            // 
            this.toolStripContainer1.TopToolStripPanel.BackColor = System.Drawing.SystemColors.Control;
            this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.toolStrip1);
            this.toolStripContainer1.TopToolStripPanel.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.toolStripContainer1.TopToolStripPanel.Click += new System.EventHandler(this.OnStartDefragmentation);
            // 
            // toolStrip1
            // 
            this.toolStrip1.BackColor = System.Drawing.SystemColors.Control;
            this.toolStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolButtonStartDefrag,
            this.toolButtonStartSimulation,
            this.toolButtonStopDefrag});
            this.toolStrip1.Location = new System.Drawing.Point(3, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.toolStrip1.Size = new System.Drawing.Size(220, 31);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "gggg";
            // 
            // toolButtonStartDefrag
            // 
            this.toolButtonStartDefrag.Image = ((System.Drawing.Image)(resources.GetObject("toolButtonStartDefrag.Image")));
            this.toolButtonStartDefrag.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolButtonStartDefrag.Name = "toolButtonStartDefrag";
            this.toolButtonStartDefrag.Size = new System.Drawing.Size(68, 28);
            this.toolButtonStartDefrag.Text = "Defrag";
            this.toolButtonStartDefrag.ToolTipText = "Defrag";
            this.toolButtonStartDefrag.Click += new System.EventHandler(this.OnStartDefragmentation);
            // 
            // toolButtonStartSimulation
            // 
            this.toolButtonStartSimulation.BackColor = System.Drawing.SystemColors.Control;
            this.toolButtonStartSimulation.Image = ((System.Drawing.Image)(resources.GetObject("toolButtonStartSimulation.Image")));
            this.toolButtonStartSimulation.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolButtonStartSimulation.Name = "toolButtonStartSimulation";
            this.toolButtonStartSimulation.Size = new System.Drawing.Size(83, 28);
            this.toolButtonStartSimulation.Text = "Simulation";
            this.toolButtonStartSimulation.ToolTipText = "Simulation";
            this.toolButtonStartSimulation.Click += new System.EventHandler(this.OnStartSimulation);
            // 
            // toolButtonStopDefrag
            // 
            this.toolButtonStopDefrag.Enabled = false;
            this.toolButtonStopDefrag.Image = ((System.Drawing.Image)(resources.GetObject("toolButtonStopDefrag.Image")));
            this.toolButtonStopDefrag.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolButtonStopDefrag.Name = "toolButtonStopDefrag";
            this.toolButtonStopDefrag.Size = new System.Drawing.Size(57, 28);
            this.toolButtonStopDefrag.Text = "Stop";
            this.toolButtonStopDefrag.Click += new System.EventHandler(this.OnStopDefrag);
            // 
            // pictureBox1
            // 
            this.diskBitmap.BackColor = System.Drawing.SystemColors.Control;
            this.diskBitmap.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.diskBitmap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.diskBitmap.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.diskBitmap.Location = new System.Drawing.Point(0, 0);
            this.diskBitmap.Margin = new System.Windows.Forms.Padding(0);
            this.diskBitmap.Name = "pictureBox1";
            this.diskBitmap.Size = new System.Drawing.Size(830, 577);
            this.diskBitmap.TabIndex = 0;
            this.diskBitmap.TabStop = false;
            this.diskBitmap.SizeChanged += new System.EventHandler(this.OnDiskBitmapSizeChanged);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(830, 630);
            this.Controls.Add(this.toolStripContainer1);
            this.Name = "MainForm";
            this.Text = "Marko\'s Defragmentation Tool";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnGuiClosing);
            this.Shown += new System.EventHandler(this.OnShow);
            this.ResizeBegin += new System.EventHandler(this.OnResizeBegin);
            this.ResizeEnd += new System.EventHandler(this.OnResizeEnd);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.toolStripContainer1.BottomToolStripPanel.ResumeLayout(false);
            this.toolStripContainer1.BottomToolStripPanel.PerformLayout();
            this.toolStripContainer1.ContentPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.PerformLayout();
            this.toolStripContainer1.ResumeLayout(false);
            this.toolStripContainer1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.diskBitmap)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DiskBitmap diskBitmap;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripContainer toolStripContainer1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton toolButtonStartDefrag;
        private System.Windows.Forms.ToolStripButton toolButtonStartSimulation;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripStatusLabel progressBarText;
        private System.Windows.Forms.ToolStripProgressBar progressBar;
        private System.Windows.Forms.ToolStripButton toolButtonStopDefrag;
        private System.Windows.Forms.ToolStripStatusLabel progressBarStatistics;
        private System.Windows.Forms.ToolStripStatusLabel LogMessageLabel;
    }
}

