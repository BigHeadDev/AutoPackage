using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace AutoPackgeCore {
    public interface IVSConfig {
        
        bool DumpToWeb { get; set; }
        
        bool DumpPackedFiles { get; set; }
        
        string USBToolClientPath { get; set; }
        
        string SignServerPath { get; set; }
        
        string ServerIP { get; set; }
        
        string ServerPort { get; set; }

        bool ShowMsg(string msg);

        void OutputLog(string msg);
    }
}
