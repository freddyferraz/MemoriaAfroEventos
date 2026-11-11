namespace TechFlow.Eventos.Core.Imagens;

/// <summary>
/// Mini rasterizador para desenhar as ilustrações dos eventos. Desenha em dobro de resolução e reduz ao final,
/// o que suaviza as bordas das formas. Coordenadas em pixels da imagem final.
/// </summary>
internal sealed class Tela
{
    private const int SS = 2;
    private readonly int _largura, _altura, _L, _A;
    private readonly byte[] _px;

    public Tela(int largura, int altura)
    {
        _largura = largura;
        _altura = altura;
        _L = largura * SS;
        _A = altura * SS;
        _px = new byte[_L * _A * 3];
    }

    public static (double R, double G, double B) Rgb(string hex) =>
        (Convert.ToInt32(hex[..2], 16), Convert.ToInt32(hex.Substring(2, 2), 16), Convert.ToInt32(hex.Substring(4, 2), 16));

    public static string Misturar(string a, string b, double t)
    {
        var (r1, g1, b1) = Rgb(a);
        var (r2, g2, b2) = Rgb(b);
        string C(double x, double y) => ((int)Math.Round(x + (y - x) * t)).ToString("X2");
        return C(r1, r2) + C(g1, g2) + C(b1, b2);
    }

    public void Degrade(string topo, string baseCor)
    {
        for (var y = 0; y < _A; y++)
        {
            var cor = Misturar(topo, baseCor, (double)y / (_A - 1));
            var (r, g, b) = Rgb(cor);
            for (var x = 0; x < _L; x++)
            {
                var i = (y * _L + x) * 3;
                _px[i] = (byte)r; _px[i + 1] = (byte)g; _px[i + 2] = (byte)b;
            }
        }
    }

    private void Preencher(double x0, double y0, double x1, double y1, Func<double, double, bool> dentro, string cor, double opacidade)
    {
        var (r, g, b) = Rgb(cor);
        var xi = Math.Max(0, (int)Math.Floor(x0 * SS));
        var xf = Math.Min(_L - 1, (int)Math.Ceiling(x1 * SS));
        var yi = Math.Max(0, (int)Math.Floor(y0 * SS));
        var yf = Math.Min(_A - 1, (int)Math.Ceiling(y1 * SS));
        for (var y = yi; y <= yf; y++)
        {
            for (var x = xi; x <= xf; x++)
            {
                if (!dentro((x + 0.5) / SS, (y + 0.5) / SS)) continue;
                var i = (y * _L + x) * 3;
                _px[i] = (byte)(_px[i] + (r - _px[i]) * opacidade);
                _px[i + 1] = (byte)(_px[i + 1] + (g - _px[i + 1]) * opacidade);
                _px[i + 2] = (byte)(_px[i + 2] + (b - _px[i + 2]) * opacidade);
            }
        }
    }

    public void Retangulo(double x, double y, double l, double a, string cor, double op = 1) =>
        Preencher(x, y, x + l, y + a, (_, _) => true, cor, op);

    public void Circulo(double cx, double cy, double r, string cor, double op = 1) => Elipse(cx, cy, r, r, cor, op);

    public void Elipse(double cx, double cy, double rx, double ry, string cor, double op = 1) =>
        Preencher(cx - rx, cy - ry, cx + rx, cy + ry,
            (x, y) => ((x - cx) * (x - cx)) / (rx * rx) + ((y - cy) * (y - cy)) / (ry * ry) <= 1, cor, op);

    public void Anel(double cx, double cy, double rExt, double rInt, string cor, double op = 1) =>
        Preencher(cx - rExt, cy - rExt, cx + rExt, cy + rExt, (x, y) =>
        {
            var d2 = (x - cx) * (x - cx) + (y - cy) * (y - cy);
            return d2 <= rExt * rExt && d2 >= rInt * rInt;
        }, cor, op);

    public void Poligono(double[] xs, double[] ys, string cor, double op = 1)
    {
        bool Dentro(double px, double py)
        {
            var dentro = false;
            for (int i = 0, j = xs.Length - 1; i < xs.Length; j = i++)
            {
                if ((ys[i] > py) != (ys[j] > py) && px < (xs[j] - xs[i]) * (py - ys[i]) / (ys[j] - ys[i]) + xs[i]) dentro = !dentro;
            }

            return dentro;
        }

        Preencher(xs.Min(), ys.Min(), xs.Max(), ys.Max(), Dentro, cor, op);
    }

    public void Linha(double x1, double y1, double x2, double y2, double espessura, string cor, double op = 1)
    {
        var dx = x2 - x1;
        var dy = y2 - y1;
        var comp = Math.Sqrt(dx * dx + dy * dy);
        if (comp < 0.001) return;
        double nx = -dy / comp * espessura / 2, ny = dx / comp * espessura / 2;
        Poligono(new[] { x1 + nx, x2 + nx, x2 - nx, x1 - nx }, new[] { y1 + ny, y2 + ny, y2 - ny, y1 - ny }, cor, op);
    }

    /// <summary>Reduz para a resolução final (média de 2 x 2 pixels) e codifica em PNG.</summary>
    public byte[] ToPng()
    {
        var saida = new byte[_largura * _altura * 3];
        for (var y = 0; y < _altura; y++)
        {
            for (var x = 0; x < _largura; x++)
            {
                for (var c = 0; c < 3; c++)
                {
                    var soma = 0;
                    for (var dy = 0; dy < SS; dy++)
                        for (var dx = 0; dx < SS; dx++)
                            soma += _px[(((y * SS) + dy) * _L + (x * SS) + dx) * 3 + c];
                    saida[(y * _largura + x) * 3 + c] = (byte)(soma / (SS * SS));
                }
            }
        }

        return Png.Codificar(_largura, _altura, saida);
    }
}
