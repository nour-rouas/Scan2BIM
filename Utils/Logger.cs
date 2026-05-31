using System;
using System.Collections.Generic;
using System.IO;

namespace Metrika.Utils
{
    public static class Logger
    {
        // Lazy: la ruta y el buffer solo se crean la primera vez que se llama a Log()
        private static string _logFilePath;
        private static readonly List<string> _buffer = new List<string>(32);

        private static string LogFilePath
        {
            get
            {
                if (_logFilePath == null)
                    _logFilePath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "Scan2BIM_Log.txt");
                return _logFilePath;
            }
        }

        /// <summary>
        /// Acumula el mensaje en memoria. Llama a Flush() al final de cada comando
        /// para escribir todo de una vez (un solo acceso a disco en lugar de uno por línea).
        /// </summary>
        public static void Log(string message)
        {
            _buffer.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        public static void LogError(Exception ex)
        {
            Log($"ERROR: {ex.Message}");
            if (ex.InnerException != null)
                Log($"  Inner: {ex.InnerException.Message}");
        }

        /// <summary>
        /// Escribe el buffer al disco de una sola vez y lo limpia.
        /// Llamar al final de cada IExternalCommand.Execute().
        /// </summary>
        public static void Flush()
        {
            if (_buffer.Count == 0) return;
            try
            {
                File.AppendAllLines(LogFilePath, _buffer);
            }
            catch { }
            finally
            {
                _buffer.Clear();
            }
        }

        /// <summary>
        /// Obtiene la ruta del archivo de log
        /// </summary>
        public static string GetLogPath()
        {
            return LogFilePath;
        }
    }
}
