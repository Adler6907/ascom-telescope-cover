// This is a console application that can be used to test an ASCOM driver

// #define UseChooser

using ASCOM.DriverAccess;

using ASCOM.DeviceInterface;
//using ASCOM.Utilities;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.ComponentModel;
using System.Runtime.ConstrainedExecution;
using System.Security.Policy;

namespace ASCOM
{
    internal class Program
    {
        static string TelescopeName = "ALL|TelescopeName1|TelescopeName2|...\t(given different names in the firmware)";
        static string TelescopeName1 = "TelescopeName1";
        static short numSwitch;
        static bool LanguageGerman, verbose, silent, UseChooser, allOpen, allClose, allState = false;
        static short CountOpen, CountClose, CountState = 0;
        static string[] ArrayOpen, ArrayClose, ArrayState;
        static ASCOM.DriverAccess.Switch device;
        static string progID = "ASCOM.TelescopeCovers.Switch";

        private const string COMMAND_OPEN = "OPEN";
        private const string COMMAND_CLOSE = "CLOSE";
        private const string COMMAND_STATE = "STATE";

        static void Main(string[] args)
        {
            short i;
            bool help = false;

            LanguageGerman = System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName == "de";

            if (args.Length > 0)
            {
                for (i = 0; i < args.Length; i++)
                {
                    help =               args[i].ToUpper() == "/HELP" ||
                                         args[i] == "/h" ||
                                         args[i] == "/?" ||
                                         args[i] == "?";
                    if (help) break;

                    if (!Program.verbose)
                    { 
                        Program.verbose =    args[i].ToUpper() == "/VERBOSE" ||
                                             args[i].ToUpper() == "/V";
                    }
                    if (!Program.silent)
                    {
                        Program.silent =     args[i].ToUpper() == "/SILENT" ||
                                             args[i].ToUpper() == "/S";
                    }
                    if (!Program.UseChooser)
                    {
                        Program.UseChooser = args[i].ToUpper() == "/C" ||
                                             args[i].ToUpper() == "/USECHOOSER" ||
                                             args[i].ToUpper() == "/CHOOSER" ||
                                             args[i].ToUpper() == "/CHOOSE";
                    }

                    if (args[i].Contains(":"))
                    {
                        if (args[i].Split(':')[1].ToUpper() == COMMAND_OPEN)
                        {
                            Program.CountOpen++;
                            if (!allOpen)
                                allOpen = args[i].Split(':')[0].ToUpper().Replace("/","") == "ALL";
                        }
                        if (args[i].Split(':')[1].ToUpper() == COMMAND_CLOSE)
                        {
                            Program.CountClose++;
                            if (!allClose) 
                                allClose = args[i].Split(':')[0].ToUpper().Replace("/", "") == "ALL";
                        }
                        if (args[i].Split(':')[1].ToUpper() == COMMAND_STATE)
                        {
                            Program.CountState++;
                            if (!allState) 
                                allState = args[i].Split(':')[0].ToUpper().Replace("/", "") == "ALL";
                        }
                    }
                }

                short j, k = 0;
                if (!allOpen && CountOpen > 0)
                {
                    Program.ArrayOpen = new string[CountOpen];
                    for (i = 0; i < CountOpen; i++)
                    {
                        for (j=k; j < args.Length; j++)
                        {
                            k++;
                            if (args[j].Contains(":") && args[j].Split(':')[1].ToUpper() == COMMAND_OPEN)
                            {
                                ArrayOpen[i] = args[j].Split(':')[0]; // read TelescopeName
                                //Console.WriteLine($"Name {i} für OPEN \t: {args[j].Split(':')[0]}");
                                break;
                            }
                        }
                    }
                }

                if (!allClose && CountClose > 0)
                {
                    Program.ArrayClose = new string[CountClose];
                    j = 0;
                    k = 0;
                    for (i = 0; i < CountClose; i++)
                    {
                        for (j = k; j < args.Length; j++)
                        {
                            k++;
                            if (args[j].Contains(":") && args[j].Split(':')[1].ToUpper() == COMMAND_CLOSE)
                            {
                                ArrayClose[i] = args[j].Split(':')[0];
                                //Console.WriteLine($"Name {i} für CLOSE\t: {args[j].Split(':')[0]}");
                                break;
                            }
                        }
                    }
                }

                if (!allState && CountState > 0)
                {
                    Program.ArrayState = new string[CountState];
                    j = 0;
                    k = 0;
                    for (i = 0; i < CountState; i++)
                    {
                        for (j = k; j < args.Length; j++)
                        {
                            k++;
                            if (args[j].Contains(":") && args[j].Split(':')[1].ToUpper() == COMMAND_STATE)
                            {
                                ArrayState[i] = args[j].Split(':')[0];
                                //Console.WriteLine($"Name {i} für STATE\t: {args[j].Split(':')[0]}");
                                break;
                            }
                        }
                    }
                }

                if (Program.verbose)
                {
                    Console.WriteLine("\nParameter List\t:");
                    for (i = 0; i < args.Length; i++)
                    {
                        Console.WriteLine($"Parameter({i})\t: {args[i]}");
                    }
                    Console.WriteLine();
                }

                if (!help)
                {
                    if (Connect())
                    {
                        if (CountOpen > 0) CommandOpen();
                        if (CountClose > 0) CommandClose();
                        if (CountState > 0) CommandState();
                        // Disconnect from the device

                        device.Connected = false;
                        //Console.WriteLine("Press Enter to finish");
                        //Console.ReadLine();
                    }
                    else 
                    { 
                        noConnection(); 
                    }
                }
                else 
                {
                    Parameters();
                }
            }
            else
            {
                //necessary parameters are missing

                //CultureInfo oCI = System.Threading.Thread.CurrentThread.CurrentUICulture; // = "de-DE"
                //Console.WriteLine($"{System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName}");

                Console.ForegroundColor = ConsoleColor.Red;
                if (LanguageGerman)
                {
                    Console.WriteLine($"\n----- Notwendige Parameter fehlen! -----");
                }
                else
                {
                    Console.WriteLine($"\n----- Required parameters are missing! -----");
                }
                //Console.ForegroundColor = ConsoleColor.Gray;
                Console.ResetColor();
                Parameters();
            }
        }

