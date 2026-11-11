namespace TechFlow.Eventos.Core.Imagens;

/// <summary>Cenas ilustradas para os eventos de demonstração: formas simples e cores quentes, no mesmo estilo.</summary>
internal static class CenasEvento
{
    public const int Largura = 800;
    public const int Altura = 450;

    public static Tela Desenhar(string tema, string topo, string baseCor)
    {
        var t = new Tela(Largura, Altura);
        switch (tema)
        {
            case "tambor": Tambor(t, topo, baseCor); break;
            case "ondas": Ondas(t, topo, baseCor); break;
            case "cafe": Cafe(t, topo, baseCor); break;
            case "casarao": Casarao(t); break;
            case "feira": Feira(t, topo, baseCor); break;
            case "quadro": Quadro(t); break;
            case "viola": Viola(t, topo, baseCor); break;
            case "cafezal": Cafezal(t); break;
            case "estrela": Estrela(t, topo, baseCor); break;
            case "microfone": Microfone(t, topo, baseCor); break;
            default: Paisagem(t, topo, baseCor); break;
        }

        return t;
    }

    private static void Tambor(Tela t, string topo, string baseCor)
    {
        t.Degrade(topo, baseCor);
        t.Circulo(650, 105, 72, "FFE3A3", 0.5);
        t.Circulo(650, 105, 46, "FFF3CF", 0.8);
        for (var i = 0; i < 12; i++) t.Circulo(55 + i * 62, 55 + (i % 3) * 14, 5, "FFE3A3", 0.5);
        t.Retangulo(0, 376, 800, 74, "2E1408", 0.5);

        void Pequeno(double cx)
        {
            t.Poligono(new[] { cx - 62, cx + 62, cx + 50, cx - 50 }, new[] { 300.0, 300, 376, 376 }, "B8682E");
            t.Elipse(cx, 376, 50, 11, "8F4C20");
            t.Elipse(cx, 300, 64, 16, "6B3615");
            t.Elipse(cx, 300, 58, 13, "EBD39C");
        }

        Pequeno(125);
        Pequeno(675);

        t.Poligono(new[] { 248.0, 552, 524, 276 }, new[] { 204.0, 204, 378, 378 }, "9A5226");
        t.Elipse(400, 378, 124, 26, "7A3F1B");
        for (var i = 0; i < 6; i++)
        {
            var x = 262 + i * 50;
            t.Linha(x, 222, x + 25, 372, 5, "F0D9A2");
            t.Linha(x + 50, 222, x + 25, 372, 5, "F0D9A2");
        }

        t.Elipse(400, 206, 154, 42, "6B3615");
        t.Elipse(400, 202, 148, 38, "E9CE94");
        t.Elipse(400, 202, 136, 31, "F5E3B6");
    }

    private static void Ondas(Tela t, string topo, string baseCor)
    {
        t.Degrade(topo, baseCor);
        t.Circulo(630, 92, 54, "F6EBCB", 0.9);
        t.Circulo(650, 82, 46, Tela.Misturar(topo, baseCor, 0.25), 0.9);
        for (var i = 0; i < 28; i++)
        {
            var x = 48 + i * 26;
            var h = 50 + 170 * Math.Abs(Math.Sin(i * 0.5 + 0.7)) * (0.55 + 0.45 * Math.Cos(i * 0.19));
            var cor = Tela.Misturar("7FD1FF", "FF9ECF", i / 27.0);
            t.Retangulo(x, 380 - h, 16, h, cor);
            t.Circulo(x + 8, 380 - h, 8, cor);
            t.Circulo(x + 8, 380, 8, cor);
            t.Retangulo(x, 394, 16, h * 0.3, cor, 0.2);
        }

        t.Retangulo(0, 388, 800, 3, "FFFFFF", 0.25);
    }

