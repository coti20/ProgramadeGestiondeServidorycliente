using System;
using System.Windows.Forms;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using NAudio.Wave;

namespace Cliente
{
    public partial class Cliente : Form
    {
        private NetworkStream salida; // Flujo de red para comunicación
        private BinaryReader lector; // Lector binario para recibir datos
        private BinaryWriter escritor; // Escritor binario para enviar datos
        private Thread lecturathread; // Hilo para leer mensajes
        private string mensaje = ""; // Mensaje recibido
        private List<string> mensajesPendientes; // Mensajes pendientes de confirmación
        private TicTacToeForm gameForm; // Formulario de juego
        private bool conectado; // Estado de conexión
        private bool desconectarSolicitado; // Indica si se solicitó desconexión
        private bool llamadaActiva = false; // Indica si hay llamada activa
        private WaveInEvent waveInCliente; // Captura de audio
        private WaveOutEvent waveOutCliente; // Reproducción de audio
        private BufferedWaveProvider bufferCliente; // Buffer para audio
        private Button btnColgarCliente; // Botón para colgar llamada

        public Cliente()
        {
            InitializeComponent(); // Inicializa componentes de la UI
            mensajesPendientes = new List<string>(); // Inicializa lista de mensajes pendientes
            mostrarTextbox.ReadOnly = true; // Configura textbox como solo lectura
            conectado = false; // Marca como no conectado
            desconectarSolicitado = false; // Inicializa estado de desconexión
            this.Activated += Cliente_Activated; // Asocia evento de activación
            this.Resize += Cliente_Resize; // Asocia evento de cambio de tamaño
        }

        private void Cliente_Activated(object sender, EventArgs e)
        {
            if (chkRecibosLectura.Checked) // Verifica si los recibos de lectura están activados
            {
                foreach (var mensaje in mensajesPendientes.ToArray()) // Procesa mensajes pendientes
                {
                    try
                    {
                        if (escritor != null && !mensaje.Contains("[DEBUG]")) // Verifica escritor y mensaje
                        {
                            escritor.Write($"CONFIRMACION_LEIDO>>>{mensaje}"); // Envía confirmación de lectura
                            mensajesPendientes.Remove(mensaje); // Elimina mensaje de pendientes
                        }
                    }
                    catch (Exception) { } // Maneja errores silenciosamente
                }
            }
        }

