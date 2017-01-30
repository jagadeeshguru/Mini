namespace Assign4
{
    partial class Form1
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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea5 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend5 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series5 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.chart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.radioDoughnutChart = new System.Windows.Forms.RadioButton();
            this.radioColumnChart = new System.Windows.Forms.RadioButton();
            this.radioPieChart = new System.Windows.Forms.RadioButton();
            this.radioBarChart = new System.Windows.Forms.RadioButton();
            this.btnUser = new System.Windows.Forms.Button();
            this.btnFile = new System.Windows.Forms.Button();
            this.btnRandom = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.btnExit = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.chart)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // chart
            // 
            chartArea5.Name = "ChartArea1";
            this.chart.ChartAreas.Add(chartArea5);
            legend5.Name = "Legend1";
            this.chart.Legends.Add(legend5);
            this.chart.Location = new System.Drawing.Point(423, 21);
            this.chart.Name = "chart";
            series5.ChartArea = "ChartArea1";
            series5.Legend = "Legend1";
            series5.Name = "Series1";
            this.chart.Series.Add(series5);
            this.chart.Size = new System.Drawing.Size(300, 300);
            this.chart.TabIndex = 0;
            this.chart.Text = "chart1";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(23, 45);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(81, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Enter a Number";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(142, 38);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(100, 20);
            this.textBox1.TabIndex = 2;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.radioDoughnutChart);
            this.groupBox1.Controls.Add(this.radioColumnChart);
            this.groupBox1.Controls.Add(this.radioPieChart);
            this.groupBox1.Controls.Add(this.radioBarChart);
            this.groupBox1.Location = new System.Drawing.Point(26, 115);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(351, 111);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Select a chart type";
            // 
            // radioDoughnutChart
            // 
            this.radioDoughnutChart.AutoSize = true;
            this.radioDoughnutChart.Location = new System.Drawing.Point(116, 77);
            this.radioDoughnutChart.Name = "radioDoughnutChart";
            this.radioDoughnutChart.Size = new System.Drawing.Size(100, 17);
            this.radioDoughnutChart.TabIndex = 3;
            this.radioDoughnutChart.TabStop = true;
            this.radioDoughnutChart.Text = "Doughnut Chart";
            this.radioDoughnutChart.UseVisualStyleBackColor = true;
            this.radioDoughnutChart.CheckedChanged += new System.EventHandler(this.radioDoughnutChart_CheckedChanged);
            // 
            // radioColumnChart
            // 
            this.radioColumnChart.AutoSize = true;
            this.radioColumnChart.Location = new System.Drawing.Point(7, 77);
            this.radioColumnChart.Name = "radioColumnChart";
            this.radioColumnChart.Size = new System.Drawing.Size(88, 17);
            this.radioColumnChart.TabIndex = 2;
            this.radioColumnChart.TabStop = true;
            this.radioColumnChart.Text = "Column Chart";
            this.radioColumnChart.UseVisualStyleBackColor = true;
            this.radioColumnChart.CheckedChanged += new System.EventHandler(this.radioColumnChart_CheckedChanged);
            // 
            // radioPieChart
            // 
            this.radioPieChart.AutoSize = true;
            this.radioPieChart.Location = new System.Drawing.Point(116, 20);
            this.radioPieChart.Name = "radioPieChart";
            this.radioPieChart.Size = new System.Drawing.Size(68, 17);
            this.radioPieChart.TabIndex = 1;
            this.radioPieChart.TabStop = true;
            this.radioPieChart.Text = "Pie Chart";
            this.radioPieChart.UseVisualStyleBackColor = true;
            this.radioPieChart.CheckedChanged += new System.EventHandler(this.radioPieChart_CheckedChanged);
            // 
            // radioBarChart
            // 
            this.radioBarChart.AutoSize = true;
            this.radioBarChart.Location = new System.Drawing.Point(7, 20);
            this.radioBarChart.Name = "radioBarChart";
            this.radioBarChart.Size = new System.Drawing.Size(69, 17);
            this.radioBarChart.TabIndex = 0;
            this.radioBarChart.TabStop = true;
            this.radioBarChart.Text = "Bar Chart";
            this.radioBarChart.UseVisualStyleBackColor = true;
            this.radioBarChart.CheckedChanged += new System.EventHandler(this.radioBarChart_CheckedChanged);
            // 
            // btnUser
            // 
            this.btnUser.Location = new System.Drawing.Point(16, 298);
            this.btnUser.Name = "btnUser";
            this.btnUser.Size = new System.Drawing.Size(105, 23);
            this.btnUser.TabIndex = 4;
            this.btnUser.Text = "Value From User";
            this.btnUser.UseVisualStyleBackColor = true;
            this.btnUser.Click += new System.EventHandler(this.btnUser_Click);
            // 
            // btnFile
            // 
            this.btnFile.Location = new System.Drawing.Point(142, 298);
            this.btnFile.Name = "btnFile";
            this.btnFile.Size = new System.Drawing.Size(100, 23);
            this.btnFile.TabIndex = 5;
            this.btnFile.Text = "Value From File";
            this.btnFile.UseVisualStyleBackColor = true;
            this.btnFile.Click += new System.EventHandler(this.btnFile_Click);
            // 
            // btnRandom
            // 
            this.btnRandom.Location = new System.Drawing.Point(275, 298);
            this.btnRandom.Name = "btnRandom";
            this.btnRandom.Size = new System.Drawing.Size(102, 23);
            this.btnRandom.TabIndex = 6;
            this.btnRandom.Text = "Value Random";
            this.btnRandom.UseVisualStyleBackColor = true;
            this.btnRandom.Click += new System.EventHandler(this.btnRandom_Click);
            // 
            // btnClear
            // 
            this.btnClear.Location = new System.Drawing.Point(248, 396);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(75, 23);
            this.btnClear.TabIndex = 7;
            this.btnClear.Text = "Clear";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // btnExit
            // 
            this.btnExit.Location = new System.Drawing.Point(375, 396);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(75, 23);
            this.btnExit.TabIndex = 8;
            this.btnExit.Text = "Exit";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(798, 442);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.btnRandom);
            this.Controls.Add(this.btnFile);
            this.Controls.Add(this.btnUser);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.chart);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.chart)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataVisualization.Charting.Chart chart;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton radioDoughnutChart;
        private System.Windows.Forms.RadioButton radioColumnChart;
        private System.Windows.Forms.RadioButton radioPieChart;
        private System.Windows.Forms.RadioButton radioBarChart;
        private System.Windows.Forms.Button btnUser;
        private System.Windows.Forms.Button btnFile;
        private System.Windows.Forms.Button btnRandom;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Button btnExit;
    }
}

