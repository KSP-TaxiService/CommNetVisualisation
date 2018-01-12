using CommNet;
using KSP.Localization;
using KSP.UI.Screens.Mapview;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;

namespace CommNetVisualisation.CommNetLayer
{
    /// <summary>
    /// CommNetUI is the view in the Model–view–controller sense. Everything a player is seeing goes through this class
    /// </summary>
    public class CNVCommNetUI : CommNetUI
    {
        /// <summary>
        /// Add own display mode in replacement of stock display mode which cannot be extended easily
        /// </summary>
        public enum CustomDisplayMode
        {
            [Description("None")]
            None,
            [Description("First Hop")]
            FirstHop,
            [Description("Working Connection")]
            Path,
            [Description("Vessel Links")]
            VesselLinks,
            [Description("Network")]
            Network,
            [Description("All Working Connections")]
            MultiPaths
        }

        //New variables related to display mode
        public static CustomDisplayMode CustomMode = CustomDisplayMode.Path;
        public static CustomDisplayMode CustomModeTrackingStation = CustomDisplayMode.Network;
        public static CustomDisplayMode CustomModeFlightMap = CustomDisplayMode.Path;
        private static int CustomModeCount = Enum.GetValues(typeof(CustomDisplayMode)).Length;

        public static new CNVCommNetUI Instance
        {
            get;
            protected set;
        }

        /// <summary>
        /// Run own display updates
        /// </summary>
        protected override void UpdateDisplay()
        {
            if (CommNetNetwork.Instance == null)
            {
                return;
            }
            else
            {
                updateCustomisedView();
            }
        }

