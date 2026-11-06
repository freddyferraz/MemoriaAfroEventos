namespace TechFlow.Eventos.Core.Imagens;

public static class TipoImagem
{
    /// <summary>Tamanho máximo aceito para a foto de um evento.</summary>
    public const int TamanhoMaximoBytes = 2 * 1024 * 1024;

    /// <summary>
    /// Identifica o formato pelos primeiros bytes (assinatura do arquivo). Retorna o content-type
    /// ou null se não for JPEG, PNG ou WebP. Não confia no nome nem no Content-Type enviados pelo cliente.
    /// </summary>
    public static string? Detectar(ReadOnlySpan<byte> b)
    {
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return "image/jpeg";

        if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47
            && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A) return "image/png";

        // WebP: "RIFF" + tamanho (4 bytes) + "WEBP"
        if (b.Length >= 12 && b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46
            && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50) return "image/webp";

        return null;
    }
}
