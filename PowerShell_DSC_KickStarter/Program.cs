using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;//If you can't use this, you should download the Windows SDK and add the reference of System.Management.Automation from C:\Program Files (x86)\Reference Assemblies\Microsoft\WindowsPowerShell\3.0
using System.Text.RegularExpressions;


namespace PowerShell_DSC_KickStarter
{
    class ReadServerBase
    {
        private static string[] server_list_array;
        private static string webrole_present;
        private static string sourcepath;
        private static string destpath;
        private static string outputpath;

        static void Main() //Greeting and display support info
        {
            Console.WriteLine("Welcome, this program is intended for helping system administrators to create mof files which could be used to manage web servers with the desired configuration." +
                " If you need to know how to use this tool please type 'help'.");
            Console.WriteLine("******************************************************************************************************************'.");
            ReadServerlist();
        }
        static void ReadServerlist()//get user input for a list of servers first.
        {
        Read_file_start_line://this section will read user input for the server file and validate the server names.
            Console.WriteLine("Please input a path for the server list text file.");
            string userinput = Console.ReadLine();
            if (userinput == "help")
            {
                Console.WriteLine("This tool is made by Joelan Zhang, and free and opensource for you to use. Please see the following help document related to this tool.\n" +
                    " To prepare a list of servers that you separate each server's name or IPv4 address into each separate line and save it as a txt file.\n" +
                    " If you have invalid server names in the list the program will let you know and you'll need to correct the naming issues before you can start using it again.\n" +
                    " Once the validating process is done, you will be prompted for entering the DSC settings related to webserver server role for these servers.\n" +
                    " Once you are fisnihsed, the program will generate the PowerShell file and then complie the .mof file for you to apply them to the servers.\n" +
                    " More features will be available in the future leases, thank you and hope you have a good day!\n\n");
                goto Read_file_start_line;
            }
            else
            {
                try // try and catch the error input.
                {
                    server_list_array = System.IO.File.ReadAllLines(userinput);
                }
                catch (FileNotFoundException) //file not found exception and restart the input.
                {
                    Console.WriteLine("File not found, please try again.");
                    goto Read_file_start_line;
                }
                catch (IOException) // IO exception and restart the input.
                {
                    Console.WriteLine("IO error, please try again.");
                    goto Read_file_start_line;
                }
                catch (UnauthorizedAccessException) // No file permission error.
                {
                    Console.WriteLine("File permission denied, please verify the permission and maybe start this program with administrative rights would help.");
                    goto Read_file_start_line;
                }
                catch (ArgumentException) // When you enter nothing.
                {
                    Console.WriteLine("Invalid input, please supply a file path.");
                    goto Read_file_start_line;
                }
                catch (Exception)// Last resort, catch'em all!
                {
                    Console.WriteLine("An unhandled exception has occured, please see deatails:");
                    throw;
                }
                ServerListValidating(server_list_array);// Call the ServerListValidating method to validate the server names.
            }

        }
        static void ServerListValidating(Array server_list)// Validating server names.
        {
            Console.WriteLine("Checking the names of server. One moment...");
            string pattern_ipv4 = @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$"; //pattern used for IPv4 addresses.
            string pattern_FQDN_and_DNS_name = @"^([\w*\d*-.])*$";//pattern used for domain names or computer names.
            List<string> bad_server_names = new List<string>();
            int bad_names = 0;
            Regex ipv4_validate = new Regex(pattern_ipv4);
            Regex DNS_validate = new Regex(pattern_FQDN_and_DNS_name);
            foreach (string server_name in server_list)//loop through the list of the server names.
            {
                if (!DNS_validate.IsMatch(server_name) & !ipv4_validate.IsMatch(server_name))
                {
                    bad_server_names.Add(server_name);
                    bad_names = +1;
                }
            }
            if (bad_names > 0)
            {
                Console.WriteLine("The following server names are invalid, please correct them before proceeding.");
                bad_server_names.ForEach(Console.WriteLine);
                Main();
            }
            else
            {
                Console.WriteLine("Looks good, now we are in business.");
                ServerOptionRead(); //Go to the next method to read user prompt for server options.
            }
        }
        static void ServerOptionRead()//Read user input for server options.
        {
        Ensure://Get ensure status of the configuration.
            Console.WriteLine("Do you want the web-server role present on these servers? Default is yes.(YES/NO)");
            string ensureinput = Console.ReadLine();
            if (string.Equals(ensureinput, "yes", StringComparison.OrdinalIgnoreCase) ^ ensureinput == "")
            {
                webrole_present = "Present";
            }
            else if (string.Equals(ensureinput, "no", StringComparison.OrdinalIgnoreCase))
            {
                webrole_present = "Adsent";
            }
            else
            {
                Console.WriteLine("Invalid input, please try agin.");
                goto Ensure;
            }
        SourcePath://The source path for configuration file that will move over to the deployed servers. 
            Console.WriteLine("Where is the source path for the web files? Default is \\\\webconfig\\index.html");
            string sourcepathinput = Console.ReadLine();
            if (sourcepathinput == "")
            {
                sourcepath = "\\\\webconfig\\index.html";
            }
            else if (Path.IsPathRooted(sourcepathinput))
            {
                sourcepath = sourcepathinput;
            }
            else
            {
                Console.WriteLine("Invalid input, please try agin.");
                goto SourcePath;
            }
        DestPath://The destination path for deployed server's web application location.
            Console.WriteLine("Where is the destination path for the web files on the deployed server? Default is C:\\www\\index.html");
            string destpathinput = Console.ReadLine();
            if (destpathinput == "")
            {
                destpath = "C:\\www\\index.html";
            }
            else if (Path.IsPathRooted(destpathinput))
            {
                destpath = destpathinput;
            }
            else
            {
                Console.WriteLine("Invalid input, please try agin.");
                goto DestPath;
            }
        OutputPath://The output file for the generated mod file on local machine.
            Console.WriteLine("Where do you want to keep your .mof files? Default is in the DSC folder of your desktop.");
            string outputpathinput = Console.ReadLine();
            if (outputpathinput == "")
            {
                outputpath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)+"\\DSC";
            }
            else if (Path.IsPathRooted(outputpathinput))//check if the supplied path is rooted or in another word if the path is valid. 
            {
                outputpath = outputpathinput;
            }
            else
            {
                Console.WriteLine("Invalid input, please try agin.");
                goto OutputPath;
            }
            var new_DSC_instance = new powershell();
            new_DSC_instance.Main(server_list_array, webrole_present, sourcepath, destpath, outputpath);//Call the new class which will do the mof magic.
        }

    }
    class powershell//Class for making the mof files.
    {
        public void Main(string[] server_list, string webrole_present, string sourcepath, string destpath, string outputpath)//A method takes 5 parameters for process. 
        {
            PowerShell create_mof = PowerShell.Create();//Create a powershell session
            create_mof.AddCommand("Set-ExecutionPolicy")//make sure the localmachine can execute the following powershell code without problem. This will only work for the current session.
                .AddParameter("ExecutionPolicy", "Unrestricted")
                .AddParameter("scope", "Process")
                .AddParameter("Force")
                .Invoke();
            foreach (string server_name in server_list)//loop through each server and add the customized server options to DSC
            {
                Console.WriteLine("Making .mof file for server: " + server_name);
                create_mof.AddScript("" + //This section does the heavylifting of making the powershell code and execute it at the end. 
                    "Configuration WebServerDSC {\n" +
                        "Import-DscResource -ModuleName PsDesiredStateConfiguration\n" +
                        "Node '" + server_name + "'{\n" +
                            "WindowsFeature WebServer {\n" +
                                "Ensure =  '" + webrole_present + "'\n" +
                                "Name = 'Web-Server'}\n" +
                        "File WebServerFile {\n" +
                                "Ensure =  '" + webrole_present + "'\n" +
                                "SourcePath = '" + sourcepath + "'\n" +
                                "DestinationPath = '" + destpath + "'\n" +
                        "}}}");
                Collection<PSObject> PSOutput = create_mof.Invoke();
                Console.WriteLine("Applying magic and gliters.....");
                create_mof.AddCommand("WebServerDSC")//This is where the configuration is executed and mof file will be generated to the output path supplied. 
                    .AddParameter("OutputPath", outputpath)
                    .Invoke();
                if (create_mof.Streams.Error.Count == 0)
                {
                    Console.WriteLine("Finished!");
                }
            }
            if (create_mof.Streams.Error.Count == 0)
            {
                Console.WriteLine("All Done! Press any key to exit.");
            }
        }
    }
}
