using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ottermation.Utils.Deploy
{
    public class DeploySettingsWindow : EditorWindow
    {
        private DeployConfigList allConfigs => DeployConfigList.instance;
        private ListView configListView;
        private VisualElement targetListContainer;

        [MenuItem("Window/OtterUtils/Deploy Settings")]
        public static void ShowWindow()
        {
            DeploySettingsWindow window = GetWindow<DeploySettingsWindow>();
            Texture2D icon = EditorGUIUtility.IconContent("Settings").image as Texture2D;
            window.titleContent = new GUIContent("Deploy Settings", icon, "Deploy Settings");
            window.minSize = new Vector2(800, 600);
        }

        private void OnEnable()
        {
            CreateUI();
        }

        private void CreateUI()
        {
            rootVisualElement.Clear();

            Toolbar toolbar = new Toolbar();
            Texture2D icon = EditorGUIUtility.IconContent("d_Toolbar Plus").image as Texture2D;
            var addConfigButton = new ToolbarButton(() => 
            {
                DeployConfig config = new DeployConfig();
                config.Description = GenerateConfigName(allConfigs);
                allConfigs.DeployConfigs.Add(config);
                EditorUtility.SetDirty(allConfigs);
                allConfigs.Save();
                AssetDatabase.SaveAssets();
                RefreshUI();
            })
            { text = "Add Config", iconImage = icon };
            toolbar.Add(addConfigButton);
            rootVisualElement.Add(toolbar);

            VisualElement contentRoot = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                }
            };
            rootVisualElement.Add(contentRoot);

            configListView = new ListView
            {
                itemsSource = allConfigs.DeployConfigs,
                selectionType = SelectionType.Single,
                style =
                {
                    flexGrow = 0,
                    flexDirection = FlexDirection.Row,
                    width = new StyleLength(200)
                },
                makeItem = () =>
                {
                    VisualElement element = new()
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Row,
                            alignItems = Align.Center,
                        }
                    };
                    element.Add(new Label()
                    {
                        style = { paddingLeft = new StyleLength(6) }
                    });
                    return element;
                },
                bindItem = (element, i) =>
                {
                    var label = element.Q<Label>();
                    label.text = allConfigs.DeployConfigs[i]?.Description ?? "[Unknown]";
                }
            };

            configListView.selectionChanged += OnConfigSelected;
            contentRoot.Add(configListView);

            targetListContainer = new()
            {
                style =
                {
                    flexGrow = 1,
                    marginLeft = new StyleLength(6),
                    marginRight = new StyleLength(6),
                    marginTop = new StyleLength(6),
                    marginBottom = new StyleLength(6)
                }
            };
            contentRoot.Add(targetListContainer);

            RefreshUI();
        }

        private void OnConfigSelected(IEnumerable<object> selected)
        {
            targetListContainer.Clear();
            IEnumerator<object> selectedEnumerator = selected.GetEnumerator();
            if (!selectedEnumerator.MoveNext())
                return;

            DeployConfig config = selectedEnumerator.Current as DeployConfig;
            if (config is null) return;

            TextField renameField = new("Config Name")
            {
                value = config.Description
            };
            renameField.RegisterValueChangedCallback(e =>
            {
                if (IsConfigNameTaken(e.newValue, allConfigs))
                {
                    Debug.LogWarning($"A config named {e.newValue} already exists.");
                    renameField.SetValueWithoutNotify(config.Description);
                    return;
                }

                config.Description = e.newValue;
                EditorUtility.SetDirty(allConfigs);
                allConfigs.Save();
                RefreshUI();
            });
            targetListContainer.Add(renameField);

            EnumField platformField = new("Platform", Platform.Linux)
            {
                value = config.Platform
            };
            platformField.RegisterValueChangedCallback(e =>
            {
                config.Platform = (Platform)e.newValue;
                EditorUtility.SetDirty(allConfigs);
                allConfigs.Save();
                RefreshUI();
            });
            targetListContainer.Add(platformField);
            
            TextField fileNameField = new("File Name")
            {
                value = config.FileName
            };
            fileNameField.RegisterValueChangedCallback(e =>
            {
                config.FileName = e.newValue;
                EditorUtility.SetDirty(allConfigs);
                allConfigs.Save();
                RefreshUI();
            });
            targetListContainer.Add(fileNameField);
            
            TextField buildOutputPathField = new("Build Output Path")
            {
                value = config.BuildOutputPath
            };
            buildOutputPathField.RegisterValueChangedCallback(e =>
            {
                config.BuildOutputPath = e.newValue;
                EditorUtility.SetDirty(allConfigs);
                allConfigs.Save();
                RefreshUI();
            });
            targetListContainer.Add(buildOutputPathField);

            Button deleteButton = new(() =>
            {
                int index = allConfigs.DeployConfigs.IndexOf(config);
                allConfigs.DeployConfigs.Remove(config);
                EditorUtility.SetDirty(allConfigs);
                allConfigs.Save();
            
                RefreshUI();
                targetListContainer.Clear();
            
                if (allConfigs.DeployConfigs.Count > 0)
                {
                    int newIndex = Mathf.Clamp(index, 0, allConfigs.DeployConfigs.Count - 1);
                    configListView.selectedIndex = newIndex;
                    OnConfigSelected(new List<object> { allConfigs.DeployConfigs[newIndex] });
                }
            })
            { text = "Delete Config" };
            targetListContainer.Add(deleteButton);

            Label header = new("Targets")
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold }
            };
            targetListContainer.Add(header);

            Button addTargetButton = new(() =>
            {
                config.Targets.Add(new() { Description = GenerateTargetName(config) });
                EditorUtility.SetDirty(allConfigs);
                allConfigs.Save();
                DrawTargets(config);
            })
            { text = "Add Target" };
            targetListContainer.Add(addTargetButton);

            DrawTargets(config);
            selectedEnumerator.Dispose();

            MainToolbar.Refresh(MainToolbarButtons.DeploySettingsButtonPath);
        }

        private void DrawTargets(DeployConfig config)
        {
            while (targetListContainer.childCount > 7)
                targetListContainer.RemoveAt(7);

            for (int i = 0; i < config.Targets.Count; i++)
            {
                TargetConfig target = config.Targets[i];
                Box box = new()
                {
                    style =
                    {
                        marginTop = 4,
                        paddingLeft = 4
                    }
                };

                TextField nameField = new TextField("Name") { value = target.Description };
                nameField.RegisterValueChangedCallback(e =>
                {
                    target.Description = e.newValue;
                    EditorUtility.SetDirty(allConfigs);
                    allConfigs.Save();
                    RefreshUI();
                });
                box.Add(nameField);

                TextField hostField = new TextField("Hostname") { value = target.Hostname };
                hostField.RegisterValueChangedCallback(e =>
                {
                    target.Hostname = e.newValue;
                    EditorUtility.SetDirty(allConfigs);
                    allConfigs.Save();
                });
                box.Add(hostField);

                IntegerField portField = new IntegerField("Port") { value = target.Port };
                portField.RegisterValueChangedCallback(e =>
                {
                    target.Port = e.newValue;
                    EditorUtility.SetDirty(allConfigs);
                    allConfigs.Save();
                });
                box.Add(portField);

                TextField userField = new("Username") { value = target.Username };
                userField.RegisterValueChangedCallback(e =>
                {
                    target.Username = e.newValue;
                    EditorUtility.SetDirty(allConfigs);
                    allConfigs.Save();
                });
                box.Add(userField);

                TextField passwordField = new("Password")
                {
                    value = target.Password,
                    isPasswordField = true
                };
                passwordField.RegisterValueChangedCallback(e =>
                {
                    target.Password = e.newValue;
                    EditorUtility.SetDirty(allConfigs);
                    allConfigs.Save();
                });
                box.Add(passwordField);

                TextField remotePathField = new("Remote Path") { value = target.RemotePath };
                remotePathField.RegisterValueChangedCallback(e =>
                {
                    target.RemotePath = e.newValue;
                    EditorUtility.SetDirty(allConfigs);
                    allConfigs.Save();
                });
                box.Add(remotePathField);

                int iClone = i;
                Button deleteTargetButton = new(() =>
                    {
                        config.Targets.RemoveAt(iClone);
                        EditorUtility.SetDirty(allConfigs);
                        allConfigs.Save();
                        DrawTargets(config);
                    })
                    { text = "Delete Target" };

                box.Add(deleteTargetButton);

                targetListContainer.Add(box);
            }
        }

        private void RefreshUI()
        {
            configListView.selectionChanged -= OnConfigSelected;
            configListView.itemsSource = allConfigs.DeployConfigs;
            configListView.Rebuild();
            configListView.selectionChanged += OnConfigSelected;
        }

        private static bool IsConfigNameTaken(string name, DeployConfigList configs)
        {
            foreach (DeployConfig config in configs.DeployConfigs)
            {
                if (config is not null && config.Description == name)
                    return true;
            }
            return false;
        }
        
        private static string GenerateConfigName(DeployConfigList configs)
        {
            string baseName = "New Config";
            string name = baseName;
            int counter = 1;

            while (configs.DeployConfigs.Exists(c => c != null && c.Description == name))
            {
                name = $"{baseName} {counter++}";
            }

            return name;
        }
        
        
        private static string GenerateTargetName(DeployConfig config)
        {
            string baseName = "New Target";
            string name = baseName;
            int counter = 1;

            while (config.Targets.Exists(c => c != null && c.Description == name))
            {
                name = $"{baseName} {counter++}";
            }

            return name;
        }
    }
}