using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ArcaneArchive.Visuals.Editor
{
    public static class SanctuaryR3PlayerBuild
    {
        [Serializable] class Report
        {
            public string result,scene,output,unity,scope;
            public ulong bytes;
            public int errors,warnings;
            public double seconds;
        }
        public static void BuildMac()
        {
            string output=Path.GetFullPath("Builds/SanctuaryR3_R15/KnightSanctuary.app");
            if(Directory.Exists(output))throw new IOException("Preserve existing player build: "+output);
            if(!File.Exists(SanctuaryR3CombatBuilder.Scene))throw new FileNotFoundException("Build R3 combat scene first");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var build=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=new[]{SanctuaryR3CombatBuilder.Scene},locationPathName=output,
                target=BuildTarget.StandaloneOSX,options=BuildOptions.Development|BuildOptions.CompressWithLz4
            });
            var summary=build.summary;
            File.WriteAllText("Docs/SanctuaryR3Build/StandaloneBuildR15.json",JsonUtility.ToJson(new Report{
                result=summary.result.ToString(),scene=SanctuaryR3CombatBuilder.Scene,output=output,
                unity=Application.unityVersion,bytes=summary.totalSize,errors=summary.totalErrors,warnings=summary.totalWarnings,
                seconds=summary.totalTime.TotalSeconds,scope="Mac development player for local knight-scene testing; not Windows, final release or target-device performance acceptance."
            },true));
            if(summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("R3 native build failed");
            Debug.Log("R3_MAC_PLAYER_BUILT "+output);
        }
    }
}
