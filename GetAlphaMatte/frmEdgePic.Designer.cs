namespace GetAlphaMatte
{
    partial class frmEdgePic
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmEdgePic));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.helplineRulerCtrl1 = new HelplineRulerControl.HelplineRulerCtrl();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.helplineRulerCtrl1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.btnSave);
            this.splitContainer1.Panel2.Controls.Add(this.btnClose);
            this.splitContainer1.Size = new System.Drawing.Size(800, 669);
            this.splitContainer1.SplitterDistance = 624;
            this.splitContainer1.TabIndex = 0;
            // 
            // helplineRulerCtrl1
            // 
            this.helplineRulerCtrl1.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.helplineRulerCtrl1.Bmp = null;
            this.helplineRulerCtrl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.helplineRulerCtrl1.DontDoLayout = false;
            this.helplineRulerCtrl1.DontHandleDoubleClick = false;
            this.helplineRulerCtrl1.DontPaintBaseImg = false;
            this.helplineRulerCtrl1.DontProcDoubleClick = false;
            this.helplineRulerCtrl1.IgnoreZoom = false;
            this.helplineRulerCtrl1.Location = new System.Drawing.Point(0, 0);
            this.helplineRulerCtrl1.MoveHelpLinesOnResize = false;
            this.helplineRulerCtrl1.Name = "helplineRulerCtrl1";
            this.helplineRulerCtrl1.SetZoomOnlyByMethodCall = false;
            this.helplineRulerCtrl1.Size = new System.Drawing.Size(800, 624);
            this.helplineRulerCtrl1.TabIndex = 0;
            this.helplineRulerCtrl1.Zoom = 1F;
            this.helplineRulerCtrl1.ZoomSetManually = false;
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.Location = new System.Drawing.Point(560, 6);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 1;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.Location = new System.Drawing.Point(713, 6);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 0;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.FileName = "Bild1.png";
            this.saveFileDialog1.Filter = "Png-Images (*.png)|*.png";
            // 
            // frmEdgePic
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 669);
            this.Controls.Add(this.splitContainer1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmEdgePic";
            this.Text = "frmEdgePic";
            this.Load += new System.EventHandler(this.frmEdgePic_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button btnClose;
        private HelplineRulerControl.HelplineRulerCtrl helplineRulerCtrl1;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
    }
}