    private static void Cafe(Tela t, string topo, string baseCor)
    {
        t.Degrade(topo, baseCor);
        t.Retangulo(0, 346, 800, 104, "2A170B");
        for (var k = 0; k < 3; k++)
            for (var j = 0; j < 7; j++)
                t.Circulo(335 + k * 65 + Math.Sin(j * 1.1 + k) * 14, 172 - j * 17, 9 + j * 1.2, "FFFFFF", 0.16);
        t.Elipse(400, 352, 204, 34, "E2D6C2");
        t.Elipse(400, 346, 184, 28, "FFFFFF");
        t.Anel(522, 286, 42, 25, "FFFFFF");
        t.Poligono(new[] { 280.0, 520, 492, 308 }, new[] { 240.0, 240, 340, 340 }, "FFFFFF");
        t.Elipse(400, 340, 92, 17, "F1EBE0");
        t.Elipse(400, 240, 120, 27, "F1EBE0");
        t.Elipse(400, 240, 108, 22, "4A2511");
        t.Elipse(400, 236, 70, 9, "6E3C1D", 0.7);
        (double X, double Y)[] graos = { (120, 392), (195, 418), (640, 396), (705, 376), (560, 420), (255, 400) };
        foreach (var (x, y) in graos)
        {
            t.Elipse(x, y, 23, 13, "5B341B");
            t.Linha(x - 16, y, x + 16, y, 2.5, "2E1A0D");
        }
    }

    private static void Casarao(Tela t)
    {
        t.Degrade("A8D8F0", "F7E9CA");
        t.Circulo(130, 95, 50, "FFE48A");
        t.Elipse(180, 480, 440, 150, "86B35E");
        t.Elipse(660, 490, 450, 165, "5F9645");
        t.Retangulo(455, 150, 28, 70, "8A3626");
        t.Retangulo(240, 235, 320, 150, "F4E7C6");
        t.Retangulo(232, 372, 336, 16, "B6A27A");
        t.Poligono(new[] { 216.0, 400, 584 }, new[] { 242.0, 128, 242 }, "A04230");
        t.Retangulo(380, 292, 42, 80, "6B3A1F");
        t.Circulo(414, 334, 3, "E8C468");
        foreach (var x in new[] { 272, 328, 456, 512 })
        {
            t.Retangulo(x, 262, 36, 50, "5E7B94");
            t.Retangulo(x + 16, 262, 4, 50, "F4E7C6");
            t.Retangulo(x, 284, 36, 4, "F4E7C6");
        }

        foreach (var x in new[] { 258, 318, 478, 536 }) t.Retangulo(x, 326, 8, 48, "FFFFFF");
        t.Retangulo(90, 300, 16, 84, "6B4423");
        t.Circulo(98, 285, 50, "4E8A3C");
        t.Circulo(68, 308, 32, "5C9A48");
        t.Retangulo(690, 312, 14, 72, "6B4423");
        t.Circulo(697, 296, 44, "4E8A3C");
    }

    private static void Feira(Tela t, string topo, string baseCor)
    {
        t.Degrade(topo, baseCor);
        for (var i = 0; i < 10; i++)
        {
            var x = 90 + i * 62;
            t.Poligono(new[] { (double)x, x + 62, x + 70, x - 8 }, new[] { 150.0, 150, 214, 214 }, i % 2 == 0 ? "E63946" : "FFFFFF");
        }

        t.Retangulo(70, 140, 660, 12, "7A4A24");
        t.Retangulo(100, 214, 10, 168, "7A4A24");
        t.Retangulo(690, 214, 10, 168, "7A4A24");
        t.Retangulo(90, 302, 620, 36, "8D5524");
        t.Retangulo(90, 338, 620, 10, "5E3817");
        var frutas = new[] { "F77F00", "D62828", "57A773", "FFD166" };
        for (var i = 0; i < 18; i++) t.Circulo(130 + i * 31, 292 + (i % 2) * 4, 15, frutas[i % 4]);
        foreach (var x in new[] { 120, 320, 520 })
        {
            t.Retangulo(x, 350, 160, 50, "A9743A");
            t.Retangulo(x, 366, 160, 4, "7A4F22");
            t.Retangulo(x, 382, 160, 4, "7A4F22");
        }

        t.Retangulo(0, 404, 800, 46, "4A2F16", 0.45);
    }

