using TechFlow.Eventos.Core.Enums;
using TechFlow.Eventos.Core.Loja;
using TechFlow.Eventos.Core.Models;

namespace TechFlow.Eventos.Core.Demonstracao;

/// <summary>
/// Dados de demonstração. Os eventos NÃO têm data fixa: cada um guarda "quantos dias à frente de hoje" e a data é recalculada
/// a partir do dia atual (em Brasília) sempre que a demonstração é atualizada. Assim a agenda nunca fica vazia nem envelhece.
/// </summary>
public static class CatalogoDemo
{
    /// <summary>Marca os eventos de demonstração (nenhum evento real usa este contato).</summary>
    public const string ContatoDemo = "demo@conectacultura.exemplo";

    /// <summary>Contato usado pela versão anterior da demonstração; ainda reconhecido para atualizar os eventos já gravados.</summary>
    public const string ContatoAntigo = "contato@exemplo.com.br";

    public const string EmailVendedor = "vendedor.demo@conectacultura.exemplo";
    public const string EmailComprador = "comprador.demo@conectacultura.exemplo";

    public record EventoDemo(int Dias, string Titulo, CategoriaEvento Categoria, string Cidade, string Endereco, string Hora, decimal Preco,
        string Atracoes, string Acessibilidade, string Ingressos, string Organizador, string Fonte, bool Destaque, StatusEvento Status = StatusEvento.Aprovado,
        string Tema = "paisagem", string CorTopo = "A8D8F0", string CorBase = "F7E9CA");

    public record ProdutoDemo(string Titulo, string Vendedor, CategoriaProduto Categoria, decimal Preco, int Estoque, string Cidade,
        string Descricao, string CorTopo, string CorBase, StatusProduto Status = StatusProduto.Aprovado);

    /// <summary>Data real de um evento de demonstração: hoje (Brasília) mais N dias.</summary>
    public static DateOnly DataDoEvento(DateOnly hoje, EventoDemo e) => hoje.AddDays(e.Dias);

    public static Evento CriarEvento(EventoDemo d, DateOnly hoje) => new()
    {
        Titulo = d.Titulo, Categoria = d.Categoria, Cidade = d.Cidade, Endereco = d.Endereco,
        Data = DataDoEvento(hoje, d), Horario = TimeOnly.Parse(d.Hora), Preco = d.Preco, Atracoes = d.Atracoes,
        Acessibilidade = d.Acessibilidade, Ingressos = d.Ingressos, Organizador = d.Organizador, FonteOficial = d.Fonte,
        Destaque = d.Destaque, Status = d.Status, ContatoEnvio = ContatoDemo, TipoAutor = TipoAutorEvento.Organizador
    };

