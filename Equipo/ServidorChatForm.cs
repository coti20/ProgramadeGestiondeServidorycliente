using System;
using System.Net;
using System.Windows.Forms;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NAudio.Wave;

namespace Equipo
{
    public partial class ServidorChatForm : Form
    {
        private TcpListener oyente; // Escucha conexiones entrantes
        private Thread escuchaThread; // Hilo para manejar conexiones
        private List<ClienteConectado> clientesConectados; // Lista de clientes conectados
        private List<string> mensajesPendientes; // Mensajes pendientes de confirmación
        private const int MAX_CLIENTES = 10; // Límite máximo de clientes
        private bool servidorActivo; // Indica si el servidor está activo
        private bool isFileDialogOpen; // Evita múltiples diálogos de archivo
        private Dictionary<ClienteConectado, TicTacToeForm> activeGames; // Juegos activos por cliente

        private delegate void DisplayDelegate(string mensaje); // Delegado para mostrar mensajes
        private delegate void UpdateListDelegate(); // Delegado para actualizar lista de IPs
        private delegate void DisplayColorDelegate(string mensaje, Color color); // Delegado para mensajes con color
        private delegate void DisableInputDelegate(bool valor); // Delegado para habilitar/deshabilitar entrada
        private delegate void ActualizarEstadoMensajeDelegate(string mensajeOriginal, string nuevoEstado); // Delegado para actualizar estado de mensajes

        public ServidorChatForm()
        {
            InitializeComponent(); // Inicializa componentes de la UI
            clientesConectados = new List<ClienteConectado>(); // Inicializa lista de clientes
            mensajesPendientes = new List<string>(); // Inicializa lista de mensajes pendientes
            servidorActivo = true; // Marca servidor como activo
            isFileDialogOpen = false; // Inicializa estado de diálogo de archivo
            mostrarTextbox.ReadOnly = true; // Configura textbox como solo lectura
            chkRecibosLectura.Checked = true; // Activa recibos de lectura por defecto
            this.Activated += ServidorChatForm_Activated; // Asocia evento de activación
            this.Resize += ServidorChatForm_Resize; // Asocia evento de cambio de tamaño
            activeGames = new Dictionary<ClienteConectado, TicTacToeForm>(); // Inicializa diccionario de juegos
        }

        private void ServidorChatForm_Activated(object sender, EventArgs e)
        {
            if (chkRecibosLectura.Checked) // Verifica si los recibos de lectura están activados
            {
                foreach (var mensaje in mensajesPendientes.ToArray()) // Procesa mensajes pendientes
                {
                    foreach (var cliente in clientesConectados) // Envía confirmación a cada cliente
                    {
                        cliente.Escribir($"CONFIRMACION_LEIDO>>>{mensaje}"); // Envía confirmación de lectura
                    }
                    mensajesPendientes.Remove(mensaje); // Elimina mensaje de pendientes
                }
            }
        }

