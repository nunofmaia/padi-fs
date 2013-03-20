namespace padiFS
{
    partial class Form1
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.launchButton = new System.Windows.Forms.Button();
            this.serversComboBox = new System.Windows.Forms.ComboBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.executeButton = new System.Windows.Forms.Button();
            this.stopOpComboBox = new System.Windows.Forms.ComboBox();
            this.stopProcessTextBox = new System.Windows.Forms.TextBox();
            this.stopProcessLabel = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.createClientTextBox = new System.Windows.Forms.TextBox();
            this.createClientLabel = new System.Windows.Forms.Label();
            this.createButton = new System.Windows.Forms.Button();
            this.wQuorumTextBox = new System.Windows.Forms.TextBox();
            this.wQuorumLabel = new System.Windows.Forms.Label();
            this.rQuorumTextBox = new System.Windows.Forms.TextBox();
            this.rQuorumLabel = new System.Windows.Forms.Label();
            this.serversNumberTextBox = new System.Windows.Forms.TextBox();
            this.serversNumberLabel = new System.Windows.Forms.Label();
            this.createNameTextBox = new System.Windows.Forms.TextBox();
            this.createNameLabel = new System.Windows.Forms.Label();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.button2 = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.launchButton);
            this.groupBox1.Controls.Add(this.serversComboBox);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(381, 54);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Launch server";
            // 
            // launchButton
            // 
            this.launchButton.Location = new System.Drawing.Point(135, 20);
            this.launchButton.Name = "launchButton";
            this.launchButton.Size = new System.Drawing.Size(75, 23);
            this.launchButton.TabIndex = 1;
            this.launchButton.Text = "Launch";
            this.launchButton.UseVisualStyleBackColor = true;
            this.launchButton.Click += new System.EventHandler(this.launchButton_Click);
            // 
            // serversComboBox
            // 
            this.serversComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.serversComboBox.FormattingEnabled = true;
            this.serversComboBox.Location = new System.Drawing.Point(7, 20);
            this.serversComboBox.Name = "serversComboBox";
            this.serversComboBox.Size = new System.Drawing.Size(121, 21);
            this.serversComboBox.TabIndex = 0;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.executeButton);
            this.groupBox2.Controls.Add(this.stopOpComboBox);
            this.groupBox2.Controls.Add(this.stopProcessTextBox);
            this.groupBox2.Controls.Add(this.stopProcessLabel);
            this.groupBox2.Location = new System.Drawing.Point(399, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(382, 54);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Stop operations";
            // 
            // executeButton
            // 
            this.executeButton.Location = new System.Drawing.Point(297, 20);
            this.executeButton.Name = "executeButton";
            this.executeButton.Size = new System.Drawing.Size(75, 23);
            this.executeButton.TabIndex = 3;
            this.executeButton.Text = "Execute";
            this.executeButton.UseVisualStyleBackColor = true;
            // 
            // stopOpComboBox
            // 
            this.stopOpComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.stopOpComboBox.FormattingEnabled = true;
            this.stopOpComboBox.Location = new System.Drawing.Point(169, 20);
            this.stopOpComboBox.Name = "stopOpComboBox";
            this.stopOpComboBox.Size = new System.Drawing.Size(121, 21);
            this.stopOpComboBox.TabIndex = 2;
            // 
            // stopProcessTextBox
            // 
            this.stopProcessTextBox.Location = new System.Drawing.Point(62, 20);
            this.stopProcessTextBox.Name = "stopProcessTextBox";
            this.stopProcessTextBox.Size = new System.Drawing.Size(100, 20);
            this.stopProcessTextBox.TabIndex = 1;
            // 
            // stopProcessLabel
            // 
            this.stopProcessLabel.AutoSize = true;
            this.stopProcessLabel.Location = new System.Drawing.Point(7, 20);
            this.stopProcessLabel.Name = "stopProcessLabel";
            this.stopProcessLabel.Size = new System.Drawing.Size(48, 13);
            this.stopProcessLabel.TabIndex = 0;
            this.stopProcessLabel.Text = "Process:";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.createClientTextBox);
            this.groupBox3.Controls.Add(this.createClientLabel);
            this.groupBox3.Controls.Add(this.createButton);
            this.groupBox3.Controls.Add(this.wQuorumTextBox);
            this.groupBox3.Controls.Add(this.wQuorumLabel);
            this.groupBox3.Controls.Add(this.rQuorumTextBox);
            this.groupBox3.Controls.Add(this.rQuorumLabel);
            this.groupBox3.Controls.Add(this.serversNumberTextBox);
            this.groupBox3.Controls.Add(this.serversNumberLabel);
            this.groupBox3.Controls.Add(this.createNameTextBox);
            this.groupBox3.Controls.Add(this.createNameLabel);
            this.groupBox3.Location = new System.Drawing.Point(12, 72);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(381, 140);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Create file";
            // 
            // createClientTextBox
            // 
            this.createClientTextBox.Location = new System.Drawing.Point(72, 25);
            this.createClientTextBox.Name = "createClientTextBox";
            this.createClientTextBox.Size = new System.Drawing.Size(100, 20);
            this.createClientTextBox.TabIndex = 10;
            // 
            // createClientLabel
            // 
            this.createClientLabel.AutoSize = true;
            this.createClientLabel.Location = new System.Drawing.Point(31, 28);
            this.createClientLabel.Name = "createClientLabel";
            this.createClientLabel.Size = new System.Drawing.Size(36, 13);
            this.createClientLabel.TabIndex = 9;
            this.createClientLabel.Text = "Client:";
            // 
            // createButton
            // 
            this.createButton.Location = new System.Drawing.Point(295, 106);
            this.createButton.Name = "createButton";
            this.createButton.Size = new System.Drawing.Size(75, 23);
            this.createButton.TabIndex = 8;
            this.createButton.Text = "Create";
            this.createButton.UseVisualStyleBackColor = true;
            this.createButton.Click += new System.EventHandler(this.createButton_Click);
            // 
            // wQuorumTextBox
            // 
            this.wQuorumTextBox.Location = new System.Drawing.Point(270, 80);
            this.wQuorumTextBox.Name = "wQuorumTextBox";
            this.wQuorumTextBox.Size = new System.Drawing.Size(100, 20);
            this.wQuorumTextBox.TabIndex = 7;
            // 
            // wQuorumLabel
            // 
            this.wQuorumLabel.AutoSize = true;
            this.wQuorumLabel.Location = new System.Drawing.Point(203, 83);
            this.wQuorumLabel.Name = "wQuorumLabel";
            this.wQuorumLabel.Size = new System.Drawing.Size(61, 13);
            this.wQuorumLabel.TabIndex = 6;
            this.wQuorumLabel.Text = "W Quorum:";
            // 
            // rQuorumTextBox
            // 
            this.rQuorumTextBox.Location = new System.Drawing.Point(72, 80);
            this.rQuorumTextBox.Name = "rQuorumTextBox";
            this.rQuorumTextBox.Size = new System.Drawing.Size(100, 20);
            this.rQuorumTextBox.TabIndex = 5;
            // 
            // rQuorumLabel
            // 
            this.rQuorumLabel.AutoSize = true;
            this.rQuorumLabel.Location = new System.Drawing.Point(8, 83);
            this.rQuorumLabel.Name = "rQuorumLabel";
            this.rQuorumLabel.Size = new System.Drawing.Size(58, 13);
            this.rQuorumLabel.TabIndex = 4;
            this.rQuorumLabel.Text = "R Quorum:";
            // 
            // serversNumberTextBox
            // 
            this.serversNumberTextBox.Location = new System.Drawing.Point(270, 52);
            this.serversNumberTextBox.Name = "serversNumberTextBox";
            this.serversNumberTextBox.Size = new System.Drawing.Size(100, 20);
            this.serversNumberTextBox.TabIndex = 3;
            // 
            // serversNumberLabel
            // 
            this.serversNumberLabel.AutoSize = true;
            this.serversNumberLabel.Location = new System.Drawing.Point(208, 55);
            this.serversNumberLabel.Name = "serversNumberLabel";
            this.serversNumberLabel.Size = new System.Drawing.Size(56, 13);
            this.serversNumberLabel.TabIndex = 2;
            this.serversNumberLabel.Text = "# Servers:";
            // 
            // createNameTextBox
            // 
            this.createNameTextBox.Location = new System.Drawing.Point(72, 52);
            this.createNameTextBox.Name = "createNameTextBox";
            this.createNameTextBox.Size = new System.Drawing.Size(100, 20);
            this.createNameTextBox.TabIndex = 1;
            // 
            // createNameLabel
            // 
            this.createNameLabel.AutoSize = true;
            this.createNameLabel.Location = new System.Drawing.Point(28, 55);
            this.createNameLabel.Name = "createNameLabel";
            this.createNameLabel.Size = new System.Drawing.Size(38, 13);
            this.createNameLabel.TabIndex = 0;
            this.createNameLabel.Text = "Name:";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.button1);
            this.groupBox4.Controls.Add(this.textBox1);
            this.groupBox4.Controls.Add(this.label1);
            this.groupBox4.Location = new System.Drawing.Point(400, 73);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(381, 67);
            this.groupBox4.TabIndex = 3;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Open file";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 27);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(26, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "File:";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(41, 24);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(100, 20);
            this.textBox1.TabIndex = 1;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(147, 22);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 2;
            this.button1.Text = "Open";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.button2);
            this.groupBox5.Controls.Add(this.textBox2);
            this.groupBox5.Controls.Add(this.label2);
            this.groupBox5.Location = new System.Drawing.Point(400, 147);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(381, 65);
            this.groupBox5.TabIndex = 4;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Close file";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 31);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(23, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "File";
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(38, 28);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(100, 20);
            this.textBox2.TabIndex = 1;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(147, 26);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 2;
            this.button2.Text = "Close";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(793, 223);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "Form1";
            this.Text = "Iurie Master";
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button launchButton;
        private System.Windows.Forms.ComboBox serversComboBox;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button executeButton;
        private System.Windows.Forms.ComboBox stopOpComboBox;
        private System.Windows.Forms.TextBox stopProcessTextBox;
        private System.Windows.Forms.Label stopProcessLabel;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label rQuorumLabel;
        private System.Windows.Forms.TextBox serversNumberTextBox;
        private System.Windows.Forms.Label serversNumberLabel;
        private System.Windows.Forms.TextBox createNameTextBox;
        private System.Windows.Forms.Label createNameLabel;
        private System.Windows.Forms.Button createButton;
        private System.Windows.Forms.TextBox wQuorumTextBox;
        private System.Windows.Forms.Label wQuorumLabel;
        private System.Windows.Forms.TextBox rQuorumTextBox;
        private System.Windows.Forms.TextBox createClientTextBox;
        private System.Windows.Forms.Label createClientLabel;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label label2;

    }
}

