using System.Drawing;
using System.Windows.Forms;

namespace AvoidAGrabCutEasy
{
    partial class frmCompose
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
            if (this.FBitmap != null)
            {
                this.FBitmap.Dispose();
                this.FBitmap = null;
            }
            if (this._bmpBU != null)
            {
                this._bmpBU.Dispose();
                this._bmpBU = null;
            }
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
            this.components = new System.ComponentModel.Container();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.toolStripStatusLabel4 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripProgressBar1 = new System.Windows.Forms.ToolStripProgressBar();
            this.Label20 = new System.Windows.Forms.Label();
            this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.btnOK = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.cmbZoom = new System.Windows.Forms.ComboBox();
            this.cbBGColor = new System.Windows.Forms.CheckBox();
            this.button8 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.luBitmapDesignerCtrl1 = new LUBitmapDesigner.LUBitmapDesignerCtrl();
            this.btnRedo = new System.Windows.Forms.Button();
            this.btnUndo = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.btnAlphaZAndGain = new System.Windows.Forms.Button();
            this.btnSetGamma = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.numGamma = new System.Windows.Forms.NumericUpDown();
            this.numAlphaZAndGain = new System.Windows.Forms.NumericUpDown();
            this.picInfoCtrl1 = new LUBitmapDesigner.PicInfoCtrl();
            this.btnLoad = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.backgroundWorker2 = new System.ComponentModel.BackgroundWorker();
            this.statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numGamma)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAlphaZAndGain)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStripStatusLabel4
            // 
            this.toolStripStatusLabel4.Font = new System.Drawing.Font("Segoe UI", 15.75F);
            this.toolStripStatusLabel4.Name = "toolStripStatusLabel4";
            this.toolStripStatusLabel4.Size = new System.Drawing.Size(37, 40);
            this.toolStripStatusLabel4.Text = "    ";
            // 
            // toolStripProgressBar1
            // 
            this.toolStripProgressBar1.Name = "toolStripProgressBar1";
            this.toolStripProgressBar1.Size = new System.Drawing.Size(467, 39);
            // 
            // Label20
            // 
            this.Label20.AutoSize = true;
            this.Label20.Location = new System.Drawing.Point(54, 823);
            this.Label20.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label20.Name = "Label20";
            this.Label20.Size = new System.Drawing.Size(53, 13);
            this.Label20.TabIndex = 655;
            this.Label20.Text = "Set Zoom";
            // 
            // toolStripStatusLabel2
            // 
            this.toolStripStatusLabel2.AutoSize = false;
            this.toolStripStatusLabel2.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this.toolStripStatusLabel2.Font = new System.Drawing.Font("Segoe UI", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            this.toolStripStatusLabel2.Size = new System.Drawing.Size(100, 40);
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this.toolStripStatusLabel1.Font = new System.Drawing.Font("Segoe UI", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(41, 40);
            this.toolStripStatusLabel1.Text = "    ";
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnOK.Location = new System.Drawing.Point(1191, 25);
            this.btnOK.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(88, 27);
            this.btnOK.TabIndex = 657;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripStatusLabel2,
            this.toolStripProgressBar1,
            this.toolStripStatusLabel4});
            this.statusStrip1.Location = new System.Drawing.Point(0, 69);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 16, 0);
            this.statusStrip1.Size = new System.Drawing.Size(1386, 45);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
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
            this.cmbZoom.Location = new System.Drawing.Point(122, 819);
            this.cmbZoom.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cmbZoom.Name = "cmbZoom";
            this.cmbZoom.Size = new System.Drawing.Size(87, 21);
            this.cmbZoom.TabIndex = 654;
            this.cmbZoom.SelectedIndexChanged += new System.EventHandler(this.cmbZoom_SelectedIndexChanged);
            // 
            // cbBGColor
            // 
            this.cbBGColor.AutoSize = true;
            this.cbBGColor.Checked = true;
            this.cbBGColor.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbBGColor.Location = new System.Drawing.Point(65, 740);
            this.cbBGColor.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cbBGColor.Name = "cbBGColor";
            this.cbBGColor.Size = new System.Drawing.Size(65, 17);
            this.cbBGColor.TabIndex = 653;
            this.cbBGColor.Text = "BG dark";
            this.cbBGColor.UseVisualStyleBackColor = true;
            this.cbBGColor.CheckedChanged += new System.EventHandler(this.cbBGColor_CheckedChanged);
            // 
            // button8
            // 
            this.button8.ForeColor = System.Drawing.SystemColors.ControlText;
            this.button8.Location = new System.Drawing.Point(56, 775);
            this.button8.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.button8.Name = "button8";
            this.button8.Size = new System.Drawing.Size(88, 27);
            this.button8.TabIndex = 652;
            this.button8.Text = "Reload";
            this.button8.UseVisualStyleBackColor = true;
            this.button8.Click += new System.EventHandler(this.button8_Click);
            // 
            // button2
            // 
            this.button2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.button2.ForeColor = System.Drawing.SystemColors.ControlText;
            this.button2.Location = new System.Drawing.Point(150, 735);
            this.button2.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(88, 27);
            this.button2.TabIndex = 651;
            this.button2.Text = "Save";
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.FileName = "Bild1.png";
            this.saveFileDialog1.Filter = "Png-Images (*.png)|*.png";
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.luBitmapDesignerCtrl1);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.btnRedo);
            this.splitContainer2.Panel2.Controls.Add(this.btnUndo);
            this.splitContainer2.Panel2.Controls.Add(this.label6);
            this.splitContainer2.Panel2.Controls.Add(this.label5);
            this.splitContainer2.Panel2.Controls.Add(this.btnAlphaZAndGain);
            this.splitContainer2.Panel2.Controls.Add(this.btnSetGamma);
            this.splitContainer2.Panel2.Controls.Add(this.label4);
            this.splitContainer2.Panel2.Controls.Add(this.numGamma);
            this.splitContainer2.Panel2.Controls.Add(this.numAlphaZAndGain);
            this.splitContainer2.Panel2.Controls.Add(this.picInfoCtrl1);
            this.splitContainer2.Panel2.Controls.Add(this.btnLoad);
            this.splitContainer2.Panel2.Controls.Add(this.label1);
            this.splitContainer2.Panel2.Controls.Add(this.Label20);
            this.splitContainer2.Panel2.Controls.Add(this.cmbZoom);
            this.splitContainer2.Panel2.Controls.Add(this.cbBGColor);
            this.splitContainer2.Panel2.Controls.Add(this.button8);
            this.splitContainer2.Panel2.Controls.Add(this.button2);
            this.splitContainer2.Size = new System.Drawing.Size(1386, 862);
            this.splitContainer2.SplitterDistance = 1080;
            this.splitContainer2.SplitterWidth = 5;
            this.splitContainer2.TabIndex = 0;
            // 
            // luBitmapDesignerCtrl1
            // 
            this.luBitmapDesignerCtrl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.luBitmapDesignerCtrl1.Location = new System.Drawing.Point(0, 0);
            this.luBitmapDesignerCtrl1.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.luBitmapDesignerCtrl1.Name = "luBitmapDesignerCtrl1";
            this.luBitmapDesignerCtrl1.ShapeList = null;
            this.luBitmapDesignerCtrl1.Size = new System.Drawing.Size(1080, 862);
            this.luBitmapDesignerCtrl1.TabIndex = 0;
            // 
            // btnRedo
            // 
            this.btnRedo.Enabled = false;
            this.btnRedo.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnRedo.Location = new System.Drawing.Point(120, 547);
            this.btnRedo.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnRedo.Name = "btnRedo";
            this.btnRedo.Size = new System.Drawing.Size(88, 27);
            this.btnRedo.TabIndex = 718;
            this.btnRedo.Text = "Redo";
            this.btnRedo.UseVisualStyleBackColor = true;
            this.btnRedo.Click += new System.EventHandler(this.btnRedo_Click);
            // 
            // btnUndo
            // 
            this.btnUndo.Enabled = false;
            this.btnUndo.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnUndo.Location = new System.Drawing.Point(24, 547);
            this.btnUndo.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnUndo.Name = "btnUndo";
            this.btnUndo.Size = new System.Drawing.Size(88, 27);
            this.btnUndo.TabIndex = 717;
            this.btnUndo.Text = "Undo";
            this.btnUndo.UseVisualStyleBackColor = true;
            this.btnUndo.Click += new System.EventHandler(this.btnUndo_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(209, 394);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(25, 13);
            this.label6.TabIndex = 715;
            this.label6.Text = "to 0";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(24, 394);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(77, 13);
            this.label5.TabIndex = 716;
            this.label5.Text = "set alpha up to";
            // 
            // btnAlphaZAndGain
            // 
            this.btnAlphaZAndGain.Location = new System.Drawing.Point(194, 422);
            this.btnAlphaZAndGain.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnAlphaZAndGain.Name = "btnAlphaZAndGain";
            this.btnAlphaZAndGain.Size = new System.Drawing.Size(88, 27);
            this.btnAlphaZAndGain.TabIndex = 713;
            this.btnAlphaZAndGain.Text = "Go";
            this.btnAlphaZAndGain.UseVisualStyleBackColor = true;
            this.btnAlphaZAndGain.Click += new System.EventHandler(this.btnAlphaZAndGain_Click);
            // 
            // btnSetGamma
            // 
            this.btnSetGamma.Location = new System.Drawing.Point(194, 497);
            this.btnSetGamma.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnSetGamma.Name = "btnSetGamma";
            this.btnSetGamma.Size = new System.Drawing.Size(88, 27);
            this.btnSetGamma.TabIndex = 714;
            this.btnSetGamma.Text = "Go";
            this.btnSetGamma.UseVisualStyleBackColor = true;
            this.btnSetGamma.Click += new System.EventHandler(this.btnSetGamma_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(24, 470);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(87, 13);
            this.label4.TabIndex = 711;
            this.label4.Text = "set AlphaGamma";
            // 
            // numGamma
            // 
            this.numGamma.DecimalPlaces = 2;
            this.numGamma.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numGamma.Location = new System.Drawing.Point(132, 468);
            this.numGamma.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.numGamma.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.numGamma.Name = "numGamma";
            this.numGamma.Size = new System.Drawing.Size(70, 20);
            this.numGamma.TabIndex = 712;
            this.numGamma.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // numAlphaZAndGain
            // 
            this.numAlphaZAndGain.Location = new System.Drawing.Point(132, 392);
            this.numAlphaZAndGain.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.numAlphaZAndGain.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.numAlphaZAndGain.Name = "numAlphaZAndGain";
            this.numAlphaZAndGain.Size = new System.Drawing.Size(70, 20);
            this.numAlphaZAndGain.TabIndex = 710;
            this.numAlphaZAndGain.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            // 
            // picInfoCtrl1
            // 
            this.picInfoCtrl1.Location = new System.Drawing.Point(9, 83);
            this.picInfoCtrl1.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.picInfoCtrl1.Name = "picInfoCtrl1";
            this.picInfoCtrl1.Size = new System.Drawing.Size(280, 300);
            this.picInfoCtrl1.TabIndex = 658;
            // 
            // btnLoad
            // 
            this.btnLoad.Location = new System.Drawing.Point(150, 35);
            this.btnLoad.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(88, 27);
            this.btnLoad.TabIndex = 657;
            this.btnLoad.Text = "Go";
            this.btnLoad.UseVisualStyleBackColor = true;
            this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(31, 40);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 13);
            this.label1.TabIndex = 656;
            this.label1.Text = "Load BG image";
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnCancel.Location = new System.Drawing.Point(1284, 25);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(88, 27);
            this.btnCancel.TabIndex = 656;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.btnCancel);
            this.splitContainer1.Panel2.Controls.Add(this.btnOK);
            this.splitContainer1.Panel2.Controls.Add(this.statusStrip1);
            this.splitContainer1.Size = new System.Drawing.Size(1386, 981);
            this.splitContainer1.SplitterDistance = 862;
            this.splitContainer1.SplitterWidth = 5;
            this.splitContainer1.TabIndex = 1;
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.Filter = "Images - (*.bmp;*.jpg;*.jpeg;*.jfif;*.png)|*.bmp;*.jpg;*.jpeg;*.jfif;*.png";
            // 
            // backgroundWorker1
            // 
            this.backgroundWorker1.WorkerReportsProgress = true;
            this.backgroundWorker1.WorkerSupportsCancellation = true;
            this.backgroundWorker1.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker1_DoWork);
            this.backgroundWorker1.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker1_RunWorkerCompleted);
            // 
            // backgroundWorker2
            // 
            this.backgroundWorker2.WorkerReportsProgress = true;
            this.backgroundWorker2.WorkerSupportsCancellation = true;
            this.backgroundWorker2.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker2_DoWork);
            this.backgroundWorker2.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker2_RunWorkerCompleted);
            // 
            // frmCompose
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1386, 981);
            this.Controls.Add(this.splitContainer1);
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "frmCompose";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "frmCompose";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmCompose_FormClosing);
            this.Load += new System.EventHandler(this.btnLoad_Click);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numGamma)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAlphaZAndGain)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel4;
        private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar1;
        internal System.Windows.Forms.Label Label20;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel2;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.StatusStrip statusStrip1;
        internal System.Windows.Forms.ComboBox cmbZoom;
        internal System.Windows.Forms.CheckBox cbBGColor;
        private System.Windows.Forms.Button button8;
        internal System.Windows.Forms.Button button2;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private LUBitmapDesigner.LUBitmapDesignerCtrl luBitmapDesignerCtrl1;
        private System.Windows.Forms.Button btnLoad;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private LUBitmapDesigner.PicInfoCtrl picInfoCtrl1;
        private Label label6;
        private Label label5;
        private Button btnAlphaZAndGain;
        private Button btnSetGamma;
        private Label label4;
        private NumericUpDown numGamma;
        private NumericUpDown numAlphaZAndGain;
        private Button btnRedo;
        private Button btnUndo;
        internal System.ComponentModel.BackgroundWorker backgroundWorker1;
        internal System.ComponentModel.BackgroundWorker backgroundWorker2;
    }
}