using Clinica.Domain.Comum;
using Clinica.Domain.Entities;

namespace Clinica.Domain.Tests.Entities;

public class UsuarioTests
{
    private static readonly DateTime Agora = new(2026, 10, 7, 10, 0, 0);

    private static Usuario Novo() => new(Usuario.GerarCodigo(), "Jeniffer", "Jeni@Clinica.com", Agora);

    [Fact]
    public void Identificador_gerado_segue_o_padrao()
    {
        for (var i = 0; i < 50; i++)
            Assert.True(Usuario.CodigoValido(Usuario.GerarCodigo()));
    }

    [Theory]
    [InlineData("@a406070@", true)]
    [InlineData("a406070", false)]
    [InlineData("@a40607@", false)]
    [InlineData("@b406070@", false)]
    public void Valida_o_formato_do_identificador(string codigo, bool valido)
    {
        Assert.Equal(valido, Usuario.CodigoValido(codigo));
    }

    [Fact]
    public void Nasce_aguardando_o_primeiro_acesso_e_com_email_normalizado()
    {
        var usuario = Novo();

        Assert.True(usuario.PrecisaDePrimeiroAcesso);
        Assert.Equal("jeni@clinica.com", usuario.Email);
        Assert.True(usuario.Ativo);
    }

    [Fact]
    public void Codigo_de_acesso_vence()
    {
        var usuario = Novo();
        usuario.PrepararPrimeiroAcesso("hash", Agora.AddDays(7));

        Assert.True(usuario.CodigoDeAcessoValido(Agora.AddDays(6)));
        Assert.False(usuario.CodigoDeAcessoValido(Agora.AddDays(8)));
    }

    [Fact]
    public void Definir_senha_encerra_o_primeiro_acesso_e_troca_o_selo()
    {
        var usuario = Novo();
        usuario.PrepararPrimeiroAcesso("hash", Agora.AddDays(7));
        var seloAntes = usuario.SeloDeSeguranca;

        usuario.DefinirSenha("hash-da-senha");

        Assert.False(usuario.PrecisaDePrimeiroAcesso);
        Assert.Null(usuario.CodigoDeAcessoHash);
        Assert.NotEqual(seloAntes, usuario.SeloDeSeguranca);
    }

    [Fact]
    public void Desativar_troca_o_selo_para_derrubar_sessoes()
    {
        var usuario = Novo();
        var seloAntes = usuario.SeloDeSeguranca;

        usuario.DefinirAtivo(false);

        Assert.NotEqual(seloAntes, usuario.SeloDeSeguranca);
    }

    [Theory]
    [InlineData("")]
    [InlineData("sem-arroba")]
    [InlineData("a@b")]
    public void Email_invalido_e_recusado(string email)
    {
        Assert.Throws<RegraDeNegocioException>(() => new Usuario(Usuario.GerarCodigo(), "X", email, Agora));
    }
}
