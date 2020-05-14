using AutoPackgeCore;
using AutoPackgeCore.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PackageTest {
    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
            S s = new S() {
                USBToolClientPath= @"",
                SignServerPath =@"",
                ServerIP="",
                ServerPort=""
            };
            CertificateSerive.Initial(s);
            PackageService.Initial(s);
        }
        
        private async void button1_Click(object sender, EventArgs e) {
            string error = "";
            var project = new UserProject("D:\\ApowerCompress\\trunck\\src\\ApowerCompress\\ApowerCompress.csproj", "D:\\ApowerCompress\\trunck\\src");
            if (CertificateSerive.StartSignServer(out error)) {
                var connected = await CertificateSerive.Connect();
                if (!connected) {
                    return;
                }
                await PackageService.Pack(project);
                CertificateSerive.DisConnect();
            }
            else {
               MessageBox.Show($"自动签名工具启动失败，{error}");
            }
        }

    }
    public class S :  IVSConfig {

        public bool DumpToWeb { get; set; } = true;
        public bool DumpPackedFiles { get; set; } = true;
        public string USBToolClientPath { get; set; } = "";
        public string SignServerPath { get; set; }
        public string ServerIP { get; set; }
        public string ServerPort { get; set; } = "33000";

        public void OutputLog(string msg) {
            Console.WriteLine(msg);
        }

        public bool ShowMsg(string msg) {
            return MessageBox.Show(msg)==DialogResult.OK;
        }
    }
}

