namespace AvoidAGrabCutEasy
{
    partial class frmRect
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
            this.numH = new System.Windows.Forms.NumericUpDown();
            this.Label4 = new System.Windows.Forms.Label();
            this.numW = new System.Windows.Forms.NumericUpDown();
            this.Label3 = new System.Windows.Forms.Label();
            this.numY = new System.Windows.Forms.NumericUpDown();
            this.Label2 = new System.Windows.Forms.Label();
            this.numX = new System.Windows.Forms.NumericUpDown();
            this.Label1 = new System.Windows.Forms.Label();
            this.Button4 = new System.Windows.Forms.Button();
            this.Button3 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numH)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numW)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numX)).BeginInit();
            this.SuspendLayout();
            // 
            // numH
            // 
            this.numH.Location = new System.Drawing.Point(79, 114);
            this.numH.Maximum = new decimal(new int[] {
            32767,
            0,
            0,
            0});
            this.numH.Name = "numH";
            this.numH.Size = new System.Drawing.Size(75, 20);
            this.numH.TabIndex = 23;
            // 
            // Label4
            // 
            this.Label4.AutoSize = true;
            this.Label4.Location = new System.Drawing.Point(14, 116);
            this.Label4.Name = "Label4";
            this.Label4.Size = new System.Drawing.Size(38, 13);
            this.Label4.TabIndex = 22;
            this.Label4.Text = "Height";
            // 
            // numW
            // 
            this.numW.Location = new System.Drawing.Point(79, 81);
            this.numW.Maximum = new decimal(new int[] {
            32767,
            0,
            0,
            0});
            this.numW.Name = "numW";
            this.numW.Size = new System.Drawing.Size(75, 20);
            this.numW.TabIndex = 21;
            // 
            // Label3
            // 
            this.Label3.AutoSize = true;
            this.Label3.Location = new System.Drawing.Point(14, 83);
            this.Label3.Name = "Label3";
            this.Label3.Size = new System.Drawing.Size(35, 13);
            this.Label3.TabIndex = 20;
            this.Label3.Text = "Width";
            // 
            // numY
            // 
            this.numY.Location = new System.Drawing.Point(79, 48);
            this.numY.Maximum = new decimal(new int[] {
            32767,
            0,
            0,
            0});
            this.numY.Name = "numY";
            this.numY.Size = new System.Drawing.Size(75, 20);
            this.numY.TabIndex = 19;
            // 
            // Label2
            // 
            this.Label2.AutoSize = true;
            this.Label2.Location = new System.Drawing.Point(14, 50);
            this.Label2.Name = "Label2";
            this.Label2.Size = new System.Drawing.Size(14, 13);
            this.Label2.TabIndex = 18;
            this.Label2.Text = "Y";
            // 
            // numX
            // 
            this.numX.Location = new System.Drawing.Point(79, 16);
            this.numX.Maximum = new decimal(new int[] {
            32767,
            0,
            0,
            0});
            this.numX.Name = "numX";
            this.numX.Size = new System.Drawing.Size(75, 20);
            this.numX.TabIndex = 17;
            // 
            // Label1
            // 
            this.Label1.AutoSize = true;
            this.Label1.Location = new System.Drawing.Point(14, 18);
            this.Label1.Name = "Label1";
            this.Label1.Size = new System.Drawing.Size(14, 13);
            this.Label1.TabIndex = 16;
            this.Label1.Text = "X";
            // 
            // Button4
            // 
            this.Button4.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Button4.Location = new System.Drawing.Point(233, 157);
            this.Button4.Name = "Button4";
            this.Button4.Size = new System.Drawing.Size(75, 23);
            this.Button4.TabIndex = 15;
            this.Button4.Text = "Cancel";
            this.Button4.UseVisualStyleBackColor = true;
            // 
            // Button3
            // 
            this.Button3.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Button3.Location = new System.Drawing.Point(139, 157);
            this.Button3.Name = "Button3";
            this.Button3.Size = new System.Drawing.Size(75, 23);
            this.Button3.TabIndex = 14;
            this.Button3.Text = "OK";
            this.Button3.UseVisualStyleBackColor = true;
            // 
            // frmRect
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(323, 196);
            this.Controls.Add(this.numH);
            this.Controls.Add(this.Label4);
            this.Controls.Add(this.numW);
            this.Controls.Add(this.Label3);
            this.Controls.Add(this.numY);
            this.Controls.Add(this.Label2);
            this.Controls.Add(this.numX);
            this.Controls.Add(this.Label1);
            this.Controls.Add(this.Button4);
            this.Controls.Add(this.Button3);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "frmRect";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "frmRect";
            ((System.ComponentModel.ISupportInitialize)(this.numH)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numW)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numX)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        internal System.Windows.Forms.NumericUpDown numH;
        internal System.Windows.Forms.Label Label4;
        internal System.Windows.Forms.NumericUpDown numW;
        internal System.Windows.Forms.Label Label3;
        internal System.Windows.Forms.NumericUpDown numY;
        internal System.Windows.Forms.Label Label2;
        internal System.Windows.Forms.NumericUpDown numX;
        internal System.Windows.Forms.Label Label1;
        internal System.Windows.Forms.Button Button4;
        internal System.Windows.Forms.Button Button3;
    }
}