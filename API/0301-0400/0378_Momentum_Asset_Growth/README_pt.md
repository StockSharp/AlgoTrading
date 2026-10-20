# Momentum Asset Growth

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Regras de negociação

Com o último relatório anual publicado e a mesma data fiscal do ano anterior calcular crescimento = TotalAssets[current] / TotalAssets[previous] − 1. Manter apenas o grupo AssetDecile superior: floor(quantidade financeira elegível / AssetDecile). Nele calcular momentum de MomLook dias com MomLook + 1 fechamentos que terminam antes do primeiro dia do mês de decisão menos SkipMonths meses completos. Comprar o Quintile superior e vender o inferior; após o filtro exigir pelo menos Quintile ativos com histórico suficiente. Rebalancear mensalmente, exceto janeiro. Em janeiro manter posições sem nova alocação ou liquidação forçada.

Somente velas encerradas sincronizadas são avaliadas. As estatísticas contam datas UTC distintas; novas velas intradiárias substituem o valor da mesma data. Há um rebalanceamento na primeira vela conjunta apta de cada mês; dados incompletos adiam a decisão. Empates são ordenados por Security.Id.

Observações devem ser publicadas até o fechamento da vela de sinal. No intradiário contam apenas datas UTC anteriores; uma vela diária completa contribui sua própria data. Datas CSV futuras não antecipam decisões.

O limite de publicação é o último CloseTime real entre as velas completas sincronizadas. Uma sessão pode terminar antes de transcorrer todo o intervalo configurado.

## Dados de entrada

FundamentalsFile é obrigatório: SecurityId,PeriodEnd,PublishedAt,TotalAssets,NetIncome,OperatingCashFlow,BookEquity,SharesOutstanding. PeriodEnd: yyyy-MM-dd; PublishedAt: yyyy-MM-ddTHH:mm:ssZ (UTC). Ativos e ações devem ser positivos. Seleciona-se o período mais recente e a última revisão publicada até o sinal. Revisões duplicadas e publicações antes do fim do período são rejeitadas. Não se extraem fundamentos de velas.

PriceHistoryFile é um CSV opcional de fechamentos diários positivos: SecurityId,Date,Close; data yyyy-MM-dd. Datas atuais e futuras não antecipam o aquecimento. Fechamentos ao vivo substituem o valor da sua data UTC. Duplicatas são rejeitadas. Permite preparar janelas longas com um arquivo de execução curto.

## Parâmetros

| Parâmetro | Padrão |
|---|---|
| `Universe` | obrigatório |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `200` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `PriceHistoryFile` | opcional — não definido |
| `MomLook` | `252` |
| `SkipMonths` | `1` |
| `AssetDecile` | `10` |
| `Quintile` | `5` |
| `MaxReportAgeDays` | `550` |
| `FundamentalsFile` | obrigatório |

## Execução

Universe é definido explicitamente. Preços e Capital usam USD por unidade negociada. Capital é um orçamento fixo; Leverage define a exposição bruta. Não são adicionados stop loss nem take profit.

Cada cesta recebe metade de Capital × Leverage, dividida igualmente. As unidades são calculadas pelo fechamento do sinal e arredondadas para zero conforme VolumeStep. Usam-se posições realmente executadas, aguardam-se ordens pendentes e respeitam-se MinVolume/MaxVolume, dividindo grandes ajustes entre velas posteriores. MinTradeUsd filtra pequenos ajustes, mas não liquidações completas.

## Verificação

Ambas as linguagens usam execuções BTC/TON incluídas e fixtures explícitas de taxas, relatórios, pontuações, textos e aquecimento. Fixtures de calendário deslocam OHLC/volume e definem cotações; não são histórico plurianual. Fórmulas independentes, direção/tamanho das ordens, execuções reais, posições residuais e datas proibidas são verificadas. Dois grupos, K=1 e AssetDecile=1 são perfis declarados de mecânica, não prova do universo completo. Sem promessa de rentabilidade.
