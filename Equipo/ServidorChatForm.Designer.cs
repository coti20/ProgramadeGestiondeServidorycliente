
namespace Equipo
{
    partial class ServidorChatForm
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
            this.lblipservidor = new System.Windows.Forms.Label();
            this.mostrarTextbox = new System.Windows.Forms.RichTextBox();
            this.btnEnviarArchivo = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.clientesListBox = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.chkRecibosLectura = new System.Windows.Forms.CheckBox();
            this.btnBloquearCliente = new System.Windows.Forms.Button();
            this.btnInvitarTateti = new System.Windows.Forms.Button();
            this.btnLlamar = new System.Windows.Forms.Button();
            this.btnColgar = new System.Windows.Forms.Button();
            this.btnEnviarArchivoPorCliente = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // entradaTextbox
            // 
            this.entradaTextbox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.entradaTextbox.Location = new System.Drawing.Point(12, 386);
            this.entradaTextbox.Name = "entradaTextbox";
            this.entradaTextbox.Size = new System.Drawing.Size(596, 23);
            this.entradaTextbox.TabIndex = 2;
            this.entradaTextbox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.entradaTextBox_KeyDown);
            // 
            // lblipservidor
            // 
            this.lblipservidor.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.lblipservidor.AutoSize = true;
            this.lblipservidor.Location = new System.Drawing.Point(349, 9);
            this.lblipservidor.Name = "lblipservidor";
            this.lblipservidor.Size = new System.Drawing.Size(72, 15);
            this.lblipservidor.TabIndex = 5;
            this.lblipservidor.Text = "lblipservidor";
            // 
            // mostrarTextbox
            // 
            this.mostrarTextbox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mostrarTextbox.Location = new System.Drawing.Point(12, 27);
            this.mostrarTextbox.Name = "mostrarTextbox";
            this.mostrarTextbox.ReadOnly = true;
            this.mostrarTextbox.Size = new System.Drawing.Size(596, 340);
            this.mostrarTextbox.TabIndex = 6;
            this.mostrarTextbox.Text = "";
            // 
            // btnEnviarArchivo
            // 
            this.btnEnviarArchivo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnEnviarArchivo.Location = new System.Drawing.Point(614, 271);
            this.btnEnviarArchivo.Name = "btnEnviarArchivo";
            this.btnEnviarArchivo.Size = new System.Drawing.Size(178, 23);
            this.btnEnviarArchivo.TabIndex = 10;
            this.btnEnviarArchivo.Text = "Enviar Archivo a todos";
            this.btnEnviarArchivo.UseVisualStyleBackColor = true;
            this.btnEnviarArchivo.Click += new System.EventHandler(this.btnEnviarArchivo_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // clientesListBox
            // 
            this.clientesListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.clientesListBox.FormattingEnabled = true;
            this.clientesListBox.ItemHeight = 15;
            this.clientesListBox.Location = new System.Drawing.Point(614, 33);
            this.clientesListBox.Name = "clientesListBox";
            this.clientesListBox.Size = new System.Drawing.Size(174, 154);
            this.clientesListBox.TabIndex = 11;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(660, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(84, 15);
            this.label1.TabIndex = 12;
            this.label1.Text = "Lista Cliente IP";
            // 
            // chkRecibosLectura
            // 
            this.chkRecibosLectura.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkRecibosLectura.AutoSize = true;
            this.chkRecibosLectura.Location = new System.Drawing.Point(753, 5);
            this.chkRecibosLectura.Name = "chkRecibosLectura";
            this.chkRecibosLectura.Size = new System.Drawing.Size(35, 19);
            this.chkRecibosLectura.TabIndex = 13;
            this.chkRecibosLectura.Text = "✓";
            this.chkRecibosLectura.UseVisualStyleBackColor = true;
            // 
            // btnBloquearCliente
            // 
            this.btnBloquearCliente.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBloquearCliente.Location = new System.Drawing.Point(713, 193);
            this.btnBloquearCliente.Name = "btnBloquearCliente";
            this.btnBloquearCliente.Size = new System.Drawing.Size(75, 23);
            this.btnBloquearCliente.TabIndex = 14;
            this.btnBloquearCliente.Text = "Bloquear";
            this.btnBloquearCliente.UseVisualStyleBackColor = true;
            this.btnBloquearCliente.Click += new System.EventHandler(this.btnBloquearCliente_Click);
            // 
            // btnInvitarTateti
            // 
            this.btnInvitarTateti.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnInvitarTateti.Location = new System.Drawing.Point(614, 222);
            this.btnInvitarTateti.Name = "btnInvitarTateti";
            this.btnInvitarTateti.Size = new System.Drawing.Size(174, 23);
            this.btnInvitarTateti.TabIndex = 15;
            this.btnInvitarTateti.Text = "Jugar TaTeTi";
            this.btnInvitarTateti.UseVisualStyleBackColor = true;
            this.btnInvitarTateti.Click += new System.EventHandler(this.btnInvitarJuego_Click_1);
            // 
            // btnLlamar
            // 
            this.btnLlamar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLlamar.Location = new System.Drawing.Point(614, 385);
            this.btnLlamar.Name = "btnLlamar";
            this.btnLlamar.Size = new System.Drawing.Size(40, 23);
            this.btnLlamar.TabIndex = 16;
            this.btnLlamar.Text = "🟩📞";
            this.btnLlamar.UseVisualStyleBackColor = true;
            this.btnLlamar.Click += new System.EventHandler(this.BtnLlamar_Click);
            // 
            // btnColgar
            // 
            this.btnColgar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnColgar.Location = new System.Drawing.Point(660, 385);
            this.btnColgar.Name = "btnColgar";
            this.btnColgar.Size = new System.Drawing.Size(43, 23);
            this.btnColgar.TabIndex = 17;
            this.btnColgar.Text = "❌📞";
            this.btnColgar.UseVisualStyleBackColor = true;
            this.btnColgar.Click += new System.EventHandler(this.BtnColgar_Click);
            // 
            // btnEnviarArchivoPorCliente
            // 
            this.btnEnviarArchivoPorCliente.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnEnviarArchivoPorCliente.Location = new System.Drawing.Point(614, 316);
            this.btnEnviarArchivoPorCliente.Name = "btnEnviarArchivoPorCliente";
            this.btnEnviarArchivoPorCliente.Size = new System.Drawing.Size(173, 23);
            this.btnEnviarArchivoPorCliente.TabIndex = 18;
            this.btnEnviarArchivoPorCliente.Text = "Enviar Archivo a un Cliente";
            this.btnEnviarArchivoPorCliente.UseVisualStyleBackColor = true;
            this.btnEnviarArchivoPorCliente.Click += new System.EventHandler(this.btnEnviarArchivoPorCliente_Click);
            // 
            // ServidorChatForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.btnEnviarArchivoPorCliente);
            this.Controls.Add(this.btnColgar);
            this.Controls.Add(this.btnLlamar);
            this.Controls.Add(this.btnInvitarTateti);
            this.Controls.Add(this.btnBloquearCliente);
            this.Controls.Add(this.chkRecibosLectura);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.clientesListBox);
            this.Controls.Add(this.btnEnviarArchivo);
            this.Controls.Add(this.mostrarTextbox);
            this.Controls.Add(this.lblipservidor);
            this.Controls.Add(this.entradaTextbox);
            this.Name = "ServidorChatForm";
            this.Text = "ServidorChatForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ServidorChatForm_FormClosing);
            this.Load += new System.EventHandler(this.ServidorChatForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TextBox entradaTextbox;
        private System.Windows.Forms.Label lblipservidor;
        private System.Windows.Forms.RichTextBox mostrarTextbox;
        private System.Windows.Forms.Button btnEnviarArchivo;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.ListBox clientesListBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox chkRecibosLectura;
        private System.Windows.Forms.Button btnBloquearCliente;
        private System.Windows.Forms.Button btnInvitarTateti;
        private System.Windows.Forms.Button btnLlamar;
        private System.Windows.Forms.Button btnColgar;
        private System.Windows.Forms.Button btnEnviarArchivoPorCliente;
    }
}
