using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using AutoPackgeCore;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace AutoPackage2019 {
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(OptionPageGrid),
    "自动打包", "选项", 0, 0, true)]
    [Guid(AutoPackage2019Package.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class AutoPackage2019Package : AsyncPackage {
        /// <summary>
        /// AutoPackage2019Package GUID string.
        /// </summary>
        public const string PackageGuidString = "71c29c6f-de9c-483d-bd73-b68af17bd0b0";
        public AutoPackage2019Package() {

        }
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var options = (OptionPageGrid)GetDialogPage(typeof(OptionPageGrid));
            CertificateSerive.Initial((IVSConfig)GetDialogPage(typeof(OptionPageGrid)));
            PackageService.Initial((IVSConfig)GetDialogPage(typeof(OptionPageGrid)));
            options.OnPropertyChanged += Options_OnPropertyChanged;
            VSOutput = (IVsOutputWindowPane)await GetServiceAsync(typeof(SVsGeneralOutputWindowPane));
            await AutoPackageCommand.InitializeAsync(this);
        }
        public static IVsOutputWindowPane VSOutput {
            get;private set;
        }
        private void Options_OnPropertyChanged() {
            CertificateSerive.Initial((IVSConfig)GetDialogPage(typeof(OptionPageGrid)));
            PackageService.Initial((IVSConfig)GetDialogPage(typeof(OptionPageGrid)));
        }
        #endregion
    }
    public class OptionPageGrid : DialogPage, IVSConfig {
        [Category("打包工具")]
        [DisplayName("是否自动跳转发布系统")]
        [Description("https://dist.wangxutech.com/admin")]
        public bool JumpToWeb { get; set; } = true;

        [Category("打包工具")]
        [DisplayName("是否自动打开文件夹")]
        [Description("注意把控制版本的iss改为version.iss")]
        public bool JumpPackedFiles { get; set; } = true;

        [Category("打包证书")]
        [DisplayName("USB Client的usbclncmd.exe完整路径")]
        [Description("连接局域网分享的USB工具")]
        public string USBToolClientPath { get; set; } = "";

        [Category("打包证书")]
        [DisplayName("SignServer.exe的完整路径")]
        [Description("自动输入密码的SignServer签名工具")]
        public string SignServerPath { get; set; }

        [Category("打包证书")]
        [DisplayName("服务器IP地址")]
        [Description("USB Over Network的服务器IP")]
        public string ServerIP { get; set; } = "192.168.0.211";

        [Category("打包证书")]
        [DisplayName("服务器的端口")]
        [Description("USB Over Network的服务器端口")]
        public string ServerPort { get; set; } = "33000";
        public delegate void propertyApplyHandle();
        public event propertyApplyHandle OnPropertyChanged;
        protected override void OnApply(PageApplyEventArgs e) {
            base.OnApply(e);
            OnPropertyChanged();
        }

        public bool ShowMsg(string msg) {
            VsShellUtilities.ShowMessageBox(
        AutoPackageCommand.Instance.ServiceProvider,
        msg,
        "自动打包",
        OLEMSGICON.OLEMSGICON_INFO,
        OLEMSGBUTTON.OLEMSGBUTTON_OK,
        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            return true;
        }

        public void OutputLog(string msg) {
            AutoPackage2019Package.VSOutput.OutputStringThreadSafe(msg+"\r\n");
        }
    }
}
