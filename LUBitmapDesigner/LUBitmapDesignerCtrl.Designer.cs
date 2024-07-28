namespace LUBitmapDesigner
{
    partial class LUBitmapDesignerCtrl
    {
        /// <summary> 
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Komponenten-Designer generierter Code

        /// <summary> 
        /// Erforderliche Methode für die Designerunterstützung. 
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.helplineRulerCtrl1 = new HelplineRulerControl.HelplineRulerCtrl();
            this.SuspendLayout();
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
            this.helplineRulerCtrl1.Size = new System.Drawing.Size(750, 750);
            this.helplineRulerCtrl1.TabIndex = 0;
            this.helplineRulerCtrl1.Zoom = 1F;
            this.helplineRulerCtrl1.ZoomSetManually = false;
            this.helplineRulerCtrl1.Load += new System.EventHandler(this.helplineRulerCtrl1_Load);
            // 
            // LUBitmapDesignerCtrl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.helplineRulerCtrl1);
            this.Name = "LUBitmapDesignerCtrl";
            this.Size = new System.Drawing.Size(750, 750);
            this.ResumeLayout(false);

        }

        #endregion

        public HelplineRulerControl.HelplineRulerCtrl helplineRulerCtrl1;
    }
}