        static bool Connect()
        {
            if (progID != "")
            {
                if (Program.UseChooser)
                {
                    progID = ASCOM.DriverAccess.Switch.Choose("ASCOM.TelescopeCovers.Switch");

                    // Exit if no device was selected
                    if (string.IsNullOrEmpty(progID))
                    {
                        if (LanguageGerman)
                        {
                            Console.WriteLine("Es wurde kein Gerät ausgewählt. Das Programm wird beendet.");
                        }
                        else
                        { 
                            Console.WriteLine("No device was selected. The program will end."); 
                        }
                        //Console.ReadLine();
                        return false;
                    }
                }
                device = new ASCOM.DriverAccess.Switch(progID);
                // Connect to the device
                device.Connected = true;
                Program.numSwitch = device.MaxSwitch;
                if (verbose)
                {
                    Console.WriteLine($"ASCOM-DeviceID  : {progID}");

                    // Now exercise some calls that are common to all drivers.
                    Console.WriteLine($"Name            : {device.Name}");
                    Console.WriteLine($"Description     : {device.Description}");
                    Console.WriteLine($"DriverInfo      : {device.DriverInfo}");
                    Console.WriteLine($"DriverVersion   : {device.DriverVersion}");
                    Console.WriteLine($"InterfaceVersion: {device.InterfaceVersion}");
                    Console.WriteLine($"MaxSwitch       : {Program.numSwitch}");
                    Console.WriteLine($"-------------------------------------------------");

                    // dynamic parameter output
                    if (Program.numSwitch > 0)
                    {
                        TelescopeName = "ALL|";
                        for (short i = 0; i < Program.numSwitch; i++)
                        {
                            TelescopeName = TelescopeName + $"{device.GetSwitchName(i)}" + "|";
                            Console.WriteLine($"\nSwitchName({i})\t\t: {device.GetSwitchName(i)}");
                            Console.WriteLine($"SwitchDescription({i})\t: {device.GetSwitchDescription(i)}\n");
                        }
                        Console.WriteLine($"-------------------------------------------------");
                        TelescopeName = TelescopeName + "...\t(given different names in the firmware)";

                        TelescopeName1 = device.GetSwitchName(0);
                    }
                    Parameters();
                }
                return Program.numSwitch > 0;
            }
            else
            {
                noConnection();
                return false;
            }
        }
        static void Parameters()
        {
            Console.WriteLine($"\nSyntax        :: TelescopeCovers.exe Command1 [Command2] ... [Option1-3]|/h");
            Console.WriteLine($"\nCommandNr     :: [TelescopeName]:[Command]");
            Console.WriteLine($"TelescopeName :: {Program.TelescopeName}");
            Console.WriteLine($"Command       :: OPEN|CLOSE|STATE\n");
            Console.WriteLine($"Option1       :: /v|/VERBOSE");
            Console.WriteLine($"Option2       :: /c|/USERCHOOSER|/CHOOSER|/CHOOSE");
            Console.WriteLine($"Option3       :: /s|/SILENT\n");
            Console.WriteLine($"Example1      :: TelescopeCovers.exe {Program.TelescopeName1}:OPEN (opens the telescope \"{Program.TelescopeName1}\")");
            Console.WriteLine($"Example2      :: TelescopeCovers.exe ALL:CLOSE (closes all telescope)");
            Console.WriteLine($"Example3      :: TelescopeCovers.exe ALL:OPEN /v (Verbose Mode)");
            Console.WriteLine($"Example4      :: TelescopeCovers.exe ALL:OPEN /c (use ASCOM Chooser, default \"ASCOM.TelescopeCovers.Switch\")");
            Console.WriteLine($"Example5      :: TelescopeCovers.exe /h (Help - all other parameters are ignored)\n");
            Console.WriteLine($"-------------------------------------------------");
        }

