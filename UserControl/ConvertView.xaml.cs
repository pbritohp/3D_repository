using Aspose.ThreeD;
using Google.Cloud.Storage.V1;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace _3d_repo.View
{
    public partial class ConvertView : UserControl
    {
        private StorageClient storageClient;
        private string bucketName = "kreph-storage-v1";

        public ConvertView()
        {
            InitializeComponent();
            InitializeStorageClient();
            ListFilesInBucket();
        }

        private void InitializeStorageClient()
        {
            string credentialPath = @"C:\Users\shoke\source\repos\3d_repo\3d_repo\resources\prefab-mountain-420617-d2461b99fd10.json";
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialPath);
            storageClient = StorageClient.Create();
        }

        private void ListFilesInBucket()
        {
            var objects = storageClient.ListObjects(bucketName, "");

            var files = objects
                .Where(obj => !obj.Name.EndsWith("/") && obj.Name.EndsWith(".obj"))
                .Select(obj => obj.Name)
                .ToList();

            fileComboBox.ItemsSource = files;
        }

        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedFile = fileComboBox.SelectedItem as string;

            if (string.IsNullOrEmpty(selectedFile))
            {
                MessageBox.Show("Please select a file to convert.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ConvertFileToSTL(selectedFile);
        }

        private void ConvertFileToSTL(string fileName)
        {
            try
            {
                // Suprimir a exceção de avaliação da Aspose.3D
                Aspose.ThreeD.TrialException.SuppressTrialException = true;

                // Baixar o arquivo do Google Cloud Storage
                var downloadStream = new MemoryStream();
                storageClient.DownloadObject(bucketName, fileName, downloadStream);
                downloadStream.Position = 0;

                // Salvar o stream baixado em um arquivo temporário
                var tempInputDir = Path.Combine(Path.GetTempPath(), "3d_repo_temp");
                Directory.CreateDirectory(tempInputDir);
                var tempInputPath = Path.Combine(tempInputDir, Path.GetFileName(fileName));
                File.WriteAllBytes(tempInputPath, downloadStream.ToArray());

                // Carregar o arquivo OBJ usando Aspose.3D
                var scene = new Scene();
                scene.Open(tempInputPath);

                // Definir o caminho do arquivo de saída
                var outputFileName = $"{Path.GetFileNameWithoutExtension(fileName)}.stl";
                var outputDir = Path.Combine(tempInputDir, "converted");
                Directory.CreateDirectory(outputDir);
                var outputPath = Path.Combine(outputDir, outputFileName);

                // Salvar como STL usando Aspose.3D
                scene.Save(outputPath, FileFormat.STLBinary);

                // Fazer upload do arquivo convertido de volta ao bucket no mesmo diretório do arquivo original
                var originalFilePath = Path.GetDirectoryName(fileName).Replace("\\", "/");
                var convertedFilePath = $"{originalFilePath}/{outputFileName}".Replace("\\", "/");
                using (var uploadStream = File.OpenRead(outputPath))
                {
                    storageClient.UploadObject(bucketName, convertedFilePath, null, uploadStream);
                }

                statusTextBlock.Text = $"File converted and uploaded to '{convertedFilePath}'";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
