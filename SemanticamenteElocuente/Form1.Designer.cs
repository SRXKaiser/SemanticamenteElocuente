namespace SemanticamenteElocuente
{
    partial class Form1
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
            btnAbrir = new Button();
            btnAnalizar = new Button();
            openFileDialog1 = new OpenFileDialog();
            rtbSalida = new RichTextBox();
            txtRuta = new TextBox();
            rtbCodigo = new RichTextBox();
            SuspendLayout();
            // 
            // btnAbrir
            // 
            btnAbrir.Location = new Point(274, 55);
            btnAbrir.Name = "btnAbrir";
            btnAbrir.Size = new Size(94, 29);
            btnAbrir.TabIndex = 0;
            btnAbrir.Text = "Abrir";
            btnAbrir.UseVisualStyleBackColor = true;
            btnAbrir.Click += btnAbrir_Click;
            // 
            // btnAnalizar
            // 
            btnAnalizar.Location = new Point(374, 55);
            btnAnalizar.Name = "btnAnalizar";
            btnAnalizar.Size = new Size(94, 29);
            btnAnalizar.TabIndex = 1;
            btnAnalizar.Text = "Analizar";
            btnAnalizar.UseVisualStyleBackColor = true;
            btnAnalizar.Click += btnAnalizar_Click;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // rtbSalida
            // 
            rtbSalida.Location = new Point(52, 156);
            rtbSalida.Name = "rtbSalida";
            rtbSalida.Size = new Size(416, 340);
            rtbSalida.TabIndex = 2;
            rtbSalida.Text = "";
            // 
            // txtRuta
            // 
            txtRuta.Location = new Point(52, 55);
            txtRuta.Name = "txtRuta";
            txtRuta.Size = new Size(216, 27);
            txtRuta.TabIndex = 3;
            // 
            // rtbCodigo
            // 
            rtbCodigo.Location = new Point(515, 156);
            rtbCodigo.Name = "rtbCodigo";
            rtbCodigo.Size = new Size(436, 340);
            rtbCodigo.TabIndex = 4;
            rtbCodigo.Text = "";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1006, 566);
            Controls.Add(rtbCodigo);
            Controls.Add(txtRuta);
            Controls.Add(rtbSalida);
            Controls.Add(btnAnalizar);
            Controls.Add(btnAbrir);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnAbrir;
        private Button btnAnalizar;
        private OpenFileDialog openFileDialog1;
        private RichTextBox rtbSalida;
        private TextBox txtRuta;
        private RichTextBox rtbCodigo;
    }
}
