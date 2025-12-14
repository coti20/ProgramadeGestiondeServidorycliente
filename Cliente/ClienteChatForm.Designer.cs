
namespace Cliente
{
    partial class Cliente
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.entradaTextbox = new System.Windows.Forms.TextBox();
            this.lblipservidor = new System.Windows.Forms.TextBox();
            this.btbConectar = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.mostrarTextbox = new System.Windows.Forms.RichTextBox();
            this.btnEnviarArchivo = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.chkRecibosLectura = new System.Windows.Forms.CheckBox();
            this.btnDesconectar = new System.Windows.Forms.Button();
            this.btnLlamar = new System.Windows.Forms.Button();
            this.btnColgar = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // entradaTextbox
            // 
            this.entradaTextbox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.entradaTextbox.Location = new System.Drawing.Point(16, 376);
            this.entradaTextbox.Name = "entradaTextbox";
            this.entradaTextbox.Size = new System.Drawing.Size(573, 23);
            this.entradaTextbox.TabIndex = 4;
            this.entradaTextbox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.entradaTextBox_KeyDown);
            // 
            // lblipservidor
            // 
            this.lblipservidor.Location = new System.Drawing.Point(22, 27);
            this.lblipservidor.Name = "lblipservidor";
            this.lblipservidor.Size = new System.Drawing.Size(143, 23);
            this.lblipservidor.TabIndex = 5;
            // 
            // btbConectar
            // 
            this.btbConectar.Location = new System.Drawing.Point(171, 26);
            this.btbConectar.Name = "btbConectar";
            this.btbConectar.Size = new System.Drawing.Size(75, 23);
            this.btbConectar.TabIndex = 6;
            this.btbConectar.Text = "Conectar";
            this.btbConectar.UseVisualStyleBackColor = true;
            this.btbConectar.Click += new System.EventHandler(this.btbConectar_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(38, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(84, 15);
            this.label1.TabIndex = 7;
            this.label1.Text = "Introducir la ip";
            // 
            // mostrarTextbox
            // 
            this.mostrarTextbox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mostrarTextbox.Location = new System.Drawing.Point(16, 56);
            this.mostrarTextbox.Name = "mostrarTextbox";
            this.mostrarTextbox.ReadOnly = true;
            this.mostrarTextbox.Size = new System.Drawing.Size(772, 314);
            this.mostrarTextbox.TabIndex = 8;
            this.mostrarTextbox.Text = "";
            // 
            // btnEnviarArchivo
            // 
            this.btnEnviarArchivo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnEnviarArchivo.Location = new System.Drawing.Point(16, 415);
            this.btnEnviarArchivo.Name = "btnEnviarArchivo";
            this.btnEnviarArchivo.Size = new System.Drawing.Size(178, 23);
            this.btnEnviarArchivo.TabIndex = 9;
            this.btnEnviarArchivo.Text = "Enviar Archivo";
            this.btnEnviarArchivo.UseVisualStyleBackColor = true;
            this.btnEnviarArchivo.Click += new System.EventHandler(this.btnEnviarArchivo_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // chkRecibosLectura
            // 
            this.chkRecibosLectura.AutoSize = true;
            this.chkRecibosLectura.Location = new System.Drawing.Point(753, 8);
            this.chkRecibosLectura.Name = "chkRecibosLectura";
            this.chkRecibosLectura.Size = new System.Drawing.Size(35, 19);
            this.chkRecibosLectura.TabIndex = 10;
            this.chkRecibosLectura.Text = "✓";
            this.chkRecibosLectura.UseVisualStyleBackColor = true;
            // 
            // btnDesconectar
            // 
            this.btnDesconectar.Location = new System.Drawing.Point(252, 26);
            this.btnDesconectar.Name = "btnDesconectar";
            this.btnDesconectar.Size = new System.Drawing.Size(81, 23);
            this.btnDesconectar.TabIndex = 11;
            this.btnDesconectar.Text = "Desconectar";
            this.btnDesconectar.UseVisualStyleBackColor = true;
            this.btnDesconectar.Click += new System.EventHandler(this.btnDesconectar_Click);
            // 
            // btnLlamar
            // 
            this.btnLlamar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLlamar.Location = new System.Drawing.Point(605, 376);
            this.btnLlamar.Name = "btnLlamar";
            this.btnLlamar.Size = new System.Drawing.Size(75, 23);
            this.btnLlamar.TabIndex = 17;
            this.btnLlamar.Text = "🟩📞";
            this.btnLlamar.UseVisualStyleBackColor = true;
            this.btnLlamar.Click += new System.EventHandler(this.btnLlamar_Click);
            // 
            // btnColgar
            // 
            this.btnColgar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnColgar.Location = new System.Drawing.Point(686, 376);
            this.btnColgar.Name = "btnColgar";
            this.btnColgar.Size = new System.Drawing.Size(75, 23);
            this.btnColgar.TabIndex = 18;
            this.btnColgar.Text = "❌📞";
            this.btnColgar.UseVisualStyleBackColor = true;
            this.btnColgar.Click += new System.EventHandler(this.btnColgar_Click);
            // 
            // Cliente
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.btnColgar);
            this.Controls.Add(this.btnLlamar);
            this.Controls.Add(this.btnDesconectar);
            this.Controls.Add(this.chkRecibosLectura);
            this.Controls.Add(this.btnEnviarArchivo);
            this.Controls.Add(this.mostrarTextbox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btbConectar);
            this.Controls.Add(this.lblipservidor);
            this.Controls.Add(this.entradaTextbox);
            this.Name = "Cliente";
            this.Text = "Cliente";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Cliente_fromClosing);
            this.Load += new System.EventHandler(this.Cliente_load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox entradaTextbox;
        private System.Windows.Forms.TextBox lblipservidor;
        private System.Windows.Forms.Button btbConectar;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RichTextBox mostrarTextbox;
        private System.Windows.Forms.Button btnEnviarArchivo;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.CheckBox chkRecibosLectura;
        private System.Windows.Forms.Button btnDesconectar;
        private System.Windows.Forms.Button btnLlamar;
        private System.Windows.Forms.Button btnColgar;
    }
}
