using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Estacionamento.Api.Models;

[ApiController]
[Route("api/veiculos")]
public class VeiculosController : ControllerBase
{
    private static readonly string DbPath = Path.Combine(AppContext.BaseDirectory, "database", "estacionamento.db");
    private static readonly string Conn = $"Data Source={DbPath}";

    [HttpPost]
    public IActionResult Adicionar([FromBody] Veiculo veiculo)
    {
        if (veiculo is null || string.IsNullOrWhiteSpace(veiculo.Placa))
            return BadRequest("Placa é obrigatória.");

        var placa = veiculo.Placa.Trim().ToUpper();

        using var conn = new SqliteConnection(Conn);
        conn.Open();

        using (var checkCmd = conn.CreateCommand())
        {
            checkCmd.CommandText = "SELECT COUNT(1) FROM Veiculos WHERE Placa = $placa";
            checkCmd.Parameters.AddWithValue("$placa", placa);
            var exists = Convert.ToInt32(checkCmd.ExecuteScalar() ?? 0);
            if (exists > 0)
                return Conflict("Esta placa já está cadastrada.");
        }

        try
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"INSERT INTO Veiculos (Placa, DataEntrada)
              VALUES ($placa, $data)";

            cmd.Parameters.AddWithValue("$placa", placa);
            cmd.Parameters.AddWithValue("$data", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            cmd.ExecuteNonQuery();
            return Ok("Veículo cadastrado com sucesso.");
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            return Conflict("Esta placa já está cadastrada.");
        }
    }

    [HttpGet]
    public IActionResult Listar()
    {
        var lista = new List<Veiculo>();

        using var conn = new SqliteConnection(Conn);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Veiculos";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(new Veiculo
            {
                Id = reader.GetInt32(0),
                Placa = reader.GetString(1),
                DataEntrada = DateTime.Parse(reader.GetString(2))
            });
        }

        return Ok(lista);
    }

    [HttpDelete("{placa}")]
    public IActionResult Remover(string placa, [FromQuery] int horas = 0, [FromQuery] int minutos = 0, [FromQuery] string modo = "duracao")
    {
        if (string.IsNullOrWhiteSpace(placa))
            return BadRequest("Placa inválida.");

        placa = placa.Trim().ToUpper();

        using var conn = new SqliteConnection(Conn);
        conn.Open();

        var selCmd = conn.CreateCommand();
        selCmd.CommandText = "SELECT Id, DataEntrada FROM Veiculos WHERE Placa = $placa";
        selCmd.Parameters.AddWithValue("$placa", placa);

        int veiculoId;
        DateTime dataEntrada;

        using (var reader = selCmd.ExecuteReader())
        {
            if (!reader.Read())
                return NotFound();

            veiculoId = reader.GetInt32(0);
            dataEntrada = DateTime.Parse(reader.GetString(1));
        }

        DateTime saida;

        if (!string.IsNullOrWhiteSpace(modo) && modo.Equals("absoluto", StringComparison.OrdinalIgnoreCase))
        {
            // Interpreta horas/minutos como hora do dia (ex.: 21:30)
            if (horas < 0 || horas > 23 || minutos < 0 || minutos > 59)
                return BadRequest("Hora de saída inválida. Horas 0-23 e minutos 0-59.");

            saida = dataEntrada.Date.AddHours(horas).AddMinutes(minutos);

            // se a hora informada ficar antes da entrada, assume o dia seguinte
            if (saida < dataEntrada)
                saida = saida.AddDays(1);
        }
        else
        {
            // Interpreta como duração (comportamento anterior)
            saida = dataEntrada.AddHours(horas).AddMinutes(minutos);
            if (saida < dataEntrada)
                saida = dataEntrada;
        }

        // Busca tabela de preços usando DataEntrada (regra existente)
        var priceCmd = conn.CreateCommand();
        priceCmd.CommandText = @"
        SELECT ValorHoraInicial, ValorHoraAdicional, DataInicio, DataFim
        FROM Precos
        WHERE datetime($entrada) BETWEEN datetime(DataInicio) AND datetime(DataFim)
        ORDER BY DataInicio DESC
        LIMIT 1
    ";
        priceCmd.Parameters.AddWithValue("$entrada", dataEntrada.ToString("yyyy-MM-dd HH:mm:ss"));

        double valorInicial = 0;
        double valorAdicional = 0;
        bool priceFound = false;

        using (var r = priceCmd.ExecuteReader())
        {
            if (r.Read())
            {
                valorInicial = r.GetDouble(0);
                valorAdicional = r.GetDouble(1);
                priceFound = true;
            }
        }

        if (!priceFound)
        {
            var fallbackCmd = conn.CreateCommand();
            fallbackCmd.CommandText = @"
            SELECT ValorHoraInicial, ValorHoraAdicional, DataInicio
            FROM Precos
            WHERE datetime(DataInicio) <= datetime($entrada)
            ORDER BY DataInicio DESC
            LIMIT 1
        ";
            fallbackCmd.Parameters.AddWithValue("$entrada", dataEntrada.ToString("yyyy-MM-dd HH:mm:ss"));
            using var r2 = fallbackCmd.ExecuteReader();
            if (r2.Read())
            {
                valorInicial = r2.GetDouble(0);
                valorAdicional = r2.GetDouble(1);
                priceFound = true;
            }
        }

        if (!priceFound)
            return Problem("Tabela de preços não encontrada para a data de entrada do veículo.");

        var totalMinutes = Math.Ceiling((saida - dataEntrada).TotalMinutes);
        if (totalMinutes < 0) totalMinutes = 0;

        decimal valorCobrado;
        var vInicial = Convert.ToDecimal(valorInicial);
        var vAdicional = Convert.ToDecimal(valorAdicional);

        if (totalMinutes <= 30)
        {
            valorCobrado = vInicial / 2m;
        }
        else if (totalMinutes <= 60)
        {
            valorCobrado = vInicial;
        }
        else
        {
            var remaining = totalMinutes - 60;
            var extraToConsider = Math.Max(0, remaining - 10);
            var adicionais = (int)Math.Ceiling(extraToConsider / 60.0);
            valorCobrado = vInicial + adicionais * vAdicional;
        }

        var delCmd = conn.CreateCommand();
        delCmd.CommandText = "DELETE FROM Veiculos WHERE Id = $id";
        delCmd.Parameters.AddWithValue("$id", veiculoId);
        delCmd.ExecuteNonQuery();

        return Ok(new
        {
            Placa = placa,
            DataEntrada = dataEntrada,
            DataSaida = saida,
            Minutos = totalMinutes,
            Valor = Math.Round(valorCobrado, 2)
        });
    }
}