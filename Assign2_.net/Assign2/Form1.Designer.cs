namespace Assign2
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.qt = new System.Windows.Forms.RadioButton();
            this.srt = new System.Windows.Forms.RadioButton();
            this.searchOffice = new System.Windows.Forms.RadioButton();
            this.searchName = new System.Windows.Forms.RadioButton();
            this.Add = new System.Windows.Forms.RadioButton();
            this.Print = new System.Windows.Forms.RadioButton();
            this.personName = new System.Windows.Forms.TextBox();
            this.personOffice = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lstOutput = new System.Windows.Forms.ListBox();
            this.label3 = new System.Windows.Forms.Label();
            this.clr = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.qt);
            this.groupBox1.Controls.Add(this.srt);
            this.groupBox1.Controls.Add(this.searchOffice);
            this.groupBox1.Controls.Add(this.searchName);
            this.groupBox1.Controls.Add(this.Add);
            this.groupBox1.Controls.Add(this.Print);
            this.groupBox1.Location = new System.Drawing.Point(34, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(268, 166);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "groupBox1";
            // 
            // qt
            // 
            this.qt.AutoSize = true;
            this.qt.Location = new System.Drawing.Point(20, 135);
            this.qt.Name = "qt";
            this.qt.Size = new System.Drawing.Size(44, 17);
            this.qt.TabIndex = 5;
            this.qt.TabStop = true;
            this.qt.Text = "Quit";
            this.qt.UseVisualStyleBackColor = true;
            this.qt.CheckedChanged += new System.EventHandler(this.RadioButtons_CheckedChanged);
            // 
            // srt
            // 
            this.srt.AutoSize = true;
            this.srt.Location = new System.Drawing.Point(20, 112);
            this.srt.Name = "srt";
            this.srt.Size = new System.Drawing.Size(77, 17);
            this.srt.TabIndex = 4;
            this.srt.TabStop = true;
            this.srt.Text = "Sort the list";
            this.srt.UseVisualStyleBackColor = true;
            this.srt.CheckedChanged += new System.EventHandler(this.RadioButtons_CheckedChanged);
            // 
            // searchOffice
            // 
            this.searchOffice.AutoSize = true;
            this.searchOffice.Location = new System.Drawing.Point(20, 88);
            this.searchOffice.Name = "searchOffice";
            this.searchOffice.Size = new System.Drawing.Size(160, 17);
            this.searchOffice.TabIndex = 3;
            this.searchOffice.TabStop = true;
            this.searchOffice.Text = "Search for an Office Number";
            this.searchOffice.UseVisualStyleBackColor = true;
            this.searchOffice.CheckedChanged += new System.EventHandler(this.RadioButtons_CheckedChanged);
            // 
            // searchName
            // 
            this.searchName.AutoSize = true;
            this.searchName.Location = new System.Drawing.Point(20, 65);
            this.searchName.Name = "searchName";
            this.searchName.Size = new System.Drawing.Size(112, 17);
            this.searchName.TabIndex = 2;
            this.searchName.TabStop = true;
            this.searchName.Text = "Search for a name";
            this.searchName.UseVisualStyleBackColor = true;
            this.searchName.CheckedChanged += new System.EventHandler(this.RadioButtons_CheckedChanged);
            // 
            // Add
            // 
            this.Add.AutoSize = true;
            this.Add.Location = new System.Drawing.Point(20, 42);
            this.Add.Name = "Add";
            this.Add.Size = new System.Drawing.Size(86, 17);
            this.Add.TabIndex = 1;
            this.Add.TabStop = true;
            this.Add.Text = "Add an Entry";
            this.Add.UseVisualStyleBackColor = true;
            this.Add.CheckedChanged += new System.EventHandler(this.RadioButtons_CheckedChanged);
            // 
            // Print
            // 
            this.Print.AutoSize = true;
            this.Print.Location = new System.Drawing.Point(20, 19);
            this.Print.Name = "Print";
            this.Print.Size = new System.Drawing.Size(79, 17);
            this.Print.TabIndex = 0;
            this.Print.TabStop = true;
            this.Print.Text = "Print the list";
            this.Print.UseVisualStyleBackColor = true;
            this.Print.CheckedChanged += new System.EventHandler(this.RadioButtons_CheckedChanged);
            // 
            // personName
            // 
            this.personName.Location = new System.Drawing.Point(159, 205);
            this.personName.Name = "personName";
            this.personName.Size = new System.Drawing.Size(100, 20);
            this.personName.TabIndex = 1;
            this.personName.Visible = false;
            // 
            // personOffice
            // 
            this.personOffice.Location = new System.Drawing.Point(159, 255);
            this.personOffice.Name = "personOffice";
            this.personOffice.Size = new System.Drawing.Size(100, 20);
            this.personOffice.TabIndex = 2;
            this.personOffice.Visible = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(31, 205);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(71, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Person Name";
            this.label1.Visible = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(31, 255);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(111, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Person Office Number";
            this.label2.Visible = false;
            // 
            // lstOutput
            // 
            this.lstOutput.FormattingEnabled = true;
            this.lstOutput.Location = new System.Drawing.Point(320, 18);
            this.lstOutput.Name = "lstOutput";
            this.lstOutput.Size = new System.Drawing.Size(206, 160);
            this.lstOutput.TabIndex = 5;
            this.lstOutput.Visible = false;
            this.lstOutput.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(201, 2);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(137, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Names and Office Numbers";
            // 
            // clr
            // 
            this.clr.Location = new System.Drawing.Point(291, 205);
            this.clr.Name = "clr";
            this.clr.Size = new System.Drawing.Size(75, 23);
            this.clr.TabIndex = 7;
            this.clr.Text = "Clear All";
            this.clr.UseVisualStyleBackColor = true;
            this.clr.Click += new System.EventHandler(this.clr_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(556, 382);
            this.Controls.Add(this.clr);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.lstOutput);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.personOffice);
            this.Controls.Add(this.personName);
            this.Controls.Add(this.groupBox1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton Print;
        private System.Windows.Forms.RadioButton qt;
        private System.Windows.Forms.RadioButton srt;
        private System.Windows.Forms.RadioButton searchOffice;
        private System.Windows.Forms.RadioButton searchName;
        private System.Windows.Forms.RadioButton Add;
        private System.Windows.Forms.TextBox personName;
        private System.Windows.Forms.TextBox personOffice;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ListBox lstOutput;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button clr;

    }
}

