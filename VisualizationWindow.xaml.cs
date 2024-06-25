using HelixToolkit.Wpf;
using System.Windows;
using System.Windows.Media.Media3D;
using System.IO;

namespace _3d_repo.View
{
    public partial class VisualizationWindow : Window
    {
        public VisualizationWindow(string modelPath)
        {
            InitializeComponent();
            LoadModel(modelPath);
        }

        private void LoadModel(string modelPath)
        {
            if (File.Exists(modelPath))
            {
                var importer = new ModelImporter();
                var model = importer.Load(modelPath);
                helixViewport.Children.Add(new ModelVisual3D { Content = model });
                helixViewport.ZoomExtents();
            }
            else
            {
                MessageBox.Show($"The model file '{modelPath}' does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