        /// <summary>
        /// Overrode ResetMode to use custom display mode
        /// </summary>
        public override void ResetMode()
        {
            CNVCommNetUI.CustomMode = CNVCommNetUI.CustomDisplayMode.None;

            if (FlightGlobals.ActiveVessel == null)
            {
                CNVCommNetUI.CustomModeTrackingStation = CNVCommNetUI.CustomMode;
            }
            else
            {
                CNVCommNetUI.CustomModeFlightMap = CNVCommNetUI.CustomMode;
            }

            this.points.Clear();
            ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_118264", new string[]
            {
                Localizer.Format(CNVCommNetUI.CustomMode.displayDescription())
            }), 5f);
        }

        /// <summary>
        /// Overrode SwitchMode to use custom display mode
        /// </summary>
        public override void SwitchMode(int step)
        {
            int modeIndex = (((int)CNVCommNetUI.CustomMode) + step + CNVCommNetUI.CustomModeCount) % CNVCommNetUI.CustomModeCount;
            CNVCommNetUI.CustomDisplayMode newMode = (CNVCommNetUI.CustomDisplayMode)modeIndex;

            if (this.useTSBehavior)
            {
                this.ClampAndSetMode(ref CNVCommNetUI.CustomModeTrackingStation, newMode);
            }
            else
            {
                this.ClampAndSetMode(ref CNVCommNetUI.CustomModeFlightMap, newMode);
            }

            this.points.Clear();
            ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_118530", new string[]
            {
                Localizer.Format(CNVCommNetUI.CustomMode.displayDescription())
            }), 5f);
        }

        /// <summary>
        /// Add new ClampAndSetMode for custom display mode
        /// </summary>
        public void ClampAndSetMode(ref CNVCommNetUI.CustomDisplayMode curMode, CNVCommNetUI.CustomDisplayMode newMode)
        {
            if (this.vessel == null || this.vessel.connection == null || this.vessel.connection.Comm.Net == null)
            {
                if (newMode != CNVCommNetUI.CustomDisplayMode.None &&
                    newMode != CNVCommNetUI.CustomDisplayMode.Network &&
                    newMode != CNVCommNetUI.CustomDisplayMode.MultiPaths)
                {
                    newMode = ((curMode != CNVCommNetUI.CustomDisplayMode.None) ? CNVCommNetUI.CustomDisplayMode.None : CNVCommNetUI.CustomDisplayMode.Network);
                }
            }

            CNVCommNetUI.CustomMode = (curMode = newMode);
        }

        /// <summary>
        /// Overrode UpdateDisplay() fully and add own customisations
        /// </summary>
        private void updateCustomisedView()
        {
            if (FlightGlobals.ActiveVessel == null)
            {
                this.useTSBehavior = true;
            }
            else
            {
                this.useTSBehavior = false;
                this.vessel = FlightGlobals.ActiveVessel;
            }

            if (this.vessel == null || this.vessel.connection == null || this.vessel.connection.Comm.Net == null) //revert to default display mode if saved mode is inconsistent in current situation
            {
                this.useTSBehavior = true;
                if (CustomModeTrackingStation != CustomDisplayMode.None)
                {
                    if (CustomModeTrackingStation != CustomDisplayMode.Network && CustomModeTrackingStation != CustomDisplayMode.MultiPaths)
                    {
                        CustomModeTrackingStation = CustomDisplayMode.Network;
                        ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_118264", new string[]
                        {
                            Localizer.Format(CustomModeTrackingStation.displayDescription())
                        }), 5f);
                    }
                }
            }

            if (this.useTSBehavior)
            {
                CNVCommNetUI.CustomMode = CNVCommNetUI.CustomModeTrackingStation;
            }
            else
            {
                CNVCommNetUI.CustomMode = CNVCommNetUI.CustomModeFlightMap;
            }

            CommNetwork net = CommNetNetwork.Instance.CommNet;
            CommNetVessel cnvessel = null;
            CommNode node = null;
            CommPath path = null;

            if (this.vessel != null && this.vessel.connection != null && this.vessel.connection.Comm.Net != null)
            {
                cnvessel = this.vessel.connection;
                node = cnvessel.Comm;
                path = cnvessel.ControlPath;
            }

            //work out which links to display
            int count = this.points.Count;//save previous value
            int numLinks = 0;
            switch (CNVCommNetUI.CustomMode)
            {
                case CNVCommNetUI.CustomDisplayMode.None:
                    numLinks = 0;
                    break;

                case CNVCommNetUI.CustomDisplayMode.FirstHop:
                case CNVCommNetUI.CustomDisplayMode.Path:
                    if (cnvessel.ControlState == VesselControlState.Probe || cnvessel.ControlState == VesselControlState.Kerbal ||
                        path.Count == 0)
                    {
                        numLinks = 0;
                    }
                    else
                    {
                        if (CNVCommNetUI.CustomMode == CNVCommNetUI.CustomDisplayMode.FirstHop)
                        {
                            path.First.GetPoints(this.points);
                            numLinks = 1;
                        }
                        else
                        {
                            path.GetPoints(this.points, true);
                            numLinks = path.Count;
                        }
                    }
                    break;

                case CNVCommNetUI.CustomDisplayMode.VesselLinks:
                    numLinks = node.Count;
                    node.GetLinkPoints(this.points);
                    break;

                case CNVCommNetUI.CustomDisplayMode.Network:
                    if (net.Links.Count == 0)
                    {
                        numLinks = 0;
                    }
                    else
                    {
                        numLinks = net.Links.Count;
                        net.GetLinkPoints(this.points);
                    }
                    break;
                case CNVCommNetUI.CustomDisplayMode.MultiPaths:
                    if (net.Links.Count == 0)
                    {
                        numLinks = 0;
                    }
                    else
                    {
                        CommPath newPath = new CommPath();

                        var nodes = net;
                        var vessels = FlightGlobals.fetch.vessels;
                        for (int i = 0; i < vessels.Count; i++)
                        {
                            var commnetvessel = vessels[i].connection;
                            if(commnetvessel == null) // like flag
                            {
                                continue;
                            }

                            SetPrivatePropertyValue<CommNetVessel>(commnetvessel, "unloadedDoOnce", true);//network update is done only once for unloaded vessels so need to manually re-trigger every time
                            //don't want to override CommNetVessel to just set the boolean flag

                            if (!(commnetvessel.ControlState == VesselControlState.Probe || commnetvessel.ControlState == VesselControlState.Kerbal ||
                                commnetvessel.ControlPath == null || commnetvessel.ControlPath.Count == 0))
                            {
                                for (int pathIndex = 0; pathIndex < commnetvessel.ControlPath.Count; pathIndex++)
                                {
                                    var link = commnetvessel.ControlPath[pathIndex];
                                    if (newPath.Find(x => x.a.precisePosition == link.a.precisePosition && x.b.precisePosition == link.b.precisePosition) == null)//not found in list of links to be displayed
                                    {
                                        newPath.Add(link); //laziness wins
                                        //KSP techincally does not care if path is consisted of non-continuous links or not
                                    }
                                }
                            }
                        }

                        path = newPath;
                        path.GetPoints(this.points, true);
                        numLinks = path.Count;
                    }
                    break;
            }// end of switch

            //check if nothing to display
            if (numLinks == 0)
            {
                if (this.line != null)
                    this.line.active = false;

                this.points.Clear();
                return;
            }

            if (this.line != null)
            {
                this.line.active = true;
            }
            else
            {
                this.refreshLines = true;
            }

            ScaledSpace.LocalToScaledSpace(this.points); //seem very important

            if (this.refreshLines || MapView.Draw3DLines != this.draw3dLines || count != this.points.Count || this.line == null)
            {
                this.CreateLine(ref this.line, this.points);//seems it is multiple separate lines not single continuous line
                this.draw3dLines = MapView.Draw3DLines;
                this.refreshLines = false;
            }

            //paint the links
            switch (CNVCommNetUI.CustomMode)
            {
                case CNVCommNetUI.CustomDisplayMode.FirstHop:
                    {
                        float lvl = Mathf.Pow((float)path.First.signalStrength, this.colorLerpPower);
                        if (this.swapHighLow)
                            this.line.SetColor(Color.Lerp(this.colorHigh, this.colorLow, lvl), 0);
                        else
                            this.line.SetColor(Color.Lerp(this.colorLow, this.colorHigh, lvl), 0);
                        break;
                    }
                case CNVCommNetUI.CustomDisplayMode.Path:
                case CNVCommNetUI.CustomDisplayMode.MultiPaths:
                    {
                        int linkIndex = numLinks;
                        for (int i = linkIndex - 1; i >= 0; i--)
                        {
                            float lvl = Mathf.Pow((float)path[i].signalStrength, this.colorLerpPower);
                            if (this.swapHighLow)
                                this.line.SetColor(Color.Lerp(this.colorHigh, this.colorLow, lvl), i);
                            else
                                this.line.SetColor(Color.Lerp(this.colorLow, this.colorHigh, lvl), i);
                        }
                        break;
                    }
                case CNVCommNetUI.CustomDisplayMode.VesselLinks:
                    {
                        var itr = node.Values.GetEnumerator();
                        int linkIndex = 0;
                        while (itr.MoveNext())
                        {
                            CommLink link = itr.Current;
                            float lvl = Mathf.Pow((float)link.GetSignalStrength(link.a != node, link.b != node), this.colorLerpPower);
                            if (this.swapHighLow)
                                this.line.SetColor(Color.Lerp(this.colorHigh, this.colorLow, lvl), linkIndex++);
                            else
                                this.line.SetColor(Color.Lerp(this.colorLow, this.colorHigh, lvl), linkIndex++);
                        }
                        break;
                    }
                case CNVCommNetUI.CustomDisplayMode.Network:
                    {
                        for (int i = numLinks - 1; i >= 0; i--)
                        {
                            CommLink commLink = net.Links[i];
                            float lvl = Mathf.Pow((float)net.Links[i].GetBestSignal(), this.colorLerpPower);
                            if (this.swapHighLow)
                                this.line.SetColor(Color.Lerp(this.colorHigh, this.colorLow, lvl), i);
                            else
                                this.line.SetColor(Color.Lerp(this.colorLow, this.colorHigh, lvl), i);
                        }
                        break;
                    }
            } // end of switch

            if (this.draw3dLines)
            {
                this.line.SetWidth(this.lineWidth3D);
                this.line.Draw3D();
            }
            else
            {
                this.line.SetWidth(this.lineWidth2D);
                this.line.Draw();
            }
        }

        /// <summary>
        /// Change the non-public property of target T
        /// </summary>
        //Copied from https://stackoverflow.com/questions/1565734/is-it-possible-to-set-private-property-via-reflection
        public static void SetPrivatePropertyValue<T>(T obj, string propertyName, object newValue)
        {
            if(obj == null)
            {
                UnityEngine.Debug.LogError("Object to access one of its non-public attributes is null!");
                return;
            }

            foreach (FieldInfo fi in obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (fi.Name.Contains(propertyName))
                {
                    fi.SetValue(obj, newValue);
                    break;
                }
            }
        }
    }
}
