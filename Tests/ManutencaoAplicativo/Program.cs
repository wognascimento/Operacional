using Operacional.Views.Manutencao;
using Newtonsoft.Json.Linq;
var linhas = new[] { new DateTime(2026,11,24), new DateTime(2026,12,1), new DateTime(2026,12,15), new DateTime(2026,12,22) }
    .Select(d => new ManutencaoAplicativoService.Linha { id_aprovado="38", fase="MANUTENÇÃO PROGRAMADA",
        data_inicio=d, data_fim=d, funcao="AJUDANTE", limite_integrantes=2 }).ToList();
var json = JObject.Parse(ManutencaoAplicativoService.MontarJson(linhas.Concat(linhas)));
var fases = (JArray)json["cronograma_fases"]!;
if (fases.Count != 4 || fases.Any(f => ((JArray)f["funcoes"]!).Count != 1 ||
    (string?)f["data_inicio"] != (string?)f["data_fim"])) throw new Exception("Datas ou funções agrupadas incorretamente.");
Console.WriteLine("OK: quatro datas preservadas e duplicatas idênticas removidas.");
linhas[0].limite_integrantes = 1.5m;
try { ManutencaoAplicativoService.MontarJson(linhas); throw new Exception("Aceitou quantidade fracionada."); }
catch (InvalidOperationException) { Console.WriteLine("OK: quantidade fracionada rejeitada."); }
linhas[0].limite_integrantes = 2;
linhas.Add(new() { id_aprovado="38", fase="MANUTENÇÃO PROGRAMADA", data_inicio=linhas[0].data_inicio,
    data_fim=linhas[0].data_fim, funcao="AJUDANTE", limite_integrantes=3 });
try { ManutencaoAplicativoService.MontarJson(linhas); throw new Exception("Aceitou conflito."); }
catch (InvalidOperationException) { Console.WriteLine("OK: quantidades divergentes rejeitadas."); }
var web = new ManutencaoWebService.Linha { usuario_web="45", id_equipe=66, id_aprovado=38,
    sigla_serv="TESTE", fase="MANUTENÇÃO PROGRAMADA", funcao="AJUDANTE", qtd_pessoas=2,
    valor_ano_atual=100, lanche=10, transporte=20, data=new DateTime(2026,11,24) };
var payloadWeb = JObject.Parse(ManutencaoWebService.MontarJson(new[] { web, web })!);
var registrosWeb = (JArray)payloadWeb["liberacao_manutencao_equipe"]!;
if (registrosWeb.Count != 1 || (int)registrosWeb[0]["id_user"]! != 45 ||
    payloadWeb["clientes_fase"] != null || payloadWeb["liberacao_equipe"] != null)
    throw new Exception("Contrato WEB incorreto.");
Console.WriteLine("OK: WEB usa ID aux, remove duplicatas e envia somente manutenção.");
web.usuario_web = null;
try { ManutencaoWebService.MontarJson(new[] { web }); throw new Exception("Aceitou usuário sem vínculo WEB."); }
catch (InvalidOperationException) { Console.WriteLine("OK: vínculo WEB ausente impede envio."); }
if (ManutencaoWebService.MontarJson(Array.Empty<ManutencaoWebService.Linha>()) != null)
    throw new Exception("Consulta vazia deve dispensar POST.");