    private static void Quadro(Tela t)
    {
        t.Degrade("EDE8DF", "CDC5B6");
        t.Retangulo(0, 394, 800, 56, "A58B6B");
        t.Retangulo(0, 386, 800, 9, "7A624A");
        for (var i = 0; i < 3; i++)
        {
            double x = 70 + i * 240, y = 70;
            t.Retangulo(x, y, 200, 270, "3A2A1D");
            t.Retangulo(x + 12, y + 12, 176, 246, "F8F4EC");
            if (i == 0)
            {
                t.Retangulo(x + 12, y + 12, 176, 160, "8FBFE0");
                t.Circulo(x + 140, y + 60, 22, "FFE48A");
                t.Poligono(new[] { x + 12, x + 60, x + 105, x + 188, x + 188, x + 12 }, new[] { y + 205, y + 165, y + 182, y + 150, y + 258, y + 258 }, "6FA35B");
            }
            else if (i == 1)
            {
                t.Retangulo(x + 12, y + 12, 176, 246, "2F4B7C");
                t.Circulo(x + 100, y + 120, 54, "F4C95D");
                t.Retangulo(x + 12, y + 190, 176, 68, "1E3252");
            }
            else
            {
                t.Retangulo(x + 12, y + 12, 176, 246, "D9A66B");
                t.Poligono(new[] { x + 30, x + 100, x + 170 }, new[] { y + 250, y + 90, y + 250 }, "A8503A");
                t.Circulo(x + 100, y + 78, 15, "F6E3C0");
            }

            t.Retangulo(x + 60, y + 290, 80, 14, "FFFFFF", 0.9);
        }
    }

    private static void Viola(Tela t, string topo, string baseCor)
    {
        t.Degrade(topo, baseCor);
        t.Retangulo(0, 404, 800, 46, "0F1A0E", 0.5);
        for (var i = 0; i < 6; i++)
        {
            t.Circulo(110 + i * 22, 330 - i * 38, 10, "FFE08A", 0.8);
            t.Linha(119 + i * 22, 330 - i * 38, 119 + i * 22, 290 - i * 38, 3, "FFE08A", 0.8);
            t.Circulo(690 - i * 22, 330 - i * 38, 10, "FFE08A", 0.8);
            t.Linha(699 - i * 22, 330 - i * 38, 699 - i * 22, 290 - i * 38, 3, "FFE08A", 0.8);
        }

        t.Retangulo(388, 40, 24, 170, "5A3A1E");
        t.Poligono(new[] { 376.0, 424, 418, 382 }, new[] { 14.0, 14, 58, 58 }, "3A2512");
        t.Elipse(400, 306, 114, 98, "C98B4B");
        t.Elipse(400, 206, 84, 74, "C98B4B");
        t.Elipse(400, 306, 100, 84, "D9A468", 0.55);
        t.Circulo(400, 296, 30, "2B1B10");
        t.Anel(400, 296, 37, 30, "6B4423");
        t.Retangulo(370, 350, 60, 10, "3A2512");
        t.Linha(394, 60, 394, 352, 1.6, "EFE6D3");
        t.Linha(400, 60, 400, 352, 1.6, "EFE6D3");
        t.Linha(406, 60, 406, 352, 1.6, "EFE6D3");
    }

