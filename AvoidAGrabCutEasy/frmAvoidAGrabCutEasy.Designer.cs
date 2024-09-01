namespace AvoidAGrabCutEasy
{
    partial class frmAvoidAGrabCutEasy
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
            this.components = new System.ComponentModel.Container();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnInitSettings = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label13 = new System.Windows.Forms.Label();
            this.cbAutoThreshold = new System.Windows.Forms.CheckBox();
            this.label12 = new System.Windows.Forms.Label();
            this.btnCompose = new System.Windows.Forms.Button();
            this.btnCache = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.btnOutline = new System.Windows.Forms.Button();
            this.cbSetPFGToFG = new System.Windows.Forms.CheckBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.cbLSBmp = new System.Windows.Forms.CheckBox();
            this.btnLoadScribbles = new System.Windows.Forms.Button();
            this.btnSaveScribbles = new System.Windows.Forms.Button();
            this.cbSkipLearn = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnMinCut = new System.Windows.Forms.Button();
            this.btnResQATH = new System.Windows.Forms.Button();
            this.btnResMaxIter = new System.Windows.Forms.Button();
            this.cbEightAdj = new System.Windows.Forms.CheckBox();
            this.btnRecut = new System.Windows.Forms.Button();
            this.btnRemStroke = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.cmbCurrentColor = new System.Windows.Forms.ComboBox();
            this.cbDraw = new System.Windows.Forms.CheckBox();
            this.numWH = new System.Windows.Forms.NumericUpDown();
            this.label40 = new System.Windows.Forms.Label();
            this.label39 = new System.Windows.Forms.Label();
            this.numQATH = new System.Windows.Forms.NumericUpDown();
            this.numAlgMaxIter = new System.Windows.Forms.NumericUpDown();
            this.Label20 = new System.Windows.Forms.Label();
            this.cmbZoom = new System.Windows.Forms.ComboBox();
            this.cbBGColor = new System.Windows.Forms.CheckBox();
            this.button10 = new System.Windows.Forms.Button();
            this.button8 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnGo = new System.Windows.Forms.Button();
            this.btnReset2 = new System.Windows.Forms.Button();
            this.cbCastTLInt = new System.Windows.Forms.CheckBox();
            this.cbMultTLCap = new System.Windows.Forms.CheckBox();
            this.numMultTLCap = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.numMaxSize = new System.Windows.Forms.NumericUpDown();
            this.numProbMult1 = new System.Windows.Forms.NumericUpDown();
            this.cbUseTh = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.numDblMult = new System.Windows.Forms.NumericUpDown();
            this.cbQuickEst = new System.Windows.Forms.CheckBox();
            this.btnRect = new System.Windows.Forms.Button();
            this.numShiftY = new System.Windows.Forms.NumericUpDown();
            this.label19 = new System.Windows.Forms.Label();
            this.numShiftX = new System.Windows.Forms.NumericUpDown();
            this.numGmmComp = new System.Windows.Forms.NumericUpDown();
            this.numMaxComponents = new System.Windows.Forms.NumericUpDown();
            this.numWHScribbles = new System.Windows.Forms.NumericUpDown();
            this.cbScribbleMode = new System.Windows.Forms.CheckBox();
            this.rbFG = new System.Windows.Forms.RadioButton();
            this.rbBG = new System.Windows.Forms.RadioButton();
            this.btnClearScribbles = new System.Windows.Forms.Button();
            this.btnRemLastScribbles = new System.Windows.Forms.Button();
            this.cbRectMode = new System.Windows.Forms.CheckBox();
            this.numGamma = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.helplineRulerCtrl1 = new HelplineRulerControl.HelplineRulerCtrl();
            this.helplineRulerCtrl2 = new HelplineRulerControl.HelplineRulerCtrl();
            this.panel3 = new System.Windows.Forms.Panel();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.ToolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripProgressBar1 = new System.Windows.Forms.ToolStripProgressBar();
            this.toolStripStatusLabel4 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel3 = new System.Windows.Forms.ToolStripStatusLabel();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.Timer3 = new System.Windows.Forms.Timer(this.components);
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.backgroundWorker2 = new System.ComponentModel.BackgroundWorker();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numWH)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numQATH)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAlgMaxIter)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMultTLCap)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numProbMult1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDblMult)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numShiftY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numShiftX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numGmmComp)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxComponents)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWHScribbles)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numGamma)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnInitSettings);
            this.panel1.Controls.Add(this.panel2);
            this.panel1.Controls.Add(this.label12);
            this.panel1.Controls.Add(this.btnCompose);
            this.panel1.Controls.Add(this.btnCache);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.btnOutline);
            this.panel1.Controls.Add(this.cbSetPFGToFG);
            this.panel1.Controls.Add(this.label11);
            this.panel1.Controls.Add(this.label10);
            this.panel1.Controls.Add(this.label9);
            this.panel1.Controls.Add(this.label8);
            this.panel1.Controls.Add(this.label7);
            this.panel1.Controls.Add(this.cbLSBmp);
            this.panel1.Controls.Add(this.btnLoadScribbles);
            this.panel1.Controls.Add(this.btnSaveScribbles);
            this.panel1.Controls.Add(this.cbSkipLearn);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.btnMinCut);
            this.panel1.Controls.Add(this.btnResQATH);
            this.panel1.Controls.Add(this.btnResMaxIter);
            this.panel1.Controls.Add(this.cbEightAdj);
            this.panel1.Controls.Add(this.btnRecut);
            this.panel1.Controls.Add(this.btnRemStroke);
            this.panel1.Controls.Add(this.label6);
            this.panel1.Controls.Add(this.cmbCurrentColor);
            this.panel1.Controls.Add(this.cbDraw);
            this.panel1.Controls.Add(this.numWH);
            this.panel1.Controls.Add(this.label40);
            this.panel1.Controls.Add(this.label39);
            this.panel1.Controls.Add(this.numQATH);
            this.panel1.Controls.Add(this.numAlgMaxIter);
            this.panel1.Controls.Add(this.Label20);
            this.panel1.Controls.Add(this.cmbZoom);
            this.panel1.Controls.Add(this.cbBGColor);
            this.panel1.Controls.Add(this.button10);
            this.panel1.Controls.Add(this.button8);
            this.panel1.Controls.Add(this.button2);
            this.panel1.Controls.Add(this.btnCancel);
            this.panel1.Controls.Add(this.btnOK);
            this.panel1.Controls.Add(this.btnGo);
            this.panel1.Controls.Add(this.btnReset2);
            this.panel1.Controls.Add(this.cbCastTLInt);
            this.panel1.Controls.Add(this.cbMultTLCap);
            this.panel1.Controls.Add(this.numMultTLCap);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.numMaxSize);
            this.panel1.Controls.Add(this.numProbMult1);
            this.panel1.Controls.Add(this.cbUseTh);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.numDblMult);
            this.panel1.Controls.Add(this.cbQuickEst);
            this.panel1.Controls.Add(this.btnRect);
            this.panel1.Controls.Add(this.numShiftY);
            this.panel1.Controls.Add(this.label19);
            this.panel1.Controls.Add(this.numShiftX);
            this.panel1.Controls.Add(this.numGmmComp);
            this.panel1.Controls.Add(this.numMaxComponents);
            this.panel1.Controls.Add(this.numWHScribbles);
            this.panel1.Controls.Add(this.cbScribbleMode);
            this.panel1.Controls.Add(this.rbFG);
            this.panel1.Controls.Add(this.rbBG);
            this.panel1.Controls.Add(this.btnClearScribbles);
            this.panel1.Controls.Add(this.btnRemLastScribbles);
            this.panel1.Controls.Add(this.cbRectMode);
            this.panel1.Controls.Add(this.numGamma);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1268, 170);
            this.panel1.TabIndex = 0;
            this.panel1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.panel1_MouseDoubleClick);
            this.panel1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panel1_MouseDown);
            // 
            // btnInitSettings
            // 
            this.btnInitSettings.Location = new System.Drawing.Point(511, 6);
            this.btnInitSettings.Name = "btnInitSettings";
            this.btnInitSettings.Size = new System.Drawing.Size(75, 23);
            this.btnInitSettings.TabIndex = 681;
            this.btnInitSettings.Text = "KM-settings";
            this.btnInitSettings.UseVisualStyleBackColor = true;
            this.btnInitSettings.Click += new System.EventHandler(this.btnInitSettings_Click);
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.label13);
            this.panel2.Controls.Add(this.cbAutoThreshold);
            this.panel2.Location = new System.Drawing.Point(628, 1);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(200, 26);
            this.panel2.TabIndex = 680;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(9, 6);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(87, 13);
            this.label13.TabIndex = 677;
            this.label13.Text = "currently testing: ";
            // 
            // cbAutoThreshold
            // 
            this.cbAutoThreshold.AutoSize = true;
            this.cbAutoThreshold.Checked = true;
            this.cbAutoThreshold.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbAutoThreshold.Location = new System.Drawing.Point(102, 5);
            this.cbAutoThreshold.Name = "cbAutoThreshold";
            this.cbAutoThreshold.Size = new System.Drawing.Size(93, 17);
            this.cbAutoThreshold.TabIndex = 676;
            this.cbAutoThreshold.Text = "auto threshold";
            this.cbAutoThreshold.UseVisualStyleBackColor = true;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(945, 80);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(50, 13);
            this.label12.TabIndex = 679;
            this.label12.Text = "compose";
            // 
            // btnCompose
            // 
            this.btnCompose.Location = new System.Drawing.Point(1001, 75);
            this.btnCompose.Name = "btnCompose";
            this.btnCompose.Size = new System.Drawing.Size(75, 23);
            this.btnCompose.TabIndex = 678;
            this.btnCompose.Text = "Go";
            this.btnCompose.UseVisualStyleBackColor = true;
            this.btnCompose.Click += new System.EventHandler(this.btnCompose_Click);
            // 
            // btnCache
            // 
            this.btnCache.Enabled = false;
            this.btnCache.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnCache.Location = new System.Drawing.Point(986, 29);
            this.btnCache.Name = "btnCache";
            this.btnCache.Size = new System.Drawing.Size(75, 23);
            this.btnCache.TabIndex = 677;
            this.btnCache.Text = "CachedPics";
            this.btnCache.UseVisualStyleBackColor = true;
            this.btnCache.Click += new System.EventHandler(this.btnCache_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(771, 79);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(80, 13);
            this.label3.TabIndex = 676;
            this.label3.Text = "process Outline";
            // 
            // btnOutline
            // 
            this.btnOutline.Location = new System.Drawing.Point(857, 74);
            this.btnOutline.Name = "btnOutline";
            this.btnOutline.Size = new System.Drawing.Size(75, 23);
            this.btnOutline.TabIndex = 675;
            this.btnOutline.Text = "Go";
            this.btnOutline.UseVisualStyleBackColor = true;
            this.btnOutline.Click += new System.EventHandler(this.btnOutline_Click);
            // 
            // cbSetPFGToFG
            // 
            this.cbSetPFGToFG.AutoSize = true;
            this.cbSetPFGToFG.Enabled = false;
            this.cbSetPFGToFG.Location = new System.Drawing.Point(1073, 110);
            this.cbSetPFGToFG.Name = "cbSetPFGToFG";
            this.cbSetPFGToFG.Size = new System.Drawing.Size(74, 17);
            this.cbSetPFGToFG.TabIndex = 674;
            this.cbSetPFGToFG.Text = "PFGToFG";
            this.toolTip1.SetToolTip(this.cbSetPFGToFG, "set all \"probably foreground\" values to \"foreground\" in mask");
            this.cbSetPFGToFG.UseVisualStyleBackColor = true;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(695, 56);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(17, 13);
            this.label11.TabIndex = 673;
            this.label11.Text = "Y:";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(568, 56);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(70, 13);
            this.label10.TabIndex = 673;
            this.label10.Text = "shiftShape X:";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(12, 56);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(50, 13);
            this.label9.TabIndex = 672;
            this.label9.Text = "Max Size";
            this.toolTip1.SetToolTip(this.label9, "Maximum size of the long picture-side.\r\nBigger pictures will be resampled down - " +
        "then processed -\r\nand re-resampled up again.\r\nRealMinCut MaxSize is fix at 300, " +
        "due to processing time.");
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(12, 35);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(71, 13);
            this.label8.TabIndex = 671;
            this.label8.Text = "Gmm Clusters";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(376, 107);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(97, 13);
            this.label7.TabIndex = 670;
            this.label7.Text = "max # components";
            this.toolTip1.SetToolTip(this.label7, "maximum output number of connected components,\r\nouter- and inner-paths (component" +
        "s which are drawn transparent).");
            // 
            // cbLSBmp
            // 
            this.cbLSBmp.AutoSize = true;
            this.cbLSBmp.Location = new System.Drawing.Point(826, 55);
            this.cbLSBmp.Name = "cbLSBmp";
            this.cbLSBmp.Size = new System.Drawing.Size(80, 17);
            this.cbLSBmp.TabIndex = 579;
            this.cbLSBmp.Text = "with Bitmap";
            this.cbLSBmp.UseVisualStyleBackColor = true;
            // 
            // btnLoadScribbles
            // 
            this.btnLoadScribbles.Enabled = false;
            this.btnLoadScribbles.Location = new System.Drawing.Point(714, 29);
            this.btnLoadScribbles.Name = "btnLoadScribbles";
            this.btnLoadScribbles.Size = new System.Drawing.Size(85, 23);
            this.btnLoadScribbles.TabIndex = 666;
            this.btnLoadScribbles.Text = "loadScribbles";
            this.btnLoadScribbles.UseVisualStyleBackColor = true;
            this.btnLoadScribbles.Click += new System.EventHandler(this.btnLoadScribbles_Click);
            // 
            // btnSaveScribbles
            // 
            this.btnSaveScribbles.Enabled = false;
            this.btnSaveScribbles.Location = new System.Drawing.Point(811, 29);
            this.btnSaveScribbles.Name = "btnSaveScribbles";
            this.btnSaveScribbles.Size = new System.Drawing.Size(85, 23);
            this.btnSaveScribbles.TabIndex = 667;
            this.btnSaveScribbles.Text = "saveScribbles";
            this.btnSaveScribbles.UseVisualStyleBackColor = true;
            this.btnSaveScribbles.Click += new System.EventHandler(this.btnSaveScribbles_Click);
            // 
            // cbSkipLearn
            // 
            this.cbSkipLearn.AutoSize = true;
            this.cbSkipLearn.Enabled = false;
            this.cbSkipLearn.Location = new System.Drawing.Point(1001, 110);
            this.cbSkipLearn.Name = "cbSkipLearn";
            this.cbSkipLearn.Size = new System.Drawing.Size(72, 17);
            this.cbSkipLearn.TabIndex = 665;
            this.cbSkipLearn.Text = "skipLearn";
            this.cbSkipLearn.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 106);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(97, 13);
            this.label1.TabIndex = 664;
            this.label1.Text = "Run a Real MinCut";
            // 
            // btnMinCut
            // 
            this.btnMinCut.Location = new System.Drawing.Point(15, 131);
            this.btnMinCut.Name = "btnMinCut";
            this.btnMinCut.Size = new System.Drawing.Size(75, 23);
            this.btnMinCut.TabIndex = 663;
            this.btnMinCut.Text = "Go";
            this.btnMinCut.UseVisualStyleBackColor = true;
            this.btnMinCut.Click += new System.EventHandler(this.btnMinCut_Click);
            // 
            // btnResQATH
            // 
            this.btnResQATH.Location = new System.Drawing.Point(712, 104);
            this.btnResQATH.Name = "btnResQATH";
            this.btnResQATH.Size = new System.Drawing.Size(42, 23);
            this.btnResQATH.TabIndex = 662;
            this.btnResQATH.Text = "res";
            this.btnResQATH.UseVisualStyleBackColor = true;
            this.btnResQATH.Click += new System.EventHandler(this.btnResQATH_Click);
            // 
            // btnResMaxIter
            // 
            this.btnResMaxIter.Location = new System.Drawing.Point(712, 76);
            this.btnResMaxIter.Name = "btnResMaxIter";
            this.btnResMaxIter.Size = new System.Drawing.Size(42, 23);
            this.btnResMaxIter.TabIndex = 662;
            this.btnResMaxIter.Text = "res";
            this.btnResMaxIter.UseVisualStyleBackColor = true;
            this.btnResMaxIter.Click += new System.EventHandler(this.btnResMaxIter_Click);
            // 
            // cbEightAdj
            // 
            this.cbEightAdj.AutoSize = true;
            this.cbEightAdj.Location = new System.Drawing.Point(388, 10);
            this.cbEightAdj.Name = "cbEightAdj";
            this.cbEightAdj.Size = new System.Drawing.Size(106, 17);
            this.cbEightAdj.TabIndex = 661;
            this.cbEightAdj.Text = "Eight_Adjacency";
            this.cbEightAdj.UseVisualStyleBackColor = true;
            // 
            // btnRecut
            // 
            this.btnRecut.Location = new System.Drawing.Point(997, 132);
            this.btnRecut.Name = "btnRecut";
            this.btnRecut.Size = new System.Drawing.Size(38, 23);
            this.btnRecut.TabIndex = 660;
            this.btnRecut.Text = "Go";
            this.toolTip1.SetToolTip(this.btnRecut, "Draw all components");
            this.btnRecut.UseVisualStyleBackColor = true;
            this.btnRecut.Visible = false;
            this.btnRecut.Click += new System.EventHandler(this.btnRecut_Click);
            // 
            // btnRemStroke
            // 
            this.btnRemStroke.Location = new System.Drawing.Point(903, 132);
            this.btnRemStroke.Name = "btnRemStroke";
            this.btnRemStroke.Size = new System.Drawing.Size(75, 23);
            this.btnRemStroke.TabIndex = 659;
            this.btnRemStroke.Text = "rem last";
            this.btnRemStroke.UseVisualStyleBackColor = true;
            this.btnRemStroke.Click += new System.EventHandler(this.btnRemStroke_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(808, 137);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(32, 13);
            this.label6.TabIndex = 658;
            this.label6.Text = "width";
            // 
            // cmbCurrentColor
            // 
            this.cmbCurrentColor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCurrentColor.FormattingEnabled = true;
            this.cmbCurrentColor.Items.AddRange(new object[] {
            "Background",
            "Foreground",
            "Probably Background",
            "Probably Foreground"});
            this.cmbCurrentColor.Location = new System.Drawing.Point(894, 108);
            this.cmbCurrentColor.Name = "cmbCurrentColor";
            this.cmbCurrentColor.Size = new System.Drawing.Size(101, 21);
            this.cmbCurrentColor.TabIndex = 657;
            this.cmbCurrentColor.SelectedIndexChanged += new System.EventHandler(this.cmbCurrentColor_SelectedIndexChanged);
            // 
            // cbDraw
            // 
            this.cbDraw.AutoSize = true;
            this.cbDraw.Location = new System.Drawing.Point(800, 110);
            this.cbDraw.Name = "cbDraw";
            this.cbDraw.Size = new System.Drawing.Size(92, 17);
            this.cbDraw.TabIndex = 656;
            this.cbDraw.Text = "draw on result";
            this.cbDraw.UseVisualStyleBackColor = true;
            this.cbDraw.CheckedChanged += new System.EventHandler(this.cbDraw_CheckedChanged);
            // 
            // numWH
            // 
            this.numWH.Location = new System.Drawing.Point(846, 135);
            this.numWH.Maximum = new decimal(new int[] {
            127,
            0,
            0,
            0});
            this.numWH.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numWH.Name = "numWH";
            this.numWH.Size = new System.Drawing.Size(45, 20);
            this.numWH.TabIndex = 655;
            this.numWH.Value = new decimal(new int[] {
            25,
            0,
            0,
            0});
            // 
            // label40
            // 
            this.label40.AutoSize = true;
            this.label40.Location = new System.Drawing.Point(567, 109);
            this.label40.Name = "label40";
            this.label40.Size = new System.Drawing.Size(37, 13);
            this.label40.TabIndex = 653;
            this.label40.Text = "QATH";
            this.toolTip1.SetToolTip(this.label40, "maximum number of re-inserting a node to the queue\r\nwhen using a mincut algorithm" +
        "\r\n(CGwQE checked, or QuickEstimation unchecked)\r\n");
            // 
            // label39
            // 
            this.label39.AutoSize = true;
            this.label39.Location = new System.Drawing.Point(568, 81);
            this.label39.Name = "label39";
            this.label39.Size = new System.Drawing.Size(41, 13);
            this.label39.TabIndex = 654;
            this.label39.Text = "maxIter";
            this.toolTip1.SetToolTip(this.label39, "maximum number of iterations\r\nwhen using a mincut algorithm\r\n(CGwQE checked, or Q" +
        "uickEstimation unchecked)");
            // 
            // numQATH
            // 
            this.numQATH.Location = new System.Drawing.Point(614, 107);
            this.numQATH.Maximum = new decimal(new int[] {
            1410065407,
            2,
            0,
            0});
            this.numQATH.Name = "numQATH";
            this.numQATH.Size = new System.Drawing.Size(89, 20);
            this.numQATH.TabIndex = 651;
            this.numQATH.Value = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.numQATH.ValueChanged += new System.EventHandler(this.numQATH_ValueChanged);
            // 
            // numAlgMaxIter
            // 
            this.numAlgMaxIter.Location = new System.Drawing.Point(615, 79);
            this.numAlgMaxIter.Maximum = new decimal(new int[] {
            1410065407,
            2,
            0,
            0});
            this.numAlgMaxIter.Name = "numAlgMaxIter";
            this.numAlgMaxIter.Size = new System.Drawing.Size(89, 20);
            this.numAlgMaxIter.TabIndex = 652;
            this.numAlgMaxIter.Value = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.numAlgMaxIter.ValueChanged += new System.EventHandler(this.numAlgMaxIter_ValueChanged);
            // 
            // Label20
            // 
            this.Label20.AutoSize = true;
            this.Label20.Location = new System.Drawing.Point(1098, 85);
            this.Label20.Name = "Label20";
            this.Label20.Size = new System.Drawing.Size(53, 13);
            this.Label20.TabIndex = 650;
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
            this.cmbZoom.Location = new System.Drawing.Point(1157, 82);
            this.cmbZoom.Name = "cmbZoom";
            this.cmbZoom.Size = new System.Drawing.Size(75, 21);
            this.cmbZoom.TabIndex = 649;
            this.cmbZoom.SelectedIndexChanged += new System.EventHandler(this.cmbZoom_SelectedIndexChanged);
            // 
            // cbBGColor
            // 
            this.cbBGColor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbBGColor.AutoSize = true;
            this.cbBGColor.Checked = true;
            this.cbBGColor.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbBGColor.Location = new System.Drawing.Point(1100, 13);
            this.cbBGColor.Name = "cbBGColor";
            this.cbBGColor.Size = new System.Drawing.Size(65, 17);
            this.cbBGColor.TabIndex = 648;
            this.cbBGColor.Text = "BG dark";
            this.cbBGColor.UseVisualStyleBackColor = true;
            this.cbBGColor.CheckedChanged += new System.EventHandler(this.cbBGColor_CheckedChanged);
            // 
            // button10
            // 
            this.button10.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button10.ForeColor = System.Drawing.SystemColors.ControlText;
            this.button10.Location = new System.Drawing.Point(1181, 44);
            this.button10.Name = "button10";
            this.button10.Size = new System.Drawing.Size(75, 23);
            this.button10.TabIndex = 647;
            this.button10.Text = "HowTo";
            this.button10.UseVisualStyleBackColor = true;
            this.button10.Click += new System.EventHandler(this.button10_Click);
            // 
            // button8
            // 
            this.button8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button8.ForeColor = System.Drawing.SystemColors.ControlText;
            this.button8.Location = new System.Drawing.Point(1100, 44);
            this.button8.Name = "button8";
            this.button8.Size = new System.Drawing.Size(75, 23);
            this.button8.TabIndex = 646;
            this.button8.Text = "Reload";
            this.button8.UseVisualStyleBackColor = true;
            this.button8.Click += new System.EventHandler(this.button8_Click);
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.button2.ForeColor = System.Drawing.SystemColors.ControlText;
            this.button2.Location = new System.Drawing.Point(1181, 9);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 645;
            this.button2.Text = "Save";
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnCancel.Location = new System.Drawing.Point(1181, 135);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 643;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Enabled = false;
            this.btnOK.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnOK.Location = new System.Drawing.Point(1101, 135);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 644;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.Button28_Click);
            // 
            // btnGo
            // 
            this.btnGo.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnGo.Location = new System.Drawing.Point(565, 135);
            this.btnGo.Name = "btnGo";
            this.btnGo.Size = new System.Drawing.Size(75, 23);
            this.btnGo.TabIndex = 642;
            this.btnGo.Text = "Go";
            this.btnGo.UseVisualStyleBackColor = true;
            this.btnGo.Click += new System.EventHandler(this.btnGo_Click);
            // 
            // btnReset2
            // 
            this.btnReset2.Enabled = false;
            this.btnReset2.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnReset2.Location = new System.Drawing.Point(646, 135);
            this.btnReset2.Name = "btnReset2";
            this.btnReset2.Size = new System.Drawing.Size(75, 23);
            this.btnReset2.TabIndex = 641;
            this.btnReset2.Text = "Reset";
            this.btnReset2.UseVisualStyleBackColor = true;
            this.btnReset2.Click += new System.EventHandler(this.button4_Click);
            // 
            // cbCastTLInt
            // 
            this.cbCastTLInt.AutoSize = true;
            this.cbCastTLInt.Checked = true;
            this.cbCastTLInt.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbCastTLInt.Location = new System.Drawing.Point(346, 138);
            this.cbCastTLInt.Name = "cbCastTLInt";
            this.cbCastTLInt.Size = new System.Drawing.Size(80, 17);
            this.cbCastTLInt.TabIndex = 640;
            this.cbCastTLInt.Text = "and CastInt";
            this.cbCastTLInt.UseVisualStyleBackColor = true;
            // 
            // cbMultTLCap
            // 
            this.cbMultTLCap.AutoSize = true;
            this.cbMultTLCap.Checked = true;
            this.cbMultTLCap.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbMultTLCap.Location = new System.Drawing.Point(229, 138);
            this.cbMultTLCap.Name = "cbMultTLCap";
            this.cbMultTLCap.Size = new System.Drawing.Size(113, 17);
            this.cbMultTLCap.TabIndex = 639;
            this.cbMultTLCap.Text = "multTLinkCapacity";
            this.cbMultTLCap.UseVisualStyleBackColor = true;
            this.cbMultTLCap.CheckedChanged += new System.EventHandler(this.cbMultTLCap_CheckedChanged);
            // 
            // numMultTLCap
            // 
            this.numMultTLCap.DecimalPlaces = 2;
            this.numMultTLCap.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numMultTLCap.Location = new System.Drawing.Point(431, 137);
            this.numMultTLCap.Maximum = new decimal(new int[] {
            1410065407,
            2,
            0,
            0});
            this.numMultTLCap.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.numMultTLCap.Name = "numMultTLCap";
            this.numMultTLCap.Size = new System.Drawing.Size(88, 20);
            this.numMultTLCap.TabIndex = 638;
            this.numMultTLCap.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(226, 107);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(54, 13);
            this.label4.TabIndex = 637;
            this.label4.Text = "probMult1";
            // 
            // numMaxSize
            // 
            this.numMaxSize.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numMaxSize.Location = new System.Drawing.Point(95, 54);
            this.numMaxSize.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.numMaxSize.Minimum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.numMaxSize.Name = "numMaxSize";
            this.numMaxSize.Size = new System.Drawing.Size(61, 20);
            this.numMaxSize.TabIndex = 636;
            this.numMaxSize.Value = new decimal(new int[] {
            1200,
            0,
            0,
            0});
            // 
            // numProbMult1
            // 
            this.numProbMult1.DecimalPlaces = 4;
            this.numProbMult1.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numProbMult1.Location = new System.Drawing.Point(283, 105);
            this.numProbMult1.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numProbMult1.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            262144});
            this.numProbMult1.Name = "numProbMult1";
            this.numProbMult1.Size = new System.Drawing.Size(61, 20);
            this.numProbMult1.TabIndex = 636;
            this.numProbMult1.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // cbUseTh
            // 
            this.cbUseTh.AutoSize = true;
            this.cbUseTh.Checked = true;
            this.cbUseTh.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbUseTh.Location = new System.Drawing.Point(450, 78);
            this.cbUseTh.Name = "cbUseTh";
            this.cbUseTh.Size = new System.Drawing.Size(93, 17);
            this.cbUseTh.TabIndex = 633;
            this.cbUseTh.Text = "use Threshold";
            this.cbUseTh.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(247, 80);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(80, 13);
            this.label5.TabIndex = 632;
            this.label5.Text = "d to 0 threshold";
            // 
            // numDblMult
            // 
            this.numDblMult.DecimalPlaces = 6;
            this.numDblMult.Location = new System.Drawing.Point(335, 77);
            this.numDblMult.Maximum = new decimal(new int[] {
            1000000000,
            0,
            0,
            0});
            this.numDblMult.Name = "numDblMult";
            this.numDblMult.Size = new System.Drawing.Size(104, 20);
            this.numDblMult.TabIndex = 631;
            this.numDblMult.Value = new decimal(new int[] {
            105,
            0,
            0,
            65536});
            // 
            // cbQuickEst
            // 
            this.cbQuickEst.AutoSize = true;
            this.cbQuickEst.Checked = true;
            this.cbQuickEst.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbQuickEst.Location = new System.Drawing.Point(229, 55);
            this.cbQuickEst.Name = "cbQuickEst";
            this.cbQuickEst.Size = new System.Drawing.Size(102, 17);
            this.cbQuickEst.TabIndex = 630;
            this.cbQuickEst.Text = "QuickExtimation";
            this.cbQuickEst.UseVisualStyleBackColor = true;
            this.cbQuickEst.CheckedChanged += new System.EventHandler(this.cbQuickEst_CheckedChanged);
            // 
            // btnRect
            // 
            this.btnRect.Location = new System.Drawing.Point(309, 6);
            this.btnRect.Name = "btnRect";
            this.btnRect.Size = new System.Drawing.Size(48, 23);
            this.btnRect.TabIndex = 629;
            this.btnRect.Text = "rect";
            this.btnRect.UseVisualStyleBackColor = true;
            this.btnRect.Click += new System.EventHandler(this.btnRect_Click);
            // 
            // numShiftY
            // 
            this.numShiftY.Location = new System.Drawing.Point(718, 54);
            this.numShiftY.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.numShiftY.Minimum = new decimal(new int[] {
            9999,
            0,
            0,
            -2147483648});
            this.numShiftY.Name = "numShiftY";
            this.numShiftY.Size = new System.Drawing.Size(45, 20);
            this.numShiftY.TabIndex = 625;
            this.numShiftY.ValueChanged += new System.EventHandler(this.numShiftY_ValueChanged);
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(350, 32);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(32, 13);
            this.label19.TabIndex = 626;
            this.label19.Text = "width";
            // 
            // numShiftX
            // 
            this.numShiftX.Location = new System.Drawing.Point(644, 54);
            this.numShiftX.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.numShiftX.Minimum = new decimal(new int[] {
            9999,
            0,
            0,
            -2147483648});
            this.numShiftX.Name = "numShiftX";
            this.numShiftX.Size = new System.Drawing.Size(45, 20);
            this.numShiftX.TabIndex = 625;
            this.numShiftX.ValueChanged += new System.EventHandler(this.numShiftX_ValueChanged);
            // 
            // numGmmComp
            // 
            this.numGmmComp.Location = new System.Drawing.Point(111, 32);
            this.numGmmComp.Maximum = new decimal(new int[] {
            25,
            0,
            0,
            0});
            this.numGmmComp.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numGmmComp.Name = "numGmmComp";
            this.numGmmComp.Size = new System.Drawing.Size(45, 20);
            this.numGmmComp.TabIndex = 625;
            this.toolTip1.SetToolTip(this.numGmmComp, "The amount of clusters for each Gmm.");
            this.numGmmComp.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // numMaxComponents
            // 
            this.numMaxComponents.Location = new System.Drawing.Point(479, 105);
            this.numMaxComponents.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.numMaxComponents.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numMaxComponents.Name = "numMaxComponents";
            this.numMaxComponents.Size = new System.Drawing.Size(45, 20);
            this.numMaxComponents.TabIndex = 625;
            this.numMaxComponents.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // numWHScribbles
            // 
            this.numWHScribbles.Location = new System.Drawing.Point(388, 30);
            this.numWHScribbles.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.numWHScribbles.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numWHScribbles.Name = "numWHScribbles";
            this.numWHScribbles.Size = new System.Drawing.Size(45, 20);
            this.numWHScribbles.TabIndex = 625;
            this.numWHScribbles.Value = new decimal(new int[] {
            25,
            0,
            0,
            0});
            // 
            // cbScribbleMode
            // 
            this.cbScribbleMode.AutoSize = true;
            this.cbScribbleMode.Location = new System.Drawing.Point(255, 32);
            this.cbScribbleMode.Name = "cbScribbleMode";
            this.cbScribbleMode.Size = new System.Drawing.Size(89, 17);
            this.cbScribbleMode.TabIndex = 624;
            this.cbScribbleMode.Text = "scribbleMode";
            this.cbScribbleMode.UseVisualStyleBackColor = true;
            this.cbScribbleMode.CheckedChanged += new System.EventHandler(this.cbScribbleMode_CheckedChanged);
            // 
            // rbFG
            // 
            this.rbFG.AutoSize = true;
            this.rbFG.Location = new System.Drawing.Point(494, 32);
            this.rbFG.Name = "rbFG";
            this.rbFG.Size = new System.Drawing.Size(39, 17);
            this.rbFG.TabIndex = 621;
            this.rbFG.Text = "FG";
            this.rbFG.UseVisualStyleBackColor = true;
            // 
            // rbBG
            // 
            this.rbBG.AutoSize = true;
            this.rbBG.Checked = true;
            this.rbBG.Location = new System.Drawing.Point(448, 32);
            this.rbBG.Name = "rbBG";
            this.rbBG.Size = new System.Drawing.Size(40, 17);
            this.rbBG.TabIndex = 622;
            this.rbBG.TabStop = true;
            this.rbBG.Text = "BG";
            this.rbBG.UseVisualStyleBackColor = true;
            // 
            // btnClearScribbles
            // 
            this.btnClearScribbles.Location = new System.Drawing.Point(614, 30);
            this.btnClearScribbles.Name = "btnClearScribbles";
            this.btnClearScribbles.Size = new System.Drawing.Size(54, 23);
            this.btnClearScribbles.TabIndex = 627;
            this.btnClearScribbles.Text = "clear";
            this.btnClearScribbles.UseVisualStyleBackColor = true;
            this.btnClearScribbles.Click += new System.EventHandler(this.btnClearScribbles_Click);
            // 
            // btnRemLastScribbles
            // 
            this.btnRemLastScribbles.Location = new System.Drawing.Point(554, 30);
            this.btnRemLastScribbles.Name = "btnRemLastScribbles";
            this.btnRemLastScribbles.Size = new System.Drawing.Size(54, 23);
            this.btnRemLastScribbles.TabIndex = 628;
            this.btnRemLastScribbles.Text = "rem last";
            this.btnRemLastScribbles.UseVisualStyleBackColor = true;
            this.btnRemLastScribbles.Click += new System.EventHandler(this.btnRemLastScribbles_Click);
            // 
            // cbRectMode
            // 
            this.cbRectMode.AutoSize = true;
            this.cbRectMode.Checked = true;
            this.cbRectMode.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbRectMode.Location = new System.Drawing.Point(229, 10);
            this.cbRectMode.Name = "cbRectMode";
            this.cbRectMode.Size = new System.Drawing.Size(74, 17);
            this.cbRectMode.TabIndex = 623;
            this.cbRectMode.Text = "rect Mode";
            this.cbRectMode.UseVisualStyleBackColor = true;
            this.cbRectMode.CheckedChanged += new System.EventHandler(this.cbRectMode_CheckedChanged);
            // 
            // numGamma
            // 
            this.numGamma.DecimalPlaces = 4;
            this.numGamma.Location = new System.Drawing.Point(81, 9);
            this.numGamma.Maximum = new decimal(new int[] {
            705032704,
            1,
            0,
            0});
            this.numGamma.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            262144});
            this.numGamma.Name = "numGamma";
            this.numGamma.Size = new System.Drawing.Size(75, 20);
            this.numGamma.TabIndex = 513;
            this.numGamma.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 11);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 13);
            this.label2.TabIndex = 512;
            this.label2.Text = "gamma";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 170);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.helplineRulerCtrl1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.helplineRulerCtrl2);
            this.splitContainer1.Size = new System.Drawing.Size(1268, 556);
            this.splitContainer1.SplitterDistance = 649;
            this.splitContainer1.TabIndex = 1;
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
            this.helplineRulerCtrl1.Size = new System.Drawing.Size(649, 556);
            this.helplineRulerCtrl1.TabIndex = 0;
            this.helplineRulerCtrl1.Zoom = 1F;
            this.helplineRulerCtrl1.ZoomSetManually = false;
            this.helplineRulerCtrl1.DBPanelDblClicked += new HelplineRulerControl.HelplineRulerCtrl.DblClickedEventHandler(this.helplineRulerCtrl1_DBPanelDblClicked);
            // 
            // helplineRulerCtrl2
            // 
            this.helplineRulerCtrl2.Bmp = null;
            this.helplineRulerCtrl2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.helplineRulerCtrl2.DontDoLayout = false;
            this.helplineRulerCtrl2.DontHandleDoubleClick = false;
            this.helplineRulerCtrl2.DontPaintBaseImg = false;
            this.helplineRulerCtrl2.DontProcDoubleClick = false;
            this.helplineRulerCtrl2.IgnoreZoom = false;
            this.helplineRulerCtrl2.Location = new System.Drawing.Point(0, 0);
            this.helplineRulerCtrl2.MoveHelpLinesOnResize = false;
            this.helplineRulerCtrl2.Name = "helplineRulerCtrl2";
            this.helplineRulerCtrl2.SetZoomOnlyByMethodCall = false;
            this.helplineRulerCtrl2.Size = new System.Drawing.Size(615, 556);
            this.helplineRulerCtrl2.TabIndex = 0;
            this.helplineRulerCtrl2.Zoom = 1F;
            this.helplineRulerCtrl2.ZoomSetManually = false;
            // 
            // panel3
            // 
            this.panel3.Location = new System.Drawing.Point(0, 0);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(200, 24);
            this.panel3.TabIndex = 225;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.ToolStripStatusLabel2,
            this.toolStripProgressBar1,
            this.toolStripStatusLabel4,
            this.toolStripStatusLabel3});
            this.statusStrip1.Location = new System.Drawing.Point(0, 726);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1268, 35);
            this.statusStrip1.TabIndex = 222;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Font = new System.Drawing.Font("Segoe UI", 16F);
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(63, 30);
            this.toolStripStatusLabel1.Text = "Hallo";
            // 
            // ToolStripStatusLabel2
            // 
            this.ToolStripStatusLabel2.AutoSize = false;
            this.ToolStripStatusLabel2.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this.ToolStripStatusLabel2.Name = "ToolStripStatusLabel2";
            this.ToolStripStatusLabel2.Size = new System.Drawing.Size(100, 30);
            this.ToolStripStatusLabel2.Text = "    ";
            // 
            // toolStripProgressBar1
            // 
            this.toolStripProgressBar1.Name = "toolStripProgressBar1";
            this.toolStripProgressBar1.Size = new System.Drawing.Size(400, 29);
            this.toolStripProgressBar1.Visible = false;
            // 
            // toolStripStatusLabel4
            // 
            this.toolStripStatusLabel4.Font = new System.Drawing.Font("Segoe UI", 16F);
            this.toolStripStatusLabel4.Name = "toolStripStatusLabel4";
            this.toolStripStatusLabel4.Size = new System.Drawing.Size(63, 30);
            this.toolStripStatusLabel4.Text = "Hallo";
            // 
            // toolStripStatusLabel3
            // 
            this.toolStripStatusLabel3.AutoSize = false;
            this.toolStripStatusLabel3.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this.toolStripStatusLabel3.Name = "toolStripStatusLabel3";
            this.toolStripStatusLabel3.Size = new System.Drawing.Size(100, 30);
            this.toolStripStatusLabel3.Text = "    ";
            // 
            // backgroundWorker1
            // 
            this.backgroundWorker1.WorkerReportsProgress = true;
            this.backgroundWorker1.WorkerSupportsCancellation = true;
            this.backgroundWorker1.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker1_DoWork);
            this.backgroundWorker1.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker1_ProgressChanged);
            this.backgroundWorker1.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker1_RunWorkerCompleted);
            // 
            // Timer3
            // 
            this.Timer3.Interval = 500;
            this.Timer3.Tick += new System.EventHandler(this.Timer3_Tick);
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.FileName = "Bild1.png";
            this.saveFileDialog1.Filter = "Png-Images (*.png)|*.png";
            // 
            // backgroundWorker2
            // 
            this.backgroundWorker2.WorkerReportsProgress = true;
            this.backgroundWorker2.WorkerSupportsCancellation = true;
            this.backgroundWorker2.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker2_DoWork);
            this.backgroundWorker2.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker2_ProgressChanged);
            this.backgroundWorker2.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker2_RunWorkerCompleted);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.Filter = "Images - (*.bmp;*.jpg;*.jpeg;*.jfif;*.png)|*.bmp;*.jpg;*.jpeg;*.jfif;*.png";
            // 
            // frmAvoidAGrabCutEasy
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1268, 761);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.statusStrip1);
            this.Name = "frmAvoidAGrabCutEasy";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "frmAvoidAGrabCutEasy";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.frmGrabCut_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numWH)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numQATH)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAlgMaxIter)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMultTLCap)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numProbMult1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDblMult)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numShiftY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numShiftX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numGmmComp)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxComponents)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWHScribbles)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numGamma)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        internal System.Windows.Forms.ToolStripStatusLabel ToolStripStatusLabel2;
        private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel4;
        internal System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel3;
        private System.Windows.Forms.Button btnGo;
        private System.Windows.Forms.Button btnReset2;
        private System.Windows.Forms.CheckBox cbCastTLInt;
        private System.Windows.Forms.CheckBox cbMultTLCap;
        private System.Windows.Forms.NumericUpDown numMultTLCap;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown numProbMult1;
        private System.Windows.Forms.CheckBox cbUseTh;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown numDblMult;
        private System.Windows.Forms.CheckBox cbQuickEst;
        private System.Windows.Forms.Button btnRect;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.NumericUpDown numWHScribbles;
        private System.Windows.Forms.CheckBox cbScribbleMode;
        private System.Windows.Forms.RadioButton rbFG;
        private System.Windows.Forms.RadioButton rbBG;
        private System.Windows.Forms.Button btnClearScribbles;
        private System.Windows.Forms.Button btnRemLastScribbles;
        private System.Windows.Forms.CheckBox cbRectMode;
        private System.Windows.Forms.NumericUpDown numGamma;
        private System.Windows.Forms.Label label2;
        internal System.Windows.Forms.Label Label20;
        internal System.Windows.Forms.ComboBox cmbZoom;
        internal System.Windows.Forms.CheckBox cbBGColor;
        private System.Windows.Forms.Button button10;
        private System.Windows.Forms.Button button8;
        internal System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Label label40;
        private System.Windows.Forms.Label label39;
        private System.Windows.Forms.NumericUpDown numQATH;
        private System.Windows.Forms.NumericUpDown numAlgMaxIter;
        private HelplineRulerControl.HelplineRulerCtrl helplineRulerCtrl1;
        private HelplineRulerControl.HelplineRulerCtrl helplineRulerCtrl2;
        private System.Windows.Forms.Button btnRecut;
        private System.Windows.Forms.Button btnRemStroke;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cmbCurrentColor;
        private System.Windows.Forms.CheckBox cbDraw;
        private System.Windows.Forms.NumericUpDown numWH;
        internal System.ComponentModel.BackgroundWorker backgroundWorker1;
        internal System.Windows.Forms.Timer Timer3;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.CheckBox cbEightAdj;
        private System.Windows.Forms.Button btnResQATH;
        private System.Windows.Forms.Button btnResMaxIter;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnMinCut;
        internal System.ComponentModel.BackgroundWorker backgroundWorker2;
        private System.Windows.Forms.CheckBox cbSkipLearn;
        private System.Windows.Forms.Button btnLoadScribbles;
        private System.Windows.Forms.Button btnSaveScribbles;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.CheckBox cbLSBmp;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.NumericUpDown numMaxComponents;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.NumericUpDown numMaxSize;
        private System.Windows.Forms.NumericUpDown numGmmComp;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.NumericUpDown numShiftY;
        private System.Windows.Forms.NumericUpDown numShiftX;
        private System.Windows.Forms.CheckBox cbSetPFGToFG;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnOutline;
        private System.Windows.Forms.Button btnCache;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Button btnCompose;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.CheckBox cbAutoThreshold;
        private System.Windows.Forms.Button btnInitSettings;
    }
}