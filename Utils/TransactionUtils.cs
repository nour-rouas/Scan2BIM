using System;
using Autodesk.Revit.DB;

namespace Metrika.Utils
{
    /// <summary>
    /// Utilidades para manejo de transacciones
    /// </summary>
    public static class TransactionUtils
    {
        /// <summary>
        /// Ejecuta una acción dentro de una transacción con manejo automático de errores
        /// </summary>
        public static T ExecuteInTransaction<T>(
            Document doc,
            string transactionName,
            Func<T> action)
        {
            using (Transaction trans = new Transaction(doc, transactionName))
            {
                trans.Start();
                try
                {
                    T result = action();
                    trans.Commit();
                    return result;
                }
                catch
                {
                    trans.RollBack();
                    throw;
                }
            }
        }

        /// <summary>
        /// Ejecuta una acción dentro de una transacción (sin retorno)
        /// </summary>
        public static void ExecuteInTransaction(
            Document doc,
            string transactionName,
            Action action)
        {
            using (Transaction trans = new Transaction(doc, transactionName))
            {
                trans.Start();
                try
                {
                    action();
                    trans.Commit();
                }
                catch
                {
                    trans.RollBack();
                    throw;
                }
            }
        }
    }
}
