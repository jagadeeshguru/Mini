/********************************************************************
Class:     CSCI 473-01
Program:   Assignment 1
Author:    Shyam S N Ammanamanchi, Jagadeesh Guru, Rajeswari Gundu, Aditya Sabbineni
Z-number:  z1776539,z1784615,z1784316,z1780715
Date Due:  09/16/2016

Purpose:   This programs reads data from the data file and prints, searches,adds,sorts the data.


*********************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
namespace Assign1
{
    
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
            Console.WriteLine(" {0,-20}     {1,-5}",Name,OfficeNumber);
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

    class Program
    {
        public static int InUse = 0 ;               // to keep track of how many persons are there
        public static Person[] p = new Person[20];     // declaring an array of person datatype 
        public void readData()                          // to read the data form the file into person array
        {
            using (StreamReader SR = new StreamReader("data1.txt"))     // stream reader to read each line from data file 
            {
                String s;
                String o;
                s = SR.ReadLine();                      // reading name
                o = SR.ReadLine();                        // reading office number
                while (s != null && o != null)          // checking wheather they are empty or not
                {
                    //Console.WriteLine(s);
                    //Console.WriteLine(o);
                    p[InUse] = new Person(s,o);           // calling the constructor 
                    s = SR.ReadLine();                      // getting the next name and office number
                    o = SR.ReadLine();
                    InUse++;                                // incrementing InUse to keep track of number of persons
                }
            }
        }
        static void Main(string[] args)
        {
            String chc;                                 // declaring a variable to get the option from user
            var r = new Program();                         // declaring and object r of Program data type 
            r.readData();                                  // reading the data from the file 
            do{                                            // do while loop to get a menu driven program.
          
            Console.WriteLine("Choose: ");                  // listing out the options 
            Console.WriteLine("A. Print the List");
            Console.WriteLine("B. Add an Entry");
            Console.WriteLine("C. Search for a Name");
            Console.WriteLine("D. Search for an Office Number");
            Console.WriteLine("E. Sort the List");
            Console.WriteLine("F. Quit");
            chc = Console.ReadLine();                       // taking data from user
            switch (chc)                                // using switch
            {
                case "A":
                case "a":
                    {
                        Console.WriteLine(" Name                Office Number");
                        for (int i = 0; i < InUse; i++)                     // printing all person name and office number
                        {
                            p[i].print();
                        }
                            break;
                    }
                case "B":
                case "b":
                    {
                        String n, o;
                        Console.Write("Please enter the Person's Name: ");   // prompting user to enter name and office number 
                        n = Console.ReadLine();                                // and reading the data 
                        Console.Write("Please enter the Office Number: ");
                        o = Console.ReadLine();
                        p[InUse] = new Person(n,o);                                 // calling the constructor 
                        InUse++;
                        Console.WriteLine("{0} has been added to the list",n);          // conformation message back to user
                        break;
                    }
                case "C":
                case "c":
                    {
                        String n;
                        Console.Write("Enter the name to be searched: ");               // searching for a name 
                        n = Console.ReadLine();
                        int flag = 0;
                        for (int i = 0; i < InUse; i++)                     // checking weather the name exists or not
                        {
                            if (p[i].checkName(n))
                            {
                                Console.WriteLine("{0}      {1}",p[i].Name,p[i].OfficeNumber );             // printing out the name if the name is found
                                flag = 1;
                            }
                        }
                        if (flag == 0)                  // if the name is not found
                        {
                            Console.WriteLine("The name {0} was not found ",n);
                        }
                            break;
                    }
                case "D":
                case "d":
                    {
                        string n;               // searching office 
                        Console.Write("Enter the office number to be searched: ");
                        n = Console.ReadLine();
                        int flag = 0;
                        for (int i = 0; i < InUse; i++)
                        {
                            if (p[i].checkOffice(n))            // checking the existance of office number
                            {
                                Console.WriteLine("{0}      {1}",p[i].Name,p[i].OfficeNumber );
                                flag = 1;
                            }
                        }
                        if (flag == 0)                  // if the office numebr does not exist
                        {
                            Console.WriteLine("Office number {0} was not found ",n);
                        }
                      
                        break;
                    }
                case "E":
                case "e":
                    {
                        Array.Sort(p,0,InUse);          // to sort the array 
                        break;
                    }
                case "F":
                case "f":                                       // to exit menu driven program
                    Console.WriteLine("Exiting the Program.");
                    break;
                default:                                        // default case
                    Console.WriteLine("That was not a choice.");
                    break;
            } 
            Console.WriteLine();
            Console.WriteLine();
            }while(!chc.Equals("f") && !chc.Equals("F"));           // end of do while 
            Console.WriteLine("Press Enter to exit");                     
            Console.ReadLine();
        }
    }
}
