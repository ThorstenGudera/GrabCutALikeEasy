namespace AvoidAGrabCutEasy
{
    partial class frmKMeansSettings
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
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.label15 = new System.Windows.Forms.Label();
            this.cmbSelMode = new System.Windows.Forms.ComboBox();
            this.numKMeansIters = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.numInitIters = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.numInitW = new System.Windows.Forms.NumericUpDown();
            this.numInitH = new System.Windows.Forms.NumericUpDown();
            this.cbInitRnd = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.numKMeansIters)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numInitIters)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numInitW)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numInitH)).BeginInit();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnCancel.Location = new System.Drawing.Point(265, 204);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 102;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnOK.Location = new System.Drawing.Point(185, 204);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 103;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(22, 17);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(78, 13);
            this.label15.TabIndex = 528;
            this.label15.Text = "SelectionMode";
            // 
            // cmbSelMode
            // 
            this.cmbSelMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSelMode.FormattingEnabled = true;
            this.cmbSelMode.Location = new System.Drawing.Point(106, 14);
            this.cmbSelMode.Name = "cmbSelMode";
            this.cmbSelMode.Size = new System.Drawing.Size(64, 21);
            this.cmbSelMode.TabIndex = 527;
            // 
            // numKMeansIters
            // 
            this.numKMeansIters.Location = new System.Drawing.Point(106, 166);
            this.numKMeansIters.Maximum = new decimal(new int[] {
            2000,
            0,
            0,
            0});
            this.numKMeansIters.Name = "numKMeansIters";
            this.numKMeansIters.Size = new System.Drawing.Size(51, 20);
            this.numKMeansIters.TabIndex = 526;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(24, 168);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(46, 13);
            this.label4.TabIndex = 525;
            this.label4.Text = "km_iters";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(24, 56);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(66, 13);
            this.label1.TabIndex = 525;
            this.label1.Text = "km_Init_iters";
            // 
            // numInitIters
            // 
            this.numInitIters.Location = new System.Drawing.Point(106, 54);
            this.numInitIters.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numInitIters.Name = "numInitIters";
            this.numInitIters.Size = new System.Drawing.Size(51, 20);
            this.numInitIters.TabIndex = 526;
            this.numInitIters.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(24, 93);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(55, 13);
            this.label2.TabIndex = 529;
            this.label2.Text = "Init at W /";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(24, 129);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(55, 13);
            this.label3.TabIndex = 529;
            this.label3.Text = "Init at H / ";
            // 
            // numInitW
            // 
            this.numInitW.DecimalPlaces = 4;
            this.numInitW.Location = new System.Drawing.Point(106, 91);
            this.numInitW.Maximum = new decimal(new int[] {
            32767,
            0,
            0,
            0});
            this.numInitW.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numInitW.Name = "numInitW";
            this.numInitW.Size = new System.Drawing.Size(64, 20);
            this.numInitW.TabIndex = 526;
            this.numInitW.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // numInitH
            // 
            this.numInitH.DecimalPlaces = 4;
            this.numInitH.Location = new System.Drawing.Point(106, 127);
            this.numInitH.Maximum = new decimal(new int[] {
            32767,
            0,
            0,
            0});
            this.numInitH.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numInitH.Name = "numInitH";
            this.numInitH.Size = new System.Drawing.Size(64, 20);
            this.numInitH.TabIndex = 526;
            this.numInitH.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // cbInitRnd
            // 
            this.cbInitRnd.AutoSize = true;
            this.cbInitRnd.Location = new System.Drawing.Point(200, 110);
            this.cbInitRnd.Name = "cbInitRnd";
            this.cbInitRnd.Size = new System.Drawing.Size(90, 17);
            this.cbInitRnd.TabIndex = 530;
            this.cbInitRnd.Text = "random value";
            this.cbInitRnd.UseVisualStyleBackColor = true;
            this.cbInitRnd.CheckedChanged += new System.EventHandler(this.cbInitRnd_CheckedChanged);
            // 
            // frmKMeansSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(352, 239);
            this.Controls.Add(this.cbInitRnd);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.cmbSelMode);
            this.Controls.Add(this.numInitH);
            this.Controls.Add(this.numInitW);
            this.Controls.Add(this.numInitIters);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.numKMeansIters);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.Name = "frmKMeansSettings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "frmKMeansSettings";
            ((System.ComponentModel.ISupportInitialize)(this.numKMeansIters)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numInitIters)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numInitW)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numInitH)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
        internal System.Windows.Forms.Label label15;
        internal System.Windows.Forms.ComboBox cmbSelMode;
        internal System.Windows.Forms.NumericUpDown numKMeansIters;
        internal System.Windows.Forms.Label label4;
        internal System.Windows.Forms.Label label1;
        internal System.Windows.Forms.NumericUpDown numInitIters;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        internal System.Windows.Forms.NumericUpDown numInitW;
        internal System.Windows.Forms.NumericUpDown numInitH;
        internal System.Windows.Forms.CheckBox cbInitRnd;
    }
}