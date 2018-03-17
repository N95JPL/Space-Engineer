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
        const string Cargo = (Ship + gap + "Cargo (Middle)"); //Name of Cargo Ref
        const string LA = (Ship + gap + "Laser Antenna"); //Name of Laser Antenna
        const string CC = (Ship + gap + "Cruise Pro");
        const string LG = (Ship + gap + "Landing Gear");
        const string CCU = (Ship + gap + "Cruise (On_Up)");
        const string CCO = (Ship + gap + "Cruise (Off)");
        const string CCD = (Ship + gap + "Cruise (On_Down)");
        double TargetAltitude = 10000; // Meters
        double AppTarget = 200;//Meters
        //Varibles End

        //No touchy below - JPL
        string Status = "Not Ready";
        string TargetAltitudeSetter;
        const string RCFailedMSG = (Ship + "Controller not found with name " + RC + "!");
        bool CCFailed = false;
        const string CCFailedMSG = (Ship + "Computer not found with name " + CC + "!");
        bool GyroFailed = false;
        const string GyroFailedMSG = (Ship + "Gyro not found with name " + Gyro + "!");
        bool RConFailed = false;
        const string RConFailedMSG = (Ship + "Cargo Ref not found with name " + Cargo + "!");
        bool LAFailed = false;
        const string LAFailedMSG = (Ship + "Antenna not found with name " + LA + "!");
        bool LGFailed = false;
        const string LGFailedMSG = (Ship + "Landing Gear Group not found with name " + LA + "!");
        bool CCTsFailed = false;
        const string CCTsFailedMSG = (Ship + "A Cruise timer block is missing!");
        bool RCFailed = false;
        bool AutoEnable;
        bool GearDown = true;
        bool Init;
        Vector3D StartLocation;
        Vector3D GyroStartLocation;
        Vector3D RConStartLocation;
        Vector3D Distance;
        Vector3D AppLocation;
        Vector3D TargetLocation;
        Vector3D Position;
        double Elev;
        double StartElev;
        double RefDist;
        double velo;
        IMyShipController RController;
        IMyRemoteControl RControllers;
        IMyGyro RGyro;
        IMyFunctionalBlock RGyros;
        IMyCargoContainer RCon;
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

            RCon = GridTerminalSystem.GetBlockWithName(Cargo) as IMyCargoContainer;
            if (RCon == null)
            {
                Echo(RConFailedMSG);
                RConFailed = true;
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
            CCOff = GridTerminalSystem.GetBlockWithName(CCO) as IMyTimerBlock;
            if (CCOff == null)
            {
                Echo(CCTsFailedMSG);
                CCTsFailed = true;
                Status = "Failed";
                return;
            }
            CCDown = GridTerminalSystem.GetBlockWithName(CCD) as IMyTimerBlock;
            if (CCDown == null)
            {
                Echo(CCTsFailedMSG);
                CCTsFailed = true;
                Status = "Failed";
                return;
            }
            AutoEnable = RControllers.IsAutoPilotEnabled;
            RController.TryGetPlanetElevation(MyPlanetElevation.Surface, out Elev);
            string TarAl = TargetAltitude.ToString();
            velo = RController.GetShipSpeed();
            Position = RController.GetPosition();

            Echo(Ship + " Control Pro");
            Echo("Status: " + Status);
            Echo("Current Altitude: " + Math.Round(Elev, 2));
            Echo("Target Altitude: " + TargetAltitude);
            Echo("Current Speed: " + Math.Round(velo, 2));

            if (Status == "Failed")
            {
                if (RCFailed == true){Echo(RCFailedMSG);return;}
                if (CCFailed == true){Echo(CCFailedMSG);return;}
                if (GyroFailed == true){Echo(GyroFailedMSG);return;}
                if (LAFailed == true){Echo(LAFailedMSG);return;}
                if (LGFailed == true){Echo(LGFailedMSG);return;}
                if (CCTsFailed == true){Echo(CCTsFailedMSG);return;}
                Echo(Status);
                string msg = ("Ship" + ":" + Ship + "," + "Status" + ":" + Status + "," + "Elevation" + ":" + Elev + "," + "Position" + ":" + Position + ",");
                var keyValuePairs = msg.Split(',').Select(x => x.Split(':')).Where(x => x.Length == 2).ToDictionary(x => x.First(), x => x.Last());
                LAntenna.TransmitMessage(msg);
                Status = "Failed";
                return;
            }

            if (!Init){Status = "Initalizing...";NotReady();}
            if (arg.Contains("Target"))
            {
                var keyValuePairs = arg.Split(',').Select(x => x.Split(':')).Where(x => x.Length == 2).ToDictionary(x => x.First(), x => x.Last());
                TargetAltitudeSetter = keyValuePairs["Target"];
                TargetAltitude = double.Parse(TargetAltitudeSetter);
                NotReady();
            }
            if (arg == "Reset"){Status = "Not Ready";NotReady();}
            if (arg == "Ready"){Status = "Ready";Ready();}
            if (arg == "Launch"){Status = "Launching";Launch();}
            if (arg == "Launched"){Status = "Launched";Climb();}
            if (arg == "Seperate"){Status = "Seperation";Seperation();}
            if (arg == "Return"){Status = "Return";Return();}
            if (arg == "Approach"){Status = "Approaching";Approach();}
            if (arg == "Land"){Status = "Landing";Land();}
            if (arg == "Landed"){Status = "Landed";}

            if (Status == "Launching"){Launch();}
            if (Status == "Launched"){Climb();}
            if (Status == "Seperation"){Seperation();}
            if (Status == "Return"){Return();}
            if (Status == "Approaching"){Approach();}
            if (Status == "Landing"){Land();}
            if (Status == "Landed"){Status = "Landed";}
        }

        public void NotReady()
        {
            StartElev = Elev;
            RControllers.SetAutoPilotEnabled(false);
            RControllers.FlightMode = FlightMode.OneWay;
            StartLocation = RController.GetPosition();
            RControllers.ClearWaypoints();
            GyroStartLocation = RGyro.GetPosition();
            RConStartLocation = RCon.GetPosition();
            RefDist = Math.Round(Vector3D.Distance(GyroStartLocation, StartLocation), 2); //Distance between RC and Gyro
            Distance = ((GyroStartLocation - StartLocation) * ((TargetAltitude - StartElev) / RefDist)); //Calculates Distance to Target
            TargetLocation = (StartLocation + Distance); ////Calculates Co-ords for Target
            RefDist = Math.Round(Vector3D.Distance(RConStartLocation, StartLocation), 2); //Distance between RC and Gyro
            Distance = ((RConStartLocation - StartLocation) * ((AppTarget + StartElev) / RefDist)); //Calculates Distance to Approach (1000m)
            AppLocation = (StartLocation + Distance); ////Calculates Co-ords for Target
            RControllers.AddWaypoint(TargetLocation, (Ship + "Target Location"));
            Init = true;
            Main("Ready");
            return;
        }

        public void Ready()
        {
            Echo("Awaiting Launch command!");
            return;
        }

        public void Launch()
        {
            CCUp.ApplyAction("TriggerNow");
            RController.DampenersOverride = true;
            RController.TryGetPlanetElevation(MyPlanetElevation.Surface, out Elev);
            if (Elev >= (StartElev + 20))
            {
                if (GearDown == true)
                {
                    LGear.ApplyAction("TriggerNow");
                    GearDown = false;
                    Main("Launched");
                }
            }
        }

        public void Climb()
        {
                RController.TryGetPlanetElevation(MyPlanetElevation.Surface, out Elev);
                if (Elev >= (TargetAltitude - 500))
                {
                    CCOff.ApplyAction("TriggerNow");
                    Echo(Ship + " Launch Cruise Deactivated!");
                    RControllers.SetAutoPilotEnabled(true);
                    RControllers.SetCollisionAvoidance(false);
                    RControllers.SetDockingMode(true);
                    Main("Seperate");
                    return;
                }
        }

        public void Seperation()
        {
            Echo("Waiting for RC to reach target");
            velo = RController.GetShipSpeed();
            if (!AutoEnable && (velo < 0.5))
            {
                Main("Return");
            }
            return;
        }

        public void Return()
        {
            RController.TryGetPlanetElevation(MyPlanetElevation.Surface, out Elev);
            if (Elev > (StartElev + 2000))
            {
                RControllers.SetAutoPilotEnabled(false);
                RController.DampenersOverride = false;
            }
            if (Elev < (StartElev + 2000))
            {
                if (!AutoEnable)
                {
                    RController.DampenersOverride = true;
                    RControllers.ClearWaypoints();
                    RControllers.AddWaypoint(AppLocation, (Ship + "Approach Location"));
                    RControllers.AddWaypoint(StartLocation, (Ship + "Start Location"));
                    RControllers.SetAutoPilotEnabled(true);
                    RControllers.SetCollisionAvoidance(false);
                    RControllers.SetDockingMode(true);
                    Main("Approach");
                }
            }
        }

        public void Approach()
        {
            RController.TryGetPlanetElevation(MyPlanetElevation.Surface, out Elev);
            if (!GearDown)
            {
                if (!GearDown && (Elev < (StartElev + 50)))
                {
                    LGear.ApplyAction("TriggerNow");
                    GearDown = true;
                    Main("Land");
                }
            }
        }

        public void Land()
        {
            RController.TryGetPlanetElevation(MyPlanetElevation.Surface, out Elev);
            if (!AutoEnable)
            {
                RControllers.SetAutoPilotEnabled(false);
                RControllers.SetCollisionAvoidance(false);
                RControllers.SetDockingMode(false);
                RController.DampenersOverride = false;
                Main("Landed");
            }
        }
    }

}
