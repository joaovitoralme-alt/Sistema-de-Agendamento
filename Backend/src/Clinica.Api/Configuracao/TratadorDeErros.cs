using Clinica.Application.Comum;
using Clinica.Domain.Comum;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.Api.Configuracao;

/// <summary>
/// Transforma as exceções em respostas HTTP. Assim os controllers não precisam de try/catch:
/// regra quebrada = 400, não encontrado = 404, o resto = 500 (e vai para o log).
/// </summary>
public class TratadorDeErros(ILogger<TratadorDeErros> log) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception excecao, CancellationToken ct)
    {
        var (status, mensagem) = excecao switch
        {
            RegraDeNegocioException e => (StatusCodes.Status400BadRequest, e.Message),
            NaoEncontradoException e => (StatusCodes.Status404NotFound, e.Message),
            NaoAutorizadoException e => (StatusCodes.Status401Unauthorized, e.Message),
            _ => (StatusCodes.Status500InternalServerError, "Ocorreu um erro inesperado. Tente novamente.")
        };

        if (status == StatusCodes.Status500InternalServerError)
            log.LogError(excecao, "Erro não tratado em {Caminho}", http.Request.Path);

        http.Response.StatusCode = status;
        await http.Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = mensagem }, ct);
        return true;
    }
}
