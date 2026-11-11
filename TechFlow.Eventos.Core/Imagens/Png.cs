using System.IO.Compression;

namespace TechFlow.Eventos.Core.Imagens;

/// <summary>Codificador PNG mínimo (RGB de 8 bits, sem dependências).</summary>
internal static class Png
{
    private static readonly byte[] Assinatura = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    public static byte[] Codificar(int largura, int altura, byte[] rgb)
    {
        using var bruto = new MemoryStream(altura * (largura * 3 + 1));
        for (var y = 0; y < altura; y++)
        {
            bruto.WriteByte(0); // filtro "nenhum" desta linha
            bruto.Write(rgb, y * largura * 3, largura * 3);
        }

        using var comprimido = new MemoryStream();
        using (var z = new ZLibStream(comprimido, CompressionLevel.Optimal, leaveOpen: true))
        {
            bruto.Position = 0;
            bruto.CopyTo(z);
        }

        using var png = new MemoryStream();
        png.Write(Assinatura);

        var cabecalho = new byte[13];
        Inteiro(largura, cabecalho, 0);
        Inteiro(altura, cabecalho, 4);
        cabecalho[8] = 8; // 8 bits por canal
        cabecalho[9] = 2; // RGB
        Bloco(png, "IHDR", cabecalho);
        Bloco(png, "IDAT", comprimido.ToArray());
        Bloco(png, "IEND", Array.Empty<byte>());
        return png.ToArray();
    }

    private static void Inteiro(int valor, byte[] destino, int posicao)
    {
        destino[posicao] = (byte)(valor >> 24);
        destino[posicao + 1] = (byte)(valor >> 16);
        destino[posicao + 2] = (byte)(valor >> 8);
        destino[posicao + 3] = (byte)valor;
    }

    private static void Bloco(Stream s, string tipo, byte[] dados)
    {
        var tamanho = new byte[4];
        Inteiro(dados.Length, tamanho, 0);
        s.Write(tamanho);

        var corpo = new byte[4 + dados.Length];
        for (var i = 0; i < 4; i++) corpo[i] = (byte)tipo[i];
        dados.CopyTo(corpo, 4);
        s.Write(corpo);

        var crc = new byte[4];
        Inteiro(unchecked((int)Crc32(corpo)), crc, 0);
        s.Write(crc);
    }

    private static readonly uint[] TabelaCrc = Enumerable.Range(0, 256).Select(n =>
    {
        var c = (uint)n;
        for (var k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
        return c;
    }).ToArray();

    private static uint Crc32(byte[] dados)
    {
        var c = 0xFFFFFFFFu;
        foreach (var b in dados) c = TabelaCrc[(c ^ b) & 0xFF] ^ (c >> 8);
        return c ^ 0xFFFFFFFFu;
    }
}