        private void ServidorChatForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Maximized && chkRecibosLectura.Checked) // Verifica si ventana está maximizada
            {
                foreach (var mensaje in mensajesPendientes.ToArray()) // Procesa mensajes pendientes
                {
                    foreach (var cliente in clientesConectados) // Envía confirmación a cada cliente
                    {
                        cliente.Escribir($"CONFIRMACION_LEIDO>>>{mensaje}"); // Envía confirmación de lectura
                    }
                    mensajesPendientes.Remove(mensaje); // Elimina mensaje de pendientes
                }
            }
        }

        private void ServidorChatForm_Load(object sender, EventArgs e)
        {
            entradaTextbox.ReadOnly = true; // Desactiva entrada de texto
            btnEnviarArchivo.Enabled = false; // Desactiva botón de enviar archivo
            btnEnviarArchivoPorCliente.Enabled = false; // Desactiva botón de enviar archivo por cliente

            string ipLocal = "No IP encontrada"; // Variable para IP local
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList) // Busca IP local
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) // Verifica si es IPv4
                {
                    ipLocal = ip.ToString(); // Asigna IP encontrada
                    break;
                }
            }
            lblipservidor.Text = "IP del servidor: " + ipLocal; // Muestra IP en la UI

            if (ipLocal == "No IP encontrada") // Verifica si no se encontró IP
            {
                MessageBox.Show("No se pudo detectar una IP válida. Usa la IP de tu red local manualmente (ej. 192.168.1.x)."); // Muestra advertencia
            }

            escuchaThread = new Thread(new ThreadStart(IniciarEscucha)); // Crea hilo para escuchar
            escuchaThread.Start(); // Inicia hilo
            btnColgar.Enabled = false; // Desactiva botón de colgar
        }

        private void ServidorChatForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            servidorActivo = false; // Marca servidor como inactivo
            oyente?.Stop(); // Detiene el listener
            foreach (var cliente in clientesConectados) // Cierra conexiones de clientes
            {
                cliente.CerrarConexión(); // Cierra conexión de cada cliente
            }
            foreach (var game in activeGames.Values) // Cierra juegos activos
            {
                game.Close(); // Cierra formulario de juego
            }
            System.Environment.Exit(System.Environment.ExitCode); // Cierra la aplicación
        }

        private void IniciarEscucha()
        {
            try
            {
                oyente = new TcpListener(IPAddress.Any, 50000); // Crea listener en puerto 50000
                oyente.Start(); // Inicia listener
                MostrarMensaje("Esperando cliente...\r\n"); // Muestra mensaje de espera

                while (servidorActivo && clientesConectados.Count < MAX_CLIENTES) // Bucle para aceptar clientes
                {
                    if (oyente.Pending()) // Verifica si hay conexiones pendientes
                    {
                        Socket nuevoSocket = oyente.AcceptSocket(); // Acepta nuevo socket
                        NetworkStream stream = new NetworkStream(nuevoSocket); // Crea flujo
                        BinaryWriter escritor = new BinaryWriter(stream); // Crea escritor
                        BinaryReader lector = new BinaryReader(stream); // Crea lector

                        string nombreCliente = $"Cliente {clientesConectados.Count + 1}"; // Asigna nombre
                        ClienteConectado nuevoCliente = new ClienteConectado(nuevoSocket, stream, escritor, lector, this, nombreCliente); // Crea cliente
                        clientesConectados.Add(nuevoCliente); // Añade cliente

                        Thread clienteThread = new Thread(new ThreadStart(nuevoCliente.Ejecutar)); // Crea hilo para cliente
                        clienteThread.Start(); // Inicia hilo

                        string clientIp = ((IPEndPoint)nuevoSocket.RemoteEndPoint).Address.ToString(); // Obtiene IP
                        MostrarMensaje($"{nombreCliente} conectado. IP: {clientIp}\r\n"); // Muestra mensaje
                        this.Invoke(new MethodInvoker(delegate
                        {
                            this.entradaTextbox.ReadOnly = false; // Habilita entrada de texto
                            this.btnEnviarArchivo.Enabled = true; // Habilita botón de enviar archivo
                            this.btnEnviarArchivoPorCliente.Enabled = true; // Habilita botón de enviar archivo por cliente
                        }));
                        ActualizarListaIPs(); // Actualiza lista de IPs
                    }
                    Thread.Sleep(100); // Pausa breve
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al iniciar la escucha: {ex.Message}\r\n"); // Muestra error
            }
        }

        public void DifundirMensaje(string mensaje, ClienteConectado remitente = null)
        {
            foreach (var cliente in clientesConectados.ToArray()) // Itera sobre clientes
            {
                if (cliente != remitente) // Excluye al remitente
                {
                    try
                    {
                        cliente.Escribir(mensaje); // Envía mensaje
                    }
                    catch
                    {
                        clientesConectados.Remove(cliente); // Elimina cliente si hay error
                        DifundirMensaje($"SERVIDOR>>> Cliente desconectado.\r\n"); // Notifica desconexión
                        ActualizarListaIPs(); // Actualiza lista de IPs
                    }
                }
            }
        }

        public void ActualizarEstadoMensaje(string mensajeOriginal, string nuevoEstado)
        {
            if (mostrarTextbox.InvokeRequired) // Verifica si se necesita invocar en hilo UI
            {
                Invoke(new ActualizarEstadoMensajeDelegate(ActualizarEstadoMensaje), new object[] { mensajeOriginal, nuevoEstado }); // Invoca en hilo UI
            }
            else
            {
                string mensajeSinEstado = Regex.Replace(mensajeOriginal, @" ✓+$", "").Trim(); // Limpia estado
                string mensajeConEstado = $"{mensajeSinEstado} {nuevoEstado}"; // Añade nuevo estado

                int index = mostrarTextbox.Text.IndexOf(mensajeOriginal); // Busca mensaje
                if (index == -1)
                {
                    index = mostrarTextbox.Text.IndexOf(mensajeSinEstado); // Busca sin estado
                }
                if (index != -1) // Si encuentra mensaje
                {
                    mostrarTextbox.Text = mostrarTextbox.Text.Remove(index, mensajeOriginal.Length).Insert(index, mensajeConEstado); // Actualiza mensaje
                    mostrarTextbox.SelectionStart = mostrarTextbox.Text.Length; // Mueve cursor
                    mostrarTextbox.ScrollToCaret(); // Desplaza vista
                }
            }
        }

        private void entradaTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !string.IsNullOrEmpty(entradaTextbox.Text.Trim())) // Verifica Enter y entrada válida
            {
                string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"); // Obtiene timestamp
                string mensaje = $"[{timestamp}] SERVIDOR>>> {entradaTextbox.Text.Trim()}"; // Formatea mensaje
                DifundirMensaje(mensaje); // Difunde mensaje
                MostrarMensaje($"{mensaje}\r\n"); // Muestra mensaje
                entradaTextbox.Clear(); // Limpia textbox
            }
        }

        private void btnEnviarArchivo_Click(object sender, EventArgs e)
        {
            if (!isFileDialogOpen && btnEnviarArchivo.Enabled) // Verifica si se puede abrir diálogo
            {
                isFileDialogOpen = true; // Marca diálogo como abierto
                try
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
                                string mensajeInicio = $"ARCHIVO_INICIO>>>{fileName}>>>{fileData.Length}"; // Crea mensaje de inicio
                                MostrarMensaje($"[{timestamp}] SERVIDOR>>> Enviando archivo '{fileName}'...\r\n"); // Muestra mensaje
                                foreach (var cliente in clientesConectados.ToArray()) // Itera sobre clientes
                                {
                                    try
                                    {
                                        cliente.Escribir(mensajeInicio); // Envía inicio
                                        cliente.Stream.Write(fileData, 0, fileData.Length); // Envía datos
                                        cliente.Stream.Flush(); // Limpia buffer
                                        cliente.Escribir("ARCHIVO_FIN>>>"); // Envía fin
                                    }
                                    catch
                                    {
                                        clientesConectados.Remove(cliente); // Elimina cliente si hay error
                                        DifundirMensaje($"SERVIDOR>>> Cliente desconectado durante envío de archivo.\r\n"); // Notifica desconexión
                                        ActualizarListaIPs(); // Actualiza lista de IPs
                                    }
                                }
                                MostrarMensaje($"[{timestamp}] SERVIDOR>>> Archivo enviado a todos.\r\n"); // Muestra mensaje
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error al enviar el archivo: {ex.Message}", "Error de Archivo", MessageBoxButtons.OK, MessageBoxIcon.Error); // Muestra error
                            }
                        }
                    }
                }
                finally
                {
                    isFileDialogOpen = false; // Marca diálogo como cerrado
                }
            }
        }

        private void btnEnviarArchivoPorCliente_Click(object sender, EventArgs e)
        {
            if (clientesListBox.SelectedIndex == -1 || clientesConectados.Count == 0) // Verifica selección
            {
                MessageBox.Show("Selecciona un cliente de la lista para enviar el archivo.", "Sin selección", MessageBoxButtons.OK, MessageBoxIcon.Warning); // Muestra advertencia
                return;
            }

            if (!isFileDialogOpen) // Verifica si se puede abrir diálogo
            {
                isFileDialogOpen = true; // Marca diálogo como abierto
                try
                {
                    using (OpenFileDialog openFileDialog = new OpenFileDialog()) // Abre diálogo de selección
                    {
                        if (openFileDialog.ShowDialog() == DialogResult.OK) // Si selecciona archivo
                        {
                            string filePath = openFileDialog.FileName; // Obtiene ruta
                            string fileName = Path.GetFileName(filePath); // Obtiene nombre
                            byte[] fileData = File.ReadAllBytes(filePath); // Lee datos

                            int indice = clientesListBox.SelectedIndex; // Obtiene índice
                            var cliente = clientesConectados[indice]; // Obtiene cliente

                            try
                            {
                                string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"); // Obtiene timestamp
                                string mensajeInicio = $"ARCHIVO_INICIO>>>{fileName}>>>{fileData.Length}"; // Crea mensaje de inicio
                                MostrarMensaje($"[{timestamp}] SERVIDOR>>> Enviando archivo '{fileName}' a {cliente.Nombre}...\r\n"); // Muestra mensaje
                                cliente.Escribir(mensajeInicio); // Envía inicio
                                cliente.Stream.Write(fileData, 0, fileData.Length); // Envía datos
                                cliente.Stream.Flush(); // Limpia buffer
                                cliente.Escribir("ARCHIVO_FIN>>>"); // Envía fin
                                MostrarMensaje($"[{timestamp}] SERVIDOR>>> Archivo enviado a {cliente.Nombre}.\r\n"); // Muestra mensaje
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error al enviar el archivo a {cliente.Nombre}: {ex.Message}", "Error de Archivo", MessageBoxButtons.OK, MessageBoxIcon.Error); // Muestra error
                                clientesConectados.Remove(cliente); // Elimina cliente
                                DifundirMensaje($"SERVIDOR>>> Cliente {cliente.Nombre} desconectado durante envío de archivo.\r\n"); // Notifica desconexión
                                ActualizarListaIPs(); // Actualiza lista de IPs
                            }
                        }
                    }
                }
                finally
                {
                    isFileDialogOpen = false; // Marca diálogo como cerrado
                }
            }
        }

        private void MostrarMensaje(string mensaje)
        {
            if (mostrarTextbox.InvokeRequired) // Verifica si se necesita invocar en hilo UI
            {
                Invoke(new DisplayDelegate(MostrarMensaje), new object[] { mensaje }); // Invoca en hilo UI
            }
            else
            {
                mostrarTextbox.Text += mensaje; // Añade mensaje
                mostrarTextbox.SelectionStart = mostrarTextbox.Text.Length; // Mueve cursor
                mostrarTextbox.ScrollToCaret(); // Desplaza vista
                if (mensaje.Contains(">>>") && !mensaje.Contains("SERVIDOR>>>") && chkRecibosLectura.Checked &&
                    (WindowState == FormWindowState.Maximized || this == Form.ActiveForm)) // Verifica condiciones para confirmación
                {
                    foreach (var cliente in clientesConectados) // Itera sobre clientes
                    {
                        cliente.Escribir($"CONFIRMACION_LEIDO>>>{mensaje.TrimEnd('\r', '\n')}"); // Envía confirmación
                    }
                }
                else if (mensaje.Contains(">>>") && !mensaje.Contains("SERVIDOR>>>")) // Si mensaje no es del servidor
                {
                    mensajesPendientes.Add(mensaje.TrimEnd('\r', '\n')); // Añade a pendientes
                }
            }
        }

        private void MostrarMensajeEnColor(string mensaje, Color color)
        {
            if (mostrarTextbox.InvokeRequired) // Verifica si se necesita invocar en hilo UI
            {
                Invoke(new DisplayColorDelegate(MostrarMensajeEnColor), new object[] { mensaje, color }); // Invoca en hilo UI
            }
            else
            {
                mostrarTextbox.SelectionStart = mostrarTextbox.Text.Length; // Mueve cursor
                mostrarTextbox.SelectionLength = 0; // Limpia selección
                mostrarTextbox.SelectionColor = color; // Establece color
                mostrarTextbox.AppendText(mensaje); // Añade mensaje
                mostrarTextbox.SelectionColor = mostrarTextbox.ForeColor; // Restaura color
            }
        }

        private void DeshabilitarEntrada(bool valor)
        {
            if (entradaTextbox.InvokeRequired) // Verifica si se necesita invocar en hilo UI
            {
                Invoke(new DisableInputDelegate(DeshabilitarEntrada), new object[] { valor }); // Invoca en hilo UI
            }
            else
            {
                entradaTextbox.ReadOnly = valor; // Habilita/desactiva entrada
            }
        }

        public void ManejarRecepcionArchivo(string fileName, long fileSize, BinaryReader reader, BinaryWriter writer, string nombreCliente)
        {
            string mensaje = $"¿Deseas aceptar el archivo '{fileName}' de {nombreCliente}?"; // Crea mensaje de confirmación
            DialogResult resultado = MessageBox.Show(mensaje, "Archivo Recibido", MessageBoxButtons.YesNo, MessageBoxIcon.Question); // Muestra diálogo

            if (resultado == DialogResult.Yes) // Si acepta archivo
            {
                using (SaveFileDialog saveDialog = new SaveFileDialog()) // Abre diálogo de guardado
                {
                    saveDialog.FileName = fileName; // Establece nombre
                    if (saveDialog.ShowDialog() == DialogResult.OK) // Si selecciona ruta
                    {
                        string savePath = saveDialog.FileName; // Obtiene ruta
                        try
                        {
                            byte[] fileData = new byte[fileSize]; // Crea buffer
                            int bytesRead = 0; // Contador de bytes
                            int currentByte = 0; // Bytes leídos en iteración

                            while (bytesRead < fileSize) // Lee datos
                            {
                                currentByte = reader.BaseStream.Read(fileData, bytesRead, (int)fileSize - bytesRead); // Lee bytes
                                if (currentByte == 0) throw new Exception("Conexión cerrada prematuramente"); // Verifica desconexión
                                bytesRead += currentByte; // Actualiza contador
                            }

                            string finConfirm = reader.ReadString(); // Lee mensaje de fin
                            if (finConfirm != "ARCHIVO_FIN>>>") throw new Exception("Error en sincronización de archivo"); // Verifica sincronización

                            File.WriteAllBytes(savePath, fileData); // Guarda archivo
                            string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"); // Obtiene timestamp
                            MostrarMensaje($"\r\n[{timestamp}] SERVIDOR>>> Se recibió el archivo de {nombreCliente}\r\n"); // Muestra mensaje
                            writer.Write($"SERVIDOR>>> Se recibió el archivo\r\n"); // Notifica recepción
                        }
                        catch (Exception ex)
                        {
                            string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"); // Obtiene timestamp
                            MostrarMensaje($"\r\n[{timestamp}] SERVIDOR>>> Error al recibir el archivo de {nombreCliente}: {ex.Message}\r\n"); // Muestra error
                            writer.Write("ARCHIVO_RECHAZADO>>>"); // Notifica rechazo
                        }
                    }
                    else // Si cancela diálogo
                    {
                        try
                        {
                            byte[] buffer = new byte[1024]; // Crea buffer
                            long bytesRead = 0; // Contador de bytes
                            while (bytesRead < fileSize) // Descarta datos
                            {
                                int currentRead = reader.BaseStream.Read(buffer, 0, (int)Math.Min(buffer.Length, fileSize - bytesRead)); // Lee bytes
                                if (currentRead == 0) throw new Exception("Conexión cerrada prematuramente"); // Verifica desconexión
                                bytesRead += currentRead; // Actualiza contador
                            }
                            string finConfirm = reader.ReadString(); // Lee mensaje de fin
                            if (finConfirm != "ARCHIVO_FIN>>>") throw new Exception("Error en sincronización de archivo"); // Verifica sincronización
                            string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"); // Obtiene timestamp
                            MostrarMensaje($"\r\n[{timestamp}] SERVIDOR>>> El archivo se canceló\r\n"); // Muestra mensaje
                            writer.Write("ARCHIVO_RECHAZADO>>>"); // Notifica rechazo
                        }
                        catch (Exception)
                        {
                            string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"); // Obtiene timestamp
                            MostrarMensaje($"\r\n[{timestamp}] SERVIDOR>>> El archivo se canceló\r\n"); // Muestra mensaje
                            writer.Write("ARCHIVO_RECHAZADO>>>"); // Notifica rechazo
                        }
                    }
                }
            }
            else // Si rechaza archivo
            {
                try
                {
                    byte[] buffer = new byte[1024]; // Crea buffer
                    long bytesRead = 0; // Contador de bytes
                    while (bytesRead < fileSize) // Descarta datos
                    {
                        int currentRead = reader.BaseStream.Read(buffer, 0, (int)Math.Min(buffer.Length, fileSize - bytesRead)); // Lee bytes
                        if (currentRead == 0) throw new Exception("Conexión cerrada prematuramente"); // Verifica desconexión
                        bytesRead += currentRead; // Actualiza contador
                    }
                    string finConfirm = reader.ReadString(); // Lee mensaje de fin
                    if (finConfirm != "ARCHIVO_FIN>>>") throw new Exception("Error en sincronización de archivo"); // Verifica sincronización
                    string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"); // Obtiene timestamp
                    MostrarMensaje($"\r\n[{timestamp}] SERVIDOR>>> El archivo se canceló\r\n"); // Muestra mensaje
                    writer.Write("ARCHIVO_RECHAZADO>>>"); // Notifica rechazo
                }
                catch (Exception)
                {
                    string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"); // Obtiene timestamp
                    MostrarMensaje($"\r\n[{timestamp}] SERVIDOR>>> El archivo se canceló\r\n"); // Muestra mensaje
                    writer.Write("ARCHIVO_RECHAZADO>>>"); // Notifica rechazo
                }
            }
        }

        private void ActualizarListaIPs()
        {
            if (clientesListBox.InvokeRequired) // Verifica si se necesita invocar en hilo UI
            {
                Invoke(new UpdateListDelegate(ActualizarListaIPs)); // Invoca en hilo UI
            }
            else
            {
                int seleccionAnterior = clientesListBox.SelectedIndex; // Guarda selección
                clientesListBox.Items.Clear(); // Limpia lista
                for (int i = 0; i < clientesConectados.Count; i++) // Itera clientes
                {
                    string clientIp = ((IPEndPoint)clientesConectados[i].Socket.RemoteEndPoint).Address.ToString(); // Obtiene IP
                    string estado = clientesConectados[i].Bloqueado ? " (Bloqueado)" : ""; // Verifica estado
                    clientesListBox.Items.Add($"IP: {clientIp} - {clientesConectados[i].Nombre}{estado}"); // Añade cliente
                }
                if (seleccionAnterior >= 0 && seleccionAnterior < clientesListBox.Items.Count) // Restaura selección
                {
                    clientesListBox.SelectedIndex = seleccionAnterior; // Restaura índice
                }
            }
        }

        private void btnBloquearCliente_Click(object sender, EventArgs e)
        {
            if (clientesListBox.SelectedIndex == -1 || clientesConectados.Count == 0) // Verifica selección
            {
                MessageBox.Show("Selecciona un cliente de la lista para bloquear/desbloquear.", "Sin selección", MessageBoxButtons.OK, MessageBoxIcon.Warning); // Muestra advertencia
                return;
            }

            int indice = clientesListBox.SelectedIndex; // Obtiene índice
            if (indice < 0 || indice >= clientesConectados.Count) // Verifica validez
            {
                MessageBox.Show("Índice de cliente inválido. Por favor, intenta de nuevo.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); // Muestra error
                return;
            }

            var cliente = clientesConectados[indice]; // Obtiene cliente

            try
            {
                if (!cliente.Bloqueado) // Si no está bloqueado
                {
                    cliente.Bloqueado = true; // Bloquea cliente
                    btnBloquearCliente.Text = "Desbloquear Seleccionado"; // Cambia texto del botón
                    MostrarMensaje($"SERVIDOR>>> Cliente bloqueado: {cliente.Nombre}\r\n"); // Muestra mensaje
                    cliente.Escribir("SERVIDOR>>> Has sido bloqueado. No puedes enviar mensajes.\r\n"); // Notifica bloqueo
                }
                else // Si está bloqueado
                {
                    cliente.Bloqueado = false; // Desbloquea cliente
                    btnBloquearCliente.Text = "Bloquear Seleccionado"; // Cambia texto del botón
                    MostrarMensaje($"SERVIDOR>>> Cliente desbloqueado: {cliente.Nombre}\r\n"); // Muestra mensaje
                    cliente.Escribir("SERVIDOR>>> Has sido desbloqueado. Ahora puedes enviar mensajes.\r\n"); // Notifica desbloqueo
                }

                ActualizarListaIPs(); // Actualiza lista de IPs
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al intentar bloquear/desbloquear al cliente: {ex.Message}", "Error de comunicación", MessageBoxButtons.OK, MessageBoxIcon.Error); // Muestra error
                cliente.CerrarConexión(); // Cierra conexión
            }
        }

        private void btnInvitarTateti_Click(object sender, EventArgs e)
        {
            if (clientesListBox.SelectedIndex == -1 || clientesConectados.Count == 0) // Verifica selección
            {
                MessageBox.Show("Selecciona un cliente de la lista para invitar a jugar.", "Sin selección", MessageBoxButtons.OK, MessageBoxIcon.Warning); // Muestra advertencia
                return;
            }

            int indice = clientesListBox.SelectedIndex; // Obtiene índice
            var cliente = clientesConectados[indice]; // Obtiene cliente

            if (activeGames.ContainsKey(cliente)) // Verifica juego activo
            {
                MessageBox.Show("Ya hay un juego en curso con este cliente.", "Juego en curso", MessageBoxButtons.OK, MessageBoxIcon.Warning); // Muestra advertencia
                return;
            }

            try
            {
                cliente.Escribir("INVITACION_JUEGO>>>"); // Envía invitación
                MostrarMensaje($"SERVIDOR>>> Invitación a jugar enviada a {cliente.Nombre}\r\n"); // Muestra mensaje
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al enviar invitación: {ex.Message}"); // Muestra error
            }
        }

        public void HandleAcceptGame(ClienteConectado cliente)
        {
            MostrarMensaje($"SERVIDOR>>> {cliente.Nombre} aceptó la invitación a jugar.\r\n"); // Muestra mensaje
            TicTacToeForm game = new TicTacToeForm(true, cliente.Escritor); // Crea juego
            activeGames[cliente] = game; // Añade juego
            game.FormClosed += (s, e) => activeGames.Remove(cliente); // Asocia evento de cierre
            game.Show(); // Muestra juego
        }

        public void HandleRejectGame(ClienteConectado cliente)
        {
            MostrarMensaje($"SERVIDOR>>> {cliente.Nombre} rechazó la invitación a jugar.\r\n"); // Muestra mensaje
        }

        public void HandleMoveForClient(ClienteConectado cliente, string mensaje)
        {
            if (activeGames.TryGetValue(cliente, out TicTacToeForm game)) // Busca juego activo
            {
                game.HandleMove(mensaje); // Procesa movimiento
            }
        }

        public class ClienteConectado
        {
            public Socket Socket { get; private set; } // Socket del cliente
            public NetworkStream Stream { get; private set; } // Flujo de red
            public BinaryWriter Escritor { get; private set; } // Escritor binario
            public BinaryReader Lector { get; private set; } // Lector binario
            private ServidorChatForm Form { get; set; } // Referencia al formulario
            private bool Conectado { get; set; } // Estado de conexión
            public string Nombre { get; private set; } // Nombre del cliente
            public bool Bloqueado { get; set; } = false; // Estado de bloqueo

            public ClienteConectado(Socket socket, NetworkStream stream, BinaryWriter escritor, BinaryReader lector, ServidorChatForm form, string nombre)
            {
                Socket = socket; // Asigna socket
                Stream = stream; // Asigna flujo
                Escritor = escritor; // Asigna escritor
                Lector = lector; // Asigna lector
                Form = form; // Asigna formulario
                Conectado = true; // Marca como conectado
                Nombre = nombre; // Asigna nombre
            }

            public void Ejecutar()
            {
                try
                {
                    Form.Invoke(new MethodInvoker(delegate
                    {
                        Form.entradaTextbox.ReadOnly = false; // Habilita entrada
                        Form.btnEnviarArchivo.Enabled = true; // Habilita botón de archivo
                        Form.btnEnviarArchivoPorCliente.Enabled = true; // Habilita botón de archivo por cliente
                    }));

                    while (Conectado) // Bucle mientras esté conectado
                    {
                        if (Socket.Poll(1000, SelectMode.SelectRead) && Socket.Available == 0) // Verifica desconexión
                        {
                            CerrarConexión(); // Cierra conexión
                            break;
                        }

                        if (Socket.Poll(1000, SelectMode.SelectRead) && Socket.Available > 0) // Verifica datos
                        {
                            try
                            {
                                string mensaje = Lector.ReadString(); // Lee mensaje
                                if (string.IsNullOrEmpty(mensaje)) continue; // Ignora mensajes vacíos

                                if (mensaje.StartsWith("ARCHIVO_INICIO>>>")) // Maneja recepción de archivo
                                {
                                    string[] partes = mensaje.Split(new string[] { ">>>" }, StringSplitOptions.None); // Divide mensaje
                                    string fileName = partes[1]; // Obtiene nombre
                                    long fileSize = long.Parse(partes[2]); // Obtiene tamaño
                                    Form.Invoke(new MethodInvoker(() =>
                                    {
                                        Form.ManejarRecepcionArchivo(fileName, fileSize, Lector, Escritor, Nombre); // Procesa archivo
                                    }));
                                }
                                else if (mensaje.StartsWith("CONFIRMACION_LEIDO>>>")) // Maneja confirmación de lectura
                                {
                                    string mensajeOriginal = mensaje.Substring("CONFIRMACION_LEIDO>>>".Length).Trim(); // Extrae mensaje original
                                    Form.ActualizarEstadoMensaje(mensajeOriginal, "✓"); // Actualiza estado
                                    Form.DifundirMensaje(mensaje, this); // Difunde confirmación
                                }
                                else if (mensaje.StartsWith("ACEPTAR_JUEGO>>>")) // Maneja aceptación de juego
                                {
                                    Form.Invoke(new Action(() => Form.HandleAcceptGame(this))); // Inicia juego
                                }
                                else if (mensaje.StartsWith("RECHAZAR_JUEGO>>>")) // Maneja rechazo de juego
                                {
                                    Form.Invoke(new Action(() => Form.HandleRejectGame(this))); // Notifica rechazo
                                }
                                else if (mensaje.StartsWith("JUEGO_MOVIMIENTO>>>")) // Maneja movimiento de juego
                                {
                                    Form.Invoke(new Action(() => Form.HandleMoveForClient(this, mensaje))); // Procesa movimiento
                                }
                                else if (mensaje.StartsWith("ACEPTAR_LLAMA>>>")) // Maneja aceptación de llamada
                                {
                                    Form.Invoke(new Action(() => Form.HandleAcceptCall(this))); // Inicia llamada
                                }
                                else if (mensaje.StartsWith("RECHAZAR_LLAMA>>>")) // Maneja rechazo de llamada
                                {
                                    Form.Invoke(new Action(() => Form.HandleRejectCall(this))); // Notifica rechazo
                                }
                                else if (mensaje.StartsWith("TERMINAR_LLAMA>>>")) // Maneja fin de llamada
                                {
                                    Form.Invoke(new TerminarLlamadaDelegate(Form.TerminarLlamada), this); // Termina llamada
                                }
                                else if (mensaje.StartsWith("AUDIO_INICIO>>>")) // Maneja recepción de audio
                                {
                                    int size = Lector.ReadInt32(); // Lee tamaño
                                    byte[] audioData = Lector.ReadBytes(size); // Lee datos
                                    Form.Invoke(new Action(() => Form.HandleAudioFromClient(this, audioData))); // Procesa audio
                                }
                                else if (mensaje.StartsWith("SOLICITUD_LLAMA>>>")) // Maneja solicitud de llamada
                                {
                                    if (Bloqueado) // Si cliente está bloqueado
                                    {
                                        Escribir("SERVIDOR>>> Estás bloqueado. No puedes iniciar ni recibir llamadas.\r\n"); // Notifica bloqueo
                                        Form.MostrarMensaje($"SERVIDOR>>> {Nombre} intentó iniciar una llamada, pero está bloqueado.\r\n"); // Muestra mensaje
                                    }
                                    else
                                    {
                                        Form.MostrarMensaje($"[DEBUG] SERVIDOR>>> Recibida solicitud de llamada de {Nombre}\r\n"); // Muestra mensaje de depuración
                                        Form.Invoke(new Action(() =>
                                        {
                                            DialogResult result = MessageBox.Show($"El cliente {Nombre} solicita iniciar una llamada de voz. ¿Aceptar?", "Solicitud de Llamada", MessageBoxButtons.YesNo, MessageBoxIcon.Question); // Muestra diálogo
                                            if (result == DialogResult.Yes) // Si acepta
                                            {
                                                Escribir("ACEPTAR_LLAMA>>>"); // Envía aceptación
                                                Form.HandleAcceptCall(this); // Inicia llamada
                                                Form.MostrarMensaje($"SERVIDOR>>> Se aceptó la llamada del {Nombre}.\r\n"); // Muestra mensaje
                                            }
                                            else // Si rechaza
                                            {
                                                Escribir("RECHAZAR_LLAMA>>>"); // Envía rechazo
                                                Form.MostrarMensaje($"SERVIDOR>>> Se rechazó la llamada del {Nombre}.\r\n"); // Muestra mensaje
                                            }
                                        }));
                                    }
                                }
                                else if (mensaje != "ARCHIVO_RECHAZADO>>>") // Maneja mensajes normales
                                {
                                    if (Bloqueado) // Si cliente está bloqueado
                                    {
                                        Escribir("SERVIDOR>>> Estás bloqueado. No puedes enviar mensajes.\r\n"); // Notifica bloqueo
                                        continue;
                                    }

                                    mensaje = mensaje.Replace("CLIENTE>>>", Nombre + ">>>"); // Reemplaza prefijo
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
                                    Form.MostrarMensaje(mensajeFormateado + "\r\n"); // Muestra mensaje
                                    Form.DifundirMensaje(mensajeFormateado, this); // Difunde mensaje
                                }
                            }
                            catch (IOException)
                            {
                                // Maneja errores de entrada/salida
                            }
                            catch (Exception ex)
                            {
                                Form.MostrarMensaje($"Error al leer mensaje de {Nombre}: {ex.Message}\r\n"); // Muestra error
                                CerrarConexión(); // Cierra conexión
                                break;
                            }
                        }
                        Thread.Sleep(100); // Pausa breve
                    }
                }
                catch (Exception ex)
                {
                    Form.MostrarMensaje($"Error en cliente {Nombre}: {ex.Message}\r\n"); // Muestra error
                    CerrarConexión(); // Cierra conexión
                }
            }

            public void Escribir(string mensaje)
            {
                if (Conectado && Escritor != null) // Verifica conexión y escritor
                {
                    try
                    {
                        Escritor.Write(mensaje); // Envía mensaje
                    }
                    catch
                    {
                        // Maneja errores silenciosamente
                    }
                }
            }

            public void CerrarConexión()
            {
                Conectado = false; // Marca como desconectado
                Escritor?.Close(); // Cierra escritor
                Lector?.Close(); // Cierra lector
                Stream?.Close(); // Cierra flujo
                Socket?.Close(); // Cierra socket
                Form.DifundirMensaje($"SERVIDOR>>> {Nombre} desconectado.\r\n"); // Notifica desconexión
                if (Form.activeGames.TryGetValue(this, out TicTacToeForm game)) // Verifica juego activo
                {
                    Form.Invoke(new Action(() => game.Close())); // Cierra juego
                    Form.activeGames.Remove(this); // Elimina juego
                }
                Form.clientesConectados.Remove(this); // Elimina cliente
                if (Form.clientesConectados.Count == 0) // Si no hay clientes
                {
                    Form.Invoke(new MethodInvoker(delegate
                    {
                        Form.btnEnviarArchivoPorCliente.Enabled = false; // Desactiva botón
                    }));
                }
                Form.ActualizarListaIPs(); // Actualiza lista de IPs
            }
        }

        private void Escribir(string mensaje)
        {
            DifundirMensaje(mensaje); // Difunde mensaje a todos los clientes
        }

        private void btbEnviar_Click(object sender, EventArgs e)
        {
            // Método vacío, posiblemente reservado para futura implementación
        }

        private void btnInvitarJuego_Click_1(object sender, EventArgs e)
        {
            MostrarMensaje("Jugar clickeado a las " + DateTime.Now.ToString("HH:mm:ss") + ".\r\n"); // Muestra mensaje de depuración
            if (clientesListBox.SelectedIndex == -1 || clientesConectados.Count == 0) // Verifica selección
            {
                MessageBox.Show("Selecciona un cliente.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); // Muestra advertencia
                return;
            }
            var cliente = clientesConectados[clientesListBox.SelectedIndex]; // Obtiene cliente
            if (activeGames.ContainsKey(cliente)) // Verifica juego activo
            {
                MessageBox.Show("Juego en curso.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); // Muestra advertencia
                return;
            }
            try
            {
                cliente.Escribir("INVITACION_JUEGO>>>"); // Envía invitación
                MostrarMensaje($"Invitación enviada a {cliente.Nombre} a las " + DateTime.Now.ToString("HH:mm:ss") + ".\r\n"); // Muestra mensaje
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}"); // Muestra error
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

        private Dictionary<ClienteConectado, bool> llamadasActivas = new Dictionary<ClienteConectado, bool>(); // Llamadas activas por cliente
        private WaveInEvent waveInServidor; // Captura de audio
        private WaveOutEvent waveOutServidor; // Reproducción de audio
        private BufferedWaveProvider bufferServidor; // Buffer para audio

        private delegate void IniciarLlamadaDelegate(ClienteConectado cliente); // Delegado para iniciar llamada
        private delegate void TerminarLlamadaDelegate(ClienteConectado cliente); // Delegado para terminar llamada

        private void BtnLlamar_Click(object sender, EventArgs e)
        {
            if (clientesListBox.SelectedIndex == -1 || clientesConectados.Count == 0) // Verifica selección
            {
                MessageBox.Show("Selecciona un cliente de la lista para llamar.", "Sin selección", MessageBoxButtons.OK, MessageBoxIcon.Warning); // Muestra advertencia
                return;
            }

            int indice = clientesListBox.SelectedIndex; // Obtiene índice
            var cliente = clientesConectados[indice]; // Obtiene cliente

            if (cliente.Bloqueado) // Si cliente está bloqueado
            {
                MessageBox.Show($"El cliente {cliente.Nombre} está bloqueado y no puede recibir llamadas.", "Cliente Bloqueado", MessageBoxButtons.OK, MessageBoxIcon.Warning); // Muestra advertencia
                cliente.Escribir("SERVIDOR>>> Estás bloqueado. No puedes iniciar ni recibir llamadas.\r\n"); // Notifica bloqueo
                return;
            }

            if (llamadasActivas.ContainsKey(cliente) && llamadasActivas[cliente]) // Verifica llamada activa
            {
                MessageBox.Show("Ya hay una llamada en curso con este cliente.", "Llamada en curso", MessageBoxButtons.OK, MessageBoxIcon.Warning); // Muestra advertencia
                return;
            }

            try
            {
                cliente.Escribir("INVITACION_LLAMA>>>"); // Envía invitación
                MostrarMensaje($"SERVIDOR>>> Invitación de llamada enviada a {cliente.Nombre}\r\n"); // Muestra mensaje
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al enviar invitación: {ex.Message}"); // Muestra error
            }
        }

        private void BtnColgar_Click(object sender, EventArgs e)
        {
            if (clientesListBox.SelectedIndex == -1) return; // Verifica selección
            var cliente = clientesConectados[clientesListBox.SelectedIndex]; // Obtiene cliente
            TerminarLlamada(cliente); // Termina llamada
        }

        public void HandleAcceptCall(ClienteConectado cliente)
        {
            if (InvokeRequired) // Verifica si se necesita invocar en hilo UI
            {
                Invoke(new IniciarLlamadaDelegate(HandleAcceptCall), cliente); // Invoca en hilo UI
                return;
            }

            MostrarMensaje($"SERVIDOR>>> {cliente.Nombre} aceptó la llamada.\r\n"); // Muestra mensaje
            IniciarLlamada(cliente); // Inicia llamada
        }

        public void HandleRejectCall(ClienteConectado cliente)
        {
            MostrarMensaje($"SERVIDOR>>> {cliente.Nombre} rechazó la llamada.\r\n"); // Muestra mensaje
        }

        public void HandleAudioFromClient(ClienteConectado cliente, byte[] audioData)
        {
            if (llamadasActivas.ContainsKey(cliente) && llamadasActivas[cliente] && bufferServidor != null) // Verifica llamada activa
            {
                bufferServidor.AddSamples(audioData, 0, audioData.Length); // Añade audio al buffer
            }
        }

        private void IniciarLlamada(ClienteConectado cliente)
        {
            llamadasActivas[cliente] = true; // Marca llamada como activa
            btnColgar.Enabled = true; // Habilita botón de colgar

            waveInServidor = new WaveInEvent { WaveFormat = new WaveFormat(8000, 16, 1) }; // Configura captura
            waveInServidor.DataAvailable += (s, args) =>
            {
                if (llamadasActivas[cliente]) // Verifica llamada activa
                {
                    cliente.Escribir("AUDIO_INICIO>>>"); // Envía inicio de audio
                    cliente.Escritor.Write(args.BytesRecorded); // Envía tamaño
                    cliente.Stream.Write(args.Buffer, 0, args.BytesRecorded); // Envía datos
                    cliente.Stream.Flush(); // Limpia buffer
                }
            };
            waveInServidor.StartRecording(); // Inicia captura

            bufferServidor = new BufferedWaveProvider(waveInServidor.WaveFormat); // Crea buffer
            waveOutServidor = new WaveOutEvent(); // Crea reproductor
            waveOutServidor.Init(bufferServidor); // Inicializa reproductor
            waveOutServidor.Play(); // Inicia reproducción
        }

        private void TerminarLlamada(ClienteConectado cliente)
        {
            if (!llamadasActivas.ContainsKey(cliente) || !llamadasActivas[cliente]) return; // Verifica llamada activa

            llamadasActivas[cliente] = false; // Marca llamada como terminada
            cliente.Escribir("TERMINAR_LLAMA>>>"); // Envía mensaje de fin
            MostrarMensaje($"SERVIDOR>>> Llamada terminada con {cliente.Nombre}\r\n"); // Muestra mensaje

            waveInServidor?.StopRecording(); // Detiene captura
            waveInServidor?.Dispose(); // Libera captura
            waveOutServidor?.Stop(); // Detiene reproducción
            waveOutServidor?.Dispose(); // Libera reproducción
            bufferServidor?.ClearBuffer(); // Limpia buffer

            btnColgar.Enabled = false; // Desactiva botón
        }
    }
}