using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LeashMod_Server
{
    public partial class ServerForm : Form
    {
        public static ServerForm Instance = null;

        public ServerForm()
        {
            InitializeComponent();

            Instance = this;
        }

        private void Server_Load(object sender, EventArgs e)
        {
            Log("Server Starting!");


        }

        public static void SendLog(string text, LogType type = LogType.Log)
        {
            Instance.textBox1.AppendText($"[{DateTime.Now:g}] [{type}]" + text + "\r\n");
        }

        public void Log(string text, LogType type = LogType.Log)
        {
            textBox1.AppendText($"[{DateTime.Now:g}] [{type}]" + text + "\r\n");
        }

        public enum LogType
        {
            Log,
            Warning,
            Error
        }
    }
}
