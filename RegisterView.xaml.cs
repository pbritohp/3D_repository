using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using MySql.Data.MySqlClient;

namespace _3d_repo.View
{
    public partial class RegisterView : Window
    {
        private connection conn;

        public RegisterView()
        {
            InitializeComponent();
            conn = new connection();
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text;
            string email = txtEmail.Text;
            string password = txtPassword.Password;
            string confirmPassword = txtConfirmPassword.Password;
            string inviteKey = txtInvitationKey.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword) || string.IsNullOrEmpty(inviteKey))
            {
                MessageBox.Show("Por favor, preencha todos os campos.");
                return;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show("As senhas não coincidem.");
                return;
            }

            // Gerar o hash da senha
            string hashedPassword = HashPassword(password);

            string sql = "INSERT INTO Usuarios (username, email, senha) VALUES (@Username, @Email, @Password)";

            if (!conn.OpenConnection())
            {
                MessageBox.Show("Erro ao conectar ao banco de dados.");
                return;
            }

            MySqlCommand command = new MySqlCommand(sql, conn.get_connection());
            command.Parameters.AddWithValue("@Username", username);
            command.Parameters.AddWithValue("@Email", email);
            command.Parameters.AddWithValue("@Password", hashedPassword);

            try
            {
                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    MessageBox.Show("Usuário registrado com sucesso!");
                }
                else
                {
                    MessageBox.Show("Falha ao registrar usuário.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao registrar usuário: " + ex.Message);
            }
            finally
            {
                LoginView loginView = new LoginView();

                Application.Current.MainWindow = loginView;

                conn.close_conn();

                loginView.Show();
                this.Close();
            }
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            LoginView loginView = new LoginView();
            loginView.Show();
            this.Close();
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                this.DragMove();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
    }
}
