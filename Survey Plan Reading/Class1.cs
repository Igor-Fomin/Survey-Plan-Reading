using System;
using System.IO;
using System.Reflection;
using Autodesk.AutoCAD.Runtime;

namespace Survey_Plan_Reading
{
    public class ExtensionApplication : IExtensionApplication
    {
        public void Initialize()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                // Specifically handle WinRT/SDK assemblies that might not be in the GAC or CAD's default search path
                if (args.Name.Contains("Microsoft.Windows.SDK.NET") || args.Name.Contains("WinRT.Runtime"))
                {
                    string folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string assemblyName = new AssemblyName(args.Name).Name + ".dll";
                    string path = Path.Combine(folder, assemblyName);
                    
                    if (File.Exists(path))
                    {
                        return Assembly.LoadFrom(path);
                    }
                }
                return null;
            };
        }

        public void Terminate()
        {
            // Cleanup if necessary
        }
    }
}