        static void noConnection()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            if (LanguageGerman)
            {
                Console.WriteLine("Keine Verbindung aufgebaut. Programm wird beendet.");
            }
            else
            {
                Console.WriteLine("No Connection establish. Program is terminated.");
            }
            Console.ResetColor();
        }
        static void noTelescopeName(short i)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            if (LanguageGerman)
            {
                Console.WriteLine($"Fehler:\tDas Teleskop mit dem Namen {ArrayOpen[i]} wurde nicht gefunden. Die Abdeckung konnte nicht geöffnet werden!");
            }
            else
            {
                Console.WriteLine($"Error:\tTelescope with the name {ArrayOpen[i]} not found. cover cannot be opened.");
            }
            Console.ResetColor();
        }
        static void CommandOpen()
        {
            if (allOpen) 
            { // open cover for all telescopes
                for (short i = 0; i < Program.numSwitch; i++)
                {
                    device.SetSwitch(i, true);
                    if (!Program.silent)
                    {
                        if (LanguageGerman)
                        {
                            Console.WriteLine($"Die Abdeckung vom Teleskop \"{device.GetSwitchName(i)}\" wurde geöffnet.");
                        }
                        else
                        {
                            Console.WriteLine($"Telecsope \"{device.GetSwitchName(i)}\" cover has been opened.");
                        }
                    }
                }
            }
            else
            { // open cover for specific telescopes
                for (short i = 0; i < CountOpen; i++)
                {
                    short j;
                    for (j = 0; j < Program.numSwitch; j++)
                    {
                        if (device.GetSwitchName(j) == ArrayOpen[i])
                        {
                            device.SetSwitch(j, true);
                            if (!Program.silent)
                            {
                                if (LanguageGerman)
                                {
                                    Console.WriteLine($"Die Abdeckung vom Teleskop \"{ArrayOpen[i]}\" wurde geöffnet.");
                                }
                                else
                                {
                                    Console.WriteLine($"Telecsope \"{ArrayOpen[i]}\" cover has been opened.");
                                }
                            }
                            break;
                        }
                    }
                    if (j == Program.numSwitch)
                    {
                        noTelescopeName(i);
                    }

                }
            }
        }
        static void CommandClose()
        {
            if (allClose)
            { // Close cover for all telescopes
                for (short i = 0; i < Program.numSwitch; i++)
                {
                    device.SetSwitch(i, false);
                    if (!Program.silent)
                    {
                        if (LanguageGerman)
                        {
                            Console.WriteLine($"Die Abdeckung vom Teleskop \"{device.GetSwitchName(i)}\" wurde geschlossen.");
                        }
                        else
                        {
                            Console.WriteLine($"Telecsope \"{device.GetSwitchName(i)}\" cover has been closed.");
                        }
                    }
                }
            }
            else
            { // Close cover for specific telescopes
                for (short i = 0; i < CountClose; i++)
                {
                    short j;
                    for (j = 0; j < Program.numSwitch; j++)
                    {
                        if (device.GetSwitchName(j) == ArrayClose[i])
                        {
                            device.SetSwitch(j, false);
                            if (!Program.silent)
                            {
                                if (LanguageGerman)
                                {
                                    Console.WriteLine($"Die Abdeckung vom Teleskop \"{ArrayClose[i]}\" wurde geschlossen.");
                                }
                                else
                                {
                                    Console.WriteLine($"Telecsope \"{ArrayClose[i]}\" cover has been closed.");
                                }
                            }
                            break;
                        }
                    }
                    if (j == Program.numSwitch)
                    {
                        noTelescopeName(i);
                    }

                }
            }
        }

        static void CommandState()
        {
            if (allState)
            { // State cover for all telescopes
                for (short i = 0; i < Program.numSwitch; i++)
                {

                    if (device.GetSwitch(i))
                    {
                        if (!Program.silent)
                        {
                            if (LanguageGerman)
                            {
                                Console.WriteLine($"Der Status vom Teleskop \"{device.GetSwitchName(i)}\" ist geöffnet.");
                            }
                            else
                            {
                                Console.WriteLine($"The status of telescope \"{device.GetSwitchName(i)}\" cover it is open.");
                            }
                        }
                    }
                    else
                    {
                        if (!Program.silent)
                        {
                            if (LanguageGerman)
                            {
                                Console.WriteLine($"Der Status vom Teleskop \"{device.GetSwitchName(i)}\" ist geschlossen.");
                            }
                            else
                            {
                                Console.WriteLine($"The status of telescope \"{device.GetSwitchName(i)}\" cover it is closed.");
                            }
                        }
                    }
                        
                }
            }
            else
            { // State cover for specific telescopes
                for (short i = 0; i < CountState; i++)
                {
                    short j;
                    for (j = 0; j < Program.numSwitch; j++)
                    {
                        if (device.GetSwitchName(j) == ArrayState[i])
                        {
                            if (device.GetSwitch(j))
                            {
                                if (!Program.silent)
                                {
                                    if (LanguageGerman)
                                    {
                                        Console.WriteLine($"Der Status vom Teleskop \"{ArrayState[i]}\" ist geöffnet.");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"The status of telescope \"{ArrayState[i]}\" cover it is open.");
                                    }
                                }
                            }
                            else
                            {
                                if (!Program.silent)
                                {
                                    if (LanguageGerman)
                                    {
                                        Console.WriteLine($"Der Status vom Teleskop \"{ArrayState[i]}\" ist geschlossen.");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"The status of telescope \"{ArrayState[i]}\" cover it is closed.");
                                    }
                                }
                            }
                            break;
                        }
                        
                    }
                    if (j == Program.numSwitch)
                    {
                        noTelescopeName(i);
                    }

                }
            }
        }
    }
}
