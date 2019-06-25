using System;
using System.Diagnostics;
using System.IO;
using Rhino;
using Rhino.DocObjects;
using Rhino.FileIO;

namespace GLTF.Exporter
{
    ///<summary>
    /// <para>Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
    /// class. DO NOT create instances of this class yourself. It is the
    /// responsibility of Rhino to create an instance of this class.</para>
    /// <para>To complete plug-in information, please also see all PlugInDescription
    /// attributes in AssemblyInfo.cs (you might need to click "Project" ->
    /// "Show All Files" to see it in the "Solution Explorer" window).</para>
    ///</summary>
    public class GLTFExporterPlugIn : Rhino.PlugIns.FileExportPlugIn

    {
        private FileSystemWatcher Watcher { get; set; }
        private string ext = null;
        private string tmpFileName = null;

        public GLTFExporterPlugIn()
        {
            Instance = this;
        }

        ///<summary>Gets the only instance of the GLTFExporterPlugIn plug-in.</summary>
        public static GLTFExporterPlugIn Instance
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

            Watcher = new FileSystemWatcher(tmp);
            Watcher.Created += Watcher_Created;
            Watcher.EnableRaisingEvents = true;

            var tmpName = Path.Combine(tmp, file);

            GLTFExporterMethods.Write3dm(doc, options, tmpName);
            GLTFExporterMethods.LaunchNodeProcess(tmpName);

            while (tmpFileName == null) { }

            // move file according to original filename

            File.Copy(tmpFileName, filename, true);

            tmpFileName = null;

            // cleanup

            Watcher.Created -= Watcher_Created;
            Watcher.Dispose();

            var di = new DirectoryInfo(tmp);

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

        // You can override methods here to change the plug-in behavior on
        // loading and shut down, add options pages to the Rhino _Option command
        // and maintain plug-in wide options in a document.

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {

            Debug.WriteLine(e.FullPath);

            if (ext == Path.GetExtension(e.FullPath))
            {
                tmpFileName = e.FullPath;
            }

        }


    }
}