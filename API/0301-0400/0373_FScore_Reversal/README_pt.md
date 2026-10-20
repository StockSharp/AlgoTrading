# F-Score Reversal

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Regras de negociação

Mensalmente calcular o retorno de Lookback dias: P[d] / P[d−Lookback] − 1. Comprar o pior retorno entre FScore publicados >= FHi; com IsShortEnabled, vender o melhor entre FScore <= FLo. Os limites são inclusivos e FHi deve superar FLo. Usar pontuações Piotroski publicadas, inteiros 0..9, com o último período/revisão conhecido e limite MaxReportAgeDays. Cada lado recebe metade do orçamento bruto; um lado ausente permanece em caixa. Liquidar toda a carteira anterior e aguardar as execuções antes das novas posições.

Somente velas encerradas sincronizadas são avaliadas. As estatísticas contam datas UTC distintas; novas velas intradiárias substituem o valor da mesma data. Há um rebalanceamento na primeira vela conjunta apta de cada mês; dados incompletos adiam a decisão. Empates são ordenados por Security.Id.

Observações devem ser publicadas até o fechamento da vela de sinal. No intradiário contam apenas datas UTC anteriores; uma vela diária completa contribui sua própria data. Datas CSV futuras não antecipam decisões.

O limite de publicação é o último CloseTime real entre as velas completas sincronizadas. Uma sessão pode terminar antes de transcorrer todo o intervalo configurado.

## Dados de entrada

PriceHistoryFile é um CSV opcional de fechamentos diários positivos: SecurityId,Date,Close; data yyyy-MM-dd. Datas atuais e futuras não antecipam o aquecimento. Fechamentos ao vivo substituem o valor da sua data UTC. Duplicatas são rejeitadas. Permite preparar janelas longas com um arquivo de execução curto.

FScoreFile é obrigatório: `SecurityId,PeriodEnd,PublishedAt,FScore`. PeriodEnd: `yyyy-MM-dd`; PublishedAt: UTC `yyyy-MM-ddTHH:mm:ssZ`. Apenas inteiros 0..9, revisões únicas e publicações desde PeriodEnd. Usar o último período/revisão conhecido; MaxReportAgeDays conta desde PeriodEnd.

## Parâmetros

| Parâmetro | Padrão |
|---|---|
| `Universe` | obrigatório |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `PriceHistoryFile` | opcional — não definido |
| `Lookback` | `21` |
| `FHi` | `7` |
| `FLo` | `3` |
| `IsShortEnabled` | `true` |
| `MaxReportAgeDays` | `550` |
| `FScoreFile` | obrigatório |

## Execução

Universe é definido explicitamente. Preços e Capital usam USD por unidade negociada. Capital é um orçamento fixo; Leverage define a exposição bruta. Não são adicionados stop loss nem take profit.

Arredondar objetivos para zero em VolumeStep. Aguardar execuções da liquidação completa antes de abrir novas posições; conciliar posições reais, respeitar MinVolume/MaxVolume e dividir ajustes grandes entre velas posteriores. MinTradeUsd filtra ajustes de objetivos não nulos; liquidação completa ignora o filtro.

## Verificação

Ambas as linguagens usam execuções BTC/TON incluídas e fixtures explícitas de taxas, relatórios, pontuações, textos e aquecimento. Fixtures de calendário deslocam OHLC/volume e definem cotações; não são histórico plurianual. Fórmulas independentes, direção/tamanho das ordens, execuções reais, posições residuais e datas proibidas são verificadas. Dois grupos, K=1 e AssetDecile=1 são perfis declarados de mecânica, não prova do universo completo. Sem promessa de rentabilidade.
