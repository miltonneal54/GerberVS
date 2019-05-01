namespace GerberView
{
    partial class LayerEditForm
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.selectedLayerButton = new System.Windows.Forms.Button();
            this.allLayerButton = new System.Windows.Forms.Button();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown2 = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown3 = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown4 = new System.Windows.Forms.NumericUpDown();
            this.rotationComboBox = new System.Windows.Forms.ComboBox();
            this.mirrorXCheckBox = new System.Windows.Forms.CheckBox();
            this.mirrorYCheckBox = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.exitButton = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown4)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.numericUpDown2);
            this.groupBox1.Controls.Add(this.numericUpDown1);
            this.groupBox1.Location = new System.Drawing.Point(12, 6);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(340, 80);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Translation";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.numericUpDown4);
            this.groupBox2.Controls.Add(this.numericUpDown3);
            this.groupBox2.Location = new System.Drawing.Point(12, 92);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(340, 80);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Scale";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.rotationComboBox);
            this.groupBox3.Location = new System.Drawing.Point(12, 178);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(340, 80);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Rotation";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.mirrorYCheckBox);
            this.groupBox4.Controls.Add(this.mirrorXCheckBox);
            this.groupBox4.Location = new System.Drawing.Point(12, 264);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(340, 80);
            this.groupBox4.TabIndex = 3;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Mirroring";
            // 
            // selectedLayerButton
            // 
            this.selectedLayerButton.Location = new System.Drawing.Point(12, 350);
            this.selectedLayerButton.Name = "selectedLayerButton";
            this.selectedLayerButton.Size = new System.Drawing.Size(105, 22);
            this.selectedLayerButton.TabIndex = 4;
            this.selectedLayerButton.Text = "Apply To Selected";
            this.selectedLayerButton.UseVisualStyleBackColor = true;
            // 
            // allLayerButton
            // 
            this.allLayerButton.Location = new System.Drawing.Point(130, 350);
            this.allLayerButton.Name = "allLayerButton";
            this.allLayerButton.Size = new System.Drawing.Size(105, 22);
            this.allLayerButton.TabIndex = 5;
            this.allLayerButton.Text = "Apply To All";
            this.allLayerButton.UseVisualStyleBackColor = true;
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.DecimalPlaces = 4;
            this.numericUpDown1.Location = new System.Drawing.Point(214, 19);
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(120, 20);
            this.numericUpDown1.TabIndex = 0;
            // 
            // numericUpDown2
            // 
            this.numericUpDown2.DecimalPlaces = 4;
            this.numericUpDown2.Location = new System.Drawing.Point(214, 45);
            this.numericUpDown2.Name = "numericUpDown2";
            this.numericUpDown2.Size = new System.Drawing.Size(120, 20);
            this.numericUpDown2.TabIndex = 1;
            // 
            // numericUpDown3
            // 
            this.numericUpDown3.DecimalPlaces = 4;
            this.numericUpDown3.Location = new System.Drawing.Point(214, 19);
            this.numericUpDown3.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown3.Name = "numericUpDown3";
            this.numericUpDown3.Size = new System.Drawing.Size(120, 20);
            this.numericUpDown3.TabIndex = 1;
            this.numericUpDown3.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // numericUpDown4
            // 
            this.numericUpDown4.DecimalPlaces = 4;
            this.numericUpDown4.Location = new System.Drawing.Point(214, 45);
            this.numericUpDown4.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown4.Name = "numericUpDown4";
            this.numericUpDown4.Size = new System.Drawing.Size(120, 20);
            this.numericUpDown4.TabIndex = 2;
            this.numericUpDown4.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // rotationComboBox
            // 
            this.rotationComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.rotationComboBox.FormattingEnabled = true;
            this.rotationComboBox.Items.AddRange(new object[] {
            "None",
            "90 degrees CCW",
            "180 degrees CCW",
            "270 degrees CCW"});
            this.rotationComboBox.Location = new System.Drawing.Point(213, 34);
            this.rotationComboBox.Name = "rotationComboBox";
            this.rotationComboBox.Size = new System.Drawing.Size(121, 21);
            this.rotationComboBox.TabIndex = 0;
            // 
            // mirrorXCheckBox
            // 
            this.mirrorXCheckBox.AutoSize = true;
            this.mirrorXCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.mirrorXCheckBox.Location = new System.Drawing.Point(214, 19);
            this.mirrorXCheckBox.Name = "mirrorXCheckBox";
            this.mirrorXCheckBox.Size = new System.Drawing.Size(88, 17);
            this.mirrorXCheckBox.TabIndex = 0;
            this.mirrorXCheckBox.Text = "About X axis:";
            this.mirrorXCheckBox.UseVisualStyleBackColor = true;
            // 
            // mirrorYCheckBox
            // 
            this.mirrorYCheckBox.AutoSize = true;
            this.mirrorYCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.mirrorYCheckBox.Location = new System.Drawing.Point(214, 57);
            this.mirrorYCheckBox.Name = "mirrorYCheckBox";
            this.mirrorYCheckBox.Size = new System.Drawing.Size(88, 17);
            this.mirrorYCheckBox.TabIndex = 1;
            this.mirrorYCheckBox.Text = "About Y axis:";
            this.mirrorYCheckBox.UseVisualStyleBackColor = true;
            this.mirrorYCheckBox.CheckedChanged += new System.EventHandler(this.checkBox2_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(93, 37);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(97, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Rotation (degrees):";
            // 
            // exitButton
            // 
            this.exitButton.Location = new System.Drawing.Point(247, 350);
            this.exitButton.Name = "exitButton";
            this.exitButton.Size = new System.Drawing.Size(105, 22);
            this.exitButton.TabIndex = 6;
            this.exitButton.Text = "Done";
            this.exitButton.UseVisualStyleBackColor = true;
            // 
            // LayerEditForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.exitButton;
            this.ClientSize = new System.Drawing.Size(362, 386);
            this.Controls.Add(this.exitButton);
            this.Controls.Add(this.allLayerButton);
            this.Controls.Add(this.selectedLayerButton);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LayerEditForm";
            this.Text = "LayerEditForm";
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown4)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.NumericUpDown numericUpDown2;
        private System.Windows.Forms.NumericUpDown numericUpDown1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.NumericUpDown numericUpDown4;
        private System.Windows.Forms.NumericUpDown numericUpDown3;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.ComboBox rotationComboBox;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.CheckBox mirrorYCheckBox;
        private System.Windows.Forms.CheckBox mirrorXCheckBox;
        private System.Windows.Forms.Button selectedLayerButton;
        private System.Windows.Forms.Button allLayerButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button exitButton;
    }
}