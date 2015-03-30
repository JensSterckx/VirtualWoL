/*
 * Using SharpPcap
 * Modified to work on Windows using VirtualBox
 * 
 * Author: Jens Sterckx
 * Date: 27/03/2015
 */

using System;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using SharpPcap.AirPcap;
using SharpPcap.WinPcap;
using System.Diagnostics;
using System.Configuration;

namespace WoL_VirtualBox
{
    class WoL
    {
        /// <summary>
        /// Wake ON Lan
        /// Original by SharpPCap
        /// Modified & fixed by Jens Sterckx
        /// </summary>
        public static void Main(string[] args)
        {
            // print SharpPcap version
            string ver = SharpPcap.Version.VersionString;
            Console.WriteLine("Sterckx Jens - 27/03/2015");
            Console.WriteLine("SharpPcap VERSION: {0}", ver);
            Console.WriteLine("\nLoading interfaces...");

            if (ConfigurationManager.AppSettings["FULL_LOG"] == "ja")
            {
                Console.WriteLine("FULL_LOG is enabled!");
            }

            // retrieve the device list
            var devices = CaptureDeviceList.Instance;

            // if no devices were found print an error
            if (devices.Count < 1)
            {
                Console.WriteLine("No devices were found on this machine");
                return;
            }

            int i = 0;
            int j = 0;

            ICaptureDevice[] SUNDev = new ICaptureDevice[devices.Count];

            // scan the list printing every entry
            foreach (var dev in devices)
            {
                if (dev.Description.Contains("Sun"))
                {
                    //Console.WriteLine("{0} {1} {2}\n", i, dev.Name, dev.Description);
                    SUNDev[j] = dev;
                    j++;
                }
                i++;
            }

            i = 0;
            if (ConfigurationManager.AppSettings["FULL_LOG"] == "ja")
            {
                Console.WriteLine("De volgende Host-Only Adapters zijn gevonden:");
                Console.WriteLine("----------------------------------------------------");
                Console.WriteLine();
            }

            //We select all the adapters that are created by Sun (Virtual Box Host-Only adapters)
            foreach (var dev in SUNDev)
            {
                if (dev == null)
                {
                    break;
                }
                if (ConfigurationManager.AppSettings["FULL_LOG"] == "ja")
                {
                    Console.WriteLine("----------------------------------------------------");
                    Console.WriteLine("Adapter " + i + ".");
                    Console.WriteLine("{0}\n{1}\n", dev.Name, dev.Description);
                }
                i++;
            }

            if (ConfigurationManager.AppSettings["FULL_LOG"] == "ja")
            {
                Console.WriteLine("----------------------------------------------------");
                Console.WriteLine("Op al deze adapters capturen...");
                Console.WriteLine("----------------------------------------------------");
                Console.WriteLine();
            }

            //Start listening on all the Sun Host-Only adapters
            foreach (var device in SUNDev)
            {
                if (device == null)
                {
                    break;
                }
                // register our handler function to the 'packet arrival' event
                device.OnPacketArrival +=
                    new PacketArrivalEventHandler(device_OnPacketArrival);

                // Open the device for capturing
                int readTimeoutMilliseconds = 1000;
                if (device is AirPcapDevice)
                {
                    // NOTE: AirPcap devices cannot disable local capture
                    var airPcap = device as AirPcapDevice;
                    airPcap.Open(SharpPcap.WinPcap.OpenFlags.DataTransferUdp, readTimeoutMilliseconds);
                }
                else if (device is WinPcapDevice)
                {
                    var winPcap = device as WinPcapDevice;
                    winPcap.Open(SharpPcap.WinPcap.OpenFlags.DataTransferUdp | SharpPcap.WinPcap.OpenFlags.NoCaptureLocal, readTimeoutMilliseconds);
                }
                else if (device is LibPcapLiveDevice)
                {
                    var livePcapDevice = device as LibPcapLiveDevice;
                    livePcapDevice.Open(DeviceMode.Promiscuous, readTimeoutMilliseconds);
                }
                else
                {
                    throw new System.InvalidOperationException("unknown device type of " + device.GetType().ToString());
                }

                if (ConfigurationManager.AppSettings["FULL_LOG"] == "ja")
                {
                    Console.WriteLine();
                    Console.WriteLine("-- Listening on {0} {1}", device.Name, device.Description);
                }

                //Filter to only capture WoL MagicPackets.
                device.Filter = "ether proto 0x0842";


                // Start the capturing process
                device.StartCapture();

            }

            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine("Bezig met het capturen van WoL packets.");
            Console.WriteLine("CTRL + C om te stoppen");
            

            //Doesn't seem necesary, (CTRL C closes all)
            /*
            Console.ReadLine();
            foreach (var device in SUNDev)
            {
                if (device == null)
                {
                    break;
                }
                // Stop the capturing process
                device.StopCapture();  
            }*/
        }

        /// <summary>
        /// Handle incoming packets
        /// </summary>
        private static void device_OnPacketArrival(object sender, CaptureEventArgs e)
        {
            //When full log is enabled in config, more packet info will be print.
            if (ConfigurationManager.AppSettings["FULL_LOG"] == "ja")
            {
                // PACKET DEBUG INFO
                var time = e.Packet.Timeval.Date;
                var len = e.Packet.Data.Length;
                Console.WriteLine("{0}:{1}:{2},{3} Len={4}", 
                    time.Hour, time.Minute, time.Second, time.Millisecond, len);
                Console.WriteLine(e.Packet.ToString());
            }

            // parse the incoming packet so we can use the seperate data values.
            var packet = PacketDotNet.Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);

            //On error, return
            if (packet == null)
                return;

            //Parse the data as an WoL packet.
            var wol = PacketDotNet.WakeOnLanPacket.GetEncapsulated(packet);
            Console.WriteLine("MagicPacket for: " + wol.DestinationMAC + " received, starting VM: \"" + ConfigurationManager.AppSettings["VM_PREFIX"] + wol.DestinationMAC + "\"");

            //Start the Virtual Box using VBoxManage.
            ProcessStartInfo SProcess = new ProcessStartInfo(ConfigurationManager.AppSettings["VirtualBoxDirectory"] + "\\VBoxManage", "startvm " + ConfigurationManager.AppSettings["VM_PREFIX"] + wol.DestinationMAC);
            Process IProcess = Process.Start(SProcess);
        }
    }
}