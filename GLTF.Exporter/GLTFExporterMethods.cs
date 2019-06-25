using Rhino;
using Rhino.DocObjects;
using Rhino.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GLTF.Exporter
{
    class GLTFExporterMethods
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename">Name of the file to process.</param>
        public static void LaunchNodeProcess(string filename)
        {
            Process process = new Process();

            process.EnableRaisingEvents = true;
            process.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(Process_OutputDataReceived);
            process.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(Process_ErrorDataReceived);
            process.Exited += new System.EventHandler(Process_Exited);

            process.StartInfo.FileName = "node.exe";
            process.StartInfo.Arguments = "index.js " + "\"" + filename + "\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            //process.StartInfo.CreateNoWindow = true;

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            //below line is optional if we want a blocking call
            process.WaitForExit();
        }

        static void Process_Exited(object sender, EventArgs e)
        {
            var process = (Process)sender;
            Console.WriteLine(string.Format("process exited with code {0}\n", process.ExitCode.ToString()));
        }

        static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data + "\n");
        }

        static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data + "\n");
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="file">3dm file to write to disk.</param>
        /// <param name="path">Path of the file to write.</param>
        /// <returns></returns>
        public static bool Write3dm(RhinoDoc doc, FileWriteOptions options, string path)
        {
            var file3dm = new File3dm();

            var objectEnumeratorSettings = new ObjectEnumeratorSettings
            {
                HiddenObjects = true,
                IncludeLights = true
            };

            foreach (var rhinoObject in doc.Objects.GetObjectList(objectEnumeratorSettings))
            {
                if ((options.WriteSelectedObjectsOnly && rhinoObject.IsSelected(true) == 1) || (!options.WriteSelectedObjectsOnly) || (rhinoObject.IsSelected(true) == 2))
                {

                    file3dm.AllMaterials.Add(doc.Materials[rhinoObject.Attributes.MaterialIndex]);
                    var matId = file3dm.AllMaterials.Count - 1;
                    var att = rhinoObject.Attributes;
                    att.MaterialIndex = matId;

                    switch (rhinoObject.ObjectType)
                    {
                        case ObjectType.Mesh:
                            file3dm.Objects.AddMesh(rhinoObject.Geometry as Rhino.Geometry.Mesh, att);
                            break;

                        case ObjectType.Brep:
                        case ObjectType.Extrusion:
                        case ObjectType.Surface:
                        case ObjectType.SubD:
                            var meshes = rhinoObject.GetMeshes(Rhino.Geometry.MeshType.Default);
                            var mesh = new Rhino.Geometry.Mesh();
                            foreach (var m in meshes)
                                mesh.Append(m);

                            file3dm.Objects.AddMesh(mesh, att);
                            break;
                    }


                }
            }

            return file3dm.Write(path, 0);

        }
    }
}
