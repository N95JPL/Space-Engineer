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
        //Varibles
        const string Ship = "Booster2"; //Name of ship + " :", for best practise label all blocks (ShipName): (Block Name)
        const string gap = ": "; //DO NOT EDIT
        const string RC = (Ship + gap + "Remote Control"); //Name of Remote Controller
        const string Gyro = (Ship + gap + "Gyro"); //Name of Gyro
        const string LA = (Ship + gap + "Laser Antenna"); //Name of Laser Antenna
        const string CC = (Ship + gap + "Cruise Pro");
        const string LG = (Ship + gap + "Landing Gear");
        const double TargetAltitude = 10000; // Meters
        //Script - No more editable functions
        string Status = "Not Ready";
        bool LaunchReady = false;
        bool TargetMet = false;
        bool RCFailed = false;
        bool GearDown = true;
        const string RCFailedMSG = (Ship + "Controller not found with name " + RC + "!");
        bool CCFailed = false;
        const string CCFailedMSG = (Ship + "Computer not found with name " + CC + "!");
        bool GyroFailed = false;
        const string GyroFailedMSG = (Ship + "Gyro not found with name " + Gyro + "!");
        bool LAFailed = false;
        const string LAFailedMSG = (Ship + "Antenna not found with name " + LA + "!");
        bool LGFailed = false;
        const string LGFailedMSG = (Ship + "Landing Gear Group not found with name " + LA + "!");
        Vector3D StartLocation;
        Vector3D GyroStartLocation;
        Vector3D Distance;
        Vector3D AppLocation;
        Vector3D Position;
        double Elev;
        double StartElev;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Save()
        {
        }
        public void Reset()
        {

        }

        public void Main(string arg)
        {
            if (arg == "ReadyUp")
            {
                Status = "Not Ready";
            }
            Echo(Ship + " Control Pro");
            IMyShipController RController;
            RController = GridTerminalSystem.GetBlockWithName(RC) as IMyShipController;
            if (RController == null)
            {

                Echo(RCFailedMSG);
                RCFailed = true;
                Status = "Failed";
                return;
            }
            IMyRemoteControl RControllers;
            RControllers = GridTerminalSystem.GetBlockWithName(RC) as IMyRemoteControl;
            if (RControllers == null)
            {
                Echo(RCFailedMSG);
                RCFailed = true;
                Status = "Failed";
                return;
            }
            IMyProgrammableBlock CCruise;
            CCruise = GridTerminalSystem.GetBlockWithName(CC) as IMyProgrammableBlock;
            if (CCruise == null)
            {
                Echo(CCFailedMSG);
                CCFailed = true;
                Status = "Failed";
                return;
            }
            IMyGyro RGyro;
            RGyro = GridTerminalSystem.GetBlockWithName(Gyro) as IMyGyro;
            if (RGyro == null)
            {
                Echo(GyroFailedMSG);
                GyroFailed = true;
                Status = "Failed";
                return;
            }
            IMyFunctionalBlock RGyros;
            RGyros = GridTerminalSystem.GetBlockWithName(Gyro) as IMyFunctionalBlock;
            if (RGyros == null)
            {
                Echo(GyroFailedMSG);
                GyroFailed = true;
                Status = "Failed";
                return;
            }
            IMyLaserAntenna LAntenna;
            LAntenna = GridTerminalSystem.GetBlockWithName(LA) as IMyLaserAntenna;
            if (LAntenna == null)
            {
                Echo(LAFailedMSG);
                LAFailed = true;
                Status = "Failed";
                return;
            }
            IMyTimerBlock LGear;
            LGear = GridTerminalSystem.GetBlockWithName(LG) as IMyTimerBlock;
            if (LGear == null)
            {
                Echo(LGFailedMSG);
                LGFailed = true;
                Status = "Failed";
                return;
            }

            RController.TryGetPlanetElevation(MyPlanetElevation.Surface, out Elev);
            Position = RController.GetPosition();

            if (Status == "Failed") //A componant has failed - Check Ship
            {
                if (RCFailed == true)
                {
                    Echo(RCFailedMSG);
                }
                if (CCFailed == true)
                {
                    Echo(CCFailedMSG);
                }
                if (GyroFailed == true)
                {
                    Echo(GyroFailedMSG);
                }
                if (LAFailed == true)
                {
                    Echo(LAFailedMSG);
                }
                if (LGFailed == true)
                {
                    Echo(LGFailedMSG);
                }
                Echo(Status);
                string msg = ("Ship" + ":" + Ship + "," + "Status" + ":" + Status + "," + "Elevation" + ":" + Elev + "," + "Position" + ":" + Position + ",");
                var keyValuePairs = msg.Split(',').Select(x => x.Split(':')).Where(x => x.Length == 2).ToDictionary(x => x.First(), x => x.Last());
                LAntenna.TransmitMessage(msg);
                Status = "Failed";
            }
            if (Status == "Not Ready") //Prepare GPS Waypoints for the Autopilot
            {
                Echo(Status);
                StartElev = Elev;
                RControllers.SetAutoPilotEnabled(false);
                StartLocation = RController.GetPosition();
                RControllers.ClearWaypoints();
                //AppLocation.X = StartLocation.X + 500;
                //AppLocation.Y = StartLocation.Y + 0; 
                //AppLocation.Z = StartLocation.Z + 0;
                GyroStartLocation = RGyro.GetPosition();
                Distance = ((GyroStartLocation - StartLocation) * 83.333);
                AppLocation = (StartLocation + Distance);
                RControllers.AddWaypoint(AppLocation, (Ship + "Approach Location"));
                RControllers.AddWaypoint(StartLocation, (Ship + "Landing Location"));
                string msg = ("Ship" + ":" + Ship + "," + "Status" + ":" + Status + "," + "Start Elevation" + ":" + StartElev + "," + "Start Position" + ":" + StartLocation + ",");
                LAntenna.TransmitMessage(msg);
                Status = "Ready";
            }
            if (Status == "Ready")
            {
                Echo(Status);
                LaunchReady = true;
            }
            if (arg == "Launch")
            {
                Status = "Launching";
                if (Status == "Launching")
                {
                    CCruise.TryRun("on");
                    Echo(Status);
                    return;
                }
                if (Status == "Launched")
                {
                    if (Elev >= StartElev + 50)
                    {
                        LGear.ApplyAction("TriggerNow");
                        GearDown = false;
                    }
                    if (Elev >= TargetAltitude)
                    {
                        TargetMet = true;
                        CCruise.TryRun("off");
                        Status = "Seperation";
                        return;
                    }
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
                    RGyro.GyroOverride = true;

                }
                if (Status == "Desending")
                {
                    if (GearDown == false)
                    {
                        Echo(Status);
                        if (Elev < StartElev + 400)
                        {
                            LGear.ApplyAction("TriggerNow");
                            GearDown = true;
                        }
                    }
                    if (Elev <= StartElev + 20)
                    {
                        Echo(Status);
                        RControllers.SetAutoPilotEnabled(false);
                        RControllers.SetCollisionAvoidance(false);
                        RControllers.SetDockingMode(false);
                        RGyro.GyroOverride = false;
                        Status = "Landed";
                    }
                }
            }
        }
    }

}