namespace TechFlow.Eventos.Core.Imagens;

/// <summary>Gera imagens PNG para a demonstração (produtos e eventos), sem arquivos externos.</summary>
public static class ImagemDemo
{
    /// <summary>Degradê com um círculo claro, usado nos produtos. Cores no formato RRGGBB.</summary>
    public static byte[] Gerar(string corTopo, string corBase, int largura = 480, int altura = 360)
    {
        var (r1, g1, b1) = Tela.Rgb(corTopo);
        var (r2, g2, b2) = Tela.Rgb(corBase);
        double cx = largura * 0.5, cy = altura * 0.48, raio = altura * 0.30;

        var rgb = new byte[largura * altura * 3];
        var i = 0;
        for (var y = 0; y < altura; y++)
        {
            var t = (double)y / (altura - 1);
            for (var x = 0; x < largura; x++)
            {
                var r = r1 + (r2 - r1) * t;
                var g = g1 + (g2 - g1) * t;
                var b = b1 + (b2 - b1) * t;

                var d = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                if (d < raio)
                {
                    var luz = 0.22 * (1 - d / raio) + 0.10; // círculo claro, mais forte no centro
                    r += (255 - r) * luz;
                    g += (255 - g) * luz;
                    b += (255 - b) * luz;
                }

                rgb[i++] = (byte)Math.Clamp(r, 0, 255);
                rgb[i++] = (byte)Math.Clamp(g, 0, 255);
                rgb[i++] = (byte)Math.Clamp(b, 0, 255);
            }
        }

        return Png.Codificar(largura, altura, rgb);
    }

    /// <summary>Ilustração temática de um evento (800 x 450). Temas: tambor, ondas, cafe, casarao, feira, quadro, viola, cafezal, estrela, microfone.</summary>
    public static byte[] GerarEvento(string tema, string corTopo, string corBase) =>
        CenasEvento.Desenhar(tema, corTopo, corBase).ToPng();
}
