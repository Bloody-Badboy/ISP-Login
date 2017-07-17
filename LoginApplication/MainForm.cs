using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.String;
using static Microsoft.Win32.Registry;

namespace LoginApplication
{
    public partial class MainForm : Form
    {
        private string _response;
        private string _userName;
        private string _password;
        private bool _logedIn = false;
        private bool _isAlive = false;

        public MainForm()
        {
            InitializeComponent();
        }

        private void login_button_Click(object sender, EventArgs e)
        {
            _userName = userNameTextBox.Text;
            _password = passwordTextBox.Text;

            if (_userName.Trim().Length == 0)
            {
                MessageBox.Show("Please enter user name", "Input Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
                userNameTextBox.Focus();
                return;
            }

            if (_password.Trim().Length == 0)
            {
                MessageBox.Show("Please enter password", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                passwordTextBox.Focus();
                return;
            }

            var registryKey = CurrentUser.OpenSubKey(@"SOFTWARE\ISP LoginApplication", true);
            if (registryKey != null)
            {
                registryKey.SetValue("username", _userName);
                registryKey.SetValue("password", _password);
                registryKey.Close();
            }

            var loginWorker = new BackgroundWorker();
            loginWorker.DoWork += Login_WorkerOnDoWork;
            loginWorker.RunWorkerCompleted += Login_WorkerOnRunWorkerCompleted;

            log_inout_button.Enabled = false;
            userNameTextBox.Enabled = false;
            passwordTextBox.Enabled = false;
            showPassCheckBox.Enabled = false;
            username_label.Enabled = false;
            password_label.Enabled = false;


            loginWorker.RunWorkerAsync();
        }


        private void Login_WorkerOnDoWork(object sender, DoWorkEventArgs e)
        {
            _response = null;
            var postParams = $"login=1&user={_userName}&pass={_password}&version=0.2.23.0";
            if (_logedIn)
            {
                postParams = $"logout=1&user={_userName}&pass={_password}&version=0.2.23.0";
            }
            var postBytes = Encoding.UTF8.GetBytes(postParams);

            var webRequest = WebRequest.Create("http://10.254.254.53/0/wl/");
            webRequest.Method = "POST";
            webRequest.Timeout = 10000;
            webRequest.ContentLength = postBytes.Length;
            webRequest.ContentType = "application/x-www-form-urlencoded";

            try
            {
                webRequest.GetRequestStream().Write(postBytes, 0, postBytes.Length);

                var httpWebResponse = (HttpWebResponse) webRequest.GetResponse();


                if (httpWebResponse.StatusCode == HttpStatusCode.OK)
                {
                    using (var responseStream = httpWebResponse.GetResponseStream())
                    {
                        if (responseStream != null)
                        {
                            _response = new StreamReader(responseStream).ReadToEnd();
                        }
                    }
                }
            }
            catch (WebException exception)
            {
            }
        }

        private void Login_WorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_response == null)
            {
                MessageBox.Show("Error!!");
                return;
            }
            var stringReader = new StringReader(_response);
            var line = stringReader.ReadLine();
            if (line != null && line.Equals("OK", StringComparison.OrdinalIgnoreCase))
            {
                if (!_logedIn)
                {
                    log_inout_button.Enabled = true;
                    userNameTextBox.Enabled = false;
                    passwordTextBox.Enabled = false;
                    showPassCheckBox.Enabled = false;
                    username_label.Enabled = false;
                    password_label.Enabled = false;

                    MessageBox.Show("Login Successfully!!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _logedIn = true;
                    log_inout_button.Text = "Logout";
                }
                else
                {
                    log_inout_button.Enabled = true;
                    userNameTextBox.Enabled = true;
                    passwordTextBox.Enabled = true;
                    showPassCheckBox.Enabled = true;
                    username_label.Enabled = true;
                    password_label.Enabled = true;

                    MessageBox.Show("Logout Successfully!!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    log_inout_button.Text = "Log In";
                    _logedIn = false;
                }
            }
        }


        private void StartUpLoginCheck_WorkerOnDoWork(object sender, DoWorkEventArgs e)
        {
            var pingReply= new Ping().Send("10.254.254.53", 5000);

            if (pingReply != null && pingReply.Status == IPStatus.Success)
            {
                _isAlive = true;
            }

            if (!_isAlive)
                return;

            var webRequest = WebRequest.Create("http://10.254.254.53/0/wl/cklogin");
            webRequest.Method = "GET";
            webRequest.Timeout = 10000;

            try
            {
                var httpWebResponse = (HttpWebResponse) webRequest.GetResponse();

                if (httpWebResponse.StatusCode == HttpStatusCode.OK)
                {
                    using (var responseStream = httpWebResponse.GetResponseStream())
                    {
                        if (responseStream != null)
                        {
                            _response = new StreamReader(responseStream).ReadToEnd();
                        }
                    }
                }
            }
            catch (WebException exception)
            {
            }

            if (_response == null)
                return;

            var stringReader = new StringReader(_response);
            var line = stringReader.ReadLine();
            if (line != null && line.Equals("YES", StringComparison.OrdinalIgnoreCase))
            {
                _logedIn = true;
            }
        }

        private void StartUpLoginCheck_WorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!_isAlive)
            {
                MessageBox.Show("Cannot connect to the ISP login server!!", "Connection Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                System.Windows.Forms.Application.Exit();
            }
            if (_logedIn)
            {
                log_inout_button.Enabled = true;
                log_inout_button.Text = "Logout";
            }
            else
            {
                log_inout_button.Enabled = true;
                userNameTextBox.Enabled = true;
                passwordTextBox.Enabled = true;
                showPassCheckBox.Enabled = true;
                username_label.Enabled = true;
                password_label.Enabled = true;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var registryKey =
                CurrentUser.OpenSubKey(@"SOFTWARE\ISP LoginApplication");
            if (registryKey == null)
            {
                registryKey = CurrentUser.CreateSubKey(@"SOFTWARE\ISP LoginApplication");
            }
            userNameTextBox.Text = (string) registryKey.GetValue("username");
            passwordTextBox.Text = (string) registryKey.GetValue("password");
            registryKey.Close();

            var loginCheck_Worker = new BackgroundWorker();
            loginCheck_Worker.DoWork += StartUpLoginCheck_WorkerOnDoWork;
            loginCheck_Worker.RunWorkerCompleted += StartUpLoginCheck_WorkerOnRunWorkerCompleted;

            log_inout_button.Enabled = false;
            userNameTextBox.Enabled = false;
            passwordTextBox.Enabled = false;
            showPassCheckBox.Enabled = false;
            username_label.Enabled = false;
            password_label.Enabled = false;

            loginCheck_Worker.RunWorkerAsync();
        }


        private void showPassCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            passwordTextBox.UseSystemPasswordChar = showPassCheckBox.CheckState != CheckState.Checked;
        }
    }
}