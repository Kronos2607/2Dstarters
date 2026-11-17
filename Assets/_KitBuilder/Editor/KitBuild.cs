// Batch-mode entry points (see ../../../build.sh). Not exported.
//   KitBuild.Prepare  - TMP Essentials and sprite pivots (run once, in its own Unity session)
//   KitBuild.BuildAll - builds both kits and exports the two .unitypackage files next to the project
using System.IO;
using UnityEditor;
using UnityEngine;

public static class KitBuild
{
    public static void Prepare()
    {
        KitHelpers.ImportTMPEssentials();
        TopDownKitBuilder.Prepare();
        AssetDatabase.SaveAssets();
        Debug.Log("KITBUILD Prepare done");
    }

    public static void BuildAll()
    {
        try
        {
            string log = PlatformerKitBuilder.Build();
            log += TopDownKitBuilder.Build();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            log += KitHelpers.Export("Assets/PlatformerStarter", Path.GetFullPath("../CRE132_PlatformerStarter.unitypackage")) + "\n";
            log += KitHelpers.Export("Assets/TopDownStarter", Path.GetFullPath("../CRE132_TopDownStarter.unitypackage")) + "\n";
            Debug.Log("KITBUILD\n" + log);
        }
        catch (System.Exception e)
        {
            Debug.LogError("KITBUILD FAILED " + e);
            EditorApplication.Exit(1);
        }
    }
}
