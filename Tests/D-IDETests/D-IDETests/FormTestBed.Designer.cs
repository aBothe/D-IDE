namespace D_IDETests
{
    partial class FormTestBed
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormTestBed));
            this.buttonRunTests = new System.Windows.Forms.Button();
            this.dataGridViewErrors = new System.Windows.Forms.DataGridView();
            this.ColumnStatus = new System.Windows.Forms.DataGridViewImageColumn();
            this.ColumnTestName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnTestStart = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnTestEnd = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnResults = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewErrors)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonRunTests
            // 
            this.buttonRunTests.Location = new System.Drawing.Point(12, 12);
            this.buttonRunTests.Name = "buttonRunTests";
            this.buttonRunTests.Size = new System.Drawing.Size(75, 23);
            this.buttonRunTests.TabIndex = 0;
            this.buttonRunTests.Text = "Run Tests";
            this.buttonRunTests.UseVisualStyleBackColor = true;
            // 
            // dataGridViewErrors
            // 
            this.dataGridViewErrors.AllowUserToAddRows = false;
            this.dataGridViewErrors.AllowUserToDeleteRows = false;
            this.dataGridViewErrors.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridViewErrors.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewErrors.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ColumnStatus,
            this.ColumnTestName,
            this.ColumnTestStart,
            this.ColumnTestEnd,
            this.ColumnResults});
            this.dataGridViewErrors.Location = new System.Drawing.Point(12, 41);
            this.dataGridViewErrors.Name = "dataGridViewErrors";
            this.dataGridViewErrors.ReadOnly = true;
            this.dataGridViewErrors.RowHeadersVisible = false;
            this.dataGridViewErrors.ShowCellErrors = false;
            this.dataGridViewErrors.ShowEditingIcon = false;
            this.dataGridViewErrors.ShowRowErrors = false;
            this.dataGridViewErrors.Size = new System.Drawing.Size(678, 401);
            this.dataGridViewErrors.TabIndex = 1;
            // 
            // ColumnStatus
            // 
            this.ColumnStatus.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.ColumnStatus.HeaderText = "Status";
            this.ColumnStatus.Image = global::D_IDETests.Properties.Resources.untested;
            this.ColumnStatus.Name = "ColumnStatus";
            this.ColumnStatus.ReadOnly = true;
            this.ColumnStatus.Width = 50;
            // 
            // ColumnTestName
            // 
            this.ColumnTestName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ColumnTestName.HeaderText = "Test Name";
            this.ColumnTestName.Name = "ColumnTestName";
            this.ColumnTestName.ReadOnly = true;
            // 
            // ColumnTestStart
            // 
            this.ColumnTestStart.HeaderText = "Start Time";
            this.ColumnTestStart.Name = "ColumnTestStart";
            this.ColumnTestStart.ReadOnly = true;
            // 
            // ColumnTestEnd
            // 
            this.ColumnTestEnd.HeaderText = "End Time";
            this.ColumnTestEnd.Name = "ColumnTestEnd";
            this.ColumnTestEnd.ReadOnly = true;
            // 
            // ColumnResults
            // 
            this.ColumnResults.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ColumnResults.FillWeight = 200F;
            this.ColumnResults.HeaderText = "Results";
            this.ColumnResults.Name = "ColumnResults";
            this.ColumnResults.ReadOnly = true;
            // 
            // FormTestBed
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(702, 454);
            this.Controls.Add(this.dataGridViewErrors);
            this.Controls.Add(this.buttonRunTests);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormTestBed";
            this.Text = "D-IDE Test Bed";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewErrors)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button buttonRunTests;
        private System.Windows.Forms.DataGridView dataGridViewErrors;
        private System.Windows.Forms.DataGridViewImageColumn ColumnStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnTestName;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnTestStart;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnTestEnd;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnResults;
    }
}

