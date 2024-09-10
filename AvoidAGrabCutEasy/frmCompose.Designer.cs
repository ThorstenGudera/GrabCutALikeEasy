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
            components = new System.ComponentModel.Container();
            toolTip1 = new ToolTip(components);
            toolStripStatusLabel4 = new ToolStripStatusLabel();
            toolStripProgressBar1 = new ToolStripProgressBar();
            Label20 = new Label();
            toolStripStatusLabel2 = new ToolStripStatusLabel();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            btnOK = new Button();
            statusStrip1 = new StatusStrip();
            cmbZoom = new ComboBox();
            cbBGColor = new CheckBox();
            button8 = new Button();
            button2 = new Button();
            saveFileDialog1 = new SaveFileDialog();
            splitContainer2 = new SplitContainer();
            luBitmapDesignerCtrl1 = new LUBitmapDesigner.LUBitmapDesignerCtrl();
            btnRedo = new Button();
            btnUndo = new Button();
            label6 = new Label();
            label5 = new Label();
            btnAlphaZAndGain = new Button();
            btnSetGamma = new Button();
            label4 = new Label();
            numGamma = new NumericUpDown();
            numAlphaZAndGain = new NumericUpDown();
            picInfoCtrl1 = new LUBitmapDesigner.PicInfoCtrl();
            btnLoad = new Button();
            label1 = new Label();
            btnCancel = new Button();
            splitContainer1 = new SplitContainer();
            openFileDialog1 = new OpenFileDialog();
            backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            backgroundWorker2 = new System.ComponentModel.BackgroundWorker();
            statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numGamma).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numAlphaZAndGain).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            SuspendLayout();
            // 
            // toolStripStatusLabel4
            // 
            toolStripStatusLabel4.Font = new Font("Segoe UI", 15.75F);
            toolStripStatusLabel4.Name = "toolStripStatusLabel4";
            toolStripStatusLabel4.Size = new Size(37, 40);
            toolStripStatusLabel4.Text = "    ";
            // 
            // toolStripProgressBar1
            // 
            toolStripProgressBar1.Name = "toolStripProgressBar1";
            toolStripProgressBar1.Size = new Size(467, 39);
            // 
            // Label20
            // 
            Label20.AutoSize = true;
            Label20.Location = new Point(54, 823);
            Label20.Margin = new Padding(4, 0, 4, 0);
            Label20.Name = "Label20";
            Label20.Size = new Size(58, 15);
            Label20.TabIndex = 655;
            Label20.Text = "Set Zoom";
            // 
            // toolStripStatusLabel2
            // 
            toolStripStatusLabel2.AutoSize = false;
            toolStripStatusLabel2.BorderSides = ToolStripStatusLabelBorderSides.Left | ToolStripStatusLabelBorderSides.Top | ToolStripStatusLabelBorderSides.Right | ToolStripStatusLabelBorderSides.Bottom;
            toolStripStatusLabel2.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            toolStripStatusLabel2.Size = new Size(100, 40);
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.BorderSides = ToolStripStatusLabelBorderSides.Left | ToolStripStatusLabelBorderSides.Top | ToolStripStatusLabelBorderSides.Right | ToolStripStatusLabelBorderSides.Bottom;
            toolStripStatusLabel1.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(41, 40);
            toolStripStatusLabel1.Text = "    ";
            // 
            // btnOK
            // 
            btnOK.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnOK.DialogResult = DialogResult.OK;
            btnOK.ForeColor = SystemColors.ControlText;
            btnOK.Location = new Point(1191, 25);
            btnOK.Margin = new Padding(4, 3, 4, 3);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(88, 27);
            btnOK.TabIndex = 657;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1, toolStripStatusLabel2, toolStripProgressBar1, toolStripStatusLabel4 });
            statusStrip1.Location = new Point(0, 69);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new Padding(1, 0, 16, 0);
            statusStrip1.Size = new Size(1386, 45);
            statusStrip1.TabIndex = 2;
            statusStrip1.Text = "statusStrip1";
            // 
            // cmbZoom
            // 
            cmbZoom.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbZoom.FormattingEnabled = true;
            cmbZoom.Items.AddRange(new object[] { "4", "2", "1", "Fit_Width", "Fit" });
            cmbZoom.Location = new Point(122, 819);
            cmbZoom.Margin = new Padding(4, 3, 4, 3);
            cmbZoom.Name = "cmbZoom";
            cmbZoom.Size = new Size(87, 23);
            cmbZoom.TabIndex = 654;
            cmbZoom.SelectedIndexChanged += cmbZoom_SelectedIndexChanged;
            // 
            // cbBGColor
            // 
            cbBGColor.AutoSize = true;
            cbBGColor.Checked = true;
            cbBGColor.CheckState = CheckState.Checked;
            cbBGColor.Location = new Point(65, 740);
            cbBGColor.Margin = new Padding(4, 3, 4, 3);
            cbBGColor.Name = "cbBGColor";
            cbBGColor.Size = new Size(67, 19);
            cbBGColor.TabIndex = 653;
            cbBGColor.Text = "BG dark";
            cbBGColor.UseVisualStyleBackColor = true;
            cbBGColor.CheckedChanged += cbBGColor_CheckedChanged;
            // 
            // button8
            // 
            button8.ForeColor = SystemColors.ControlText;
            button8.Location = new Point(56, 775);
            button8.Margin = new Padding(4, 3, 4, 3);
            button8.Name = "button8";
            button8.Size = new Size(88, 27);
            button8.TabIndex = 652;
            button8.Text = "Reload";
            button8.UseVisualStyleBackColor = true;
            button8.Click += button8_Click;
            // 
            // button2
            // 
            button2.FlatStyle = FlatStyle.System;
            button2.ForeColor = SystemColors.ControlText;
            button2.Location = new Point(150, 735);
            button2.Margin = new Padding(4, 3, 4, 3);
            button2.Name = "button2";
            button2.Size = new Size(88, 27);
            button2.TabIndex = 651;
            button2.Text = "Save";
            button2.Click += button2_Click;
            // 
            // saveFileDialog1
            // 
            saveFileDialog1.FileName = "Bild1.png";
            saveFileDialog1.Filter = "Png-Images (*.png)|*.png";
            // 
            // splitContainer2
            // 
            splitContainer2.Dock = DockStyle.Fill;
            splitContainer2.FixedPanel = FixedPanel.Panel2;
            splitContainer2.Location = new Point(0, 0);
            splitContainer2.Margin = new Padding(4, 3, 4, 3);
            splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(luBitmapDesignerCtrl1);
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(btnRedo);
            splitContainer2.Panel2.Controls.Add(btnUndo);
            splitContainer2.Panel2.Controls.Add(label6);
            splitContainer2.Panel2.Controls.Add(label5);
            splitContainer2.Panel2.Controls.Add(btnAlphaZAndGain);
            splitContainer2.Panel2.Controls.Add(btnSetGamma);
            splitContainer2.Panel2.Controls.Add(label4);
            splitContainer2.Panel2.Controls.Add(numGamma);
            splitContainer2.Panel2.Controls.Add(numAlphaZAndGain);
            splitContainer2.Panel2.Controls.Add(picInfoCtrl1);
            splitContainer2.Panel2.Controls.Add(btnLoad);
            splitContainer2.Panel2.Controls.Add(label1);
            splitContainer2.Panel2.Controls.Add(Label20);
            splitContainer2.Panel2.Controls.Add(cmbZoom);
            splitContainer2.Panel2.Controls.Add(cbBGColor);
            splitContainer2.Panel2.Controls.Add(button8);
            splitContainer2.Panel2.Controls.Add(button2);
            splitContainer2.Size = new Size(1386, 862);
            splitContainer2.SplitterDistance = 1081;
            splitContainer2.SplitterWidth = 5;
            splitContainer2.TabIndex = 0;
            // 
            // luBitmapDesignerCtrl1
            // 
            luBitmapDesignerCtrl1.Dock = DockStyle.Fill;
            luBitmapDesignerCtrl1.Location = new Point(0, 0);
            luBitmapDesignerCtrl1.Margin = new Padding(5, 3, 5, 3);
            luBitmapDesignerCtrl1.Name = "luBitmapDesignerCtrl1";
            luBitmapDesignerCtrl1.ShapeList = null;
            luBitmapDesignerCtrl1.Size = new Size(1081, 862);
            luBitmapDesignerCtrl1.TabIndex = 0;
            // 
            // btnRedo
            // 
            btnRedo.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnRedo.Enabled = false;
            btnRedo.ForeColor = SystemColors.ControlText;
            btnRedo.Location = new Point(120, 547);
            btnRedo.Margin = new Padding(4, 3, 4, 3);
            btnRedo.Name = "btnRedo";
            btnRedo.Size = new Size(88, 27);
            btnRedo.TabIndex = 718;
            btnRedo.Text = "Redo";
            btnRedo.UseVisualStyleBackColor = true;
            btnRedo.Click += btnRedo_Click;
            // 
            // btnUndo
            // 
            btnUndo.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnUndo.Enabled = false;
            btnUndo.ForeColor = SystemColors.ControlText;
            btnUndo.Location = new Point(24, 547);
            btnUndo.Margin = new Padding(4, 3, 4, 3);
            btnUndo.Name = "btnUndo";
            btnUndo.Size = new Size(88, 27);
            btnUndo.TabIndex = 717;
            btnUndo.Text = "Undo";
            btnUndo.UseVisualStyleBackColor = true;
            btnUndo.Click += btnUndo_Click;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(209, 394);
            label6.Name = "label6";
            label6.Size = new Size(27, 15);
            label6.TabIndex = 715;
            label6.Text = "to 0";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(24, 394);
            label5.Name = "label5";
            label5.Size = new Size(85, 15);
            label5.TabIndex = 716;
            label5.Text = "set alpha up to";
            // 
            // btnAlphaZAndGain
            // 
            btnAlphaZAndGain.Location = new Point(194, 422);
            btnAlphaZAndGain.Margin = new Padding(4, 3, 4, 3);
            btnAlphaZAndGain.Name = "btnAlphaZAndGain";
            btnAlphaZAndGain.Size = new Size(88, 27);
            btnAlphaZAndGain.TabIndex = 713;
            btnAlphaZAndGain.Text = "Go";
            btnAlphaZAndGain.UseVisualStyleBackColor = true;
            btnAlphaZAndGain.Click += btnAlphaZAndGain_Click;
            // 
            // btnSetGamma
            // 
            btnSetGamma.Location = new Point(194, 497);
            btnSetGamma.Margin = new Padding(4, 3, 4, 3);
            btnSetGamma.Name = "btnSetGamma";
            btnSetGamma.Size = new Size(88, 27);
            btnSetGamma.TabIndex = 714;
            btnSetGamma.Text = "Go";
            btnSetGamma.UseVisualStyleBackColor = true;
            btnSetGamma.Click += btnSetGamma_Click;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(24, 470);
            label4.Margin = new Padding(4, 0, 4, 0);
            label4.Name = "label4";
            label4.Size = new Size(98, 15);
            label4.TabIndex = 711;
            label4.Text = "set AlphaGamma";
            // 
            // numGamma
            // 
            numGamma.DecimalPlaces = 2;
            numGamma.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            numGamma.Location = new Point(132, 468);
            numGamma.Margin = new Padding(4, 3, 4, 3);
            numGamma.Minimum = new decimal(new int[] { 1, 0, 0, 131072 });
            numGamma.Name = "numGamma";
            numGamma.Size = new Size(70, 23);
            numGamma.TabIndex = 712;
            numGamma.Value = new decimal(new int[] { 2, 0, 0, 0 });
            // 
            // numAlphaZAndGain
            // 
            numAlphaZAndGain.Location = new Point(132, 392);
            numAlphaZAndGain.Margin = new Padding(4, 3, 4, 3);
            numAlphaZAndGain.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            numAlphaZAndGain.Name = "numAlphaZAndGain";
            numAlphaZAndGain.Size = new Size(70, 23);
            numAlphaZAndGain.TabIndex = 710;
            numAlphaZAndGain.Value = new decimal(new int[] { 50, 0, 0, 0 });
            // 
            // picInfoCtrl1
            // 
            picInfoCtrl1.Location = new Point(9, 83);
            picInfoCtrl1.Margin = new Padding(5, 3, 5, 3);
            picInfoCtrl1.Name = "picInfoCtrl1";
            picInfoCtrl1.Size = new Size(280, 300);
            picInfoCtrl1.TabIndex = 658;
            // 
            // btnLoad
            // 
            btnLoad.Location = new Point(150, 35);
            btnLoad.Margin = new Padding(4, 3, 4, 3);
            btnLoad.Name = "btnLoad";
            btnLoad.Size = new Size(88, 27);
            btnLoad.TabIndex = 657;
            btnLoad.Text = "Go";
            btnLoad.UseVisualStyleBackColor = true;
            btnLoad.Click += btnLoad_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(31, 40);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(87, 15);
            label1.TabIndex = 656;
            label1.Text = "Load BG image";
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.ForeColor = SystemColors.ControlText;
            btnCancel.Location = new Point(1284, 25);
            btnCancel.Margin = new Padding(4, 3, 4, 3);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(88, 27);
            btnCancel.TabIndex = 656;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Margin = new Padding(4, 3, 4, 3);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(btnCancel);
            splitContainer1.Panel2.Controls.Add(btnOK);
            splitContainer1.Panel2.Controls.Add(statusStrip1);
            splitContainer1.Size = new Size(1386, 981);
            splitContainer1.SplitterDistance = 862;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 1;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            openFileDialog1.Filter = "Images - (*.bmp;*.jpg;*.jpeg;*.jfif;*.png)|*.bmp;*.jpg;*.jpeg;*.jfif;*.png";
            // 
            // backgroundWorker1
            // 
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true;
            backgroundWorker1.DoWork += backgroundWorker1_DoWork;
            backgroundWorker1.RunWorkerCompleted += backgroundWorker1_RunWorkerCompleted;
            // 
            // backgroundWorker2
            // 
            backgroundWorker2.WorkerReportsProgress = true;
            backgroundWorker2.WorkerSupportsCancellation = true;
            backgroundWorker2.DoWork += backgroundWorker2_DoWork;
            backgroundWorker2.RunWorkerCompleted += backgroundWorker2_RunWorkerCompleted;
            // 
            // frmCompose
            // 
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1386, 981);
            Controls.Add(splitContainer1);
            Margin = new Padding(4, 3, 4, 3);
            Name = "frmCompose";
            StartPosition = FormStartPosition.CenterParent;
            Text = "frmCompose";
            FormClosing += frmCompose_FormClosing;
            Load += frmCompose_Load;
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel2.ResumeLayout(false);
            splitContainer2.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numGamma).EndInit();
            ((System.ComponentModel.ISupportInitialize)numAlphaZAndGain).EndInit();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ResumeLayout(false);
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