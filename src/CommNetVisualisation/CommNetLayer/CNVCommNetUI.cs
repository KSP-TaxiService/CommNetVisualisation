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
            [Description("#autoLOC_6003083")]
            None,
            [Description("#autoLOC_6003084")]
            FirstHop,
            [Description("#autoLOC_CommNetVisualisation_ModePath")]
            Path,
            [Description("#autoLOC_6003086")]
            VesselLinks,
            [Description("#autoLOC_6003087")]
            Network,
            [Description("#autoLOC_CommNetVisualisation_ModeMultiPaths")]
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
            bool pathLinkExist;
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
                        path = new CommPath();
                        path.Capacity = net.Links.Count;

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
                                //add each link in control path to overall path
                                for (int controlpathIndex = 0; controlpathIndex < commnetvessel.ControlPath.Count; controlpathIndex++)
                                {
                                    pathLinkExist = false;
                                    for (int overallpathIndex = 0; overallpathIndex < path.Count; overallpathIndex++)//check if overall path has this link already
                                    {
                                        if (path[overallpathIndex].a.precisePosition == commnetvessel.ControlPath[controlpathIndex].a.precisePosition &&
                                            path[overallpathIndex].b.precisePosition == commnetvessel.ControlPath[controlpathIndex].b.precisePosition)
                                        {
                                            pathLinkExist = true;
                                            break;
                                        }
                                    }
                                    if (!pathLinkExist)
                                    {
                                        path.Add(commnetvessel.ControlPath[controlpathIndex]); //laziness wins
                                        //KSP techincally does not care if path is consisted of non-continuous links or not
                                    }
                                }
                            }
                        }

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
                        this.line.SetColor(colorBlending(this.colorHigh,
                                                         this.colorLow,
                                                         Mathf.Pow((float)path.First.signalStrength, this.colorLerpPower)),
                                                         0);
                        break;
                }
                case CNVCommNetUI.CustomDisplayMode.Path:
                case CNVCommNetUI.CustomDisplayMode.MultiPaths:
                {
                        for (int i = numLinks - 1; i >= 0; i--)
                        {
                            this.line.SetColor(colorBlending(this.colorHigh,
                                                             this.colorLow,
                                                             Mathf.Pow((float)path[i].signalStrength, this.colorLerpPower)),
                                                             i);
                        }
                        break;
                }
                case CNVCommNetUI.CustomDisplayMode.VesselLinks:
                {
                        CommLink[] links = new CommLink[node.Count];
                        node.Values.CopyTo(links, 0);
                        for (int i = 0; i < links.Length; i++)
                        {
                            this.line.SetColor(colorBlending(this.colorHigh,
                                                             this.colorLow,
                                                             Mathf.Pow((float)links[i].GetSignalStrength(links[i].a != node, links[i].b != node), this.colorLerpPower)),
                                                             i);
                        }
                        break;
                }
                case CNVCommNetUI.CustomDisplayMode.Network:
                {
                        for (int i = numLinks - 1; i >= 0; i--)
                        {
                            this.line.SetColor(colorBlending(this.colorHigh,
                                                             this.colorLow,
                                                             Mathf.Pow((float)net.Links[i].GetBestSignal(), this.colorLerpPower)),
                                                             i);
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

        /// <summary>
        /// Compute final color based on inputs
        /// </summary>
        private Color colorBlending(Color colorHigh, Color colorLow, float colorLevel)
        {
            if (colorHigh == Color.clear)
            {
                return colorHigh;
            }
            else if (this.swapHighLow)
            {
                return Color.Lerp(colorHigh, colorLow, colorLevel);
            }
            else
            {
                return Color.Lerp(colorLow, colorHigh, colorLevel);
            }
        }
    }
}
