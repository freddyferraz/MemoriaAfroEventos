using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace TechFlow.Eventos.Core.Pagamentos;

public record OpcaoParcela(int Parcelas, decimal ValorParcela);

/// <summary>Parcelamento no cartão de crédito: até 12x sem juros, com parcela mínima de R$ 5,00.</summary>
public static class Parcelamento
{
    public const decimal ParcelaMinima = 5m;
    public const int MaximoParcelas = 12;

    public static int Maximo(decimal valor) => (int)Math.Clamp(Math.Floor(valor / ParcelaMinima), 1, MaximoParcelas);

    public static IReadOnlyList<OpcaoParcela> Opcoes(decimal valor) =>
        Enumerable.Range(1, Maximo(valor)).Select(n => new OpcaoParcela(n, Math.Round(valor / n, 2, MidpointRounding.AwayFromZero))).ToList();
}

/// <summary>
/// Códigos FICTÍCIOS de Pix e boleto, só para a apresentação. O Pix segue a estrutura do BR Code (com o CRC correto), mas a chave
/// não existe em nenhum banco; o boleto usa o código de banco 999 e não pode ser pago em lugar algum.
/// </summary>
public static class SimulacaoPagamento
{
    private static readonly DateOnly BaseFatorVencimento = new(1997, 10, 7);

    // Desde 22/02/2025 o fator de vencimento recomeça em 1000.
    private static readonly DateOnly NovaBaseFator = new(2025, 2, 22);

    public static string CodigoPix(Pagamento p)
    {
        var chaveHex = (p.Referencia + "00000000").PadRight(32, '0')[..32];
        var chave = $"{chaveHex[..8]}-{chaveHex.Substring(8, 4)}-{chaveHex.Substring(12, 4)}-{chaveHex.Substring(16, 4)}-{chaveHex.Substring(20, 12)}";
        var txid = new string(p.Referencia.ToUpperInvariant().Where(char.IsLetterOrDigit).Take(20).ToArray());

        var corpo =
            Campo("00", "01") +
            Campo("26", Campo("00", "br.gov.bcb.pix") + Campo("01", chave)) +
            Campo("52", "0000") +
            Campo("53", "986") +
            Campo("54", p.Valor.ToString("0.00", CultureInfo.InvariantCulture)) +
            Campo("58", "BR") +
            Campo("59", "CONECTA CULTURA DEMO") +
            Campo("60", "SAO PAULO") +
            Campo("62", Campo("05", txid)) +
            "6304";
        return corpo + Crc16(corpo);
    }

    /// <summary>Linha digitável de 47 dígitos no formato de boleto bancário (fictícia).</summary>
    public static string LinhaDigitavel(Pagamento p)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(p.Referencia));
        var digitos = string.Concat(hash.Select(b => (b % 10).ToString(CultureInfo.InvariantCulture)));
        var posicao = 0;
        string Pegar(int n) { var s = digitos.Substring(posicao, n); posicao += n; return s; }

        var campo1 = "9999" + Pegar(5) + Pegar(1);
        var campo2 = Pegar(10) + Pegar(1);
        var campo3 = Pegar(10) + Pegar(1);
        var dv = Pegar(1);

        var vencimento = DateOnly.FromDateTime((p.VenceEm ?? DateTime.UtcNow.AddDays(3)).AddHours(-3));
        var dias = vencimento >= NovaBaseFator ? 1000 + (vencimento.DayNumber - NovaBaseFator.DayNumber) : vencimento.DayNumber - BaseFatorVencimento.DayNumber;
        var fator = Math.Clamp(dias, 0, 9999).ToString("0000", CultureInfo.InvariantCulture);
        var centavos = ((long)Math.Round(p.Valor * 100, MidpointRounding.AwayFromZero)).ToString("0000000000", CultureInfo.InvariantCulture);

        return $"{campo1[..5]}.{campo1[5..]} {campo2[..5]}.{campo2[5..]} {campo3[..5]}.{campo3[5..]} {dv} {fator}{centavos}";
    }

    private static string Campo(string id, string valor) => id + valor.Length.ToString("00", CultureInfo.InvariantCulture) + valor;

    /// <summary>CRC-16/CCITT-FALSE (polinômio 0x1021, valor inicial 0xFFFF), exigido ao final do BR Code.</summary>
    public static string Crc16(string texto)
    {
        ushort crc = 0xFFFF;
        foreach (var b in Encoding.ASCII.GetBytes(texto))
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++) crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
        }

        return crc.ToString("X4", CultureInfo.InvariantCulture);
    }
}
