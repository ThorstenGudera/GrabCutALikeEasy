namespace AvoidAGrabCutEasy
{
    partial class frmCachedPictures
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmCachedPictures));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.helplineRulerCtrl1 = new HelplineRulerControl.HelplineRulerCtrl();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.Label20 = new System.Windows.Forms.Label();
            this.cmbZoom = new System.Windows.Forms.ComboBox();
            this.cbBGColor = new System.Windows.Forms.CheckBox();
            this.btnOpen = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.cbLoadTo = new System.Windows.Forms.CheckBox();
            this.rbHlc12 = new System.Windows.Forms.RadioButton();
            this.rbHlc2 = new System.Windows.Forms.RadioButton();
            this.rbHlc1 = new System.Windows.Forms.RadioButton();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.panel1.SuspendLayout();
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
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.Label20);
            this.splitContainer1.Panel2.Controls.Add(this.cmbZoom);
            this.splitContainer1.Panel2.Controls.Add(this.cbBGColor);
            this.splitContainer1.Panel2.Controls.Add(this.btnOpen);
            this.splitContainer1.Panel2.Controls.Add(this.label2);
            this.splitContainer1.Panel2.Controls.Add(this.btnSave);
            this.splitContainer1.Panel2.Controls.Add(this.label1);
            this.splitContainer1.Panel2.Controls.Add(this.panel1);
            this.splitContainer1.Panel2.Controls.Add(this.btnCancel);
            this.splitContainer1.Panel2.Controls.Add(this.btnOK);
            this.splitContainer1.Size = new System.Drawing.Size(1200, 761);
            this.splitContainer1.SplitterDistance = 635;
            this.splitContainer1.TabIndex = 0;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.helplineRulerCtrl1);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.listBox1);
            this.splitContainer2.Size = new System.Drawing.Size(1200, 635);
            this.splitContainer2.SplitterDistance = 942;
            this.splitContainer2.TabIndex = 0;
            // 
            // helplineRulerCtrl1
            // 
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
            this.helplineRulerCtrl1.Size = new System.Drawing.Size(942, 635);
            this.helplineRulerCtrl1.TabIndex = 0;
            this.helplineRulerCtrl1.Zoom = 1F;
            this.helplineRulerCtrl1.ZoomSetManually = false;
            this.helplineRulerCtrl1.DBPanelDblClicked += new HelplineRulerControl.HelplineRulerCtrl.DblClickedEventHandler(this.helplineRulerCtrl1_DBPanelDblClicked);
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(16, 12);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(226, 602);
            this.listBox1.TabIndex = 0;
            this.listBox1.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
            // 
            // Label20
            // 
            this.Label20.AutoSize = true;
            this.Label20.Location = new System.Drawing.Point(352, 40);
            this.Label20.Name = "Label20";
            this.Label20.Size = new System.Drawing.Size(53, 13);
            this.Label20.TabIndex = 317;
            this.Label20.Text = "Set Zoom";
            // 
            // cmbZoom
            // 
            this.cmbZoom.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbZoom.FormattingEnabled = true;
            this.cmbZoom.Items.AddRange(new object[] {
            "4",
            "2",
            "1",
            "Fit_Width",
            "Fit"});
            this.cmbZoom.Location = new System.Drawing.Point(411, 37);
            this.cmbZoom.Name = "cmbZoom";
            this.cmbZoom.Size = new System.Drawing.Size(75, 21);
            this.cmbZoom.TabIndex = 316;
            this.cmbZoom.SelectedIndexChanged += new System.EventHandler(this.cmbZoom_SelectedIndexChanged);
            // 
            // cbBGColor
            // 
            this.cbBGColor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbBGColor.AutoSize = true;
            this.cbBGColor.Checked = true;
            this.cbBGColor.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbBGColor.Location = new System.Drawing.Point(1033, 38);
            this.cbBGColor.Name = "cbBGColor";
            this.cbBGColor.Size = new System.Drawing.Size(65, 17);
            this.cbBGColor.TabIndex = 315;
            this.cbBGColor.Text = "BG dark";
            this.cbBGColor.UseVisualStyleBackColor = true;
            this.cbBGColor.CheckedChanged += new System.EventHandler(this.cbBGColor_CheckedChanged);
            // 
            // btnOpen
            // 
            this.btnOpen.Location = new System.Drawing.Point(670, 78);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(75, 23);
            this.btnOpen.TabIndex = 108;
            this.btnOpen.Text = "Go";
            this.btnOpen.UseVisualStyleBackColor = true;
            this.btnOpen.Click += new System.EventHandler(this.btnOpen_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(561, 83);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(99, 13);
            this.label2.TabIndex = 107;
            this.label2.Text = "Open Cache Folder";
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(670, 35);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 106;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(567, 40);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(93, 13);
            this.label1.TabIndex = 105;
            this.label1.Text = "Save selected Pic";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.cbLoadTo);
            this.panel1.Controls.Add(this.rbHlc12);
            this.panel1.Controls.Add(this.rbHlc2);
            this.panel1.Controls.Add(this.rbHlc1);
            this.panel1.Location = new System.Drawing.Point(780, 28);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(216, 82);
            this.panel1.TabIndex = 104;
            // 
            // cbLoadTo
            // 
            this.cbLoadTo.AutoSize = true;
            this.cbLoadTo.Location = new System.Drawing.Point(14, 11);
            this.cbLoadTo.Name = "cbLoadTo";
            this.cbLoadTo.Size = new System.Drawing.Size(75, 17);
            this.cbLoadTo.TabIndex = 105;
            this.cbLoadTo.Text = "load pic to";
            this.cbLoadTo.UseVisualStyleBackColor = true;
            // 
            // rbHlc12
            // 
            this.rbHlc12.AutoSize = true;
            this.rbHlc12.Location = new System.Drawing.Point(98, 56);
            this.rbHlc12.Name = "rbHlc12";
            this.rbHlc12.Size = new System.Drawing.Size(47, 17);
            this.rbHlc12.TabIndex = 0;
            this.rbHlc12.Text = "Both";
            this.rbHlc12.UseVisualStyleBackColor = true;
            // 
            // rbHlc2
            // 
            this.rbHlc2.AutoSize = true;
            this.rbHlc2.Location = new System.Drawing.Point(98, 33);
            this.rbHlc2.Name = "rbHlc2";
            this.rbHlc2.Size = new System.Drawing.Size(109, 17);
            this.rbHlc2.TabIndex = 0;
            this.rbHlc2.Text = "HelplineRulerCtrl2";
            this.rbHlc2.UseVisualStyleBackColor = true;
            // 
            // rbHlc1
            // 
            this.rbHlc1.AutoSize = true;
            this.rbHlc1.Checked = true;
            this.rbHlc1.Location = new System.Drawing.Point(98, 10);
            this.rbHlc1.Name = "rbHlc1";
            this.rbHlc1.Size = new System.Drawing.Size(109, 17);
            this.rbHlc1.TabIndex = 0;
            this.rbHlc1.TabStop = true;
            this.rbHlc1.Text = "HelplineRulerCtrl1";
            this.rbHlc1.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnCancel.Location = new System.Drawing.Point(1113, 87);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 102;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnOK.Location = new System.Drawing.Point(1033, 87);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 103;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.FileName = "Bild1.png";
            this.saveFileDialog1.Filter = "Png-Images (*.png)|*.png";
            // 
            // frmCachedPictures
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(1200, 761);
            this.Controls.Add(this.splitContainer1);
            this.Name = "frmCachedPictures";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "frmCachedPictures";
            this.Load += new System.EventHandler(this.frmCachedPictures_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private HelplineRulerControl.HelplineRulerCtrl helplineRulerCtrl1;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnOpen;
        internal System.Windows.Forms.Label Label20;
        internal System.Windows.Forms.ComboBox cmbZoom;
        internal System.Windows.Forms.CheckBox cbBGColor;
        internal System.Windows.Forms.ListBox listBox1;
        internal System.Windows.Forms.CheckBox cbLoadTo;
        internal System.Windows.Forms.RadioButton rbHlc12;
        internal System.Windows.Forms.RadioButton rbHlc2;
        internal System.Windows.Forms.RadioButton rbHlc1;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
    }
}