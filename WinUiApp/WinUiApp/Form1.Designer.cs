namespace WinUiApp
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.edgesView = new System.Windows.Forms.DataGridView();
            this.vertexView = new System.Windows.Forms.DataGridView();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.statusView = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.progressView = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.edgesView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.vertexView)).BeginInit();
            this.SuspendLayout();
            // 
            // edgesView
            // 
            this.edgesView.AllowUserToAddRows = false;
            this.edgesView.AllowUserToDeleteRows = false;
            this.edgesView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.edgesView.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.edgesView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.edgesView.Location = new System.Drawing.Point(24, 536);
            this.edgesView.Name = "edgesView";
            this.edgesView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            this.edgesView.RowTemplate.Height = 33;
            this.edgesView.Size = new System.Drawing.Size(1024, 655);
            this.edgesView.TabIndex = 2;
            // 
            // vertexView
            // 
            this.vertexView.AllowUserToAddRows = false;
            this.vertexView.AllowUserToDeleteRows = false;
            this.vertexView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.vertexView.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.vertexView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.vertexView.Location = new System.Drawing.Point(24, 76);
            this.vertexView.Name = "vertexView";
            this.vertexView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            this.vertexView.RowTemplate.Height = 33;
            this.vertexView.Size = new System.Drawing.Size(1024, 361);
            this.vertexView.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.label1.Location = new System.Drawing.Point(29, 32);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(66, 28);
            this.label1.TabIndex = 4;
            this.label1.Text = "Seeds";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.label2.Location = new System.Drawing.Point(29, 489);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(60, 28);
            this.label2.TabIndex = 5;
            this.label2.Text = "Links";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.label3.Location = new System.Drawing.Point(38, 1232);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(76, 28);
            this.label3.TabIndex = 6;
            this.label3.Text = "Status:";
            // 
            // statusView
            // 
            this.statusView.AutoSize = true;
            this.statusView.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point);
            this.statusView.Location = new System.Drawing.Point(148, 1233);
            this.statusView.Name = "statusView";
            this.statusView.Size = new System.Drawing.Size(107, 28);
            this.statusView.TabIndex = 7;
            this.statusView.Text = "In progress";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.label4.Location = new System.Drawing.Point(38, 1282);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(98, 28);
            this.label4.TabIndex = 8;
            this.label4.Text = "Progress:";
            // 
            // progressView
            // 
            this.progressView.AutoSize = true;
            this.progressView.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point);
            this.progressView.Location = new System.Drawing.Point(154, 1283);
            this.progressView.Name = "progressView";
            this.progressView.Size = new System.Drawing.Size(38, 28);
            this.progressView.TabIndex = 9;
            this.progressView.Text = "0.0";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1134, 1411);
            this.Controls.Add(this.progressView);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.statusView);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.vertexView);
            this.Controls.Add(this.edgesView);
            this.Name = "Form1";
            this.Text = "Crawler UI";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            ((System.ComponentModel.ISupportInitialize)(this.edgesView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.vertexView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        public System.Windows.Forms.DataGridView edgesView;
        public System.Windows.Forms.DataGridView vertexView;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        public System.Windows.Forms.Label statusView;
        private System.Windows.Forms.Label label4;
        public System.Windows.Forms.Label progressView;
    }
}

