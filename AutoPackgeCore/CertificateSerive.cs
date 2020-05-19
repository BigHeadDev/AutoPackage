using AutoPackgeCore.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AutoPackgeCore {
    public static class CertificateSerive {

        private static USBServer usbNetwork;
        private static IVSConfig ivsConfig;
        public static void Initial(IVSConfig config) {
            ivsConfig = config;
            //string exePath,string signPath,string ip,string port
            usbNetwork = new USBServer(config.USBToolClientPath,config.ServerIP, config.ServerPort);
        }
        public static async Task<bool> Connect() {
            string msg = "";
            bool connected = true;
            if (string.IsNullOrEmpty(ivsConfig.USBToolClientPath)|| string.IsNullOrEmpty(ivsConfig.ServerIP) || string.IsNullOrEmpty(ivsConfig.ServerPort)) {
                ivsConfig.ShowMsg("请在 工具-选项-自动打包 中填写正确的USB客户端，服务器IP和端口！");
                return false;
            }
            if (usbNetwork.AddServer()) {
                int checkTime = 0;
                while (!usbNetwork.GetServerAvailable()) {
                    checkTime++;
                    ivsConfig.OutputLog("尝试连接服务器");
                    await Task.Delay(2000);
                    if (checkTime>5) {
                        ivsConfig.ShowMsg("连接服务器失败");
                        return false;
                    }
                }
                ivsConfig.OutputLog("连接服务器成功");
                if (usbNetwork.ConnectSafeNetDevice(out msg)) {
                    ivsConfig.OutputLog(msg);
                }
                else {
                    ivsConfig.ShowMsg($"连接证书失败，{msg}");
                    connected = false;
                }
            }
            else {
                ivsConfig.ShowMsg($"连接服务器失败,{msg}");
                connected = false;
            }
            return connected;
        }

        public static void DisConnect() {
            string msg = "";
            if (!usbNetwork.DisConnectSafeNetDevice(out msg)) {
                ivsConfig.ShowMsg($"证书断开失败,{msg}");
            }else {
                ivsConfig.OutputLog($"证书断开成功");
            }
        }

        public static bool StartSignServer(out string error) {
            bool sucess = false;
            error = "启动成功";
            var processes = Process.GetProcessesByName("SignServer");
            if (processes.Length>0) {
                sucess = true;
                ivsConfig.OutputLog("自动签名之前已经启动");
                return sucess;
            }
            if (File.Exists(ivsConfig.SignServerPath)) {
                var proc = Process.Start(ivsConfig.SignServerPath);
                if (proc.Responding) {
                    sucess = true;
                    ivsConfig.OutputLog("自动签名工具启动启动成功");
                }
                else {
                    error = "请手动启动";
                }
            }
            else {
                error = "自动签名工具不存在";
            }
            return sucess;
        }
    }
}
