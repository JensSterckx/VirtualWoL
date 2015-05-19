/*
 * Based on SharpPcap
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

namespace SharpPcap.Examples
{
    class WakeOnLanCapture
    {
        /// <summary>
        /// Wake ON Lan
        /// Original by SharpPCap
        /// Modified & fixed by Jens Sterckx
        /// </summary>
        public static void Main (string[] args)
        {
            // print SharpPcap version
            string ver = SharpPcap.Version.VersionString;
            Console.WriteLine("SharpPcap VERSION: {0}", ver);

            // retrieve the device list
            var devices = CaptureDeviceList.Instance;

            // if no devices were found print an error
            if(devices.Count < 1)
            {
                Console.WriteLine("No devices were found on this machine");
                return;
            }
            //HOST ONLY 4 = 02BAF9CA-13E6-4E15-8629-1E564944A4BA

            int i = 0;
            int j = 0;

            ICaptureDevice[] SUNDev = new ICaptureDevice[devices.Count];

            // scan the list printing every entry
            foreach(var dev in devices)
            {
                //Console.WriteLine("{0} {1} {2}\n", i, dev.Name, dev.Description);
                //Console.WriteLine("{0} {1}", i, dev.Description);
                if (dev.Description.Contains("Sun"))
                {
                    //Console.WriteLine("{0} {1} {2}\n", i, dev.Name, dev.Description);
                    SUNDev[j] = dev;
                    j++;
                }
                i++;
            }

            i = 0;
            Console.WriteLine("De volgende Host-Only Adapters zijn gevonden:");
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine();

            foreach(var dev in SUNDev)
            {
                if(dev == null)
                {
                    break;
                }
                Console.WriteLine("----------------------------------------------------");
                Console.WriteLine("Adapter " + i + ".");
                Console.WriteLine("{0}\n{1}\n", dev.Name, dev.Description);
                i++;
            }

            /*Console.WriteLine();
            Console.Write("-- Please choose a device to capture: ");
            i = int.Parse(Console.ReadLine());*/

            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine("Op al deze adapters capturen...");
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine();

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

                Console.WriteLine();
                Console.WriteLine("-- Listening on {0} {1}", device.Name, device.Description);

                //Filter to only capture WoL MagicPackets.
                device.Filter = "ether proto 0x0842";


                // Start the capturing process
                device.StartCapture();

            }
            
            // Wait for 'Enter' from the user.
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine("CTRL + C om te stoppen");
            //Console.ReadLine();

            /*foreach (var device in SUNDev)
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
            /* Packet info data
             * 
            var time = e.Packet.Timeval.Date;
            var len = e.Packet.Data.Length;
            Console.WriteLine("{0}:{1}:{2},{3} Len={4}", 
                time.Hour, time.Minute, time.Second, time.Millisecond, len);
            Console.WriteLine(e.Packet.ToString());*/

            // parse the incoming packet
            var packet = PacketDotNet.Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);

            if (packet == null)
                return;
        
            var wol = WakeOnLanPacket.GetEncapsulated(packet);
            Console.WriteLine(wol.DestinationMAC + " => Send to VBoxManage");

            ProcessStartInfo SProcess = new ProcessStartInfo("D:\\Program Files\\Oracle\\VirtualBox\\VBoxManage", "startvm " + wol.DestinationMAC);
            Process IProcess = Process.Start(SProcess);
        }
    }
}