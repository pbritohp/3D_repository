using Google.Cloud.Storage.V1;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace _3d_repo.View
{
    public partial class UploadView : UserControl
    {
        private StorageClient storageClient;
        private string selectedFilePath;
        private string bucketName = "kreph-storage-v1";

        public event EventHandler FileUploaded; // Evento para notificar sobre upload

        public UploadView()
        {
            InitializeComponent();
            InitializeStorageClient();
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
            var objects = storageClient.ListObjects(bucketName, "");

            var folders = objects
                .Where(obj => obj.Name.EndsWith("/"))
                .Select(obj => obj.Name.TrimEnd('/'))
                .Distinct()
                .ToList();

            folderComboBox.ItemsSource = folders;
        }

        private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                selectedFilePath = openFileDialog.FileName;
                filePathTextBlock.Text = selectedFilePath;
            }
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(selectedFilePath))
            {
                string selectedFolder = folderComboBox.SelectedItem as string;
                string newFolder = newFolderTextBox.Text;

                if (!string.IsNullOrEmpty(newFolder))
                {
                    CreateFolderIfNotExists(newFolder);
                    UploadFileToFolder(selectedFilePath, newFolder);
                }
                else if (!string.IsNullOrEmpty(selectedFolder))
                {
                    UploadFileToFolder(selectedFilePath, selectedFolder);
                }
                else
                {
                    MessageBox.Show("Please select or create a folder.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a file to upload.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateFolderIfNotExists(string folderName)
        {
            string objectName = $"{folderName}/"; // Objeto fictício para representar o diretório

            var existingObjects = storageClient.ListObjects(bucketName, objectName);

            if (!existingObjects.Any())
            {
                var content = new MemoryStream(); // Conteúdo vazio
                storageClient.UploadObject(bucketName, objectName, null, content);
            }
        }

        private void UploadFileToFolder(string filePath, string folderName)
        {
            string objectName = $"{folderName}/{Path.GetFileName(filePath)}";

            try
            {
                using (var fileStream = File.OpenRead(filePath))
                {
                    storageClient.UploadObject(bucketName, objectName, null, fileStream);
                }

                MessageBox.Show($"File '{objectName}' uploaded successfully to bucket '{bucketName}'.", "Upload Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                // Disparar evento após upload bem-sucedido
                FileUploaded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
