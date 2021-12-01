namespace GerberView
{
    partial class GerberStatsForm
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.errorLabel = new System.Windows.Forms.Label();
            this.generalDataGridView = new System.Windows.Forms.DataGridView();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.gCodeLabel = new System.Windows.Forms.Label();
            this.gCodeDataGridView = new System.Windows.Forms.DataGridView();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.dCodeLabel = new System.Windows.Forms.Label();
            this.dCodeDataGridView = new System.Windows.Forms.DataGridView();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.mCodeLabel = new System.Windows.Forms.Label();
            this.mCodeDataGridView = new System.Windows.Forms.DataGridView();
            this.tabPage5 = new System.Windows.Forms.TabPage();
            this.miscLabel = new System.Windows.Forms.Label();
            this.miscCodeDataGridView = new System.Windows.Forms.DataGridView();
            this.tabPage6 = new System.Windows.Forms.TabPage();
            this.apDefLabel = new System.Windows.Forms.Label();
            this.apertureDefinitionGridView = new System.Windows.Forms.DataGridView();
            this.tabPage7 = new System.Windows.Forms.TabPage();
            this.apUseLabel = new System.Windows.Forms.Label();
            this.apertureUseDataGridView = new System.Windows.Forms.DataGridView();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.generalDataGridView)).BeginInit();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gCodeDataGridView)).BeginInit();
            this.tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dCodeDataGridView)).BeginInit();
            this.tabPage4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mCodeDataGridView)).BeginInit();
            this.tabPage5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.miscCodeDataGridView)).BeginInit();
            this.tabPage6.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.apertureDefinitionGridView)).BeginInit();
            this.tabPage7.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.apertureUseDataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Controls.Add(this.tabPage4);
            this.tabControl1.Controls.Add(this.tabPage5);
            this.tabControl1.Controls.Add(this.tabPage6);
            this.tabControl1.Controls.Add(this.tabPage7);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(484, 212);
            this.tabControl1.TabIndex = 1;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.errorLabel);
            this.tabPage1.Controls.Add(this.generalDataGridView);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Size = new System.Drawing.Size(476, 186);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "General";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // errorLabel
            // 
            this.errorLabel.AutoSize = true;
            this.errorLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.errorLabel.ForeColor = System.Drawing.Color.MediumSeaGreen;
            this.errorLabel.Location = new System.Drawing.Point(0, 5);
            this.errorLabel.Name = "errorLabel";
            this.errorLabel.Size = new System.Drawing.Size(39, 13);
            this.errorLabel.TabIndex = 3;
            this.errorLabel.Text = "errors";
            // 
            // generalDataGridView
            // 
            this.generalDataGridView.AllowUserToAddRows = false;
            this.generalDataGridView.AllowUserToDeleteRows = false;
            this.generalDataGridView.AllowUserToResizeColumns = false;
            this.generalDataGridView.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.generalDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.generalDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.generalDataGridView.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.generalDataGridView.Location = new System.Drawing.Point(0, 30);
            this.generalDataGridView.Name = "generalDataGridView";
            this.generalDataGridView.ReadOnly = true;
            this.generalDataGridView.RowHeadersVisible = false;
            this.generalDataGridView.RowHeadersWidth = 40;
            this.generalDataGridView.RowTemplate.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.MediumSeaGreen;
            this.generalDataGridView.RowTemplate.DefaultCellStyle.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            this.generalDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.generalDataGridView.Size = new System.Drawing.Size(476, 156);
            this.generalDataGridView.TabIndex = 2;
            this.generalDataGridView.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.GeneralDataGridView_CellClick);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.gCodeLabel);
            this.tabPage2.Controls.Add(this.gCodeDataGridView);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Size = new System.Drawing.Size(476, 186);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "G Codes";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // gCodeLabel
            // 
            this.gCodeLabel.AutoSize = true;
            this.gCodeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gCodeLabel.ForeColor = System.Drawing.Color.MediumSeaGreen;
            this.gCodeLabel.Location = new System.Drawing.Point(0, 5);
            this.gCodeLabel.Name = "gCodeLabel";
            this.gCodeLabel.Size = new System.Drawing.Size(54, 13);
            this.gCodeLabel.TabIndex = 2;
            this.gCodeLabel.Text = "filename";
            // 
            // gCodeDataGridView
            // 
            this.gCodeDataGridView.AllowUserToAddRows = false;
            this.gCodeDataGridView.AllowUserToDeleteRows = false;
            this.gCodeDataGridView.AllowUserToResizeColumns = false;
            this.gCodeDataGridView.AllowUserToResizeRows = false;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.MenuHighlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.gCodeDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.gCodeDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.gCodeDataGridView.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.gCodeDataGridView.Location = new System.Drawing.Point(0, 30);
            this.gCodeDataGridView.MultiSelect = false;
            this.gCodeDataGridView.Name = "gCodeDataGridView";
            this.gCodeDataGridView.ReadOnly = true;
            this.gCodeDataGridView.RowHeadersVisible = false;
            this.gCodeDataGridView.RowHeadersWidth = 40;
            this.gCodeDataGridView.RowTemplate.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.MediumSeaGreen;
            this.gCodeDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gCodeDataGridView.Size = new System.Drawing.Size(476, 156);
            this.gCodeDataGridView.TabIndex = 1;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.dCodeLabel);
            this.tabPage3.Controls.Add(this.dCodeDataGridView);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(476, 186);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "D Codes";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // dCodeLabel
            // 
            this.dCodeLabel.AutoSize = true;
            this.dCodeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dCodeLabel.ForeColor = System.Drawing.Color.MediumSeaGreen;
            this.dCodeLabel.Location = new System.Drawing.Point(0, 5);
            this.dCodeLabel.Name = "dCodeLabel";
            this.dCodeLabel.Size = new System.Drawing.Size(54, 13);
            this.dCodeLabel.TabIndex = 3;
            this.dCodeLabel.Text = "filename";
            // 
            // dCodeDataGridView
            // 
            this.dCodeDataGridView.AllowUserToAddRows = false;
            this.dCodeDataGridView.AllowUserToDeleteRows = false;
            this.dCodeDataGridView.AllowUserToResizeColumns = false;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dCodeDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.dCodeDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dCodeDataGridView.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.dCodeDataGridView.Location = new System.Drawing.Point(0, 30);
            this.dCodeDataGridView.Name = "dCodeDataGridView";
            this.dCodeDataGridView.ReadOnly = true;
            this.dCodeDataGridView.RowHeadersVisible = false;
            this.dCodeDataGridView.RowHeadersWidth = 40;
            this.dCodeDataGridView.RowTemplate.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.MediumSeaGreen;
            this.dCodeDataGridView.RowTemplate.DefaultCellStyle.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            this.dCodeDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dCodeDataGridView.Size = new System.Drawing.Size(476, 156);
            this.dCodeDataGridView.TabIndex = 1;
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.mCodeLabel);
            this.tabPage4.Controls.Add(this.mCodeDataGridView);
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Size = new System.Drawing.Size(476, 186);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "M Codes";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // mCodeLabel
            // 
            this.mCodeLabel.AutoSize = true;
            this.mCodeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.mCodeLabel.ForeColor = System.Drawing.Color.MediumSeaGreen;
            this.mCodeLabel.Location = new System.Drawing.Point(0, 5);
            this.mCodeLabel.Name = "mCodeLabel";
            this.mCodeLabel.Size = new System.Drawing.Size(54, 13);
            this.mCodeLabel.TabIndex = 4;
            this.mCodeLabel.Text = "filename";
            // 
            // mCodeDataGridView
            // 
            this.mCodeDataGridView.AllowUserToAddRows = false;
            this.mCodeDataGridView.AllowUserToDeleteRows = false;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.mCodeDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle4;
            this.mCodeDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.mCodeDataGridView.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.mCodeDataGridView.Location = new System.Drawing.Point(0, 30);
            this.mCodeDataGridView.MinimumSize = new System.Drawing.Size(0, 26);
            this.mCodeDataGridView.Name = "mCodeDataGridView";
            this.mCodeDataGridView.ReadOnly = true;
            this.mCodeDataGridView.RowTemplate.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.MediumSeaGreen;
            this.mCodeDataGridView.RowTemplate.DefaultCellStyle.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            this.mCodeDataGridView.Size = new System.Drawing.Size(476, 156);
            this.mCodeDataGridView.TabIndex = 0;
            // 
            // tabPage5
            // 
            this.tabPage5.Controls.Add(this.miscLabel);
            this.tabPage5.Controls.Add(this.miscCodeDataGridView);
            this.tabPage5.Location = new System.Drawing.Point(4, 22);
            this.tabPage5.Name = "tabPage5";
            this.tabPage5.Size = new System.Drawing.Size(476, 186);
            this.tabPage5.TabIndex = 4;
            this.tabPage5.Text = "Misc. Codes";
            this.tabPage5.UseVisualStyleBackColor = true;
            // 
            // miscLabel
            // 
            this.miscLabel.AutoSize = true;
            this.miscLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.miscLabel.ForeColor = System.Drawing.Color.MediumSeaGreen;
            this.miscLabel.Location = new System.Drawing.Point(0, 5);
            this.miscLabel.Name = "miscLabel";
            this.miscLabel.Size = new System.Drawing.Size(54, 13);
            this.miscLabel.TabIndex = 5;
            this.miscLabel.Text = "filename";
            // 
            // miscCodeDataGridView
            // 
            this.miscCodeDataGridView.AllowUserToAddRows = false;
            this.miscCodeDataGridView.AllowUserToDeleteRows = false;
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle5.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle5.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle5.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle5.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.miscCodeDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle5;
            this.miscCodeDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.miscCodeDataGridView.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.miscCodeDataGridView.Location = new System.Drawing.Point(0, 30);
            this.miscCodeDataGridView.Name = "miscCodeDataGridView";
            this.miscCodeDataGridView.ReadOnly = true;
            this.miscCodeDataGridView.RowTemplate.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.MediumSeaGreen;
            this.miscCodeDataGridView.RowTemplate.DefaultCellStyle.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            this.miscCodeDataGridView.Size = new System.Drawing.Size(476, 156);
            this.miscCodeDataGridView.TabIndex = 0;
            // 
            // tabPage6
            // 
            this.tabPage6.Controls.Add(this.apDefLabel);
            this.tabPage6.Controls.Add(this.apertureDefinitionGridView);
            this.tabPage6.Location = new System.Drawing.Point(4, 22);
            this.tabPage6.Name = "tabPage6";
            this.tabPage6.Size = new System.Drawing.Size(476, 186);
            this.tabPage6.TabIndex = 5;
            this.tabPage6.Text = "Aperture Definitions";
            this.tabPage6.UseVisualStyleBackColor = true;
            // 
            // apDefLabel
            // 
            this.apDefLabel.AutoSize = true;
            this.apDefLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.apDefLabel.ForeColor = System.Drawing.Color.MediumSeaGreen;
            this.apDefLabel.Location = new System.Drawing.Point(0, 5);
            this.apDefLabel.Name = "apDefLabel";
            this.apDefLabel.Size = new System.Drawing.Size(54, 13);
            this.apDefLabel.TabIndex = 6;
            this.apDefLabel.Text = "filename";
            // 
            // apertureDefinitionGridView
            // 
            this.apertureDefinitionGridView.AllowUserToAddRows = false;
            this.apertureDefinitionGridView.AllowUserToDeleteRows = false;
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle6.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle6.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle6.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle6.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.apertureDefinitionGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle6;
            this.apertureDefinitionGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.apertureDefinitionGridView.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.apertureDefinitionGridView.Location = new System.Drawing.Point(0, 30);
            this.apertureDefinitionGridView.Name = "apertureDefinitionGridView";
            this.apertureDefinitionGridView.ReadOnly = true;
            this.apertureDefinitionGridView.RowTemplate.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.MediumSeaGreen;
            this.apertureDefinitionGridView.RowTemplate.DefaultCellStyle.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            this.apertureDefinitionGridView.Size = new System.Drawing.Size(476, 156);
            this.apertureDefinitionGridView.TabIndex = 0;
            // 
            // tabPage7
            // 
            this.tabPage7.Controls.Add(this.apUseLabel);
            this.tabPage7.Controls.Add(this.apertureUseDataGridView);
            this.tabPage7.Location = new System.Drawing.Point(4, 22);
            this.tabPage7.Name = "tabPage7";
            this.tabPage7.Size = new System.Drawing.Size(476, 186);
            this.tabPage7.TabIndex = 6;
            this.tabPage7.Text = "Aperture Usage";
            this.tabPage7.UseVisualStyleBackColor = true;
            // 
            // apUseLabel
            // 
            this.apUseLabel.AutoSize = true;
            this.apUseLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.apUseLabel.ForeColor = System.Drawing.Color.MediumSeaGreen;
            this.apUseLabel.Location = new System.Drawing.Point(0, 5);
            this.apUseLabel.Name = "apUseLabel";
            this.apUseLabel.Size = new System.Drawing.Size(54, 13);
            this.apUseLabel.TabIndex = 7;
            this.apUseLabel.Text = "filename";
            // 
            // apertureUseDataGridView
            // 
            this.apertureUseDataGridView.AllowUserToAddRows = false;
            this.apertureUseDataGridView.AllowUserToDeleteRows = false;
            dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle7.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle7.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle7.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle7.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.apertureUseDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle7;
            this.apertureUseDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.apertureUseDataGridView.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.apertureUseDataGridView.Location = new System.Drawing.Point(0, 30);
            this.apertureUseDataGridView.Name = "apertureUseDataGridView";
            this.apertureUseDataGridView.ReadOnly = true;
            this.apertureUseDataGridView.RowTemplate.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.MediumSeaGreen;
            this.apertureUseDataGridView.RowTemplate.DefaultCellStyle.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            this.apertureUseDataGridView.RowTemplate.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.apertureUseDataGridView.Size = new System.Drawing.Size(476, 156);
            this.apertureUseDataGridView.TabIndex = 0;
            // 
            // GerberStatsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 212);
            this.Controls.Add(this.tabControl1);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "GerberStatsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Gerber File Statistics [Visible Layers]";
            this.TopMost = true;
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.GerberStatsForm_FormClosed);
            this.Load += new System.EventHandler(this.GerberStatsForm_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.generalDataGridView)).EndInit();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gCodeDataGridView)).EndInit();
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dCodeDataGridView)).EndInit();
            this.tabPage4.ResumeLayout(false);
            this.tabPage4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mCodeDataGridView)).EndInit();
            this.tabPage5.ResumeLayout(false);
            this.tabPage5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.miscCodeDataGridView)).EndInit();
            this.tabPage6.ResumeLayout(false);
            this.tabPage6.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.apertureDefinitionGridView)).EndInit();
            this.tabPage7.ResumeLayout(false);
            this.tabPage7.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.apertureUseDataGridView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.DataGridView gCodeDataGridView;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.TabPage tabPage5;
        private System.Windows.Forms.TabPage tabPage6;
        private System.Windows.Forms.TabPage tabPage7;
        private System.Windows.Forms.DataGridView generalDataGridView;
        private System.Windows.Forms.DataGridView mCodeDataGridView;
        private System.Windows.Forms.DataGridView miscCodeDataGridView;
        private System.Windows.Forms.DataGridView apertureDefinitionGridView;
        private System.Windows.Forms.DataGridView apertureUseDataGridView;
        private System.Windows.Forms.Label gCodeLabel;
        private System.Windows.Forms.Label dCodeLabel;
        private System.Windows.Forms.Label mCodeLabel;
        private System.Windows.Forms.Label miscLabel;
        private System.Windows.Forms.Label apDefLabel;
        private System.Windows.Forms.Label apUseLabel;
        private System.Windows.Forms.DataGridView dCodeDataGridView;
        private System.Windows.Forms.Label errorLabel;

    }
}