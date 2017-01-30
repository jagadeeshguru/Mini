/********************************************************************
Class:     CSCI 473-01
Program:   Assignment 4
Author:    Shyam S N Ammanamanchi, Jagadeesh Guru, Rajeswari Gundu, Aditya Sabbineni . Group 6
Z-number:  z1776539,z1784615,z1784316,z1780715
Date Due:  11/09/2016

Purpose:   This program generates various types of charts based on the user selection


*********************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;

namespace Assign4
{
    public partial class Form1 : Form
    {
        //double list to hold numbers from file
        List<double> numbers = new List<double>();
        
        //counter to keep track of number list
        int fileCounter = 0;

        //random object to generate random double values
        Random random = new Random();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //create default bar chart
            chart.Series.Clear();
            var series1 = new System.Windows.Forms.DataVisualization.Charting.Series
            {
                Name = "Series1",
                Color = System.Drawing.Color.Green,
                IsVisibleInLegend = false,
                IsXValueIndexed = true,
                ChartType = SeriesChartType.Bar
            };

            this.chart.Series.Add(series1);

            series1.Points.AddXY(1, 1);
            series1.Points.AddXY(2, 2);
            series1.Points.AddXY(3, 3);

            //read the files and insert data into List
            using (StreamReader sr = new StreamReader("Numbers.txt"))
            {
                String data;
                while ((data = sr.ReadLine()) != null)
                {
                    numbers.Add(double.Parse(data));
                }
            }

        }

        //clear series from chart
        private void btnClear_Click(object sender, EventArgs e)
        {
            chart.Series["Series1"].Points.Clear();
        }

        //add point from file to series
        private void btnFile_Click(object sender, EventArgs e)
        {
            if (fileCounter < numbers.Count)
            {
                chart.Series["Series1"].Points.AddY(numbers[fileCounter]);
                fileCounter++;
            }
        }

        //exit the application
        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        //take value from user and point to the series
        private void btnUser_Click(object sender, EventArgs e)
        {
            string val = textBox1.Text;
            double convertedVal;
            if (string.IsNullOrEmpty(val))
            {
                MessageBox.Show("Please enter a point");
            }
            else if (double.TryParse(val, out convertedVal))
            {
                textBox1.Text = string.Empty;
                chart.Series["Series1"].Points.AddY(convertedVal);
            }
            else
            {
                MessageBox.Show("Please enter a valid value");
            }
        }

        private void radioPieChart_CheckedChanged(object sender, EventArgs e)
        {
            chart.Series["Series1"].ChartType = SeriesChartType.Pie;
        }

        private void radioBarChart_CheckedChanged(object sender, EventArgs e)
        {
            chart.Series["Series1"].ChartType = SeriesChartType.Bar;
        }

        private void radioColumnChart_CheckedChanged(object sender, EventArgs e)
        {
            chart.Series["Series1"].ChartType = SeriesChartType.Column;
        }

        private void radioDoughnutChart_CheckedChanged(object sender, EventArgs e)
        {
            chart.Series["Series1"].ChartType = SeriesChartType.Doughnut;
        }

        private void btnRandom_Click(object sender, EventArgs e)
        {
            chart.Series["Series1"].Points.AddY((random.NextDouble() * 10));
        }

    }
}
