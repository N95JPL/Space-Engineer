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
        const double TargetAltitude = 1000; // Meters
        //Script - No more editable functions
        string Status = "Not Ready";
        bool LaunchReady = false;
        bool TargetMet = false;
        bool RCFailed = false; const string RCFailedMSG = (Ship + "Controller not found with name " + RC + "!");
        bool GyroFailed = false; const string GyroFailedMSG = (Ship + "Gyro not found with name " + Gyro + "!");
        bool LAFailed = false; const string LAFailedMSG = (Ship + "Antenna not found with name " + LA + "!");
        Vector3D StartLocation;
        Vector3D AppLocation;
        Vector3D Position;
        double elev;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Save()
        {
        }

        public void Main()
        {
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

            RController.TryGetPlanetElevation(MyPlanetElevation.Surface, out elev);
            if (Status == "Failed")
            {
                string msg = ("Ship" + ":" + Ship + "," + "Status" + ":" + Status + "," + "Elevation" + ":" + elev + "," + "Position" + ":" + Position + ",");
                Dictionary<string, string> keyValuePairs = msg.Split(',').Select(value => value.Split(':')).ToDictionary(pair => pair[0], pair => pair[1]);
                string LogShipName = keyValuePairs["Ship"];
                Echo(LogShipName);
                LAntenna.TransmitMessage(msg);

            }
            if (Status == "Not Ready") //Prepare GPS Waypoints for the Autopilot
            {
                double StartAltitude = elev;
                RControllers.SetAutoPilotEnabled(false);
                StartLocation = RController.GetPosition();
                RControllers.ClearWaypoints();
                AppLocation.X = StartLocation.X + TargetAltitude;
                AppLocation.Y = StartLocation.Y + 0;
                AppLocation.Z = StartLocation.Z + 0;
                RControllers.AddWaypoint(AppLocation, (Ship + "Approach Location"));
                RControllers.AddWaypoint(StartLocation, (Ship + "Landing Location"));
                Status = "Ready";
            }
            if (Status == "Ready")
            {

            }
            if (Status == "Launching")
            {
                if (elev <= TargetAltitude)
                {

                }
            }
            if (Status == "Seperation")
            {

            }
            if (Status == "Desending")
            {
                RControllers.SetAutoPilotEnabled(true);
                RControllers.SetCollisionAvoidance(false);
                RControllers.SetDockingMode(true);
            }
        }
    }

}
