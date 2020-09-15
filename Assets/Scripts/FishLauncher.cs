using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityTools.Common;

namespace UnityFishSimulation
{
    public class FishLauncher : Launcher<FishLauncher.Data>
    {
        [Serializable]
        public class Data
        {
        }

        protected override void ConfigureEnvironment()
        {
            base.ConfigureEnvironment();
            
            #if !DEBUG
            this.environment.runtime = Environment.Runtime.Production;
            #endif
        }

    }
}