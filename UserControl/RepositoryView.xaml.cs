using Google.Cloud.Storage.V1;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static _3d_repo.View.RepositoryView;

namespace _3d_repo.View
{
    public partial class RepositoryView : UserControl
    {
        private StorageClient storageClient;

        public RepositoryView()
        {
            InitializeComponent();
            InitializeStorageClient();
            ListFoldersInBucket();
        }

        public void UpdateFolders()
        {
            ListFoldersInBucket();
        }

        private void InitializeStorageClient()
        {
            string credentialPath = @"C:\Users\shoke\source\repos\3d_repo\3d_repo\resources\prefab-mountain-420617-d2461b99fd10.json";
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialPath);
            storageClient = StorageClient.Create();
        }

        private void ListFoldersInBucket()
        {
            string bucketName = "kreph-storage-v1";
            var objects = storageClient.ListObjects(bucketName, "");

            var folders = objects
                .Where(obj => obj.Name.EndsWith("/"))
                .Select(obj => obj.Name.TrimEnd('/'))
                .Distinct()
                .ToList();

            repositoryListBox.ItemsSource = folders.Select(folder => new FolderViewModel
            {
                FolderName = folder,
                Files = ListFilesInFolder(folder)
            }).ToList();
        }

        public class FolderViewModel
        {
            public string FolderName { get; set; }
            public List<string> Files { get; set; }
        }

        public class FileTag
        {
            public string FolderName { get; set; }
            public string FileName { get; set; }
        }

