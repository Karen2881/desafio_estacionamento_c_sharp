using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace Estacionamento.Api.Data
{
    public static class DatabaseInitializer
    {
        public static void Inicializar()
        {
            try
            {
                var currentDir = Directory.GetCurrentDirectory();
                var baseDir = AppContext.BaseDirectory;
                var dbDir = Path.Combine(baseDir, "database");
                Directory.CreateDirectory(dbDir);
                var dbPath = Path.Combine(dbDir, "estacionamento.db");

                Console.WriteLine($"[DB] CurrentDirectory: {currentDir}");
                Console.WriteLine($"[DB] AppContext.BaseDirectory: {baseDir}");
                Console.WriteLine($"[DB] Using DB path: {dbPath}");

                var connectionString = $"Data Source={dbPath}";
                using var conn = new SqliteConnection(connectionString);
                conn.Open();

                // Tabela Veiculos
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Veiculos (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Placa TEXT NOT NULL UNIQUE,
                        DataEntrada TEXT NOT NULL
                    );
                ";
                cmd.ExecuteNonQuery();


                var cmd2 = conn.CreateCommand();
                cmd2.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Precos (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        DataInicio TEXT NOT NULL,
                        DataFim TEXT NOT NULL,
                        ValorHoraInicial REAL NOT NULL,
                        ValorHoraAdicional REAL NOT NULL
                    );
                ";
                cmd2.ExecuteNonQuery();


                var start = new DateTime(DateTime.Now.Year, 1, 1).ToString("yyyy-MM-dd");
                var end = new DateTime(2026, 12, 31).ToString("yyyy-MM-dd");

                var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = "SELECT COUNT(1) FROM Precos WHERE DataInicio = $start AND DataFim = $end";
                checkCmd.Parameters.AddWithValue("$start", start);
                checkCmd.Parameters.AddWithValue("$end", end);
                var exists = Convert.ToInt32(checkCmd.ExecuteScalar() ?? 0);

                if (exists == 0)
                {
                    var insertCmd = conn.CreateCommand();
                    insertCmd.CommandText = @"
                        INSERT INTO Precos (DataInicio, DataFim, ValorHoraInicial, ValorHoraAdicional)
                        VALUES ($start, $end, $valorInicial, $valorAdicional)
                    ";
                    insertCmd.Parameters.AddWithValue("$start", start);
                    insertCmd.Parameters.AddWithValue("$end", end);

                    insertCmd.Parameters.AddWithValue("$valorInicial", 2.0);
                    insertCmd.Parameters.AddWithValue("$valorAdicional", 1.0);
                    insertCmd.ExecuteNonQuery();

                    Console.WriteLine("[DB] Inserido preço exemplo (hora inicial 2.00, hora adicional 1.00).");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DB] Inicializar falhou: {ex.GetType()}: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
                throw;
            }
        }
    }
}