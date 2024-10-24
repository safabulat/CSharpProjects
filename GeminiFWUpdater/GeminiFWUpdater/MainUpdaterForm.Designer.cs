using System;
using System.Drawing;
using System.Windows.Forms;

namespace GeminiReaderUpdaterGUI
{
    partial class MainUpdaterForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label readerLabel, consoleLabel, readerStatusLabel;
        private System.Windows.Forms.Button clearConsoleBTN, connectToTheReaderBTN, forceTheBootloaderBTN, reloadComPortsBTN, disconnectFromTheBootloaderBTN;
        private System.Windows.Forms.Button readTestCardUIDBTN;

        private void InitializeComponent()
        {
            this.programInfoLabel = new System.Windows.Forms.Label();
            this.lblUpdateStatus = new System.Windows.Forms.Label();
            this.readerLabel = new System.Windows.Forms.Label();
            this.clearConsoleBTN = new System.Windows.Forms.Button();
            this.deviceUIDTextBox = new System.Windows.Forms.TextBox();
            this.consoleLabel = new System.Windows.Forms.Label();
            this.consoleTextBox = new System.Windows.Forms.TextBox();
            this.comboBoxPorts = new System.Windows.Forms.ComboBox();
            this.connectToTheReaderBTN = new System.Windows.Forms.Button();
            this.forceTheBootloaderBTN = new System.Windows.Forms.Button();
            this.reloadComPortsBTN = new System.Windows.Forms.Button();
            this.disconnectFromTheBootloaderBTN = new System.Windows.Forms.Button();
            this.readTestCardUIDBTN = new System.Windows.Forms.Button();
            this.readerStatusLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // programInfoLabel
            // 
            this.programInfoLabel.AccessibleName = "lblStatus";
            this.programInfoLabel.Font = new System.Drawing.Font("Arial", 10F);
            this.programInfoLabel.Location = new System.Drawing.Point(15, 150);
            this.programInfoLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.programInfoLabel.Name = "programInfoLabel";
            this.programInfoLabel.Size = new System.Drawing.Size(651, 63);
            this.programInfoLabel.TabIndex = 0;
            this.programInfoLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblUpdateStatus
            // 
            this.lblUpdateStatus.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold);
            this.lblUpdateStatus.Location = new System.Drawing.Point(17, 118);
            this.lblUpdateStatus.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblUpdateStatus.Name = "lblUpdateStatus";
            this.lblUpdateStatus.Size = new System.Drawing.Size(651, 32);
            this.lblUpdateStatus.TabIndex = 0;
            this.lblUpdateStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // readerLabel
            // 
            this.readerLabel.Font = new System.Drawing.Font("Arial", 9F);
            this.readerLabel.Location = new System.Drawing.Point(272, 12);
            this.readerLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.readerLabel.Name = "readerLabel";
            this.readerLabel.Size = new System.Drawing.Size(75, 25);
            this.readerLabel.TabIndex = 0;
            this.readerLabel.Text = "Reader Info: ";
            this.readerLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // clearConsoleBTN
            // 
            this.clearConsoleBTN.Location = new System.Drawing.Point(610, 226);
            this.clearConsoleBTN.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.clearConsoleBTN.Name = "clearConsoleBTN";
            this.clearConsoleBTN.Size = new System.Drawing.Size(56, 19);
            this.clearConsoleBTN.TabIndex = 4;
            this.clearConsoleBTN.Text = "Clear";
            this.clearConsoleBTN.Click += new System.EventHandler(this.clearConsoleBTN_Click);
            // 
            // deviceUIDTextBox
            // 
            this.deviceUIDTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.deviceUIDTextBox.Font = new System.Drawing.Font("Arial", 10F);
            this.deviceUIDTextBox.Location = new System.Drawing.Point(351, 12);
            this.deviceUIDTextBox.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.deviceUIDTextBox.Multiline = true;
            this.deviceUIDTextBox.Name = "deviceUIDTextBox";
            this.deviceUIDTextBox.ReadOnly = true;
            this.deviceUIDTextBox.Size = new System.Drawing.Size(317, 102);
            this.deviceUIDTextBox.TabIndex = 2;
            // 
            // consoleLabel
            // 
            this.consoleLabel.Font = new System.Drawing.Font("Arial", 9F);
            this.consoleLabel.Location = new System.Drawing.Point(12, 226);
            this.consoleLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.consoleLabel.Name = "consoleLabel";
            this.consoleLabel.Size = new System.Drawing.Size(75, 25);
            this.consoleLabel.TabIndex = 0;
            this.consoleLabel.Text = "Console:";
            this.consoleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // consoleTextBox
            // 
            this.consoleTextBox.Font = new System.Drawing.Font("Arial", 9F);
            this.consoleTextBox.Location = new System.Drawing.Point(15, 264);
            this.consoleTextBox.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.consoleTextBox.Multiline = true;
            this.consoleTextBox.Name = "consoleTextBox";
            this.consoleTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.consoleTextBox.Size = new System.Drawing.Size(647, 467);
            this.consoleTextBox.TabIndex = 0;
            // 
            // comboBoxPorts
            // 
            this.comboBoxPorts.Font = new System.Drawing.Font("Arial", 9F);
            this.comboBoxPorts.Location = new System.Drawing.Point(15, 12);
            this.comboBoxPorts.Margin = new System.Windows.Forms.Padding(2);
            this.comboBoxPorts.Name = "comboBoxPorts";
            this.comboBoxPorts.Size = new System.Drawing.Size(150, 23);
            this.comboBoxPorts.TabIndex = 0;
            this.comboBoxPorts.Text = "Select COM Port";
            // 
            // connectToTheReaderBTN
            // 
            this.connectToTheReaderBTN.Enabled = false;
            this.connectToTheReaderBTN.Font = new System.Drawing.Font("Arial", 9.5F);
            this.connectToTheReaderBTN.Location = new System.Drawing.Point(15, 40);
            this.connectToTheReaderBTN.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.connectToTheReaderBTN.Name = "connectToTheReaderBTN";
            this.connectToTheReaderBTN.Size = new System.Drawing.Size(150, 32);
            this.connectToTheReaderBTN.TabIndex = 5;
            this.connectToTheReaderBTN.Text = "Connect";
            this.connectToTheReaderBTN.Click += new System.EventHandler(this.toggleTheReaderConnectionAppBTN_Click);
            // 
            // forceTheBootloaderBTN
            // 
            this.forceTheBootloaderBTN.Enabled = false;
            this.forceTheBootloaderBTN.Font = new System.Drawing.Font("Arial", 9.5F);
            this.forceTheBootloaderBTN.Location = new System.Drawing.Point(15, 78);
            this.forceTheBootloaderBTN.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.forceTheBootloaderBTN.Name = "forceTheBootloaderBTN";
            this.forceTheBootloaderBTN.Size = new System.Drawing.Size(150, 32);
            this.forceTheBootloaderBTN.TabIndex = 5;
            this.forceTheBootloaderBTN.Text = "Connect BL";
            this.forceTheBootloaderBTN.Click += new System.EventHandler(this.forceTheBootloaderBTN_Click);
            // 
            // reloadComPortsBTN
            // 
            this.reloadComPortsBTN.Font = new System.Drawing.Font("Arial", 9.5F);
            this.reloadComPortsBTN.Location = new System.Drawing.Point(169, 12);
            this.reloadComPortsBTN.Margin = new System.Windows.Forms.Padding(1);
            this.reloadComPortsBTN.Name = "reloadComPortsBTN";
            this.reloadComPortsBTN.Size = new System.Drawing.Size(25, 25);
            this.reloadComPortsBTN.TabIndex = 5;
            this.reloadComPortsBTN.Text = "R";
            this.reloadComPortsBTN.Click += new System.EventHandler(this.reloadComPortsBTN_Click);
            // 
            // disconnectFromTheBootloaderBTN
            // 
            this.disconnectFromTheBootloaderBTN.Enabled = false;
            this.disconnectFromTheBootloaderBTN.Font = new System.Drawing.Font("Arial", 9.5F);
            this.disconnectFromTheBootloaderBTN.Location = new System.Drawing.Point(169, 78);
            this.disconnectFromTheBootloaderBTN.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.disconnectFromTheBootloaderBTN.Name = "disconnectFromTheBootloaderBTN";
            this.disconnectFromTheBootloaderBTN.Size = new System.Drawing.Size(150, 32);
            this.disconnectFromTheBootloaderBTN.TabIndex = 5;
            this.disconnectFromTheBootloaderBTN.Text = "Disconnect BL";
            this.disconnectFromTheBootloaderBTN.Click += new System.EventHandler(this.disconnectFromTheBootloaderBTN_Click);
            // 
            // readTestCardUIDBTN
            // 
            this.readTestCardUIDBTN.Enabled = false;
            this.readTestCardUIDBTN.Font = new System.Drawing.Font("Arial", 9.5F);
            this.readTestCardUIDBTN.Location = new System.Drawing.Point(169, 40);
            this.readTestCardUIDBTN.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.readTestCardUIDBTN.Name = "readTestCardUIDBTN";
            this.readTestCardUIDBTN.Size = new System.Drawing.Size(150, 32);
            this.readTestCardUIDBTN.TabIndex = 5;
            this.readTestCardUIDBTN.Text = "Read Card UID";
            this.readTestCardUIDBTN.Click += new System.EventHandler(this.readTestCardUIDBTN_Click);
            // 
            // readerStatusLabel
            // 
            this.readerStatusLabel.AccessibleName = "readerStatusLabel";
            this.readerStatusLabel.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.readerStatusLabel.Location = new System.Drawing.Point(13, 740);
            this.readerStatusLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.readerStatusLabel.Name = "readerStatusLabel";
            this.readerStatusLabel.Size = new System.Drawing.Size(329, 34);
            this.readerStatusLabel.TabIndex = 3;
            this.readerStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(675, 781);
            this.Controls.Add(this.consoleTextBox);
            this.Controls.Add(this.programInfoLabel);
            this.Controls.Add(this.lblUpdateStatus);
            this.Controls.Add(this.readerLabel);
            this.Controls.Add(this.deviceUIDTextBox);
            this.Controls.Add(this.consoleLabel);
            this.Controls.Add(this.clearConsoleBTN);
            this.Controls.Add(this.comboBoxPorts);
            this.Controls.Add(this.connectToTheReaderBTN);
            this.Controls.Add(this.forceTheBootloaderBTN);
            this.Controls.Add(this.reloadComPortsBTN);
            this.Controls.Add(this.readTestCardUIDBTN);
            this.Controls.Add(this.disconnectFromTheBootloaderBTN);
            this.Controls.Add(this.readerStatusLabel);
            this.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.Name = "Form1";
            this.Text = "GeminiFWUpdater";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}
