using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Renci.SshNet;
using UnityEditor;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Ottermation.Utils.Deploy
{
    public static class DeployHandler
    {
        public static void Deploy(DeployConfig config)
        {
            if (config is null)
            {
                Debug.LogWarning("Attempted to deploy with no DeployConfig");
                return;
            }
            
            BuildAndDeployConfig(config);
        }

        private static void BuildAndDeployConfig(DeployConfig config)
        {
            Directory.CreateDirectory(config.BuildOutputPath);
            string[] scenes = Enumerable.Range(0, SceneManager.sceneCountInBuildSettings)
                .Select(SceneUtility.GetScenePathByBuildIndex).ToArray();
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions()
            {
                scenes = scenes,
                target = config.GetBuildTarget(),
                locationPathName = config.GetBuildPath(),
            };

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.LogError($"Build failed for {config.Description}");
                return;
            }

            foreach (TargetConfig target in config.Targets)
            {
                DeployToMachine(config, target);
            }
        }

        private static void DeployToMachine(DeployConfig config, TargetConfig target)
        {
            Debug.Log($"Deploying {config.Description} to {target.Description} ({target.Hostname})");
            using (SftpClient client = new SftpClient(target.Hostname, target.Port, target.Username, target.Password))
            {
                client.Connect();
                UploadDirectory(client, config.GetBuildDirectory(), target.RemotePath);
            }

            using (SshClient client = new SshClient(target.Hostname, target.Port, target.Username, target.Password))
            {
                client.Connect();
                string fileName = $"{config.FileName}{DeployConfig.PlatformToExtension(config.Platform)}";
                client.RunCommand($"cd {target.RemotePath} && chmod +x {fileName} && screen -A -m -d -S {config.FileName} ./{fileName} &");
                client.Disconnect();
            }
            Debug.Log($"Deploy {config.Description} to {target.Description} ({target.Hostname}) completed");
        }

        private static void UploadDirectory(SftpClient client, string localPath, string remotePath)
        {
            string path = String.Empty;
            foreach (var p in remotePath.Split('/'))
            {
                path += p;
                if (!client.Exists(path))
                {
                    client.CreateDirectory(path);
                }

                path += "/";
            }
            
            IEnumerable<FileSystemInfo> infos =
                new DirectoryInfo(localPath).EnumerateFileSystemInfos();
            foreach (FileSystemInfo info in infos)
            {
                if (info.Attributes.HasFlag(FileAttributes.Directory))
                {
                    string subPath = remotePath + "/" + info.Name;
                    if (!client.Exists(subPath))
                    {
                        client.CreateDirectory(subPath);
                    }
                    UploadDirectory(client, info.FullName, remotePath + "/" + info.Name);
                }
                else
                {
                    using var fileStream = new FileStream(info.FullName, FileMode.Open);
                    client.UploadFile(fileStream, remotePath + "/" + info.Name);
                }
            }
        }
    }
}