    private static void Cafezal(Tela t)
    {
        t.Degrade("FDE9B8", "F3B562");
        t.Circulo(610, 125, 82, "FFF3C4", 0.9);
        t.Elipse(200, 440, 520, 150, "5E9A4B");
        t.Elipse(620, 460, 560, 170, "3F7D3A");
        for (var linha = 0; linha < 5; linha++)
        {
            double y = 296 + linha * 34, passo = 38 + linha * 10;
            for (var x = 20.0 + (linha % 2) * 15; x < 800; x += passo)
            {
                t.Circulo(x, y, 14 + linha * 2, "2F6B33");
                t.Circulo(x - 5, y - 3, 3, "C1121F");
                t.Circulo(x + 6, y + 2, 3, "C1121F");
            }
        }
    }

    private static void Estrela(Tela t, string topo, string baseCor)
    {
        t.Degrade(topo, baseCor);
        for (var i = 0; i < 44; i++) t.Circulo((i * 97 + 30) % 780 + 10, (i * 53 + 20) % 230 + 10, 1.5 + (i % 3) * 0.7, "FFFFFF", 0.8);
        var xs = new double[10];
        var ys = new double[10];
        for (var i = 0; i < 10; i++)
        {
            var ang = -Math.PI / 2 + i * Math.PI / 5;
            var raio = i % 2 == 0 ? 100.0 : 42.0;
            xs[i] = 400 + raio * Math.Cos(ang);
            ys[i] = 170 + raio * Math.Sin(ang);
        }

        t.Circulo(400, 170, 130, "FFD166", 0.12);
        t.Poligono(xs, ys, "FFD166");
        t.Circulo(400, 170, 26, "FFF1B8", 0.85);
        t.Retangulo(205, 150, 6, 240, "6B4423");
        t.Poligono(new[] { 211.0, 331, 331, 211 }, new[] { 160.0, 160, 218, 218 }, "E63946");
        t.Linha(211, 218, 190, 310, 4, "FFD166");
        t.Linha(211, 218, 234, 320, 4, "06D6A0");
        foreach (var x in new[] { 110, 560, 640, 720 })
        {
            t.Circulo(x, 318, 18, "0A0F24");
            t.Poligono(new[] { (double)x - 30, x + 30, x + 38, x - 38 }, new[] { 338.0, 338, 424, 424 }, "0A0F24");
        }

        t.Retangulo(0, 424, 800, 26, "0A0F24");
    }

    private static void Microfone(Tela t, string topo, string baseCor)
    {
        t.Degrade(topo, baseCor);
        t.Poligono(new[] { 370.0, 430, 640, 160 }, new[] { 0.0, 0, 450, 450 }, "FFFFFF", 0.07);
        t.Poligono(new[] { 86.0, 250, 250, 86 }, new[] { 362.0, 346, 420, 436 }, "F6F1E7");
        t.Poligono(new[] { 250.0, 414, 414, 250 }, new[] { 346.0, 362, 436, 420 }, "EBE3D2");
        for (var i = 0; i < 4; i++)
        {
            t.Linha(106, 374 + i * 11, 234, 360 + i * 11, 2, "B9AE98");
            t.Linha(266, 360 + i * 11, 394, 374 + i * 11, 2, "B9AE98");
        }

        t.Retangulo(388, 250, 24, 110, "2B2B34");
        t.Elipse(400, 376, 90, 16, "15151B");
        t.Retangulo(396, 350, 8, 28, "2B2B34");
        t.Elipse(400, 175, 58, 72, "CFCFD6");
        t.Elipse(400, 175, 52, 66, "A9A9B5");
        for (var y = 120; y <= 230; y += 14)
        {
            var meia = 52 * Math.Sqrt(Math.Max(0, 1 - Math.Pow((y - 175) / 66.0, 2)));
            t.Linha(400 - meia, y, 400 + meia, y, 2, "7C7C88");
        }

        t.Linha(400, 112, 400, 238, 2, "7C7C88");
    }

    private static void Paisagem(Tela t, string topo, string baseCor)
    {
        t.Degrade(topo, baseCor);
        t.Circulo(560, 120, 70, "FFF3C4", 0.8);
        t.Elipse(220, 470, 520, 160, "6FA35B");
        t.Elipse(640, 490, 520, 170, "4C8A45");
    }
}