        private void Cliente_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Maximized && chkRecibosLectura.Checked) // Verifica si ventana está maximizada
            {
                foreach (var mensaje in mensajesPendientes.ToArray()) // Procesa mensajes pendientes
                {
                    try
                    {
                        if (escritor != null && !mensaje.Contains("[DEBUG]")) // Verifica escritor y mensaje
                        {
                            escritor.Write($"CONFIRMACION_LEIDO>>>{mensaje}"); // Envía confirmación de lectura
                            mensajesPendientes.Remove(mensaje); // Elimina mensaje de pendientes
                        }
                    }
                    catch (Exception) { } // Maneja errores silenciosamente
                }
            }
        }

        private void Cliente_load(object sender, EventArgs e)
        {
            lecturathread = new Thread(new ThreadStart(EjecutarCliente)); // Crea hilo para cliente
            lecturathread.Start(); // Inicia hilo
            entradaTextbox.ReadOnly = true; // Desactiva entrada de texto
            btnEnviarArchivo.Enabled = false; // Desactiva botón de enviar archivo
            chkRecibosLectura.Checked = true; // Activa recibos de lectura por defecto
            InicializarControlesLlamadaCliente(); // Inicializa controles de llamada
        }

        private void Cliente_fromClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                desconectarSolicitado = true; // Marca desconexión solicitada
                if (conectado) // Si está conectado
                {
                    escritor?.Close(); // Cierra escritor
                    lector?.Close(); // Cierra lector
                    salida?.Close(); // Cierra flujo
                }
                if (gameForm != null) // Si hay juego activo
                {
                    gameForm.Close(); // Cierra juego
                }
                TerminarLlamadaCliente(); // Termina llamada activa
            }
            catch { } // Maneja errores silenciosamente
            System.Environment.Exit(System.Environment.ExitCode); // Cierra la aplicación
        }

        private delegate void DisplayDelegate(string message); // Delegado para mostrar mensajes
        private delegate void DisableInputDelegate(bool value); // Delegado para habilitar/deshabilitar entrada
        private delegate void ActualizarEstadoMensajeDelegate(string mensajeOriginal, string nuevoEstado); // Delegado para actualizar estado de mensajes

        private void MostrarMensaje(string mensaje)
        {
            if (mostrarTextbox.InvokeRequired) // Verifica si se necesita invocar en hilo UI
            {
                Invoke(new DisplayDelegate(MostrarMensaje), new object[] { mensaje }); // Invoca en hilo UI
            }
            else
            {
                mostrarTextbox.Text += mensaje; // Añade mensaje al textbox
                mostrarTextbox.SelectionStart = mostrarTextbox.Text.Length; // Mueve cursor al final
                mostrarTextbox.ScrollToCaret(); // Desplaza vista al final
                if (mensaje.Contains(">>>") && !mensaje.Contains("CLIENTE>>>") && !mensaje.Contains("[DEBUG]") && chkRecibosLectura.Checked &&
                    (WindowState == FormWindowState.Maximized || this == Form.ActiveForm)) // Verifica condiciones para confirmación
                {
                    try
                    {
                        if (escritor != null) // Verifica escritor
                        {
                            escritor.Write($"CONFIRMACION_LEIDO>>>{mensaje.TrimEnd('\r', '\n')}"); // Envía confirmación de lectura
                        }
                    }
                    catch (Exception) { } // Maneja errores silenciosamente
                }
                else if (mensaje.Contains(">>>") && !mensaje.Contains("CLIENTE>>>") && !mensaje.Contains("[DEBUG]") && !mensajesPendientes.Contains(mensaje.TrimEnd('\r', '\n'))) // Si mensaje es válido
                {
                    mensajesPendientes.Add(mensaje.TrimEnd('\r', '\n')); // Añade a mensajes pendientes
                }
            }
        }

        private void ActualizarEstadoMensaje(string mensajeOriginal, string nuevoEstado)
        {
            if (mostrarTextbox.InvokeRequired) // Verifica si se necesita invocar en hilo UI
            {
                Invoke(new ActualizarEstadoMensajeDelegate(ActualizarEstadoMensaje), new object[] { mensajeOriginal, nuevoEstado }); // Invoca en hilo UI
            }
            else
            {
                string mensajeSinEstado = Regex.Replace(mensajeOriginal, @" ✓+$", "").Trim(); // Limpia estado previo
                string pattern = @"\[(\d{2}/\d{2}/\d{4} \d{2}:\d{2}:\d{2})\] (?:CLIENTE|Cliente \d+)>>>(.*)"; // Patrón para extraer timestamp
                Match match = Regex.Match(mensajeSinEstado, pattern); // Busca coincidencia
                string mensajeContenido = mensajeSinEstado; // Contenido del mensaje
                string timestamp = ""; // Timestamp
                if (match.Success) // Si hay coincidencia
                {
                    timestamp = match.Groups[1].Value; // Asigna timestamp
                    mensajeContenido = match.Groups[2].Value.Trim(); // Asigna contenido
                }

                string mensajeLocal = $"[{timestamp}] CLIENTE>>> {mensajeContenido}"; // Formatea mensaje
                string mensajeConEstado = $"{mensajeLocal} {nuevoEstado}"; // Añade estado

                int index = mostrarTextbox.Text.IndexOf(mensajeLocal); // Busca mensaje
                if (index == -1)
                {
                    mensajeLocal = Regex.Replace(mensajeLocal, @" ✓+$", "").Trim(); // Limpia estado
                    index = mostrarTextbox.Text.IndexOf(mensajeLocal); // Busca sin estado
                }

                if (index != -1) // Si encuentra mensaje
                {
                    mostrarTextbox.Text = mostrarTextbox.Text.Remove(index, mensajeLocal.Length).Insert(index, mensajeConEstado); // Actualiza mensaje
                    mostrarTextbox.SelectionStart = mostrarTextbox.Text.Length; // Mueve cursor al final
                    mostrarTextbox.ScrollToCaret(); // Desplaza vista al final
                }
            }
        }

        private void DeshabilitarSalida(bool valor)
        {
            if (entradaTextbox.InvokeRequired) // Verifica si se necesita invocar en hilo UI
            {
                Invoke(new DisableInputDelegate(DeshabilitarSalida), new object[] { valor }); // Invoca en hilo UI
            }
            else
            {
                entradaTextbox.ReadOnly = valor; // Habilita/desactiva entrada de texto
                btnEnviarArchivo.Enabled = !valor; // Habilita/desactiva botón de enviar archivo
                btnDesconectar.Enabled = !valor; // Habilita/desactiva botón de desconexión
            }
        }

        private void entradaTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter && !entradaTextbox.ReadOnly && !string.IsNullOrEmpty(entradaTextbox.Text.Trim())) // Verifica Enter y entrada válida
                {
                    string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"); // Obtiene timestamp
                    string mensajeEnviado = $"[{timestamp}] CLIENTE>>> {entradaTextbox.Text.Trim()}"; // Formatea mensaje
                    if (escritor != null) // Verifica escritor
                    {
                        escritor.Write(mensajeEnviado); // Envía mensaje
                    }
                    MostrarMensaje($"{mensajeEnviado}\r\n"); // Muestra mensaje
                    entradaTextbox.Clear(); // Limpia textbox
                }
            }
            catch (Exception) { } // Maneja errores silenciosamente
        }

        private delegate void ManejarRecepcionArchivoDelegate(string fileName, long fileSize); // Delegado para recepción de archivo

        private void ManejarRecepcionArchivo(string fileName, long fileSize)
        {
            string mensaje = $"¿Deseas aceptar el archivo '{fileName}'?"; // Crea mensaje de confirmación
            DialogResult resultado = MessageBox.Show(mensaje, "Archivo Recibido", MessageBoxButtons.YesNo, MessageBoxIcon.Question); // Muestra diálogo

            if (resultado == DialogResult.Yes) // Si acepta archivo
            {
                using (SaveFileDialog saveDialog = new SaveFileDialog()) // Abre diálogo de guardado
                {
                    saveDialog.FileName = fileName; // Establece nombre del archivo
                    if (saveDialog.ShowDialog() == DialogResult.OK) // Si selecciona ruta
                    {
                        string savePath = saveDialog.FileName; // Obtiene ruta
                        try
                        {
                            byte[] fileData = new byte[fileSize]; // Crea buffer
                            int bytesRead = 0; // Contador de bytes
                            while (bytesRead < fileSize) // Lee datos
                            {
                                int currentByte = salida.Read(fileData, bytesRead, (int)fileSize - bytesRead); // Lee bytes
                                if (currentByte == 0) throw new Exception("Conexión cerrada prematuramente"); // Verifica desconexión
                                bytesRead += currentByte; // Actualiza contador
                            }
                            string finConfirm = lector.ReadString(); // Lee mensaje de fin
                            if (finConfirm != "ARCHIVO_FIN>>>") throw new Exception("Error en sincronización de archivo"); // Verifica sincronización
                            File.WriteAllBytes(savePath, fileData); // Guarda archivo
                            string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"); // Obtiene timestamp
                            MostrarMensaje($"\r\n[{timestamp}] CLIENTE>>> Se recibió el archivo '{fileName}'\r\n"); // Muestra mensaje
                            if (escritor != null) // Verifica escritor
                            {
                                escritor.Write($"SERVIDOR>>> Se recibió el archivo\r\n"); // Notifica recepción
                            }
                        }
                        catch (Exception ex)
                        {
                            string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"); // Obtiene timestamp
                            MostrarMensaje($"\r\n[{timestamp}] CLIENTE>>> Error al recibir el archivo '{fileName}': {ex.Message}\r\n"); // Muestra error
                            if (escritor != null) // Verifica escritor
                            {
                                escritor.Write("ARCHIVO_RECHAZADO>>>"); // Notifica rechazo
                            }
                        }
                    }
                    else // Si cancela diálogo
                    {
                        try
                        {
                            byte[] buffer = new byte[1024]; // Crea buffer para descartar
                            long bytesRead = 0; // Contador de bytes
                            while (bytesRead < fileSize) // Descarta datos
                            {
                                int currentRead = salida.Read(buffer, 0, (int)Math.Min(buffer.Length, fileSize - bytesRead)); // Lee bytes
                                if (currentRead == 0) throw new Exception("Conexión cerrada prematuramente"); // Verifica desconexión
                                bytesRead += currentRead; // Actualiza contador
                            }
                            string finConfirm = lector.ReadString(); // Lee mensaje de fin
                            if (finConfirm != "ARCHIVO_FIN>>>") throw new Exception("Error en sincronización de archivo"); // Verifica sincronización
                            string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"); // Obtiene timestamp
                            MostrarMensaje($"\r\n[{timestamp}] CLIENTE>>> El archivo se canceló\r\n"); // Muestra mensaje
                            if (escritor != null) // Verifica escritor
                            {
                                escritor.Write("ARCHIVO_RECHAZADO>>>"); // Notifica rechazo
                            }
                        }
                        catch (Exception)
                        {
                            string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"); // Obtiene timestamp
                            MostrarMensaje($"\r\n[{timestamp}] CLIENTE>>> El archivo se canceló\r\n"); // Muestra mensaje
                            if (escritor != null) // Verifica escritor
                            {
                                escritor.Write("ARCHIVO_RECHAZADO>>>"); // Notifica rechazo
                            }
                        }
                    }
                }
            }
            else // Si rechaza archivo
            {
                try
                {
                    byte[] buffer = new byte[1024]; // Crea buffer para descartar
                    long bytesRead = 0; // Contador de bytes
                    while (bytesRead < fileSize) // Descarta datos
                    {
                        int currentRead = salida.Read(buffer, 0, (int)Math.Min(buffer.Length, fileSize - bytesRead)); // Lee bytes
                        if (currentRead == 0) throw new Exception("Conexión cerrada prematuramente"); // Verifica desconexión
                        bytesRead += currentRead; // Actualiza contador
                    }
                    string finConfirm = lector.ReadString(); // Lee mensaje de fin
                    if (finConfirm != "ARCHIVO_FIN>>>") throw new Exception("Error en sincronización de archivo"); // Verifica sincronización
                    string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"); // Obtiene timestamp
                    MostrarMensaje($"\r\n[{timestamp}] CLIENTE>>> El archivo se canceló\r\n"); // Muestra mensaje
                    if (escritor != null) // Verifica escritor
                    {
                        escritor.Write("ARCHIVO_RECHAZADO>>>"); // Notifica rechazo
                    }
                }
                catch (Exception)
                {
                    string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"); // Obtiene timestamp
                    MostrarMensaje($"\r\n[{timestamp}] CLIENTE>>> El archivo se canceló\r\n"); // Muestra mensaje
                    if (escritor != null) // Verifica escritor
                    {
                        escritor.Write("ARCHIVO_RECHAZADO>>>"); // Notifica rechazo
                    }
                }
            }
        }

        public void EjecutarCliente()
        {
            TcpClient cliente = null; // Cliente TCP
            try
            {
                MostrarMensaje("Tratando de conectar\r\n"); // Muestra mensaje de conexión
                cliente = new TcpClient(); // Crea cliente TCP
                cliente.Connect(lblipservidor.Text, 50000); // Conecta al servidor
                salida = cliente.GetStream(); // Obtiene flujo
                escritor = new BinaryWriter(salida); // Crea escritor
                lector = new BinaryReader(salida); // Crea lector

                conectado = true; // Marca como conectado
                DeshabilitarSalida(false); // Habilita controles
                MostrarMensaje("\r\nSe recibieron flujos de E/S\r\n"); // Muestra mensaje

                do
                {
                    try
                    {
                        if (desconectarSolicitado) // Verifica desconexión solicitada
                        {
                            break;
                        }

                        if (salida.DataAvailable) // Verifica datos disponibles
                        {
                            mensaje = lector.ReadString(); // Lee mensaje
                            if (mensaje.StartsWith("ARCHIVO_INICIO>>>")) // Maneja recepción de archivo
                            {
                                string[] partes = mensaje.Split(new string[] { ">>>" }, StringSplitOptions.None); // Divide mensaje
                                string fileName = partes[1]; // Obtiene nombre
                                long fileSize = long.Parse(partes[2]); // Obtiene tamaño
                                Invoke(new ManejarRecepcionArchivoDelegate(ManejarRecepcionArchivo), new object[] { fileName, fileSize }); // Procesa archivo
                            }
                            else if (mensaje.StartsWith("ARCHIVO_RECHAZADO>>>")) // Maneja rechazo de archivo
                            {
                                string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"); // Obtiene timestamp
                                MostrarMensaje($"\r\n[{timestamp}] CLIENTE>>> El archivo se canceló\r\n"); // Muestra mensaje
                            }
                            else if (mensaje.StartsWith("CONFIRMACION_LEIDO>>>")) // Maneja confirmación de lectura
                            {
                                string mensajeOriginal = mensaje.Substring("CONFIRMACION_LEIDO>>>".Length).Trim(); // Extrae mensaje original
                                ActualizarEstadoMensaje(mensajeOriginal, "✓"); // Actualiza estado
                            }
                            else if (mensaje.StartsWith("INVITACION_JUEGO>>>")) // Maneja invitación de juego
                            {
                                Invoke(new Action(() =>
                                {
                                    if (gameForm != null) // Verifica juego activo
                                    {
                                        MostrarMensaje("Ya hay un juego en curso.\r\n"); // Muestra mensaje
                                        return;
                                    }
                                    DialogResult result = MessageBox.Show("El servidor te invita a jugar Tic-Tac-Toe. ¿Aceptar?", "Invitación a Juego", MessageBoxButtons.YesNo, MessageBoxIcon.Question); // Muestra diálogo
                                    if (result == DialogResult.Yes) // Si acepta
                                    {
                                        escritor.Write("ACEPTAR_JUEGO>>>"); // Envía aceptación
                                        gameForm = new TicTacToeForm(false, escritor); // Crea juego
                                        gameForm.FormClosed += (s, e) => gameForm = null; // Asocia evento de cierre
                                        gameForm.Show(); // Muestra juego
                                        MostrarMensaje("Aceptaste la invitación a jugar.\r\n"); // Muestra mensaje
                                    }
                                    else // Si rechaza
                                    {
                                        escritor.Write("RECHAZAR_JUEGO>>>"); // Envía rechazo
                                        MostrarMensaje("Rechazaste la invitación a jugar.\r\n"); // Muestra mensaje
                                    }
                                }));
                            }
                            else if (mensaje.StartsWith("JUEGO_MOVIMIENTO>>>")) // Maneja movimiento de juego
                            {
                                if (gameForm != null) // Verifica juego activo
                                {
                                    Invoke(new Action(() => gameForm.HandleMove(mensaje))); // Procesa movimiento
                                }
                            }
                            else if (mensaje.StartsWith("INVITACION_LLAMA>>>")) // Maneja invitación de llamada
                            {
                                Invoke(new Action(() =>
                                {
                                    if (llamadaActiva) // Verifica llamada activa
                                    {
                                        MostrarMensaje("Ya hay una llamada en curso.\r\n"); // Muestra mensaje
                                        return;
                                    }
                                    DialogResult result = MessageBox.Show("El servidor te invita a una llamada de voz. ¿Aceptar?", "Invitación a Llamada", MessageBoxButtons.YesNo, MessageBoxIcon.Question); // Muestra diálogo
                                    if (result == DialogResult.Yes) // Si acepta
                                    {
                                        escritor.Write("ACEPTAR_LLAMA>>>"); // Envía aceptación
                                        IniciarLlamadaCliente(); // Inicia llamada
                                        MostrarMensaje("Se aceptó la llamada del servidor.\r\n"); // Muestra mensaje
                                    }
                                    else // Si rechaza
                                    {
                                        escritor.Write("RECHAZAR_LLAMA>>>"); // Envía rechazo
                                        MostrarMensaje("Rechazaste la llamada.\r\n"); // Muestra mensaje
                                    }
                                }));
                            }
                            else if (mensaje.StartsWith("ACEPTAR_LLAMA>>>")) // Maneja aceptación de llamada
                            {
                                Invoke(new Action(() =>
                                {
                                    IniciarLlamadaCliente(); // Inicia llamada
                                    MostrarMensaje("Se aceptó la llamada del servidor.\r\n"); // Muestra mensaje
                                }));
                            }
                            else if (mensaje.StartsWith("TERMINAR_LLAMA>>>")) // Maneja fin de llamada
                            {
                                Invoke(new Action(() => TerminarLlamadaCliente())); // Termina llamada
                            }
                            else if (mensaje.StartsWith("SERVIDOR>>> Estás bloqueado. No puedes iniciar ni recibir llamadas.")) // Maneja mensaje de bloqueo
                            {
                                Invoke(new Action(() =>
                                {
                                    MostrarMensaje($"[{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}] {mensaje}\r\n"); // Muestra mensaje
                                    MessageBox.Show("Estás bloqueado por el servidor. No puedes iniciar ni recibir llamadas.", "Bloqueado", MessageBoxButtons.OK, MessageBoxIcon.Warning); // Muestra advertencia
                                }));
                            }
                            else if (mensaje.StartsWith("AUDIO_INICIO>>>")) // Maneja recepción de audio
                            {
                                int size = lector.ReadInt32(); // Lee tamaño
                                byte[] audioData = lector.ReadBytes(size); // Lee datos
                                if (llamadaActiva && bufferCliente != null) // Verifica llamada activa
                                {
                                    bufferCliente.AddSamples(audioData, 0, audioData.Length); // Añade audio al buffer
                                }
                            }
                            else // Maneja mensajes normales
                            {
                                string mensajeLimpio = mensaje.Trim(); // Limpia mensaje
                                string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"); // Obtiene timestamp
                                if (Regex.IsMatch(mensajeLimpio, @"\[\d{2}/\d{2}/\d{4} \d{2}:\d{2}:\d{2}\]")) // Verifica formato
                                {
                                    Match match = Regex.Match(mensajeLimpio, @"\[(\d{2}/\d{2}/\d{4} \d{2}:\d{2}:\d{2})\] (.*)"); // Extrae timestamp
                                    if (match.Success)
                                    {
                                        timestamp = match.Groups[1].Value; // Asigna timestamp
                                        mensajeLimpio = match.Groups[2].Value; // Asigna contenido
                                    }
                                }
                                string mensajeFormateado = $"[{timestamp}] {mensajeLimpio}"; // Formatea mensaje
                                MostrarMensaje(mensajeFormateado + "\r\n"); // Muestra mensaje
                            }
                        }
                        else
                        {
                            Thread.Sleep(100); // Pausa breve
                        }
                    }
                    catch (Exception)
                    {
                        if (!desconectarSolicitado) // Verifica si no se solicitó desconexión
                        {
                            Invoke(new Action(() =>
                            {
                                DeshabilitarSalida(true); // Desactiva controles
                                MostrarMensaje($"[{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}] CLIENTE>>> Error de conexión\r\n"); // Muestra error
                            }));
                            break;
                        }
                    }
                } while (mensaje != "SERVIDOR>>> TERMINAR"); // Bucle hasta mensaje de terminación

                if (!desconectarSolicitado) // Si no se solicitó desconexión
                {
                    escritor?.Close(); // Cierra escritor
                    lector?.Close(); // Cierra lector
                    salida?.Close(); // Cierra flujo
                    cliente?.Close(); // Cierra cliente
                    conectado = false; // Marca como desconectado
                    TerminarLlamadaCliente(); // Termina llamada
                    Invoke(new Action(() =>
                    {
                        DeshabilitarSalida(true); // Desactiva controles
                        MostrarMensaje($"[{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}] CLIENTE>>> Desconectado del servidor\r\n"); // Muestra mensaje
                    }));
                }
            }
            catch (Exception)
            {
                if (!desconectarSolicitado) // Verifica si no se solicitó desconexión
                {
                    Invoke(new Action(() =>
                    {
                        DeshabilitarSalida(true); // Desactiva controles
                        MostrarMensaje($"[{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}] CLIENTE>>> Error de conexión\r\n"); // Muestra error
                    }));
                }
            }
        }

        private void btbConectar_Click(object sender, EventArgs e)
        {
            if (!conectado && (lecturathread == null || !lecturathread.IsAlive)) // Verifica si no está conectado
            {
                desconectarSolicitado = false; // Resetea estado
                lecturathread = new Thread(new ThreadStart(EjecutarCliente)); // Crea hilo
                lecturathread.Start(); // Inicia hilo
            }
        }

        private void btnDesconectar_Click(object sender, EventArgs e)
        {
            if (conectado) // Si está conectado
            {
                desconectarSolicitado = true; // Marca desconexión
                try
                {
                    escritor?.Close(); // Cierra escritor
                    lector?.Close(); // Cierra lector
                    salida?.Close(); // Cierra flujo
                    if (gameForm != null) // Si hay juego
                    {
                        gameForm.Close(); // Cierra juego
                        gameForm = null; // Limpia referencia
                    }
                    conectado = false; // Marca como desconectado
                    DeshabilitarSalida(true); // Desactiva controles
                    TerminarLlamadaCliente(); // Termina llamada
                    MostrarMensaje($"[{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}] CLIENTE>>> Desconectado del servidor\r\n"); // Muestra mensaje
                }
                catch (Exception) { } // Maneja errores silenciosamente
            }
        }

        private void btnEnviarArchivo_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog()) // Abre diálogo de selección
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK) // Si selecciona archivo
                {
                    string filePath = openFileDialog.FileName; // Obtiene ruta
                    string fileName = Path.GetFileName(filePath); // Obtiene nombre
                    byte[] fileData = File.ReadAllBytes(filePath); // Lee datos
                    try
                    {
                        string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"); // Obtiene timestamp
                        if (escritor != null) // Verifica escritor
                        {
                            escritor.Write($"ARCHIVO_INICIO>>>{fileName}>>>{fileData.Length}"); // Envía inicio
                            salida.Write(fileData, 0, fileData.Length); // Envía datos
                            salida.Flush(); // Limpia buffer
                            escritor.Write("ARCHIVO_FIN>>>"); // Envía fin
                        }
                        MostrarMensaje($"\r\n[{timestamp}] CLIENTE>>> Se envió el archivo '{fileName}'\r\n"); // Muestra mensaje
                    }
                    catch (Exception) { } // Maneja errores silenciosamente
                }
            }
        }

        public partial class TicTacToeForm : Form
        {
            private char mySymbol; // Símbolo del jugador
            private char opponentSymbol; // Símbolo del oponente
            private bool myTurn; // Indica turno
            private BinaryWriter writer; // Escritor para enviar movimientos
            private Button[,] buttons = new Button[3, 3]; // Matriz de botones
            private char[,] board = new char[3, 3]; // Tablero

            public TicTacToeForm(bool isServer, BinaryWriter writer)
            {
                this.writer = writer; // Asigna escritor
                mySymbol = isServer ? 'X' : 'O'; // Asigna símbolo
                opponentSymbol = isServer ? 'O' : 'X'; // Asigna símbolo del oponente
                myTurn = isServer; // Define turno inicial

                this.Text = "Tic-Tac-Toe"; // Título del formulario
                this.Size = new Size(200, 200); // Tamaño del formulario

                for (int i = 0; i < 3; i++) // Inicializa tablero
                {
                    for (int j = 0; j < 3; j++)
                    {
                        board[i, j] = '\0'; // Casilla vacía
                        Button btn = new Button // Crea botón
                        {
                            Location = new Point(50 * j, 50 * i), // Posición
                            Size = new Size(50, 50), // Tamaño
                            Font = new Font("Arial", 24) // Fuente
                        };
                        int row = i, col = j; // Captura índices
                        btn.Click += (s, e) => MakeMove(row, col); // Asocia clic
                        this.Controls.Add(btn); // Añade botón
                        buttons[i, j] = btn; // Asigna a matriz
                    }
                }
                UpdateButtons(); // Actualiza botones
            }

            private void UpdateButtons()
            {
                for (int i = 0; i < 3; i++) // Itera botones
                {
                    for (int j = 0; j < 3; j++)
                    {
                        buttons[i, j].Text = board[i, j] == '\0' ? "" : board[i, j].ToString(); // Muestra símbolo
                        buttons[i, j].Enabled = myTurn && board[i, j] == '\0'; // Habilita según turno
                    }
                }
            }

            private void MakeMove(int row, int col)
            {
                if (!myTurn || board[row, col] != '\0') return; // Verifica turno y casilla
                board[row, col] = mySymbol; // Marca casilla
                writer.Write($"JUEGO_MOVIMIENTO>>>{row},{col}"); // Envía movimiento
                myTurn = false; // Cambia turno
                UpdateButtons(); // Actualiza botones
                CheckWin(); // Verifica victoria
            }

            public void HandleMove(string mensaje)
            {
                var parts = mensaje.Substring("JUEGO_MOVIMIENTO>>>".Length).Split(','); // Extrae coordenadas
                int row = int.Parse(parts[0]); // Fila
                int col = int.Parse(parts[1]); // Columna
                board[row, col] = opponentSymbol; // Marca casilla
                myTurn = true; // Cambia turno
                UpdateButtons(); // Actualiza botones
                CheckWin(); // Verifica victoria
            }

            private void CheckWin()
            {
                if (CheckWinner(mySymbol)) // Verifica si ganó
                {
                    MessageBox.Show("¡Ganaste!"); // Muestra mensaje
                    this.Close(); // Cierra formulario
                }
                else if (CheckWinner(opponentSymbol)) // Verifica si perdió
                {
                    MessageBox.Show("¡Perdiste!"); // Muestra mensaje
                    this.Close(); // Cierra formulario
                }
                else if (IsDraw()) // Verifica empate
                {
                    MessageBox.Show("Empate"); // Muestra mensaje
                    this.Close(); // Cierra formulario
                }
            }

            private bool CheckWinner(char symbol)
            {
                for (int i = 0; i < 3; i++) // Verifica filas
                {
                    if (board[i, 0] == symbol && board[i, 1] == symbol && board[i, 2] == symbol) return true;
                }
                for (int j = 0; j < 3; j++) // Verifica columnas
                {
                    if (board[0, j] == symbol && board[1, j] == symbol && board[2, j] == symbol) return true;
                }
                if (board[0, 0] == symbol && board[1, 1] == symbol && board[2, 2] == symbol) return true; // Verifica diagonal
                if (board[0, 2] == symbol && board[1, 1] == symbol && board[2, 0] == symbol) return true; // Verifica otra diagonal
                return false; // No hay ganador
            }

            private bool IsDraw()
            {
                for (int i = 0; i < 3; i++) // Verifica casillas vacías
                    for (int j = 0; j < 3; j++)
                        if (board[i, j] == '\0') return false;
                return true; // Tablero lleno, empate
            }
        }

        private void InicializarControlesLlamadaCliente()
        {
            btnColgarCliente = new Button { Text = "Colgar Llamada", Location = new Point(10, 300), Size = new Size(150, 30), Enabled = false }; // Crea botón de colgar
            btnColgarCliente.Click += BtnColgarCliente_Click; // Asocia evento de clic
            this.Controls.Add(btnColgarCliente); // Añade botón al formulario
        }

        private void BtnColgarCliente_Click(object sender, EventArgs e)
        {
            TerminarLlamadaCliente(); // Termina llamada
        }

        private void IniciarLlamadaCliente()
        {
            try
            {
                // Limpiar objetos de audio previos
                waveInCliente?.StopRecording(); // Detiene captura
                waveInCliente?.Dispose(); // Libera captura
                waveOutCliente?.Stop(); // Detiene reproducción
                waveOutCliente?.Dispose(); // Libera reproducción
                bufferCliente?.ClearBuffer(); // Limpia buffer

                // Verificar conexión
                if (escritor == null || salida == null) // Verifica conexión
                {
                    return;
                }

                // Establecer estado de llamada
                llamadaActiva = true; // Marca llamada como activa
                btnColgarCliente.Enabled = true; // Habilita botón de colgar

                // Configurar micrófono
                waveInCliente = new WaveInEvent { WaveFormat = new WaveFormat(8000, 16, 1) }; // Configura captura
                waveInCliente.DataAvailable += (s, args) =>
                {
                    if (llamadaActiva && escritor != null && salida != null) // Verifica llamada activa
                    {
                        try
                        {
                            escritor.Write("AUDIO_INICIO>>>"); // Envía inicio de audio
                            escritor.Write(args.BytesRecorded); // Envía tamaño
                            salida.Write(args.Buffer, 0, args.BytesRecorded); // Envía datos
                            salida.Flush(); // Limpia buffer
                        }
                        catch (Exception) { } // Maneja errores silenciosamente
                    }
                };
                waveInCliente.StartRecording(); // Inicia captura

                // Configurar parlantes
                bufferCliente = new BufferedWaveProvider(waveInCliente.WaveFormat); // Crea buffer
                waveOutCliente = new WaveOutEvent(); // Crea reproductor
                waveOutCliente.Init(bufferCliente); // Inicializa reproductor
                waveOutCliente.Play(); // Inicia reproducción
            }
            catch (Exception)
            {
                llamadaActiva = false; // Marca llamada como inactiva
                btnColgarCliente.Enabled = false; // Desactiva botón
            }
        }

        private void TerminarLlamadaCliente()
        {
            if (!llamadaActiva) return; // Verifica llamada activa

            llamadaActiva = false; // Marca llamada como terminada
            if (escritor != null) escritor.Write("TERMINAR_LLAMA>>>"); // Envía mensaje de fin
            MostrarMensaje("Llamada terminada.\r\n"); // Muestra mensaje

            waveInCliente?.StopRecording(); // Detiene captura
            waveInCliente?.Dispose(); // Libera captura
            waveOutCliente?.Stop(); // Detiene reproducción
            waveOutCliente?.Dispose(); // Libera reproducción
            bufferCliente?.ClearBuffer(); // Limpia buffer

            btnColgarCliente.Enabled = false; // Desactiva botón
        }

        private void btnLlamar_Click(object sender, EventArgs e)
        {
            if (!conectado) // Verifica conexión
            {
                MessageBox.Show("Debes estar conectado al servidor para llamar.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); // Muestra advertencia
                return;
            }
            if (llamadaActiva) // Verifica llamada activa
            {
                MessageBox.Show("Ya hay una llamada activa. No puedes iniciar otra.", "Llamada en curso", MessageBoxButtons.OK, MessageBoxIcon.Warning); // Muestra advertencia
                return;
            }
            try
            {
                if (escritor != null) // Verifica escritor
                {
                    escritor.Write("SOLICITUD_LLAMA>>>"); // Envía solicitud de llamada
                    MostrarMensaje($"CLIENTE>>> Solicitud de llamada enviada al servidor.\r\n"); // Muestra mensaje
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"CLIENTE>>> Error al enviar solicitud: {ex.Message}\r\n"); // Muestra error
            }
        }

        private void btnColgar_Click(object sender, EventArgs e)
        {
            if (!llamadaActiva) // Verifica llamada activa
            {
                MessageBox.Show("No hay una llamada activa para colgar.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); // Muestra advertencia
                return;
            }
            DialogResult result = MessageBox.Show("¿Estás seguro de que quieres colgar la llamada?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question); // Muestra diálogo
            if (result == DialogResult.Yes) // Si confirma
            {
                TerminarLlamadaCliente(); // Termina llamada
                MostrarMensaje("CLIENTE>>> Llamada colgada con confirmación.\r\n"); // Muestra mensaje
            }
        }
    }
}