﻿using SharpUp.Classes;
using System;
using static SharpUp.Utilities.RegistryUtils;
using System.Diagnostics;
using System.Collections.Generic;
using static SharpUp.Utilities.FileUtils;
using System.Security.Principal;
using static SharpUp.Native.Win32;

namespace SharpUp.Checks
{
    public class ProcessDLLHijack : VulnerabilityCheck
	{
        private static string _regPath = "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\KnownDlls";
        
        // For future use
        private static string GetProcessUser(Process process)
        {
            IntPtr processHandle = IntPtr.Zero;
            try
            {
                OpenProcessToken(process.Handle, 8, out processHandle);
                WindowsIdentity wi = new WindowsIdentity(processHandle);
                string user = wi.Name;
                return user.Contains(@"\") ? user.Substring(user.IndexOf(@"\") + 1) : user;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (processHandle != IntPtr.Zero)
                {
                    CloseHandle(processHandle);
                }
            }
        }

        public ProcessDLLHijack()
		{
            // TODO: Take argements and add them to an array for scoping
            //string[] options = args;

            // Registry key where known DLLs are listed
            string[] keyname = GetRegSubkeys("HKLM", _regPath);


            // Create List for DLL values
            List<string> Dlls = new List<string>();

            //Get the value for each of the values and add it to the list
            foreach (string valuename in keyname)
            {
                string dllname = valuename.ToLower();
                Dlls.Add(dllname);
            }

            // Get all running processes
            Process[] processes = Process.GetProcesses();

            foreach (Process process in processes)
            {
                // Try to check the modules loaded for the process
                try
                {
                    Console.WriteLine("[+] Checking modules for {0}", process.ProcessName);
                    var processmodules = process.Modules;

                    // Go through each module loaded in the process
                    foreach (ProcessModule module in processmodules)
                    {
                        string modules = module.ModuleName;
                        modules = modules.ToLower();
                        string filepath = module.FileName.ToLower();

                        // Exclude items that do not end with .dll, exclude known dlls, exclude items in c:\\windows\\system32
                        if (module.FileName.EndsWith(".dll") && !Dlls.Contains(modules) && !filepath.Contains("c:\\windows"))
                        {
                            // Final output for full path to DLLs that meet the parameters
                            Console.WriteLine("[+] Hijackable DLL: {0} ", module.FileName.ToString());                            
                        }
                    }
                }
                catch
                {
                    // Output for when the current user doesn't have permissions for a process
                    Console.WriteLine("[-] Access denied for {0} under PID {1}", process.ProcessName.ToString(), process.Id.ToString());
                }
            }
        }
    }
}
