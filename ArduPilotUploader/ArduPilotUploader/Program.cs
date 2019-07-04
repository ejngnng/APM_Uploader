using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ArduPilotUploader
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "ArduPilotUploader";
            Console.CursorVisible = false;
            Console.WriteLine(@"|**********************************************************|");
            Console.WriteLine(@"|****************    ArduPilot Uploader    ****************|");
            Console.WriteLine(@"|****************    Author:   ninja       ****************|");
            Console.WriteLine(@"|****************    Version:  V1.0.0      ****************|");
            Console.WriteLine(@"|****************    Build:    2019-07-04  ****************|");
            Console.WriteLine(@"|**********************************************************|");
            Console.WriteLine(@"Usage: ");
            Console.WriteLine(@"      1. Click ArduPilotUploader.exe");
            Console.WriteLine(@"      2. Drag the firmware into console");
            Console.WriteLine(@"      3. Reconnect USB");
            Console.WriteLine(@"============================================================");
            if (args.Length == 0)
            {
                Console.WriteLine(@"Please drag the firmware to here...");
                string input = Console.ReadLine();
                if(input != null)
                {
                    bool rs = false;
                    rs = Uploader(input);
                    if (rs)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("upload success...");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("upload failed...");
                        Console.ResetColor();
                    }
                    Console.WriteLine("press any key to exit...");
                    Console.ReadLine();
                }
            }
            else
            {
                Uploader(args[0]);
            }

           // Console.WriteLine(args[0]);


        }


        public static bool Uploader(string fn)
        {
            APMFirmware fw;
            APMUploader up;

            fw = APMFirmware.ProcessFirmware(fn);

            Console.WriteLine("Loaded firmware for {0},{1} waiting for the bootloader...", fw.board_id, fw.board_revision);

            while (true)
            {
                string[] ports = GetPortNames();

                //ports = new string[] { "COM9"};

                foreach (string port in ports)
                {

                    if (!port.StartsWith("COM") && !port.Contains("APM") && !port.Contains("ACM") && !port.Contains("usb"))
                        continue;

                    Console.WriteLine(DateTime.Now.Millisecond + " Trying Port " + port);

                    try
                    {
                        up = new APMUploader(port, 115200);
                    }
                    catch (Exception ex)
                    {
                        //System.Threading.Thread.Sleep(50);
                        Console.WriteLine(DateTime.Now.Millisecond + " " + ex.Message);
                        continue;
                    }

                    try
                    {
                        up.identify();
                        Console.WriteLine("Found board type {0} boardrev {1} bl rev {2} fwmax {3} on {4}", up.board_type, up.board_rev, up.bl_rev, up.fw_maxsize, port);
                    }
                    catch (Exception)
                    {
                        try
                        {
                            //System.Threading.ThreadPool.QueueUserWorkItem(up.__mavlinkreboot);
                        }
                        catch
                        {
                            //up.close();
                        }

                        Console.WriteLine(DateTime.Now.Millisecond + " " + "Not There..");
                        //Console.WriteLine(ex.Message);
                        try
                        {
                            up.close();
                        }
                        catch { }
                        continue;
                    }

                    try
                    {
                        up.currentChecksum(fw);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("No need to upload. already on the board");
                        up.close();
                        return true;
                    }

                    try
                    {
                        up.upload(fw);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }

                    up.close();

                    return true;
                }
            }
        }

        public static string[] GetPortNames()
        {
            List<string> allPorts = new List<string>();

            if (Directory.Exists("/dev/"))
            {
                // cleanup now
                GC.Collect();
                // mono is failing in here on linux "too many open files"
                try
                {
                    if (Directory.Exists("/dev/serial/by-id/"))
                        allPorts.AddRange(Directory.GetFiles("/dev/serial/by-id/", "*"));
                }
                catch { }
                try
                {
                    allPorts.AddRange(Directory.GetFiles("/dev/", "ttyACM*"));
                }
                catch { }
                try
                {
                    allPorts.AddRange(Directory.GetFiles("/dev/", "ttyUSB*"));
                }
                catch { }
                try
                {
                    allPorts.AddRange(Directory.GetFiles("/dev/", "rfcomm*"));
                }
                catch { }
                try
                {
                    allPorts.AddRange(Directory.GetFiles("/dev/", "*usb*"));
                }
                catch { }
            }


            string[] ports = System.IO.Ports.SerialPort.GetPortNames();

            ports = ports.Select(p => trimcomportname(p.TrimEnd())).ToArray();

            allPorts.AddRange(ports);

            return allPorts.ToArray();
        }

        static string trimcomportname(string input)
        {
            var match = Regex.Match(input.ToUpper(), "(COM[0-9]+)");

            if (match.Success)
            {
                return match.Groups[0].Value;
            }

            return input;
        }


    }
}
