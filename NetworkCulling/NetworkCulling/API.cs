using System;
using HarmonyLib;
using System.Collections.Generic;
using FullSerializer;
using System.Runtime.CompilerServices;

namespace NetworkCulling
{
    public class API : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            Log.Out("[NetworkCulling]: InitMod(modinstance): Trying to patch with harmony...");
            Harmony harmony = new Harmony("com.redhatdragon.networkculling");
            NetPackage_write_Pre.init();
            harmony.PatchAll();
            Log.Out("[NetworkCulling]: InitMod(modinstance): Harmony patch gone through...");
        }
    }



    class NetPackageLog : LogData {
        public uint totalBytesSent;
        public override void mergeOtherOfSameType(LogData other) {
            totalBytesSent += ((NetPackageLog)other).totalBytesSent;
        }
        public override string getAsString() {
            return "Total bytes sent: " + totalBytesSent.ToString();
        }
        public override void setFromString(string str) {

        }
    }



    [HarmonyPatch(typeof(NetPackage), "write")]
    [HarmonyPatch(new Type[] { typeof(PooledBinaryWriter) })]
    public static class NetPackage_write_Pre {
        private static Logger logger = new Logger();
        private static uint count = 0;

        public static void init() {
            logger = new Logger();
        }

        //[HarmonyReversePatch]
        //[MethodImpl(MethodImplOptions.NoInlining)]
        //static void reversePatch(NetPackage netPackage, PooledBinaryWriter _writer) {
        //_writer.Write((byte)netPackage.PackageId);
        //}

        [HarmonyReversePatch]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool Prefix(NetPackage __instance, PooledBinaryWriter _writer) {
            try {
                string instanceName = __instance.GetType().CSharpName();
                //string str = instanceName + ',' + __instance.GetLength().ToString();

                NetPackageLog log = new NetPackageLog();
                log.startTime();

                //NOTE: their code copied into this block...
                _writer.Write((byte)__instance.PackageId);
                //END

                log.endTime();
                if (__instance.GetLength() >= 0)
                    log.totalBytesSent = (uint)__instance.GetLength();
                else
                    throw new Exception("Error: __instance.GetLength() < 0?!");
                logger.log(instanceName, log);

                if (count == 100) {
                    //Log.Out(logger.ToString());
                    //Console.WriteLine(logger.ToString());

                    //string outStr = logger.ToString();
                    //outStr = outStr.Substring(0, 20);
                    //Log.Out(outStr);
                    Log.Out("START");
                    Log.Out(logger.getAsStr());
                    Log.Out("END");
                    count = 0;
                }
                count++;
            } catch (Exception e) {
                    Log.Error($"NetPackage_write_Pre reported: {e.Message}");
            }
            return false;
        }
    }

    /*[HarmonyPatch(typeof(StabilityCalculator), "Init")]
     public class OnStabilityCalculatorInit_Pre
     {
         static bool Prefix(WorldBase _world)
         {
             try
             {
                 StabilityCalculatorEx.Init(_world);
             }
             catch (Exception e) { Log.Error($"OnStabilityCalculatorInit_Pre reported: {e.Message}"); }

             return false;
         }
     }
     [HarmonyPatch(typeof(StabilityCalculator), "BlockRemovedAt")]
     public class OnBlockRemovedAt_Pre
     {
         static bool Prefix(Vector3i _pos)
         {
             try
             {
                 StabilityCalculatorEx.RemovedBlock(_pos);
             }
             catch (Exception e) { Log.Error($"OnBlockRemovedAt_Pre reported: {e.Message}"); }

             return false;
         }
     }
     [HarmonyPatch(typeof(StabilityCalculator), "BlockPlacedAt")]
     public class OnBlockPlacedAt_Pre
     {
         static bool Prefix(Vector3i _pos, bool _bForceFullStabe = false)
         {
             try
             {
                 StabilityCalculatorEx.PlacedBlock(_pos, _bForceFullStabe);
             }
             catch (Exception e) { Log.Error($"OnBlockPlacedAt_Pre reported: {e.Message}"); }

             return false;
         }
     }*/
}

/*public class API : IModApi {
    public void InitMod(Mod _modInstance) {
        Harmony harmony = new Harmony("com.zipcore.stabilitymanager");
        harmony.PatchAll();
        Log.Out("InitMod(modinstance): Harmony patch gone through...");
    }
}
[HarmonyPatch(typeof(StabilityCalculator), "Init")]
public class OnStabilityCalculatorInit_Pre {
    static bool Prefix(WorldBase _world) {
        try {
            StabilityManager.StabilityCalculatorEx.Init(_world);
        } catch (Exception e) { Log.Error($"OnStabilityCalculatorInit_Pre reported: {e.Message}"); }

        return false;
    }
}
[HarmonyPatch(typeof(StabilityCalculator), "BlockRemovedAt")]
public class OnBlockRemovedAt_Pre {
    static bool Prefix(Vector3i _pos) {
        try {
            StabilityManager.StabilityCalculatorEx.RemovedBlock(_pos);
        } catch (Exception e) { Log.Error($"OnBlockRemovedAt_Pre reported: {e.Message}"); }

        return false;
    }
}
[HarmonyPatch(typeof(StabilityCalculator), "BlockPlacedAt")]
public class OnBlockPlacedAt_Pre {
    static bool Prefix(Vector3i _pos, bool _bForceFullStabe = false) {
        try {
            StabilityManager.StabilityCalculatorEx.PlacedBlock(_pos, _bForceFullStabe);
        } catch (Exception e) { Log.Error($"OnBlockPlacedAt_Pre reported: {e.Message}"); }

        return false;
    }
}*/