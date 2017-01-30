/********************************************************************
Class:     CSCI 473-01
Program:   Assignment 2
Author:    Shyam S N Ammanamanchi, Jagadeesh Guru, Rajeswari Gundu, Aditya Sabbineni
Z-number:  z1776539,z1784615,z1784316,z1780715
Date Due:  09/16/2016

Purpose:   This programs reads data from the data file and prints, searches,adds,sorts the data in a form .


*********************************************************************/



using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;
namespace Assign2
{
    public partial class Form1 : Form
    {
        List<Person> person = new List<Person>();
      
        public Form1()
        {
            InitializeComponent();
        }

        private void RadioButtons_CheckedChanged(object sender, EventArgs e)            // if any of the radio button is checked 
        {
            if (Print.Checked)                          // if print is checked 
            {
                invisibleControl();
                lstOutput.Visible = true;
                lstOutput.Items.Clear();
                foreach(Person p in person)                 // printing the lsit 
                {
                    lstOutput.Items.Add(p.Name+"\t\t"+p.OfficeNumber);
          
                }
            }
            if (Add.Checked)                        // IF add is checked 
            {
                invisibleControl();
                String s = personName.Text;            // getting the name from personname text box 
                String o = personOffice.Text;           // gettting the offic number from person office text box
                int i = -1;

                for (int j = 0; j < person.Count; j++)      // if name already exists then donot add 
                {
                    if (person[j].Name.Equals(s))
                    {
                        i = j;
                        break;
                    }

                }
                if (!String.IsNullOrEmpty(s) && !String.IsNullOrEmpty(o) )      // if name is null or if office number is null 
                {
                    if (i < 0)
                    {
                        person.Add(new Person(s, o));           // adding name and office number to person 
                    }

                    foreach (Person p in person)
                    {
                        lstOutput.Items.Add(p.Name + "\t\t" + p.OfficeNumber);              // printing out the output in the list

                    }
                }
                else
                {
                    lstOutput.Items.Add("Please enter name and office\n number");
                }
            }
            if (srt.Checked )               // if sort is checked 
            {
               invisibleControl();
               person.Sort();               // sorting the list 
               foreach (Person p in person)             // printing the list in list item
               {    
                   lstOutput.Items.Add(p.Name + "\t\t" + p.OfficeNumber);

               }
            }
            if(searchName.Checked && !String.IsNullOrEmpty(personName.Text))                    // if search name is checked
            {
                invisibleControl();
                personName.Visible = label1.Visible = true;
                string s = personName.Text;
                    int i = -1;
                    for (int j = 0; j < person.Count; j++)
                    {
                        if (person[j].Name.Equals(s))               // if name is in the person list 
                        {
                            i = j;
                            break;
                        }

                    }
                    if (i < 0)
                    {
                        lstOutput.Visible = true;
                        lstOutput.Items.Add(s + " not found");                  // if name is found
                    }
                    else
                    {
                        lstOutput.Items.Add(person[i].Name + "\t\t" + person[i].OfficeNumber);          // if the name is found then print it in the list box
                    }
                personName.Clear();                     // clearing the person name text box
            }
            if(searchOffice.Checked && !String.IsNullOrEmpty(personOffice.Text))                // if search office is checked 
            {
                invisibleControl();
                personName.Visible = label1.Visible = true;
                string s = personOffice.Text;
                    int i = -1;                            
                    for (int j = 0; j < person.Count; j++)
                    {
                        if (person[j].OfficeNumber.Equals(s))           // if the office number is found in the list
                        {
                            i = j;
                            break;
                        }

                    }
                    if (i < 0)
                    {
                        lstOutput.Visible = true;                   // if searched item is not found 
                        lstOutput.Items.Add(s + " not found");
                    }
                    else
                    {
                        lstOutput.Items.Add(person[i].Name + "\t\t" + person[i].OfficeNumber);          // if searched item is found it will be printed to output list 
                    }
                personOffice.Clear();                   // clearing the text box after search is done
            }

            if(qt.Checked)                  // quit is checked 
            {
                Application.Exit();         // application will close
            }

        }

        private void invisibleControl()             // for the visibility of the labels text boxes and list output 
        {
            lstOutput.Visible = true;                   // list output visibility 
            label1.Visible = personName.Visible = personOffice.Visible = label2.Visible = true;     // visibility of labels and text boxes 
            lstOutput.Items.Clear();                    // to clear the list output 
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadList(person);               // to add data to the list Person 
        }

private void LoadList(System.Collections.Generic.List<Person> person)
{
 	//throw new NotImplementedException();
    using (StreamReader SR = new StreamReader("data1.txt"))     // stream reader to read each line from data file 
            {
                String s;
                String o;
                s = SR.ReadLine();                      // reading name
                o = SR.ReadLine();                        // reading office number
                while (s != null && o != null)          // checking wheather they are empty or not
                {
                    Person p = new Person(s,o);
                    person.Add(p);
                    s = SR.ReadLine();                      // getting the next name and office number
                    o = SR.ReadLine();
                }
            }

}

private void clr_Click(object sender, EventArgs e)
{
    personName.Text = string.Empty;
    personOffice.Text = string.Empty;
    lstOutput.Items.Clear();

}
    }


}
class Person : IComparable  // Person class with data members pName and pOffice Number and extends Icomparabele 
{
    private string pName;               // Person Name
    private string pOfficeNumber;        // Person Office Number
    public string Name                      // Property for Person Name
    {
        get { return pName; }               // getters and setters
        set { pName = value; }
    }
    public string OfficeNumber                  // Property for person office number
    {
        get { return pOfficeNumber; }               // setters and getters
        set { pOfficeNumber = value; }
    }
    public bool checkName(string s)                 //compares the name passed in the arguments with name in the object 
    {                                               //and returns true or false depending on the whether the name matches or not 
        return Name.ToLower().Equals(s.ToLower());
    }
    public bool checkOffice(string s)               //compares the office number passed in the arguments with office number in the object 
    {                                                //and returns true or false depending on the whether the office number matches or not 
        return OfficeNumber.ToLower().Equals(s.ToLower());
    }
    public Person(String n, String o)               // Constructor for Person class. to give values to pName and pOfficeNumber
    {
        pName = n;
        pOfficeNumber = o;
    }
    public void print()                                 // to Print out the objects name and office number
    {
        Console.WriteLine(" {0,-20}     {1,-5}", Name, OfficeNumber);
    }
    public int CompareTo(object obj)                // this method is used by sort(Array) to sort names 
    {

        Person oPerson = obj as Person;

        // return Name.CompareTo(oPerson.Name);
        int i = String.Compare(pName, oPerson.Name, true);
        //Console.WriteLine(i);
        if (i == 0)                     // if the string matches 
        {
            return 0;
        }
        if (i > 0)                         // if the string is bigger
        {
            return 1;
        }
        if (i < 0)                          // if the string is smaller
        {
            return -1;
        }
        return 1;
    }
}

