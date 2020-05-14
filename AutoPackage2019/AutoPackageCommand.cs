using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AutoPackgeCore;
using AutoPackgeCore.Model;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace AutoPackage2019 {
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class AutoPackageCommand {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("25f90b2e-b310-42bc-b9a7-a4525499685c");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoPackageCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private AutoPackageCommand(AsyncPackage package, OleMenuCommandService commandService) {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static AutoPackageCommand Instance {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        public AsyncPackage ServiceProvider {
            get {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package) {
            // Switch to the main thread - the call to AddCommand in AutoPackageCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new AutoPackageCommand(package, commandService);
        }
        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e) {
            ThreadHelper.ThrowIfNotOnUIThread();
            string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            var dte2 = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
            var solution = dte2.Solution;
            var solutionName = Path.GetFileName(solution.FullName);//解决方案名称
            if (!string.IsNullOrEmpty(solutionName)) {
                AutoPackage2019Package.VSOutput.OutputStringThreadSafe($"【{DateTime.Now}】:自动打包---{solutionName}\r\n");
                var projects = (EnvDTE.UIHierarchyItem[])dte2?.ToolWindows.SolutionExplorer.SelectedItems;
                var project = projects[0].Object as EnvDTE.Project;
                string projectFullName = project.FullName;
                string solutionDir = Path.GetDirectoryName(solution.FullName);
                UserProject userProject = new UserProject(projectFullName, solutionDir);
                InnerExcuteAsync(userProject);
            }
            else {
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    "请先打开一个解决方案！",
                    "错误",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }
        private async Task InnerExcuteAsync(UserProject project) {
            string error = "";
            if (CertificateSerive.StartSignServer(out error)) {
                var connected = await CertificateSerive.Connect();
                if (!connected) {
                    return;
                }
                await PackageService.Pack(project);
                CertificateSerive.DisConnect();
            }
            else {
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"自动签名工具启动失败，{error}",
                    "错误",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }
    }
}
