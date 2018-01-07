using CommNet;
using KSP.UI.Screens.Flight;
using System;
using static CommNetVisualisation.CommNetLayer.CNVTelemetryUpdate;

namespace CommNetVisualisation.CommNetLayer
{
    /// <summary>
    /// This class is the key that allows to break into and customise KSP's CommNet. This is possibly the secondary model in the Model–view–controller sense
    /// </summary>
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, new GameScenes[] { GameScenes.FLIGHT, GameScenes.TRACKSTATION})]
    public class CNVCommNetScenario : CommNetScenario
    {
        /* Note:
         * 1) On entering a desired scene, OnLoad() and then Start() are called.
         * 2) On leaving the scene, OnSave() is called
         * 3) GameScenes.SPACECENTER is recommended so that the constellation data can be verified and error-corrected in advance
         */

        private CNVCommNetUI CustomCommNetUI = null;
        private CNVTelemetryUpdate CustomCommNetTelemetry = null;
        private CNVCommNetUIModeButton CustomCommNetModeButton = null;

        public static new CNVCommNetScenario Instance
        {
            get;
            protected set;
        }

        protected override void Start()
        {
            CNVCommNetScenario.Instance = this;

            UnityEngine.Debug.Log("CommNet Scenario loading ...");

            //Replace the CommNet user interface
            CommNetUI ui = FindObjectOfType<CommNetUI>(); // the order of the three lines is important
            CustomCommNetUI = gameObject.AddComponent<CNVCommNetUI>(); // gameObject.AddComponent<>() is "new" keyword for Monohebaviour class
            UnityEngine.Object.Destroy(ui);

            //Replace the TelemetryUpdate
            TelemetryUpdate tel = TelemetryUpdate.Instance; //only appear in flight
            CommNetUIModeButton cnmodeUI = FindObjectOfType<CommNetUIModeButton>(); //only appear in tracking station; initialised separately by TelemetryUpdate in flight
            if (tel != null && HighLogic.LoadedSceneIsFlight)
            {
                TelemetryUpdateData tempData = new TelemetryUpdateData(tel);
                UnityEngine.Object.DestroyImmediate(tel); //seem like UE won't initialise CNCTelemetryUpdate instance in presence of TelemetryUpdate instance
                CustomCommNetTelemetry = gameObject.AddComponent<CNVTelemetryUpdate>();
                CustomCommNetTelemetry.copyOf(tempData);
            }
            else if (cnmodeUI != null && HighLogic.LoadedScene == GameScenes.TRACKSTATION)
            {
                CustomCommNetModeButton = cnmodeUI.gameObject.AddComponent<CNVCommNetUIModeButton>();
                CustomCommNetModeButton.copyOf(cnmodeUI);
                UnityEngine.Object.DestroyImmediate(cnmodeUI);
            }

            UnityEngine.Debug.Log("CommNet Scenario loading done! ");
        }

        public override void OnAwake()
        {
            //override to turn off CommNetScenario's instance check
        }

        private void OnDestroy()
        {
            if (this.CustomCommNetUI != null)
                UnityEngine.Object.Destroy(this.CustomCommNetUI);

            if (this.CustomCommNetTelemetry != null)
                UnityEngine.Object.Destroy(this.CustomCommNetTelemetry);

            if (this.CustomCommNetModeButton != null)
                UnityEngine.Object.Destroy(this.CustomCommNetModeButton);
        }

        public override void OnLoad(ConfigNode gameNode)
        {
            base.OnLoad(gameNode);
            UnityEngine.Debug.Log(string.Format("Scenario content to be read:\n{0}", gameNode));

            //Other variables
            for (int i = 0; i < gameNode.values.Count; i++)
            {
                ConfigNode.Value value = gameNode.values[i];
                string name = value.name;
                switch (name)
                {
                    case "DisplayModeTracking":
                        CNVCommNetUI.CustomModeTrackingStation = (CNVCommNetUI.CustomDisplayMode)((int)Enum.Parse(typeof(CNVCommNetUI.CustomDisplayMode), value.value));
                        break;
                    case "DisplayModeFlight":
                        CNVCommNetUI.CustomModeFlightMap = (CNVCommNetUI.CustomDisplayMode)((int)Enum.Parse(typeof(CNVCommNetUI.CustomDisplayMode), value.value));
                        break;
                }
            }
        }

        public override void OnSave(ConfigNode gameNode)
        {
            //Other variables
            gameNode.AddValue("DisplayModeTracking", CNVCommNetUI.CustomModeTrackingStation);
            gameNode.AddValue("DisplayModeFlight", CNVCommNetUI.CustomModeFlightMap);

            UnityEngine.Debug.Log(string.Format("Scenario content to be saved:\n{0}", gameNode));
            base.OnSave(gameNode);
        }
    }
}
