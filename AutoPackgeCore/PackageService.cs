using AutoPackgeCore.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoPackgeCore {
    public static class PackageService {
        private static IVSConfig ivsConfig;
        static bool hasError = false;

        public static void Initial(IVSConfig config) {
            ivsConfig = config;
        }
        public static async Task Pack(UserProject project) {
            string SetVersionTool(string input, string version) {
                //var outputVersionString = version.Remove(version.LastIndexOf('.')).Replace(".", "");
                version = version.Remove(version.LastIndexOf('.'));
                var outputVersionString = version.Replace(".", "");
                var buildNomberString = string.Format("(Build {0}/{1}/{2})", DateTime.Now.Month.ToString("00"), DateTime.Now.Day.ToString("00"), DateTime.Now.Year.ToString("00"));
                var stringResult = Regex.Replace(
                                        Regex.Replace(
                                            Regex.Replace(input,
                                                "MyAppVersion \"[0-9.]*\"",
                                                $"MyAppVersion \"{version}\""),
                                            "MyAppBuildNo \"\\(Build [0-9/]*\\)\"",
                                            $"MyAppBuildNo \"{buildNomberString}\""),
                                    "OutputVersion \"[0-9.]*\"",
                                    $"OutputVersion \"{outputVersionString}\"");

                return stringResult;
            }
            Match GetVersionTool(string input) {
                var result = Regex.Matches(input, "\\[assembly: AssemblyVersion\\(\"[0-9.]*\"\\)\\]");
                if (result != null && result.Count > 0) {
                    return result[0];
                }
                return null;
            }

            if (File.Exists(project.AssemblySource) && File.Exists(project.VersionIssSource)) {
                string text = File.ReadAllText(project.AssemblySource);
                var result = GetVersionTool(text)?.Value;
                //[assembly: AssemblyVersion("1.4.4.13")]
                if (!string.IsNullOrEmpty(result)) {
                    var results = result.Split('"');
                    if (results.Length > 2) {
                        var version = results[1];

                        string issText = File.ReadAllText(project.VersionIssSource);
                        var changeIssTextResult = SetVersionTool(issText, version);
                        ivsConfig.OutputLog($"version.iss:\n{changeIssTextResult}");

                        if (string.IsNullOrEmpty(changeIssTextResult)) {
                            ivsConfig.ShowMsg("修改版本错误");
                        }
                        else {
                            ivsConfig.OutputLog("修改版本和日期成功");
                            File.WriteAllText(project.VersionIssSource, changeIssTextResult,Encoding.UTF8);
                        }
                    }
                }
            }

            await Task.Factory.StartNew(() => {
                ProcessStartInfo psi = new ProcessStartInfo($@"{project.Source}\package\ProVersion\Pack_normal.bat") {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    ErrorDialog = true,
                    StandardOutputEncoding = Encoding.UTF8
                };
                Process p = new Process { StartInfo = psi };
                //ProcessHelper.AddProcess(p);
                p.OutputDataReceived += ((sender, e) => {
                    if (e.Data == null) {
                        return;
                    }
                    UTF8Encoding encoder = new UTF8Encoding();
                    Byte[] encodedBytes = encoder.GetBytes(e.Data);
                    string decodedString = encoder.GetString(encodedBytes);
                    ivsConfig.OutputLog(decodedString);
                    if (e.Data.Contains("打包完成，请手动检查生成的文件")) {
                        (sender as Process).Kill();
                        ivsConfig.ShowMsg("打包完成，请手动检查生成的文件,并检查控制台是否有错误");
                        if (ivsConfig.JumpPackedFiles) {
                            Process.Start($@"{project.Source}\package\ProVersion\PackedFiles");
                        }
                        if (ivsConfig.JumpToWeb) {
                            Process.Start($@"https://dist.wangxutech.com/admin");
                        }
                    }
                });
                p.Start();
                p.BeginOutputReadLine();
                p.WaitForExit();
                return true;
            });
        }
    }
}
