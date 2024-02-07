using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GeminiUIDReader
{
    partial class Form1
    {
        private System.Windows.Forms.Label readerLabel, consoleLbl;
        private System.Windows.Forms.Button clearConsole;

        private Button openFileButton, openFileLocationButton;

        private void InitializeComponent()
        {
            this.lblStatus = new System.Windows.Forms.Label();
            this.readerLabel = new System.Windows.Forms.Label();
            this.clearConsole = new System.Windows.Forms.Button();
            this.deviceUID = new System.Windows.Forms.TextBox();
            this.consoleLbl = new System.Windows.Forms.Label();
            this.console = new System.Windows.Forms.TextBox();
            this.openFileButton = new System.Windows.Forms.Button();
            this.openFileLocationButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblStatus
            // 
            this.lblStatus.Font = new System.Drawing.Font("Arial", 10F);
            this.lblStatus.Location = new System.Drawing.Point(16, 62);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(872, 91);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // readerLabel
            // 
            this.readerLabel.Font = new System.Drawing.Font("Arial", 9F);
            this.readerLabel.Location = new System.Drawing.Point(12, 9);
            this.readerLabel.Name = "readerLabel";
            this.readerLabel.Size = new System.Drawing.Size(101, 30);
            this.readerLabel.TabIndex = 0;
            this.readerLabel.Text = "Reader UID: ";
            this.readerLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // clearConsole
            // 
            this.clearConsole.Location = new System.Drawing.Point(813, 187);
            this.clearConsole.Name = "clearConsole";
            this.clearConsole.Size = new System.Drawing.Size(75, 23);
            this.clearConsole.TabIndex = 4;
            this.clearConsole.Text = "Clear";
            this.clearConsole.Click += new System.EventHandler(this.clearConsole_Click);
            // 
            // deviceUID
            // 
            this.deviceUID.Font = new System.Drawing.Font("Arial", 10F);
            this.deviceUID.Location = new System.Drawing.Point(113, 9);
            this.deviceUID.Multiline = true;
            this.deviceUID.Name = "deviceUID";
            this.deviceUID.ReadOnly = true;
            this.deviceUID.Size = new System.Drawing.Size(775, 30);
            this.deviceUID.TabIndex = 2;
            // 
            // consoleLbl
            // 
            this.consoleLbl.Font = new System.Drawing.Font("Arial", 9F);
            this.consoleLbl.Location = new System.Drawing.Point(13, 187);
            this.consoleLbl.Name = "consoleLbl";
            this.consoleLbl.Size = new System.Drawing.Size(100, 30);
            this.consoleLbl.TabIndex = 0;
            this.consoleLbl.Text = "UID List:";
            this.consoleLbl.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // console
            // 
            this.console.Font = new System.Drawing.Font("Arial", 9F);
            this.console.Location = new System.Drawing.Point(16, 220);
            this.console.Multiline = true;
            this.console.Name = "console";
            this.console.Size = new System.Drawing.Size(872, 420);
            this.console.TabIndex = 0;
            // 
            // openFileButton
            // 
            this.openFileButton.Font = new System.Drawing.Font("Arial", 9F);
            this.openFileButton.Location = new System.Drawing.Point(15, 646);
            this.openFileButton.Name = "openFileButton";
            this.openFileButton.Size = new System.Drawing.Size(98, 42);
            this.openFileButton.TabIndex = 0;
            this.openFileButton.Text = "Open File";
            this.openFileButton.Click += new System.EventHandler(this.OpenFileButton_Click);
            // 
            // openFileLocationButton
            // 
            this.openFileLocationButton.Font = new System.Drawing.Font("Arial", 9F);
            this.openFileLocationButton.Location = new System.Drawing.Point(119, 646);
            this.openFileLocationButton.Name = "openFileLocationButton";
            this.openFileLocationButton.Size = new System.Drawing.Size(98, 42);
            this.openFileLocationButton.TabIndex = 1;
            this.openFileLocationButton.Text = "Show File";
            this.openFileLocationButton.Click += new System.EventHandler(this.OpenFileLocationButton_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 700);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.readerLabel);
            this.Controls.Add(this.deviceUID);
            this.Controls.Add(this.consoleLbl);
            this.Controls.Add(this.console);
            this.Controls.Add(this.clearConsole);
            this.Controls.Add(this.openFileButton);
            this.Controls.Add(this.openFileLocationButton);
            this.Name = "Form1";
            this.Text = "GeminiFWUpdater";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void OpenFileButton_Click(object sender, EventArgs e)
        {
            string filePath = "GeminiReaderUIDs.txt";
            if (File.Exists(filePath))
            {
                Process.Start(filePath);
            }
            else
            {
                MessageBox.Show("The file does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenFileLocationButton_Click(object sender, EventArgs e)
        {
            string filePath = "GeminiReaderUIDs.txt";
            if (File.Exists(filePath))
            {
                Process.Start("explorer.exe", $"/select,\"{Path.GetFullPath(filePath)}\"");
            }
            else
            {
                MessageBox.Show("The file does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
