using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;

namespace ItemCollage
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Technique for loading DLLs from resources in the application
            // Taken from http://blogs.msdn.com/b/microsoft_press/archive/2010/02/03/jeffrey-richter-excerpt-2-from-clr-via-c-third-edition.aspx
//            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
  //          {
    //            String resourceName = "ItemCollage.lib." +
        //           new AssemblyName(args.Name).Name + ".dll";
      //          using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
          //      {
              //      Byte[] assemblyData = new Byte[stream.Length];
            //        stream.Read(assemblyData, 0, assemblyData.Length);
//                    return Assembly.Load(assemblyData);
  //              }
    //        };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