    // Dias à frente de hoje: 0 = hoje (permite testar avaliações, que só valem para eventos que já começaram).
    public static readonly IReadOnlyList<EventoDemo> Eventos = new[]
    {
        new EventoDemo(0, "Roda de jongo na praça", CategoriaEvento.CulturaAfro, "Sacra Família do Tinguá", "Praça central", "19:00", 0,
            "Grupo de jongo da comunidade e tambores", "Local plano; banheiro adaptado", "Entrada gratuita", "Associação Cultural do Tinguá", "Perfil oficial da associação", true, Tema: "tambor", CorTopo: "E8A55A", CorBase: "8E3B1F"),
        new EventoDemo(1, "Caminhada pelo circuito histórico", CategoriaEvento.Patrimonio, "Sacra Família do Tinguá", "Saída da estação", "08:30", 0,
            "Guia local e paradas em casarões do ciclo do café", "Trajeto com ladeiras", "Inscrição gratuita por mensagem", "Grupo de Memória Afro", "Projeto Memória Afro", true, Tema: "casarao", CorTopo: "A8D8F0", CorBase: "F7E9CA"),
        new EventoDemo(2, "Feira de produtores rurais", CategoriaEvento.Feira, "Miguel Pereira", "Praça da Feira", "07:00", 0,
            "Hortifrúti, queijos, doces e artesanato", "Terreno plano", "Entrada gratuita", "Cooperativa de produtores", "Página da cooperativa", false, Tema: "feira", CorTopo: "FFD9A0", CorBase: "F4A261"),
        new EventoDemo(3, "Noite de choro e samba", CategoriaEvento.Musica, "Engenheiro Paulo de Frontin", "Rua principal, 120", "20:00", 25,
            "Quarteto de choro e roda de samba aberta", "Térreo, sem degraus", "Bilheteria do local", "Casa de Cultura", "Instagram da Casa de Cultura", false, Tema: "ondas", CorTopo: "23395B", CorBase: "0E1A33"),
        new EventoDemo(4, "Festival do Café e da Memória", CategoriaEvento.Gastronomia, "Sacra Família do Tinguá", "Largo da Igreja Matriz", "10:00", 15,
            "Degustação de cafés, contação de histórias e feira de artesanato", "Rampa de acesso", "Ingresso na porta", "Produtores locais", "Site da prefeitura", true, Tema: "cafe", CorTopo: "B98355", CorBase: "3B2314"),
        new EventoDemo(5, "Exposição fotográfica Raízes", CategoriaEvento.CulturaAfro, "Vassouras", "Museu Municipal", "14:00", 10,
            "Fotografias e relatos de moradores", "Elevador e banheiro adaptado", "Bilheteria do museu", "Museu Municipal", "Site do museu", false, Tema: "quadro", CorTopo: "EDE8DF", CorBase: "CDC5B6"),
        new EventoDemo(6, "Oficina de percussão para jovens", CategoriaEvento.CulturaAfro, "Paracambi", "Centro Cultural, sala 2", "15:00", 0,
            "Aula aberta de tambores com mestres da região", "Sala térrea com rampa", "Inscrição gratuita no local", "Coletivo Estação Viva", "Instagram do coletivo", false, Tema: "tambor", CorTopo: "6FA8C8", CorBase: "1F3F5B"),
        new EventoDemo(8, "Sarau de poesia na estação", CategoriaEvento.CulturaAfro, "Paracambi", "Antiga estação ferroviária", "18:30", 0,
            "Microfone aberto e música ao vivo", "Acesso por rampa", "Entrada gratuita", "Coletivo Estação Viva", "Instagram do coletivo", false, Tema: "microfone", CorTopo: "5B3E8A", CorBase: "24143F"),
        new EventoDemo(10, "Feira de artesanato do Tinguá", CategoriaEvento.Feira, "Sacra Família do Tinguá", "Quadra da escola", "09:00", 0,
            "Artesãos da região", "Não informado", "Entrada gratuita", "Morador (envio comunitário)", "", false, StatusEvento.Pendente, Tema: "feira", CorTopo: "D4E8B6", CorBase: "7FB069"),
        new EventoDemo(12, "Encontro de violeiros", CategoriaEvento.Musica, "Vassouras", "Teatro Municipal", "19:30", 20,
            "Duelo de violas, modas de viola e canto", "Teatro com acesso para cadeirantes", "Bilheteria do teatro", "Associação de Violeiros", "Site do teatro", false, Tema: "viola", CorTopo: "3E5C3A", CorBase: "1E3320"),
        new EventoDemo(15, "Festa da colheita do café", CategoriaEvento.Gastronomia, "Sacra Família do Tinguá", "Fazenda histórica, entrada principal", "09:00", 30,
            "Colheita guiada, almoço típico e visita ao terreiro de secagem", "Percurso parcialmente acessível", "Ingressos antecipados com o organizador", "Produtores locais", "Site da prefeitura", true, Tema: "cafezal", CorTopo: "FDE9B8", CorBase: "F3B562"),
        new EventoDemo(20, "Cortejo de folia de reis", CategoriaEvento.Patrimonio, "Miguel Pereira", "Praça da Matriz", "17:00", 0,
            "Cortejo com bandeira, cantoria e encontro de foliões", "Percurso plano pelo centro", "Entrada gratuita", "Companhia de Reis do Vale", "Perfil da companhia", false, Tema: "estrela", CorTopo: "1B2A5C", CorBase: "0B1331")
    };

