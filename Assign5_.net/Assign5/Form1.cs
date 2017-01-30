/********************************************************************
Class:     CSCI 473-01
Program:   Assignment 5
Author:    Shyam S N Ammanamanchi, Jagadeesh Guru, Rajeswari Gundu, Aditya Sabbineni . Group 6
Z-number:  z1776539,z1784615,z1784316,z1780715
Date Due:  12/02/2016

Purpose:   This program make the user to guess a color. Initially a color wil be displyed to the user in a rectangle. They can enter colors using
 *         the provided dropdowns. Entered color and appropriate message will be displyed.

*********************************************************************/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Assign5
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private PictureBox pictureBox1 = new PictureBox();
        private PictureBox pictureBox2 = new PictureBox();
        private int rColor;
        private int gColor;
        private int bColor;

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;
            // Dock the PictureBox to the form and set its background to random coloR.
            pictureBox1.Size = new Size(122,50);
            pictureBox1.Location = new Point(95, 80);
            pictureBox1.Dock = DockStyle.None;
            Random rnd = new Random();
            rColor = rnd.Next(0, 256);
            gColor = rnd.Next(0, 256);
            bColor = rnd.Next(0, 256);

            Console.WriteLine("r = " + rColor + " g = " + gColor + " b = " + bColor);
            pictureBox1.BackColor = Color.FromArgb(rColor, gColor, bColor); 
            // Connect the Paint event of the PictureBox to the event handler method.
            pictureBox1.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBox1_Paint);

            // Add the PictureBox control to the Form.
            this.Controls.Add(pictureBox1);

        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Retrieve the drop down list values of rgb
            int r = Int32.Parse(comboBox1.Text);
            int g = Int32.Parse(comboBox2.Text);
            int b = Int32.Parse(comboBox3.Text);

            //Error range checking for color
            if ((r < 0) || (r > 255) || (g < 0) || (g > 255) || (b < 0) || (b > 255))
            {
                MessageBox.Show("Select appropriate RGB Values");
                comboBox1.SelectedIndex = 0;
                comboBox2.SelectedIndex = 0;
                comboBox3.SelectedIndex = 0;
                return;
            }

            //Create a new picture box 
            pictureBox2.Size = new Size(122, 50);
            pictureBox2.Location = new Point(425, 80);
            pictureBox2.Dock = DockStyle.None;
            pictureBox2.BackColor = Color.FromArgb(r, g, b); 
            bool flag = false;
            label9.Refresh();
            
            // Connect the Paint event of the PictureBox to the event handler method.
            pictureBox2.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBox2_Paint);

            //Check for + and - 10 of the rgb values for the color
            if ((r - 10 >= rColor) && (r + 10 <= rColor))
                if ((g - 10 >= gColor) && (g + 10 <= gColor))
                    if ((b - 10 >= bColor) && (b + 10 <= bColor))
                    {
                        label9.Text = "Awesome! You set a similar Color!";
                        flag = true;
                    }
                    
            if(flag == false)   label9.Text = "Oops! Try once again!";
            
            // Add the PictureBox control to the Form.
            this.Controls.Add(pictureBox2);
         
        }

        private void pictureBox1_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            // Create a local version of the graphics object for the PictureBox.
            Graphics g = e.Graphics;

            // Draw a string on the PictureBox.
            //g.DrawString("This is a diagonal line drawn on the control",
            //    new Font("Arial", 10), System.Drawing.Brushes.Blue, new Point(30, 30));
            // Draw a line in the PictureBox.
            g.DrawRectangle(System.Drawing.Pens.Black, pictureBox1.Left, pictureBox1.Top,
                10, 10);
           
        }

        private void pictureBox2_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            // Create a local version of the graphics object for the PictureBox.
            Graphics g = e.Graphics;

            // Draw a string on the PictureBox.
            //g.DrawString("This is a diagonal line drawn on the control",
            //    new Font("Arial", 10), System.Drawing.Brushes.Blue, new Point(30, 30));
            // Draw a line in the PictureBox.
            g.DrawRectangle(System.Drawing.Pens.Black, pictureBox1.Left, pictureBox1.Top,
                10, 10);

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
           // cbxCategory.SelectedIndex = 2;
        }

        
    }
}
