using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;


namespace _3d_repo.View
{
    public partial class LoginView : System.Windows.Window
    {
        private string usernameOrEmail, password, sql;
        private connection conn = new connection();
        private MySqlCommand command;

        public LoginView()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            Application.Current.Shutdown();
        }

        private void txtPass_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void txtUser_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            usernameOrEmail = txtUser.Text;
            password = txtPass.Password;

            if (string.IsNullOrEmpty(usernameOrEmail) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Nome/Senha está vazio");
            }
            else
            {
                sql = "SELECT senha FROM Usuarios WHERE (username = @usernameOrEmail OR email = @usernameOrEmail)";
                if (conn.OpenConnection())
                {
                    try
                    {
                        command = new MySqlCommand(sql, conn.get_connection());
                        command.Parameters.AddWithValue("@usernameOrEmail", usernameOrEmail);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string storedPasswordHash = reader.GetString("senha");
                                string providedPasswordHash = HashPassword(password);
                                if (storedPasswordHash == providedPasswordHash)
                                {
                                    MainWindow dsb = new MainWindow();
                                    dsb.Show();
                                    this.Close();
                                }
                                else
                                {
                                    MessageBox.Show("Nome/Senha inválido");
                                }
                            }
                            else
                            {
                                MessageBox.Show("Nome/Senha inválido");
                            }
                        }
                    }
                    catch (MySqlException x)
                    {
                        MessageBox.Show($"Erro: {x.Message}");
                    }
                    finally
                    {
                        conn.close_conn();
                    }
                }
            }
            txtUser.Text = "";
            txtPass.Password = "";
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


        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnRegisterPage_Click(object sender, RoutedEventArgs e)
        {
            RegisterView registerWindow = new RegisterView();
            registerWindow.Show();

            this.Close();
        }

    }
}
