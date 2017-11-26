using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Blueprint2MapGenConverter
{
    public partial class MainForm : Form
    {
        private string path;
        private string workFilePath;

        public MainForm()
        {
            InitializeComponent();
            
            string pathLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string pathLocalLow = string.Concat(pathLocal, "Low");
            string pathRimWorldSettings = Path.Combine(pathLocalLow, @"Ludeon Studios\RimWorld by Ludeon Studios");

            path = pathRimWorldSettings;

            if (Directory.Exists(Path.Combine(pathRimWorldSettings, "Blueprints")))
                path = Path.Combine(pathRimWorldSettings, "Blueprints");

            txtPath.Text = path;
        }

        private void buttonSelectFile_Click(object sender, EventArgs e)
        {
            OFD.InitialDirectory = path;
            OFD.Multiselect = false;
            OFD.Filter = "xml files | *.xml";
            OFD.FileName = "";

            if (OFD.ShowDialog() == DialogResult.OK)
                if (File.Exists(OFD.FileName))
                {
                    workFilePath = OFD.FileName;
                    txtFileSelected.Text = Path.GetFileName(workFilePath);

                    if (Path.GetDirectoryName(OFD.FileName) != path)
                    {
                        path = Path.GetDirectoryName(OFD.FileName);
                        txtPath.Text = path;
                    }
                }
        }

        private void buttonConvertFile_Click(object sender, EventArgs e)
        {
            if (!File.Exists(workFilePath) || txtFileSelected.Text == null || txtFileSelected.Text == "")
                return;

            string outFile = "MapGenerator_" + Path.GetFileNameWithoutExtension(txtFileSelected.Text) + ".xml";
            string outFilePath = System.IO.Path.Combine(path, outFile);

            // Load Fluffy Blueprint
            Fluffy_Blueprint blueprintIN = new Fluffy_Blueprint();

            Scribe.InitLoading(workFilePath);
            Scribe.EnterNode("Blueprint");
            blueprintIN.ExposeData();
            Scribe.ExitNode();
            Scribe.FinalizeLoading();

            // Write data to MapGen Blueprint
            Misc_Blueprint blueprintOUT = new Misc_Blueprint();
            Converter.FillMiscBlueprintFromFluffyBlueprint(blueprintIN, ref blueprintOUT);

            Scribe.InitWriting(outFilePath, "Defs");
            Scribe.EnterNode("MapGenerator.MapGeneratorBaseBlueprintDef");
            Scribe.WriteAttribute("Name", "TODO_enter_a_name_here");
            blueprintOUT.ExposeData();
            Scribe.FinalizeWriting();

            
            txtFileCreated.Text = outFile;
        }

        private void buttonOpenFolder_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", path);
        }

        private void buttonChangeSourcePath_Click(object sender, EventArgs e)
        {
            FBD.SelectedPath = txtPath.Text;
            FBD.ShowNewFolderButton = false;

            if (FBD.ShowDialog() == DialogResult.OK)
                if (Directory.Exists(FBD.SelectedPath))
                    txtPath.Text = FBD.SelectedPath;

        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }
    }
}
