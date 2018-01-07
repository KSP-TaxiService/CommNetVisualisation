using CommNet;

namespace CommNetVisualisation.CommNetLayer
{
    public class CNVCommNetVessel : CommNetVessel
    {
        /// <summary>
        /// On-demand method to do network update manually
        /// </summary>
        public void computeUnloadedUpdate()
        {
            this.unloadedDoOnce = true;
        }
    }
}
