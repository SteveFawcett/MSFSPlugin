using MSFSPlugin.Controls;

namespace MSFSPlugin.Forms
{
    partial class DisplayLogging
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            MsgTxtBox = new LogPanel();
            SuspendLayout();
            // 
            // MsgTxtBox
            // 
            MsgTxtBox.BackColor = Color.White;
            MsgTxtBox.BorderStyle = BorderStyle.None;
            MsgTxtBox.Enabled = false;
            MsgTxtBox.Location = new Point(9, 10);
            MsgTxtBox.Name = "MsgTxtBox";
            MsgTxtBox.Size = new Size(781, 387);
            MsgTxtBox.TabIndex = 0;
            // 
            // DisplayLogging
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(MsgTxtBox);
            Name = "DisplayLogging";
            Size = new Size(800, 450);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private LogPanel MsgTxtBox;
    }
}