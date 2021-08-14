namespace GerberView
{
    partial class DrillStatsForm
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
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.generalTabPage = new System.Windows.Forms.TabPage();
            this.errorLabel = new System.Windows.Forms.Label();
            this.generalDataGridView = new System.Windows.Forms.DataGridView();
            this.gCodesTabPage = new System.Windows.Forms.TabPage();
            this.gCodeLabel = new System.Windows.Forms.Label();
            this.gCodeDataGridView = new System.Windows.Forms.DataGridView();
            this.mCodeTabPage = new System.Windows.Forms.TabPage();
            this.mCodeLabel = new System.Windows.Forms.Label();
            this.mCodeDataGridView = new System.Windows.Forms.DataGridView();
            this.MiscTabPage = new System.Windows.Forms.TabPage();
            this.miscLabel = new System.Windows.Forms.Label();
            this.miscCodeDataGridView = new System.Windows.Forms.DataGridView();
            this.drillUseTabPage = new System.Windows.Forms.TabPage();
            this.drillUseLabel = new System.Windows.Forms.Label();
            this.drillUseDataGridView = new System.Windows.Forms.DataGridView();
            this.tabControl1.SuspendLayout();
            this.generalTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.generalDataGridView)).BeginInit();
            this.gCodesTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gCodeDataGridView)).BeginInit();
            this.mCodeTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mCodeDataGridView)).BeginInit();
            this.MiscTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.miscCodeDataGridView)).BeginInit();
            this.drillUseTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.drillUseDataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.generalTabPage);
            this.tabControl1.Controls.Add(this.gCodesTabPage);
            this.tabControl1.Controls.Add(this.mCodeTabPage);
            this.tabControl1.Controls.Add(this.MiscTabPage);
            this.tabControl1.Controls.Add(this.drillUseTabPage);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(384, 212);
            this.tabControl1.TabIndex = 0;
            // 
            // generalTabPage
            // 
            this.generalTabPage.Controls.Add(this.errorLabel);
            this.generalTabPage.Controls.Add(this.generalDataGridView);
            this.generalTabPage.Location = new System.Drawing.Point(4, 22);
            this.generalTabPage.Name = "generalTabPage";
            this.generalTabPage.Size = new System.Drawing.Size(376, 186);
            this.generalTabPage.TabIndex = 0;
            this.generalTabPage.Text = "Gerneral";
            this.generalTabPage.UseVisualStyleBackColor = true;
            // 
            // errorLabel
            // 
            this.errorLabel.AutoSize = true;
            this.errorLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.errorLabel.ForeColor = System.Drawing.Color.MediumSeaGreen;
            this.errorLabel.Location = new System.Drawing.Point(0, 5);
            this.errorLabel.Name = "errorLabel";
            this.errorLabel.Size = new System.Drawing.Size(39, 13);
            this.errorLabel.TabIndex = 5;
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
            this.generalDataGridView.MultiSelect = false;
            this.generalDataGridView.Name = "generalDataGridView";
            this.generalDataGridView.ReadOnly = true;
            this.generalDataGridView.RowTemplate.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.MediumSeaGreen;
            this.generalDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.generalDataGridView.Size = new System.Drawing.Size(376, 156);
            this.generalDataGridView.TabIndex = 1;
            this.generalDataGridView.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.GeneralDataGridView_CellClick);
            // 
            // gCodesTabPage
            // 
            this.gCodesTabPage.Controls.Add(this.gCodeLabel);
            this.gCodesTabPage.Controls.Add(this.gCodeDataGridView);
            this.gCodesTabPage.Location = new System.Drawing.Point(4, 22);
            this.gCodesTabPage.Name = "gCodesTabPage";
            this.gCodesTabPage.Size = new System.Drawing.Size(376, 186);
            this.gCodesTabPage.TabIndex = 1;
            this.gCodesTabPage.Text = "G Codes";
            this.gCodesTabPage.UseVisualStyleBackColor = true;
            // 
            // gCodeLabel
            // 
            this.gCodeLabel.AutoSize = true;
            this.gCodeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gCodeLabel.ForeColor = System.Drawing.Color.MediumSeaGreen;
            this.gCodeLabel.Location = new System.Drawing.Point(0, 5);
            this.gCodeLabel.Name = "gCodeLabel";
            this.gCodeLabel.Size = new System.Drawing.Size(54, 13);
            this.gCodeLabel.TabIndex = 4;
            this.gCodeLabel.Text = "filename";
            // 
            // gCodeDataGridView
            // 
            this.gCodeDataGridView.AllowUserToAddRows = false;
            this.gCodeDataGridView.AllowUserToDeleteRows = false;
            this.gCodeDataGridView.AllowUserToOrderColumns = true;
            this.gCodeDataGridView.AllowUserToResizeColumns = false;
            this.gCodeDataGridView.AllowUserToResizeRows = false;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.gCodeDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.gCodeDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.gCodeDataGridView.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.gCodeDataGridView.Location = new System.Drawing.Point(0, 30);
            this.gCodeDataGridView.MultiSelect = false;
            this.gCodeDataGridView.Name = "gCodeDataGridView";
            this.gCodeDataGridView.ReadOnly = true;
            this.gCodeDataGridView.RowTemplate.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.MediumSeaGreen;
            this.gCodeDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gCodeDataGridView.Size = new System.Drawing.Size(376, 156);
            this.gCodeDataGridView.TabIndex = 1;
            // 
            // mCodeTabPage
            // 
            this.mCodeTabPage.Controls.Add(this.mCodeLabel);
            this.mCodeTabPage.Controls.Add(this.mCodeDataGridView);
            this.mCodeTabPage.Location = new System.Drawing.Point(4, 22);
            this.mCodeTabPage.Name = "mCodeTabPage";
            this.mCodeTabPage.Size = new System.Drawing.Size(376, 186);
            this.mCodeTabPage.TabIndex = 2;
            this.mCodeTabPage.Text = "M Codes";
            this.mCodeTabPage.UseVisualStyleBackColor = true;
            // 
            // mCodeLabel
            // 
            this.mCodeLabel.AutoSize = true;
            this.mCodeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.mCodeLabel.ForeColor = System.Drawing.Color.MediumSeaGreen;
            this.mCodeLabel.Location = new System.Drawing.Point(0, 5);
            this.mCodeLabel.Name = "mCodeLabel";
            this.mCodeLabel.Size = new System.Drawing.Size(54, 13);
            this.mCodeLabel.TabIndex = 5;
            this.mCodeLabel.Text = "filename";
            // 
            // mCodeDataGridView
            // 
            this.mCodeDataGridView.AllowUserToAddRows = false;
            this.mCodeDataGridView.AllowUserToDeleteRows = false;
            this.mCodeDataGridView.AllowUserToResizeColumns = false;
            this.mCodeDataGridView.AllowUserToResizeRows = false;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.mCodeDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.mCodeDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.mCodeDataGridView.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.mCodeDataGridView.Location = new System.Drawing.Point(0, 30);
            this.mCodeDataGridView.MultiSelect = false;
            this.mCodeDataGridView.Name = "mCodeDataGridView";
            this.mCodeDataGridView.ReadOnly = true;
            this.mCodeDataGridView.RowTemplate.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.MediumSeaGreen;
            this.mCodeDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.mCodeDataGridView.Size = new System.Drawing.Size(376, 156);
            this.mCodeDataGridView.TabIndex = 1;
            // 
            // MiscTabPage
            // 
            this.MiscTabPage.Controls.Add(this.miscLabel);
            this.MiscTabPage.Controls.Add(this.miscCodeDataGridView);
            this.MiscTabPage.Location = new System.Drawing.Point(4, 22);
            this.MiscTabPage.Name = "MiscTabPage";
            this.MiscTabPage.Size = new System.Drawing.Size(376, 186);
            this.MiscTabPage.TabIndex = 3;
            this.MiscTabPage.Text = "Misc Codes";
            this.MiscTabPage.UseVisualStyleBackColor = true;
            // 
            // miscLabel
            // 
            this.miscLabel.AutoSize = true;
            this.miscLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.miscLabel.ForeColor = System.Drawing.Color.MediumSeaGreen;
            this.miscLabel.Location = new System.Drawing.Point(0, 5);
            this.miscLabel.Name = "miscLabel";
            this.miscLabel.Size = new System.Drawing.Size(54, 13);
            this.miscLabel.TabIndex = 6;
            this.miscLabel.Text = "filename";
            // 
            // miscCodeDataGridView
            // 
            this.miscCodeDataGridView.AllowUserToAddRows = false;
            this.miscCodeDataGridView.AllowUserToDeleteRows = false;
            this.miscCodeDataGridView.AllowUserToResizeColumns = false;
            this.miscCodeDataGridView.AllowUserToResizeRows = false;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.miscCodeDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle4;
            this.miscCodeDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.miscCodeDataGridView.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.miscCodeDataGridView.Location = new System.Drawing.Point(0, 30);
            this.miscCodeDataGridView.MultiSelect = false;
            this.miscCodeDataGridView.Name = "miscCodeDataGridView";
            this.miscCodeDataGridView.ReadOnly = true;
            this.miscCodeDataGridView.RowTemplate.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.MediumSeaGreen;
            this.miscCodeDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.miscCodeDataGridView.Size = new System.Drawing.Size(376, 156);
            this.miscCodeDataGridView.TabIndex = 1;
            // 
            // drillUseTabPage
            // 
            this.drillUseTabPage.Controls.Add(this.drillUseLabel);
            this.drillUseTabPage.Controls.Add(this.drillUseDataGridView);
            this.drillUseTabPage.Location = new System.Drawing.Point(4, 22);
            this.drillUseTabPage.Name = "drillUseTabPage";
            this.drillUseTabPage.Size = new System.Drawing.Size(376, 186);
            this.drillUseTabPage.TabIndex = 4;
            this.drillUseTabPage.Text = "Drill Usage";
            this.drillUseTabPage.UseVisualStyleBackColor = true;
            // 
            // drillUseLabel
            // 
            this.drillUseLabel.AutoSize = true;
            this.drillUseLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.drillUseLabel.ForeColor = System.Drawing.Color.MediumSeaGreen;
            this.drillUseLabel.Location = new System.Drawing.Point(0, 5);
            this.drillUseLabel.Name = "drillUseLabel";
            this.drillUseLabel.Size = new System.Drawing.Size(54, 13);
            this.drillUseLabel.TabIndex = 7;
            this.drillUseLabel.Text = "filename";
            // 
            // drillUseDataGridView
            // 
            this.drillUseDataGridView.AllowUserToAddRows = false;
            this.drillUseDataGridView.AllowUserToDeleteRows = false;
            this.drillUseDataGridView.AllowUserToResizeColumns = false;
            this.drillUseDataGridView.AllowUserToResizeRows = false;
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle5.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle5.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle5.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle5.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.drillUseDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle5;
            this.drillUseDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.drillUseDataGridView.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.drillUseDataGridView.Location = new System.Drawing.Point(0, 30);
            this.drillUseDataGridView.MultiSelect = false;
            this.drillUseDataGridView.Name = "drillUseDataGridView";
            this.drillUseDataGridView.ReadOnly = true;
            this.drillUseDataGridView.RowTemplate.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.MediumSeaGreen;
            this.drillUseDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.drillUseDataGridView.Size = new System.Drawing.Size(376, 156);
            this.drillUseDataGridView.TabIndex = 1;
            // 
            // DrillStatsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 212);
            this.Controls.Add(this.tabControl1);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "DrillStatsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Drill File Statistics [Visible Layers]";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.GerberStatsForm_FormClosed);
            this.Load += new System.EventHandler(this.DrillStatsForm_Load);
            this.tabControl1.ResumeLayout(false);
            this.generalTabPage.ResumeLayout(false);
            this.generalTabPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.generalDataGridView)).EndInit();
            this.gCodesTabPage.ResumeLayout(false);
            this.gCodesTabPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gCodeDataGridView)).EndInit();
            this.mCodeTabPage.ResumeLayout(false);
            this.mCodeTabPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mCodeDataGridView)).EndInit();
            this.MiscTabPage.ResumeLayout(false);
            this.MiscTabPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.miscCodeDataGridView)).EndInit();
            this.drillUseTabPage.ResumeLayout(false);
            this.drillUseTabPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.drillUseDataGridView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage generalTabPage;
        private System.Windows.Forms.TabPage gCodesTabPage;
        private System.Windows.Forms.TabPage mCodeTabPage;
        private System.Windows.Forms.TabPage MiscTabPage;
        private System.Windows.Forms.TabPage drillUseTabPage;
        private System.Windows.Forms.DataGridView generalDataGridView;
        private System.Windows.Forms.DataGridView gCodeDataGridView;
        private System.Windows.Forms.DataGridView mCodeDataGridView;
        private System.Windows.Forms.DataGridView miscCodeDataGridView;
        private System.Windows.Forms.DataGridView drillUseDataGridView;
        private System.Windows.Forms.Label gCodeLabel;
        private System.Windows.Forms.Label mCodeLabel;
        private System.Windows.Forms.Label miscLabel;
        private System.Windows.Forms.Label drillUseLabel;
        private System.Windows.Forms.Label errorLabel;
    }
}