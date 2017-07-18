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
        private string _serverResponse;
        private string _username;
        private string _password;
        private bool _isAlreadyLogedIn;
        private bool _isHostAlive;
        private bool _isAutoLogin = false;

        public MainForm(string[] args)
        {
            InitializeComponent();
            if (args.Contains("/onboot"))
            {
                _isAutoLogin = true;
            }
        }

        private void login_button_Click(object sender, EventArgs e)
        {
            _username = userNameTextBox.Text;
            _password = passwordTextBox.Text;

            if (_username.Trim().Length == 0)
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
                registryKey.SetValue("username", _username);
                registryKey.SetValue("password", _password);
                registryKey.SetValue("autoLogin", autoLoginCheckBox.Checked);
                registryKey.Close();
            }

            var loginWorker = new BackgroundWorker();
            loginWorker.DoWork += Login_WorkerOnDoWork;
            loginWorker.RunWorkerCompleted += Login_WorkerOnRunWorkerCompleted;

            log_inout_button.Enabled = false;
            userNameTextBox.Enabled = false;
            passwordTextBox.Enabled = false;
            showPassCheckBox.Enabled = false;
            autoLoginCheckBox.Enabled = false;
            username_label.Enabled = false;
            password_label.Enabled = false;


            loginWorker.RunWorkerAsync();
        }


        private void Login_WorkerOnDoWork(object sender, DoWorkEventArgs e)
        {
            _serverResponse = null;
            var postParams = $"login=1&user={_username}&pass={_password}&version=0.2.23.0";
            if (_isAlreadyLogedIn)
            {
                postParams = $"logout=1&user={_username}&pass={_password}&version=0.2.23.0";
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
                            _serverResponse = new StreamReader(responseStream).ReadToEnd();
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
            if (_serverResponse == null)
            {
                MessageBox.Show("Error!!");
                return;
            }
            var stringReader = new StringReader(_serverResponse);
            var line = stringReader.ReadLine();
            if (line != null && line.Equals("OK", StringComparison.OrdinalIgnoreCase))
            {
                if (!_isAlreadyLogedIn)
                {
                    log_inout_button.Enabled = true;
                    userNameTextBox.Enabled = false;
                    passwordTextBox.Enabled = false;
                    showPassCheckBox.Enabled = false;
                    autoLoginCheckBox.Enabled = false;
                    username_label.Enabled = false;
                    password_label.Enabled = false;

                    MessageBox.Show("Login Successfully!!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _isAlreadyLogedIn = true;
                    log_inout_button.Text = "Logout";
                }
                else
                {
                    log_inout_button.Enabled = true;
                    userNameTextBox.Enabled = true;
                    passwordTextBox.Enabled = true;
                    showPassCheckBox.Enabled = true;
                    autoLoginCheckBox.Enabled = true;
                    username_label.Enabled = true;
                    password_label.Enabled = true;

                    MessageBox.Show("Logout Successfully!!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    log_inout_button.Text = "Log In";
                    _isAlreadyLogedIn = false;
                }
            }
            if (_isAutoLogin)
            {
                System.Windows.Forms.Application.Exit();
            }
        }


        private void StartUpLoginCheck_WorkerOnDoWork(object sender, DoWorkEventArgs e)
        {
            var pingReply = new Ping().Send("10.254.254.53", 5000);

            if (pingReply != null && pingReply.Status == IPStatus.Success)
            {
                _isHostAlive = true;
            }

            if (!_isHostAlive)
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
                            _serverResponse = new StreamReader(responseStream).ReadToEnd();
                        }
                    }
                }
            }
            catch (WebException exception)
            {
            }

            if (_serverResponse == null)
                return;

            var stringReader = new StringReader(_serverResponse);
            var line = stringReader.ReadLine();
            if (line != null && line.Equals("YES", StringComparison.OrdinalIgnoreCase))
            {
                _isAlreadyLogedIn = true;
            }
        }

        private void StartUpLoginCheck_WorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!_isHostAlive)
            {
                MessageBox.Show("Cannot connect to the ISP login server!!", "Connection Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                System.Windows.Forms.Application.Exit();
            }
            if (_isAlreadyLogedIn)
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
                autoLoginCheckBox.Enabled = true;
                username_label.Enabled = true;
                password_label.Enabled = true;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var registryKey =
                CurrentUser.OpenSubKey(@"SOFTWARE\ISP LoginApplication", true);
            if (registryKey == null)
            {
                registryKey = CurrentUser.CreateSubKey(@"SOFTWARE\ISP LoginApplication");
            }
            if (registryKey != null)
            {
                userNameTextBox.Text = (string) registryKey.GetValue("username", "");
                passwordTextBox.Text = (string) registryKey.GetValue("password", "");
                autoLoginCheckBox.Checked = Convert.ToBoolean(registryKey.GetValue("autoLogin", false));

                registryKey.Close();
            }

            var loginCheckWorker = new BackgroundWorker();
            loginCheckWorker.DoWork += StartUpLoginCheck_WorkerOnDoWork;
            loginCheckWorker.RunWorkerCompleted += StartUpLoginCheck_WorkerOnRunWorkerCompleted;

            log_inout_button.Enabled = false;
            userNameTextBox.Enabled = false;
            passwordTextBox.Enabled = false;
            showPassCheckBox.Enabled = false;
            autoLoginCheckBox.Enabled = false;
            username_label.Enabled = false;
            password_label.Enabled = false;

            loginCheckWorker.RunWorkerAsync();
            if (_isAutoLogin)
            {
                var loginWorker = new BackgroundWorker();
                loginWorker.DoWork += Login_WorkerOnDoWork;
                loginWorker.RunWorkerCompleted += Login_WorkerOnRunWorkerCompleted;

                log_inout_button.Enabled = false;
                userNameTextBox.Enabled = false;
                passwordTextBox.Enabled = false;
                showPassCheckBox.Enabled = false;
                autoLoginCheckBox.Enabled = false;
                username_label.Enabled = false;
                password_label.Enabled = false;


                loginWorker.RunWorkerAsync();
            }
        }


        private void showPassCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            passwordTextBox.UseSystemPasswordChar = showPassCheckBox.CheckState != CheckState.Checked;
        }

        private void autoLoginCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            var registryKey =
                CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (registryKey != null)
            {
                if (autoLoginCheckBox.CheckState == CheckState.Checked)
                {
                    registryKey.SetValue(System.Diagnostics.Process.GetCurrentProcess().ProcessName,
                        Application.ExecutablePath + "/onboot");
                }
                else
                {
                    registryKey.SetValue(System.Diagnostics.Process.GetCurrentProcess().ProcessName, false);
                }
                registryKey.Close();
            }
            registryKey =
                CurrentUser.OpenSubKey(@"SOFTWARE\ISP LoginApplication", true);
            if (registryKey == null)
            {
                registryKey = CurrentUser.CreateSubKey(@"SOFTWARE\ISP LoginApplication");
            }
            if (registryKey != null)
            {
                bool isChecked = autoLoginCheckBox.CheckState == CheckState.Checked;
                registryKey.SetValue("autoLogin", isChecked);
                registryKey.Close();
            }
        }
    }
}