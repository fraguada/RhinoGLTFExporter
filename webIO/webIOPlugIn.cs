using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Rhino;
using Rhino.DocObjects;
using Rhino.FileIO;

namespace WebIO
{
    ///<summary>
    /// <para>Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
    /// class. DO NOT create instances of this class yourself. It is the
    /// responsibility of Rhino to create an instance of this class.</para>
    /// <para>To complete plug-in information, please also see all PlugInDescription
    /// attributes in AssemblyInfo.cs (you might need to click "Project" ->
    /// "Show All Files" to see it in the "Solution Explorer" window).</para>
    ///</summary>
    public class WebIOPlugIn : Rhino.PlugIns.FileExportPlugIn

    {
        private FileSystemWatcher Watcher { get; set; }
        TaskCompletionSource<bool> tcs = null;
        string ext = null;
        string tmpFileName = null;

        public WebIOPlugIn()
        {
            Instance = this;
        }

        ///<summary>Gets the only instance of the webIOPlugIn plug-in.</summary>
        public static WebIOPlugIn Instance
        {
            get; private set;
        }

        /// <summary>Defines file extensions that this export plug-in is designed to write.</summary>
        /// <param name="options">Options that specify how to write files.</param>
        /// <returns>A list of file types that can be exported.</returns>
        protected override Rhino.PlugIns.FileTypeList AddFileTypes(Rhino.FileIO.FileWriteOptions options)
        {
            var result = new Rhino.PlugIns.FileTypeList();
            result.AddFileType("Graphics Language Transfer Format (*.glTF)", "glTF");
            return result;
        }

        /// <summary>
        /// Is called when a user requests to export a ."glTF file.
        /// It is actually up to this method to write the file itself.
        /// </summary>
        /// <param name="filename">The complete path to the new file.</param>
        /// <param name="index">The index of the file type as it had been specified by the AddFileTypes method.</param>
        /// <param name="doc">The document to be written.</param>
        /// <param name="options">Options that specify how to write file.</param>
        /// <returns>A value that defines success or a specific failure.</returns>
        protected override Rhino.PlugIns.WriteFileResult WriteFile(string filename, int index, RhinoDoc doc, Rhino.FileIO.FileWriteOptions options)
        {

            ext = Path.GetExtension(filename);
            var originalDir = Path.GetDirectoryName(filename);
            var file = Path.GetFileName(filename);
            file = Path.ChangeExtension(file, ".3dm");
            var tmp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Guid.NewGuid().ToString());
            var dir = Directory.CreateDirectory(tmp);

            // add a directory watcher

            Watcher = new FileSystemWatcher(tmp);
            Watcher.Created += Watcher_Created;
            Watcher.EnableRaisingEvents = true;

            var file3dm = new File3dm();
            var tmpName = Path.Combine(tmp, file);

            // populate file3dm

            var objectEnumeratorSettings = new ObjectEnumeratorSettings
            {
                HiddenObjects = true,
                IncludeLights = true
            };

            foreach (var rhinoObject in doc.Objects.GetObjectList(objectEnumeratorSettings))
            {
                if ((options.WriteSelectedObjectsOnly && rhinoObject.IsSelected(true) == 1) || (!options.WriteSelectedObjectsOnly) || (rhinoObject.IsSelected(true) == 2))
                {
                    
                    file3dm.Materials.Add(doc.Materials[rhinoObject.Attributes.MaterialIndex]);
                    var matId = file3dm.Materials.Count - 1;
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



            var result = file3dm.Write(tmpName, 0);

            //RunNode(tmpName);
            LaunchProcess(tmpName);

            while (tmpFileName == null) { }

            // move file according to original filename

            File.Copy(tmpFileName, filename, true);

            tmpFileName = null;

            // cleanup

            Watcher.Created -= Watcher_Created;
            Watcher.Dispose();

            System.IO.DirectoryInfo di = new DirectoryInfo(tmp);

            foreach (FileInfo f in di.GetFiles())
            {
                f.Delete();
            }
            foreach (DirectoryInfo d in di.GetDirectories())
            {
                d.Delete(true);
            }

            Directory.Delete(tmp, true);

            return Rhino.PlugIns.WriteFileResult.Success;
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            
            Debug.WriteLine(e.FullPath);

            if (ext == Path.GetExtension(e.FullPath))
            {
                tmpFileName = e.FullPath;
            }
            
        }

        // You can override methods here to change the plug-in behavior on
        // loading and shut down, add options pages to the Rhino _Option command
        // and maintain plug-in wide options in a document.

        private async void RunNode(string filename)
        {
            var fn = System.Text.Encoding.UTF8.GetBytes(filename);

            System.Diagnostics.ProcessStartInfo psi;

            psi = new System.Diagnostics.ProcessStartInfo("node.exe")
            {
                WorkingDirectory = "../../../WebIOApp/",
                UseShellExecute = true,
                Arguments = "index.js " + "\"" + filename + "\"",
                WindowStyle = ProcessWindowStyle.Maximized
                
            };

            try
            {
                System.Diagnostics.Process.Start(psi);
                tcs = new TaskCompletionSource<bool>();
                await tcs.Task;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Launching node failed. " + e.Message, "WebIO");
            }

            

        }

        Process process = new Process();

        void LaunchProcess(string file)
        {
            process.EnableRaisingEvents = true;
            process.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(Process_OutputDataReceived);
            process.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(Process_ErrorDataReceived);
            process.Exited += new System.EventHandler(Process_Exited);

            process.StartInfo.FileName = "node.exe";
            process.StartInfo.Arguments = "index.js " + "\"" + file + "\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.WorkingDirectory = "../../../WebIOApp/";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            //below line is optional if we want a blocking call
            process.WaitForExit();
        }

        void Process_Exited(object sender, EventArgs e)
        {
            Console.WriteLine(string.Format("process exited with code {0}\n", process.ExitCode.ToString()));
        }

        void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data + "\n");
        }

        void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data + "\n");
        }
    }
}