using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

#if NET6_0_OR_GREATER 
[assembly:System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif

namespace showkeyinputs
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
#if NET6_0_OR_GREATER 
	        ApplicationConfiguration.Initialize();
#else			
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
#endif
            Application.Run(new Form1());
        }
    }
}
