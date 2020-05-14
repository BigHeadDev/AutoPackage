using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AutoPackgeCore.Model {
    internal class USBServer {
        public USBServer(string exePath, string ip, string port) {
            usbCmdPath = exePath;
            IP = ip;
            Port = port;
        }

        private string usbCmdPath;
        public int SerId { get; set; }
        public string IP { get; set; }
        public string Port { get; set; }
        public USBDevice Device { get; set; }

        public bool AddServer() {
            bool result = false;
            var list = GetServers();
            if (list.Count>0&&list.Contains(IP + ":" + Port)) {
                result = true;
            }
            else {
                var cmdResult = ExcuteCommand($"add {IP} {Port}");
                if (cmdResult.Contains("successfully")) {
                    result = true;
                }
            }
            return result;
        }

        public bool ConnectSafeNetDevice(out string error) {
            bool result = false;
            error = "连接成功";
            var signDevice = GetSignDevice();
            if (signDevice!=null) {
                switch (signDevice.Status) {
                    case DeviceStatus.Connected:
                        error = "证书已经被连接了，不用再次连接";
                        break;
                    case DeviceStatus.Shared:
                        var cmdResult = ExcuteCommand($"connect -ip {IP}:{Port} {signDevice.DevId}");
                        if (cmdResult.Contains("Device is connected")) {
                            result = true;
                        }
                        break;
                    case DeviceStatus.Busy:
                        error = "证书正在被使用请稍等几分钟后重试";
                        break;
                    default:
                        break;
                }
            }
            else {
                //没有设备
                error = "没有发现证书设备,请等待服务器连上或者检查证书工具是否存在";
            }
            return result;
        }

        public bool DisConnectSafeNetDevice(out string error) {
            bool result = false;
            error = "成功断开";
            var signDevice = GetSignDevice();
            if (signDevice != null) {
                if (signDevice.Status==DeviceStatus.Connected) {
                    var cmdResult = ExcuteCommand($"disconnect -ip {IP}:{Port} {signDevice.DevId}");
                    if (cmdResult.Contains("Device is disconnected")) {
                        result = true;
                    }
                    else {
                        error = "请务必手动断开，避免抢占他人使用";
                    }
                }
                else {
                    //有人占用
                    error = "证书已经被人为断开了或者抢占了";
                }
            }
            else {
                //没有设备
                error = "没有发现证书设备,可能手动拔掉了";
            }
            return result;
        }

        public bool GetServerAvailable() {
            bool available = false;
            string cmd = "list";
            var cmdResult = ExcuteCommand(cmd);
            var regex = new Regex(@"\s" + IP + ":" + Port + @"\s\((?<des>\S+)\b");
            var match = regex.Match(cmdResult);
            var groups = match.Groups;
            if (groups["des"].Value.Equals("available")) {
                available = true;
            }
            return available;
        }

        private USBDevice GetSignDevice() {
            //srvID:0  -  7.105.94.12:33000 (available connections: 0 of 1)\r\n   DevId:0    0529:0620:0001        1-10        Busy         \"\"  SafeNet Token \r\n\r\n---Discovered servers---\r\n
            string cmd = "list -a";
            USBDevice device =null;
            var cmdResult = ExcuteCommand(cmd);
            var regex = new Regex(@"DevId:(?<devId>\d).+\b(?<status>\S+)\b.+\sSafe");
            var match = regex.Match(cmdResult);
            var groups = match.Groups;
            if (groups.Count>1) {
                device = new USBDevice();
                device.DevId = groups["devId"].Value;
                device.Status = (DeviceStatus)Enum.Parse(typeof(DeviceStatus), groups["status"].Value, true);
            }
            return device;
        }

        private List<string> GetServers() {
            //srvID:2  -  7.105.94.12:33000 (available connections: 0 of 1)
            //srvID:4  -  7.105.94.12:33000 (unreachable)
            string cmd = "list";
            var cmdResult = ExcuteCommand(cmd);
            var regex = new Regex(@"\s(?<server>\S+)\s\((?<des>\S+)\b");
            var matches = regex.Matches(cmdResult);
            List<string> servers = new List<string>();
            foreach (Match item in matches) {
                var groups = item.Groups;
                if (groups["des"].Value.Equals("available")) {
                    servers.Add(groups["server"].Value);
                }
            }
            return servers;
        }

        private string ExcuteCommand(string cmdPatameter) {
            Process p = new Process();
            p.StartInfo.FileName = usbCmdPath;
            p.StartInfo.Arguments = cmdPatameter;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            p.StandardInput.AutoFlush = true;
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            p.Close();
            return output;
        }
    }
}
