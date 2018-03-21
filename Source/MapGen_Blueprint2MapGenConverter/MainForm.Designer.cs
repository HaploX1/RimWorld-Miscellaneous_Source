namespace Blueprint2MapGenConverter
{
    partial class MainForm
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.OFD = new System.Windows.Forms.OpenFileDialog();
            this.txtFileSelected = new System.Windows.Forms.TextBox();
            this.buttonSelectFile = new System.Windows.Forms.Button();
            this.txtFileCreated = new System.Windows.Forms.TextBox();
            this.buttonConvertFile = new System.Windows.Forms.Button();
            this.buttonOpenFolder = new System.Windows.Forms.Button();
            this.txtPath = new System.Windows.Forms.TextBox();
            this.ToolTipText = new System.Windows.Forms.ToolTip(this.components);
            this.FBD = new System.Windows.Forms.FolderBrowserDialog();
            this.rbMapGen = new System.Windows.Forms.RadioButton();
            this.rbMapGenFB = new System.Windows.Forms.RadioButton();
            this.SuspendLayout();
            // 
            // OFD
            // 
            this.OFD.FileName = "openFileDialog1";
            // 
            // txtFileSelected
            // 
            this.txtFileSelected.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFileSelected.Enabled = false;
            this.txtFileSelected.Location = new System.Drawing.Point(146, 55);
            this.txtFileSelected.Name = "txtFileSelected";
            this.txtFileSelected.ReadOnly = true;
            this.txtFileSelected.Size = new System.Drawing.Size(225, 20);
            this.txtFileSelected.TabIndex = 2;
            this.txtFileSelected.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.ToolTipText.SetToolTip(this.txtFileSelected, "\'Sect File\' to open an exported Blueprint file of Fluffy\'s Blueprint mod");
            // 
            // buttonSelectFile
            // 
            this.buttonSelectFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSelectFile.Location = new System.Drawing.Point(377, 53);
            this.buttonSelectFile.Name = "buttonSelectFile";
            this.buttonSelectFile.Size = new System.Drawing.Size(98, 23);
            this.buttonSelectFile.TabIndex = 3;
            this.buttonSelectFile.Text = "Select File";
            this.ToolTipText.SetToolTip(this.buttonSelectFile, "Select the exported Blueprint file of Fluffy\'s Blueprint mod.");
            this.buttonSelectFile.UseVisualStyleBackColor = true;
            this.buttonSelectFile.Click += new System.EventHandler(this.buttonSelectFile_Click);
            // 
            // txtFileCreated
            // 
            this.txtFileCreated.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFileCreated.Enabled = false;
            this.txtFileCreated.Location = new System.Drawing.Point(146, 84);
            this.txtFileCreated.Name = "txtFileCreated";
            this.txtFileCreated.ReadOnly = true;
            this.txtFileCreated.Size = new System.Drawing.Size(225, 20);
            this.txtFileCreated.TabIndex = 4;
            this.txtFileCreated.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.ToolTipText.SetToolTip(this.txtFileCreated, "This will show the name of the exported file.");
            // 
            // buttonConvertFile
            // 
            this.buttonConvertFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonConvertFile.Location = new System.Drawing.Point(377, 82);
            this.buttonConvertFile.Name = "buttonConvertFile";
            this.buttonConvertFile.Size = new System.Drawing.Size(98, 23);
            this.buttonConvertFile.TabIndex = 5;
            this.buttonConvertFile.Text = "Convert File";
            this.ToolTipText.SetToolTip(this.buttonConvertFile, "Convert the data to a usable Misc. MapGeneratorFactionBase file");
            this.buttonConvertFile.UseVisualStyleBackColor = true;
            this.buttonConvertFile.Click += new System.EventHandler(this.buttonConvertFile_Click);
            // 
            // buttonOpenFolder
            // 
            this.buttonOpenFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOpenFolder.Location = new System.Drawing.Point(377, 111);
            this.buttonOpenFolder.Name = "buttonOpenFolder";
            this.buttonOpenFolder.Size = new System.Drawing.Size(98, 23);
            this.buttonOpenFolder.TabIndex = 6;
            this.buttonOpenFolder.Text = "Open Folder";
            this.ToolTipText.SetToolTip(this.buttonOpenFolder, "Open the folder where the files can be found.");
            this.buttonOpenFolder.UseVisualStyleBackColor = true;
            this.buttonOpenFolder.Click += new System.EventHandler(this.buttonOpenFolder_Click);
            // 
            // txtPath
            // 
            this.txtPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPath.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtPath.Cursor = System.Windows.Forms.Cursors.Default;
            this.txtPath.Enabled = false;
            this.txtPath.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtPath.Location = new System.Drawing.Point(12, 29);
            this.txtPath.Name = "txtPath";
            this.txtPath.ReadOnly = true;
            this.txtPath.Size = new System.Drawing.Size(549, 11);
            this.txtPath.TabIndex = 1;
            this.txtPath.Text = "...";
            this.txtPath.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // rbMapGen
            // 
            this.rbMapGen.AutoSize = true;
            this.rbMapGen.Location = new System.Drawing.Point(12, 56);
            this.rbMapGen.Name = "rbMapGen";
            this.rbMapGen.Size = new System.Drawing.Size(66, 17);
            this.rbMapGen.TabIndex = 7;
            this.rbMapGen.Text = "MapGen";
            this.rbMapGen.UseVisualStyleBackColor = true;
            // 
            // rbMapGenFB
            // 
            this.rbMapGenFB.AutoSize = true;
            this.rbMapGenFB.Checked = true;
            this.rbMapGenFB.Location = new System.Drawing.Point(12, 79);
            this.rbMapGenFB.Name = "rbMapGenFB";
            this.rbMapGenFB.Size = new System.Drawing.Size(128, 17);
            this.rbMapGenFB.TabIndex = 8;
            this.rbMapGenFB.TabStop = true;
            this.rbMapGenFB.Text = "MapGen FactionBase";
            this.rbMapGenFB.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(573, 181);
            this.Controls.Add(this.rbMapGenFB);
            this.Controls.Add(this.rbMapGen);
            this.Controls.Add(this.txtPath);
            this.Controls.Add(this.buttonOpenFolder);
            this.Controls.Add(this.buttonConvertFile);
            this.Controls.Add(this.txtFileCreated);
            this.Controls.Add(this.buttonSelectFile);
            this.Controls.Add(this.txtFileSelected);
            this.Name = "MainForm";
            this.Text = "Converter - Blueprint to MapGenerator ";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog OFD;
        private System.Windows.Forms.TextBox txtFileSelected;
        private System.Windows.Forms.Button buttonSelectFile;
        private System.Windows.Forms.TextBox txtFileCreated;
        private System.Windows.Forms.Button buttonConvertFile;
        private System.Windows.Forms.Button buttonOpenFolder;
        private System.Windows.Forms.TextBox txtPath;
        public System.Windows.Forms.ToolTip ToolTipText;
        private System.Windows.Forms.FolderBrowserDialog FBD;
        private System.Windows.Forms.RadioButton rbMapGen;
        private System.Windows.Forms.RadioButton rbMapGenFB;
    }
}

