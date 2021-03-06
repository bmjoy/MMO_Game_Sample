using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MultiPlayersBuildAndRun
{
    [MenuItem("Tools/RunMultipayer/2 Players")]
    static void PerformWin64Build2()
    {
        PerformWin64Build(2);
    }
    [MenuItem("Tools/RunMultipayer/3 Players")]
    static void PerformWin64Build3()
    {
        PerformWin64Build(3);
    }
    [MenuItem("Tools/RunMultipayer/4 Players")]
    static void PerformWin64Build4()
    {
        PerformWin64Build(4);
    }
    [MenuItem("Tools/RunMultipayer/6 Players")]
    static void PerformWin64Build6()
    {
        PerformWin64Build(6);
    }
    [MenuItem("Tools/RunMultipayer/10 Players")]
    static void PerformWin64Build10()
    {
        PerformWin64Build(10);
    }

    static void PerformWin64Build(int playerCount)
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(
            BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows);
        for (int i = 1; i <= playerCount; i++)
        {
            BuildPipeline.BuildPlayer(GetScenePaths(), 
                "Builds/Win64/" + GetProjectName() + i.ToString() + "/" + GetProjectName() + i.ToString() + ".exe", 
                BuildTarget.StandaloneWindows64, BuildOptions.AutoRunPlayer);
        }
    }

    static string GetProjectName()
    {
        string[] s = Application.dataPath.Split('/');
        return s[s.Length - 2];
    }

    static string[] GetScenePaths()
    {
        string[] scene = new string[EditorBuildSettings.scenes.Length];
        for (int i = 0; i < scene.Length; i++)
        {
            scene[i] = EditorBuildSettings.scenes[i].path;
        }
        return scene;
    }
}
