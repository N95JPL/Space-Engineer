using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        //Commands
        // Argument - Reset (Resets all ships Navigation data)
        // Target:##### (Sets target Altitude in Meters - Default 10000m this does NOT reset with the Reset argument)
        // Launch (Start launch)
        // Abort (Aborts current mission and returns to base)
        //Commands End
        //Varibles
        const string Ship = "Booster2"; //Name of ship + " :", for best practise label all blocks (ShipName): (Block Name)
        const string gap = ": "; //DO NOT EDIT
        const string RC = (Ship + gap + "Remote Control"); //Name of Remote Controller
        const string Gyro = (Ship + gap + "Gyro"); //Name of Gyro
        const string LA = (Ship + gap + "Laser Antenna"); //Name of Laser Antenna
        const string CC = (Ship + gap + "Cruise Pro");
        const string LG = (Ship + gap + "Landing Gear");
        const string CCU = (Ship + gap + "Cruise (On_Up)");
        const string CCO = (Ship + gap + "Cruise (Off)");
        const string CCD = (Ship + gap + "Cruise (On_Down)");
        double TargetAltitude = 10000; // Meters

        //Script - No more editable functions
        string Status = "Not Ready";
        bool LaunchReady = false;
        bool TargetMet = false;
        bool RCFailed = false;
        bool GearDown = true;
        string TargetAltitudeSetter;
        const string RCFailedMSG = (Ship + "Controller not found with name " + RC + "!");
        bool CCFailed = false;
        const string CCFailedMSG = (Ship + "Computer not found with name " + CC + "!");
        bool GyroFailed = false;
        const string GyroFailedMSG = (Ship + "Gyro not found with name " + Gyro + "!");
        bool LAFailed = false;
        const string LAFailedMSG = (Ship + "Antenna not found with name " + LA + "!");
        bool LGFailed = false;
        const string LGFailedMSG = (Ship + "Landing Gear Group not found with name " + LA + "!");
        bool CCTsFailed = false;
        const string CCTsFailedMSG = (Ship + "A Cruise timer block is missing!");
        //Varibles End

        //No touchy below - JPL
        Vector3D StartLocation;
        Vector3D GyroStartLocation;
        Vector3D Distance;
        Vector3D AppLocation;
        Vector3D Position;
        double Elev;
        double StartElev;
        double RefDist;
        string CCarg;
        IMyShipController RController;
        IMyRemoteControl RControllers;
        IMyGyro RGyro;
        IMyFunctionalBlock RGyros;
        IMyLaserAntenna LAntenna;
        IMyTimerBlock LGear;
        IMyTimerBlock CCUp;
        IMyTimerBlock CCOff;
        IMyTimerBlock CCDown;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Save()
        {
        }

        public void Main(string arg)
        {
            if (arg == "Reset")
            {
                Status = "Not Ready";
            }
            if (arg == "Abort")
            {
                Status = "Abort";
            }
            if (arg == "Launch")
            {
                Status = "Launching";
                Echo("Launch commanded");
                return;
            }
            if (arg.Contains("Target"))
            {
                var keyValuePairs = arg.Split(',').Select(x => x.Split(':')).Where(x => x.Length == 2).ToDictionary(x => x.First(), x => x.Last());
                TargetAltitudeSetter = keyValuePairs["Target"];
                TargetAltitude = double.Parse(TargetAltitudeSetter);
            }

            RController = GridTerminalSystem.GetBlockWithName(RC) as IMyShipController;
            if (RController == null)
            {
                Echo(RCFailedMSG);
                RCFailed = true;
                Status = "Failed";
                return;
            }

            RControllers = GridTerminalSystem.GetBlockWithName(RC) as IMyRemoteControl;
            if (RControllers == null)
            {
                Echo(RCFailedMSG);
                RCFailed = true;
                Status = "Failed";
                return;
            }

            var CCruise = GridTerminalSystem.GetBlockWithName(CC) as IMyProgrammableBlock;
            if (CCruise == null)
            {
                Echo(CCFailedMSG);
                CCFailed = true;
                Status = "Failed";
                return;
            }

            RGyro = GridTerminalSystem.GetBlockWithName(Gyro) as IMyGyro;
            if (RGyro == null)
            {
                Echo(GyroFailedMSG);
                GyroFailed = true;
                Status = "Failed";
                return;
            }

            RGyros = GridTerminalSystem.GetBlockWithName(Gyro) as IMyFunctionalBlock;
            if (RGyros == null)
            {
                Echo(GyroFailedMSG);
                GyroFailed = true;
                Status = "Failed";
                return;
            }

            LAntenna = GridTerminalSystem.GetBlockWithName(LA) as IMyLaserAntenna;
            if (LAntenna == null)
            {
                Echo(LAFailedMSG);
                LAFailed = true;
                Status = "Failed";
                return;
            }

            LGear = GridTerminalSystem.GetBlockWithName(LG) as IMyTimerBlock;
            if (LGear == null)
            {
                Echo(LGFailedMSG);
                LGFailed = true;
                Status = "Failed";
                return;
            }

            CCUp = GridTerminalSystem.GetBlockWithName(CCU) as IMyTimerBlock;
            if (CCUp == null)
            {
                Echo(CCTsFailedMSG);
                CCTsFailed = true;
                Status = "Failed";
                return;
            }
            CCOff = GridTerminalSystem.GetBlockWithName(CCU) as IMyTimerBlock;
            if (CCOff == null)
            {
                Echo(CCTsFailedMSG);
                CCTsFailed = true;
                Status = "Failed";
                return;
            }
            CCDown = GridTerminalSystem.GetBlockWithName(CCU) as IMyTimerBlock;
            if (CCDown == null)
            {
                Echo(CCTsFailedMSG);
                CCTsFailed = true;
                Status = "Failed";
                return;
            }

            RController.TryGetPlanetElevation(MyPlanetElevation.Surface, out Elev);
            Position = RController.GetPosition();
            Echo(Ship + " Control Pro");
            string TarAl = TargetAltitude.ToString();
            Echo("Current Altitude: " + Elev);
            Echo("Target Altitude: " + TarAl);

            if (Status == "Failed") //A componant has failed - Check Ship
            {
                if (RCFailed == true)
                {
                    Echo(RCFailedMSG);
                    return;
                }
                if (CCFailed == true)
                {
                    Echo(CCFailedMSG);
                    return;
                }
                if (GyroFailed == true)
                {
                    Echo(GyroFailedMSG);
                    return;
                }
                if (LAFailed == true)
                {
                    Echo(LAFailedMSG);
                    return;
                }
                if (LGFailed == true)
                {
                    Echo(LGFailedMSG);
                    return;
                }
                if (CCTsFailed == true)
                {
                    Echo(CCTsFailedMSG);
                    return;
                }
                Echo(Status);
                string msg = ("Ship" + ":" + Ship + "," + "Status" + ":" + Status + "," + "Elevation" + ":" + Elev + "," + "Position" + ":" + Position + ",");
                var keyValuePairs = msg.Split(',').Select(x => x.Split(':')).Where(x => x.Length == 2).ToDictionary(x => x.First(), x => x.Last());
                LAntenna.TransmitMessage(msg);
                Status = "Failed";
                return;
            }
            if (Status == "Not Ready") //Prepare GPS Waypoints for the Autopilot
            {
                Echo(Status);
                StartElev = Elev;
                RControllers.SetAutoPilotEnabled(false);
                StartLocation = RController.GetPosition();
                RControllers.ClearWaypoints();
                GyroStartLocation = RGyro.GetPosition();
                RefDist = Math.Round(Vector3D.Distance(GyroStartLocation, StartLocation), 2); //Distance between RC and Gyro
                Distance = ((GyroStartLocation - StartLocation) * (TargetAltitude / RefDist)); //Calculates Distance to Target
                AppLocation = (StartLocation + Distance); ////Calculates Co-ords for Target
                //RControllers.AddWaypoint(AppLocation, (Ship + "Approach Location"));
                RControllers.AddWaypoint(StartLocation, (Ship + "Landing Location"));
                string msg = ("Ship" + ":" + Ship + "," + "Status" + ":" + Status + "," + "Start Elevation" + ":" + StartElev + "," + "Start Position" + ":" + StartLocation + ",");
                LAntenna.TransmitMessage(msg);
                Status = "Ready";
                return;
            }
            if (Status == "Ready")
            {
                Echo(Status);
                LaunchReady = true;
                return;
            }
            if (Status == "Launching")
            {
                Echo("Attempting Launch Script");
                CCUp.ApplyAction("TriggerNow");
                Echo("Cruise Control activated1");
                return;
            }
            if (Status == "Launched")
            {
                Echo(Status);
                if (Elev >= StartElev + 20)
                {
                    if (GearDown == true)
                    {
                        LGear.ApplyAction("TriggerNow");
                        GearDown = false;
                        return;
                    }
                }
                if (Elev >= (TargetAltitude))
                {
                    TargetMet = true;
                    CCOff.ApplyAction("TriggerNow");
                    Echo(Ship + " Launch Cruise Deactivated!");
                    Status = "Seperation";
                    return;
                }
            }
            if (Status == "Abort")
            {
                CCarg = "off";
                if (CCruise.TryRun(CCarg))
                {
                    Echo(Ship + " Launch Cruise Deactivated!");
                    return;
                }
                Status = "Prep Decent";
                return;
            }

            if (Status == "Seperation")
            {
                Echo(Status);
                Status = "Prep Decent";
                return;
            }
            if (Status == "Prep Decent")
            {
                Echo(Status);
                RControllers.SetAutoPilotEnabled(true);
                RControllers.SetCollisionAvoidance(false);
                RControllers.SetDockingMode(true);
                CCDown.ApplyAction("TriggerNow");
                Status = "Desending";
                return;
            }
            if (Status == "Desending")
            {
                if (!GearDown)
                {
                    Echo(Status);
                    if (Elev < StartElev + 1000)
                    {
                        CCOff.ApplyAction("TriggerNow");
                        return;
                    }
                    if (Elev < StartElev + 50)
                    {
                        LGear.ApplyAction("TriggerNow");
                        GearDown = true;
                        return;
                    }
                }
                if (Elev <= StartElev + 200)
                {
                    Echo(Status);
                    RControllers.SetAutoPilotEnabled(false);
                    RControllers.SetCollisionAvoidance(false);
                    RControllers.SetDockingMode(false);
                    RGyro.GyroOverride = false;
                    RController.ApplyAction("DampenersOverride");
                    return;
                }
                if (Elev == StartElev)
                {
                    Status = "Landed";
                    return;
                }
            }
        }

    }

}