    public static readonly IReadOnlyList<ProdutoDemo> Produtos = new[]
    {
        new ProdutoDemo("Tapete de fibra de bananeira", "Ateliê Raízes", CategoriaProduto.CasaEDecoracao, 89m, 6, "Sacra Família do Tinguá",
            "Tapete trançado à mão com fibra de bananeira, 60 x 90 cm. Cada peça é única e leva cerca de três dias para ficar pronta.", "C8864A", "7A4B22"),
        new ProdutoDemo("Café especial torrado 250 g", "Café da Serra", CategoriaProduto.Alimentos, 32m, 40, "Sacra Família do Tinguá",
            "Grãos de altitude colhidos à mão, torra média, notas de chocolate e caramelo. Moído na hora ou em grãos, conforme combinado.", "6B4226", "2E1A0E"),
        new ProdutoDemo("Tambor de jongo artesanal", "Mestre Zeca Instrumentos", CategoriaProduto.Artesanato, 350m, 2, "Vassouras",
            "Tambor de madeira de lei com couro de boi, afinado pelo mestre. Acompanha baqueta e cordas extras.", "A0522D", "4A2312"),
        new ProdutoDemo("Quadro em aquarela: casarão do café", "Ateliê Aquarela do Vale", CategoriaProduto.Arte, 180m, 1, "Miguel Pereira",
            "Aquarela original em papel 300 g, 30 x 40 cm, com moldura simples. Inspirada nos casarões do ciclo do café.", "3F7CAC", "1B3A57"),
        new ProdutoDemo("Geleia de goiaba artesanal 300 g", "Sabores do Vale", CategoriaProduto.Alimentos, 18.5m, 30, "Paracambi",
            "Feita em tacho de cobre com goiabas do quintal, sem conservantes. Combina com queijo fresco.", "D9534F", "8B1E1A"),
        new ProdutoDemo("Turbante de algodão estampado", "Ateliê Raízes", CategoriaProduto.Moda, 45m, 12, "Sacra Família do Tinguá",
            "Turbante de algodão com estampa exclusiva, tecido macio e respirável. Disponível em várias cores sob encomenda.", "E0A030", "A8641A"),
        new ProdutoDemo("Cesta de bambu trançada", "Trançados do Vale", CategoriaProduto.CasaEDecoracao, 65m, 8, "Engenheiro Paulo de Frontin",
            "Cesta de bambu com alças, ótima para pães, frutas ou organização. Aproximadamente 30 cm de diâmetro.", "8DA05A", "4F6330"),
        new ProdutoDemo("Colar de sementes naturais", "Ateliê Raízes", CategoriaProduto.Moda, 28m, 20, "Sacra Família do Tinguá",
            "Colar de sementes de açaí e capim-dourado, fecho de madeira e comprimento ajustável.", "7C5BA8", "3E2A63"),
        new ProdutoDemo("Doce de leite caseiro 400 g", "Sabores do Vale", CategoriaProduto.Alimentos, 24m, 0, "Paracambi",
            "Doce de leite cremoso feito em fogão a lenha. Produto esgotado: exemplo de como a loja mostra itens sem estoque.", "D8B76A", "8F6B24"),
        new ProdutoDemo("Sabonete artesanal de ervas", "Ervas e Aromas", CategoriaProduto.Outros, 12m, 25, "Vassouras",
            "Sabonete vegetal com ervas da horta. Exemplo de anúncio aguardando aprovação de um moderador.", "5FAE8C", "2D6B52", StatusProduto.Pendente)
    };
}