        private List<string> ListFilesInFolder(string folderName)
        {
            string bucketName = "kreph-storage-v1";
            var objects = storageClient.ListObjects(bucketName, folderName + "/");

            List<string> fileList = new List<string>();
            foreach (var obj in objects)
            {
                if (!obj.Name.EndsWith("/"))
                {
                    fileList.Add(obj.Name.Substring(folderName.Length + 1));
                }
            }

            return fileList;
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is Expander expander && expander.DataContext is FolderViewModel folder)
            {
                List<string> filesInFolder = ListFilesInFolder(folder.FolderName);

                if (expander.Content is ItemsControl filesContainer)
                {
                    filesContainer.ItemsSource = null; // Desvincular o ItemsSource

                    var items = new List<StackPanel>();

                    foreach (var file in filesInFolder)
                    {
                        StackPanel stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

                        TextBlock fileNameTextBlock = new TextBlock { Text = file, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };

                        Button downloadButton = new Button();
                        downloadButton.Content = "Download";
                        downloadButton.Click += DownloadButton_Click;
                        downloadButton.Tag = new FileTag { FolderName = folder.FolderName, FileName = file }; // Passar dados corretos

                        Button renameButton = new Button();
                        renameButton.Content = "Rename";
                        renameButton.Click += RenameFileButton_Click;
                        renameButton.Tag = new FileTag { FolderName = folder.FolderName, FileName = file }; // Passar dados corretos
                        renameButton.Margin = new Thickness(10, 0, 0, 0);

                        Button moveButton = new Button();
                        moveButton.Content = "Move";
                        moveButton.Click += MoveFileButton_Click;
                        moveButton.Tag = new FileTag { FolderName = folder.FolderName, FileName = file }; // Passar dados corretos
                        moveButton.Margin = new Thickness(10, 0, 0, 0);

                        Button deleteButton = new Button();
                        deleteButton.Content = "Delete";
                        deleteButton.Click += DeleteButton_Click;
                        deleteButton.Tag = new FileTag { FolderName = folder.FolderName, FileName = file }; // Passar dados corretos
                        deleteButton.Margin = new Thickness(10, 0, 0, 0);

                        Button visualizeButton = new Button();
                        visualizeButton.Content = "Visualize";
                        visualizeButton.Click += VisualizeButton_Click;
                        visualizeButton.Tag = new FileTag { FolderName = folder.FolderName, FileName = file };
                        visualizeButton.Margin = new Thickness(10, 0, 0, 0);
                        visualizeButton.Visibility = IsVisualizableFile(file) ? Visibility.Visible : Visibility.Collapsed;

                        // Adicionar TextBlock e Buttons ao StackPanel
                        stackPanel.Children.Add(fileNameTextBlock);
                        stackPanel.Children.Add(downloadButton);
                        stackPanel.Children.Add(renameButton);
                        stackPanel.Children.Add(moveButton);
                        stackPanel.Children.Add(deleteButton);
                        stackPanel.Children.Add(visualizeButton);

                        // Adicionar StackPanel à lista de itens
                        items.Add(stackPanel);
                    }

                    filesContainer.ItemsSource = items; // Redefinir o ItemsSource para a lista de itens
                }
            }
        }

        private bool IsVisualizableFile(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();
            return extension == ".obj" || extension == ".stl";
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is FileTag tagData)
            {
                string folderName = tagData.FolderName;
                string fileName = tagData.FileName;

                if (!string.IsNullOrEmpty(folderName) && !string.IsNullOrEmpty(fileName))
                {
                    DownloadFile(folderName, fileName);
                }
                else
                {
                    MessageBox.Show($"Folder or file name is null or empty. FolderName: {folderName}, FileName: {fileName}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DownloadFile(string folderName, string fileName)
        {
            string bucketName = "kreph-storage-v1";
            string objectName = $"{folderName}/{fileName}";

            try
            {
                var objectExists = storageClient.ListObjects(bucketName, folderName + "/")
                                                .Any(obj => obj.Name == objectName);

                if (!objectExists)
                {
                    MessageBox.Show($"The object {objectName} does not exist in the bucket {bucketName}.", "Object Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var memoryStream = new MemoryStream();
                storageClient.DownloadObject(bucketName, objectName, memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    FileName = fileName,
                    DefaultExt = Path.GetExtension(fileName),
                    Filter = "All files (*.*)|*.*"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string downloadPath = saveFileDialog.FileName;

                    using (var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write))
                    {
                        memoryStream.CopyTo(fileStream);
                    }

                    MessageBox.Show($"File downloaded to {downloadPath}", "Download Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Google.GoogleApiException ex)
            {
                MessageBox.Show($"Google API error: {ex.Message}\n\nDetails: {ex.Error?.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is FileTag tagData)
            {
                string folderName = tagData.FolderName;
                string fileName = tagData.FileName;

                if (!string.IsNullOrEmpty(folderName) && !string.IsNullOrEmpty(fileName))
                {
                    var result = MessageBox.Show($"Are you sure you want to delete the file '{fileName}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        DeleteFile(folderName, fileName);
                        UpdateFolders(); // Refresh the list after deletion
                    }
                }
                else
                {
                    MessageBox.Show($"Folder or file name is null or empty. FolderName: {folderName}, FileName: {fileName}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteFile(string folderName, string fileName)
        {
            string bucketName = "kreph-storage-v1";
            string objectName = $"{folderName}/{fileName}";

            try
            {
                storageClient.DeleteObject(bucketName, objectName);
                MessageBox.Show($"File '{fileName}' deleted successfully.", "Delete Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Google.GoogleApiException ex)
            {
                MessageBox.Show($"Google API error: {ex.Message}\n\nDetails: {ex.Error?.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string folderName)
            {
                if (!string.IsNullOrEmpty(folderName))
                {
                    var result = MessageBox.Show($"Are you sure you want to delete the folder '{folderName}' and all its contents?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        DeleteFolder(folderName);
                        UpdateFolders(); // Refresh the list after deletion
                    }
                }
                else
                {
                    MessageBox.Show($"Folder name is null or empty. FolderName: {folderName}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteFolder(string folderName)
        {
            string bucketName = "kreph-storage-v1";

            try
            {
                var objects = storageClient.ListObjects(bucketName, folderName + "/").ToList();
                foreach (var obj in objects)
                {
                    storageClient.DeleteObject(bucketName, obj.Name);
                }

                MessageBox.Show($"Folder '{folderName}' and all its contents deleted successfully.", "Delete Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Google.GoogleApiException ex)
            {
                MessageBox.Show($"Google API error: {ex.Message}\n\nDetails: {ex.Error?.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RenameFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string folderName)
            {
                string newFolderName = Microsoft.VisualBasic.Interaction.InputBox("Enter new folder name:", "Rename Folder", folderName);

                if (!string.IsNullOrEmpty(newFolderName) && newFolderName != folderName)
                {
                    RenameFolder(folderName, newFolderName);
                    UpdateFolders(); // Refresh the list after renaming
                }
                else
                {
                    MessageBox.Show($"Invalid folder name. FolderName: {newFolderName}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RenameFolder(string oldFolderName, string newFolderName)
        {
            string bucketName = "kreph-storage-v1";

            try
            {
                var objects = storageClient.ListObjects(bucketName, oldFolderName + "/").ToList();
                foreach (var obj in objects)
                {
                    string newObjectName = obj.Name.Replace(oldFolderName + "/", newFolderName + "/");
                    storageClient.CopyObject(bucketName, obj.Name, bucketName, newObjectName);
                    storageClient.DeleteObject(bucketName, obj.Name);
                }

                MessageBox.Show($"Folder '{oldFolderName}' renamed to '{newFolderName}' successfully.", "Rename Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Google.GoogleApiException ex)
            {
                MessageBox.Show($"Google API error: {ex.Message}\n\nDetails: {ex.Error?.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RenameFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is FileTag tagData)
            {
                string folderName = tagData.FolderName;
                string fileName = tagData.FileName;

                string newFileName = Microsoft.VisualBasic.Interaction.InputBox("Enter new file name:", "Rename File", fileName);

                if (!string.IsNullOrEmpty(newFileName) && newFileName != fileName)
                {
                    RenameFile(folderName, fileName, newFileName);
                    UpdateFolders(); // Refresh the list after renaming
                }
                else
                {
                    MessageBox.Show($"Invalid file name. FileName: {newFileName}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RenameFile(string folderName, string oldFileName, string newFileName)
        {
            string bucketName = "kreph-storage-v1";
            string oldObjectName = $"{folderName}/{oldFileName}";
            string newObjectName = $"{folderName}/{newFileName}";

            try
            {
                storageClient.CopyObject(bucketName, oldObjectName, bucketName, newObjectName);
                storageClient.DeleteObject(bucketName, oldObjectName);

                MessageBox.Show($"File '{oldFileName}' renamed to '{newFileName}' successfully.", "Rename Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Google.GoogleApiException ex)
            {
                MessageBox.Show($"Google API error: {ex.Message}\n\nDetails: {ex.Error?.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MoveFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is FileTag tagData)
            {
                string sourceFolderName = tagData.FolderName;
                string fileName = tagData.FileName;

                // Lista de pastas disponíveis
                var folders = ((List<FolderViewModel>)repositoryListBox.ItemsSource).Select(f => f.FolderName).ToList();
                MoveFileDialog moveFileDialog = new MoveFileDialog(folders);
                if (moveFileDialog.ShowDialog() == true)
                {
                    string targetFolderName = moveFileDialog.SelectedFolder;
                    if (!string.IsNullOrEmpty(targetFolderName) && targetFolderName != sourceFolderName)
                    {
                        MoveFile(sourceFolderName, fileName, targetFolderName);
                        UpdateFolders(); // Refresh the list after moving
                    }
                    else
                    {
                        MessageBox.Show($"Invalid target folder name. FolderName: {targetFolderName}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void MoveFile(string sourceFolderName, string fileName, string targetFolderName)
        {
            string bucketName = "kreph-storage-v1";
            string sourceObjectName = $"{sourceFolderName}/{fileName}";
            string targetObjectName = $"{targetFolderName}/{fileName}";

            try
            {
                storageClient.CopyObject(bucketName, sourceObjectName, bucketName, targetObjectName);
                storageClient.DeleteObject(bucketName, sourceObjectName);

                MessageBox.Show($"File '{fileName}' moved to '{targetFolderName}' successfully.", "Move Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Google.GoogleApiException ex)
            {
                MessageBox.Show($"Google API error: {ex.Message}\n\nDetails: {ex.Error?.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void VisualizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is FileTag tagData)
            {
                string folderName = tagData.FolderName;
                string fileName = tagData.FileName;
                Visualize3DModel(folderName, fileName);
            }
        }

        private void Visualize3DModel(string folderName, string fileName)
        {
            string bucketName = "kreph-storage-v1";
            string objectName = $"{folderName}/{fileName}";
            string localFilePath = Path.Combine(Path.GetTempPath(), fileName);

            try
            {
                using (var fileStream = File.Create(localFilePath))
                {
                    storageClient.DownloadObject(bucketName, objectName, fileStream);
                }

                var visualizationWindow = new VisualizationWindow(localFilePath);
                visualizationWindow.Show();
            }
            catch (Google.GoogleApiException ex)
            {
                MessageBox.Show($"Google API error: {ex.Message}\n\nDetails: {ex.Error?.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class MoveFileDialog : Window
    {
        public string SelectedFolder { get; private set; }

        public MoveFileDialog(List<string> folders)
        {
            Title = "Move File";
            Width = 300;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            StackPanel stackPanel = new StackPanel();

            ComboBox folderComboBox = new ComboBox
            {
                ItemsSource = folders,
                Margin = new Thickness(10)
            };
            stackPanel.Children.Add(folderComboBox);

            Button okButton = new Button
            {
                Content = "OK",
                Width = 75,
                Margin = new Thickness(10)
            };
            okButton.Click += (s, e) =>
            {
                SelectedFolder = folderComboBox.SelectedItem as string;
                DialogResult = true;
                Close();
            };
            stackPanel.Children.Add(okButton);

            Content = stackPanel;
        }
    }
}
