/********************************************************************
Class:     CSCI 473-01
Program:   Assignment 2
Author:    Shyam S N Ammanamanchi, Jagadeesh Guru, Rajeswari Gundu, Aditya Sabbineni
Z-number:  z1776539,z1784615,z1784316,z1780715
Date Due:  09/27/2016

Purpose:   This programs reads data from the data file and prints, searches,adds,sorts the data in a form .


*********************************************************************/



using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
namespace Assign2
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
