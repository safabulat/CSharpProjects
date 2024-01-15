using System;
using System.Drawing;
using System.Windows.Forms;

namespace GeminiReaderUpdaterGUI
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label readerLabel, consoleLbl;
        private System.Windows.Forms.Button clearConsole;

        private void InitializeComponent()
        {
            this.lblStatus = new System.Windows.Forms.Label();
            this.readerLabel = new System.Windows.Forms.Label();
            this.btnUpdateFirmware = new System.Windows.Forms.Button();
            this.clearConsole = new System.Windows.Forms.Button();
            this.deviceUID = new System.Windows.Forms.TextBox();
            this.status = new System.Windows.Forms.Panel();
            this.consoleLbl = new System.Windows.Forms.Label();
            this.console = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Arial", 10F);
            this.lblStatus.Location = new System.Drawing.Point(12, 57);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(0, 19);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // readerLabel
            // 
            this.readerLabel.Font = new System.Drawing.Font("Arial", 9F);
            this.readerLabel.Location = new System.Drawing.Point(48, 9);
            this.readerLabel.Name = "readerLabel";
            this.readerLabel.Size = new System.Drawing.Size(100, 30);
            this.readerLabel.TabIndex = 0;
            this.readerLabel.Text = "Reader UID: ";
            this.readerLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnUpdateFirmware
            // 
            this.btnUpdateFirmware.Enabled = false;
            this.btnUpdateFirmware.Location = new System.Drawing.Point(350, 550);
            this.btnUpdateFirmware.Name = "btnUpdateFirmware";
            this.btnUpdateFirmware.Size = new System.Drawing.Size(200, 30);
            this.btnUpdateFirmware.TabIndex = 1;
            this.btnUpdateFirmware.Text = "Update Firmware";
            this.btnUpdateFirmware.UseVisualStyleBackColor = true;
            this.btnUpdateFirmware.Click += new System.EventHandler(this.btnUpdateFirmware_Click);
            // 
            // clearConsole
            // 
            this.clearConsole.Location = new System.Drawing.Point(813, 94);
            this.clearConsole.Name = "clearConsole";
            this.clearConsole.Size = new System.Drawing.Size(75, 23);
            this.clearConsole.TabIndex = 4;
            this.clearConsole.Text = "Clear";
            this.clearConsole.Click += new System.EventHandler(this.clearConsole_Click);
            // 
            // deviceUID
            // 
            this.deviceUID.Font = new System.Drawing.Font("Arial", 10F);
            this.deviceUID.Location = new System.Drawing.Point(160, 9);
            this.deviceUID.Multiline = true;
            this.deviceUID.Name = "deviceUID";
            this.deviceUID.ReadOnly = true;
            this.deviceUID.Size = new System.Drawing.Size(750, 30);
            this.deviceUID.TabIndex = 2;
            // 
            // status
            // 
            this.status.Location = new System.Drawing.Point(12, 9);
            this.status.Name = "status";
            this.status.Size = new System.Drawing.Size(30, 30);
            this.status.TabIndex = 3;
            this.status.Paint += new System.Windows.Forms.PaintEventHandler(this.status_Paint);
            // 
            // consoleLbl
            // 
            this.consoleLbl.Font = new System.Drawing.Font("Arial", 9F);
            this.consoleLbl.Location = new System.Drawing.Point(12, 90);
            this.consoleLbl.Name = "consoleLbl";
            this.consoleLbl.Size = new System.Drawing.Size(100, 30);
            this.consoleLbl.TabIndex = 0;
            this.consoleLbl.Text = "Console:";
            this.consoleLbl.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // console
            // 
            this.console.Font = new System.Drawing.Font("Arial", 9F);
            this.console.Location = new System.Drawing.Point(16, 123);
            this.console.Multiline = true;
            this.console.Name = "console";
            this.console.Size = new System.Drawing.Size(872, 396);
            this.console.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 600);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.readerLabel);
            this.Controls.Add(this.btnUpdateFirmware);
            this.Controls.Add(this.deviceUID);
            this.Controls.Add(this.status);
            this.Controls.Add(this.consoleLbl);
            this.Controls.Add(this.console);
            this.Controls.Add(this.clearConsole);
            this.Name = "Form1";
            this.Text = "GeminiFWUpdater";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

    }
}
