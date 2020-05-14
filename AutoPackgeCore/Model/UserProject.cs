using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AutoPackgeCore.Model {
    public class UserProject {
        public UserProject(string projectFullName,string solutionDir) {
            this.Source = Directory.GetParent(solutionDir).FullName;
            var projectDir = Path.GetDirectoryName(projectFullName);
            this.AssemblySource = Path.Combine(projectDir, "Properties\\AssemblyInfo.cs");
            this.VersionIssSource = $@"{this.Source}\package\ProVersion\IssFiles\version.iss";
        }
        public string AssemblySource { get; set; }
        public string VersionIssSource { get; set; }
        public string Source { get; set; }
    }
}
