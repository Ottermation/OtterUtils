using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ottermation.Utils.Deploy
{
    [Serializable]
    public class DeployConfig
    {
        public string Description;
        public Platform Platform = Platform.Linux;
        public string FileName = "Game";
        public string BuildOutputPath = "Builds/";
        public List<TargetConfig> Targets = new();

        public static BuildTarget PlatformToBuildTarget(Platform platform) => platform switch
        {
            Platform.Linux => BuildTarget.StandaloneLinux64,
            Platform.Windows => BuildTarget.StandaloneWindows64,
            Platform.MacOS => BuildTarget.StandaloneOSX,
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
        };

        public static string PlatformToExtension(Platform platform) => platform switch
        {
            Platform.Linux => ".x86_64",
            Platform.Windows => ".exe",
            Platform.MacOS => ".app",
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
        };
        
        public string GetExtension() => PlatformToExtension(Platform);
        public string GetBuildDirectory() => $"{BuildOutputPath}/{Platform.ToString()}/";
        public string GetBuildPath() => $"{GetBuildDirectory()}{FileName}{PlatformToExtension(Platform)}";
        public BuildTarget GetBuildTarget() => PlatformToBuildTarget(Platform);
    }
    
    [Serializable]
    public class TargetConfig
    {
        public string Description;
        public string Hostname;
        public int Port = 22;
        public string Username;
        public string Password;
        public string RemotePath = "~/deploys";
    }
    
    public enum Platform
    {
        Windows,
        Linux,
        MacOS
    }
}