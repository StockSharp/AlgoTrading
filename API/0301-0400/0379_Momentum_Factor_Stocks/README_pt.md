# Momentum Factor Stocks

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Regras de negociação

Em cada revisão mensal exigir LookbackDays + 1 fechamentos diários completos. Momentum = P[d−SkipDays] / P[d−LookbackDays] − 1; SkipDays deve ser menor que LookbackDays. Excluir os últimos SkipDays intervalos, sem subtrair dois indicadores ROC separados. Comprar o Quintile superior e vender o inferior, floor(quantidade elegível / Quintile) por lado. Manter as quantidades nativas arredondadas até a próxima revisão.

Somente velas encerradas sincronizadas são avaliadas. As estatísticas contam datas UTC distintas; novas velas intradiárias substituem o valor da mesma data. Há um rebalanceamento na primeira vela conjunta apta de cada mês; dados incompletos adiam a decisão. Empates são ordenados por Security.Id.

Observações devem ser publicadas até o fechamento da vela de sinal. No intradiário contam apenas datas UTC anteriores; uma vela diária completa contribui sua própria data. Datas CSV futuras não antecipam decisões.

## Dados de entrada

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
| `LookbackDays` | `252` |
| `SkipDays` | `21` |
| `Quintile` | `5` |

## Execução

Universe é definido explicitamente. Preços e Capital usam USD por unidade negociada. Capital é um orçamento fixo; Leverage define a exposição bruta. Não são adicionados stop loss nem take profit.

Cada cesta recebe metade de Capital × Leverage, dividida igualmente. As unidades são calculadas pelo fechamento do sinal e arredondadas para zero conforme VolumeStep. Usam-se posições realmente executadas, aguardam-se ordens pendentes e respeitam-se MinVolume/MaxVolume, dividindo grandes ajustes entre velas posteriores. MinTradeUsd filtra pequenos ajustes, mas não liquidações completas.

## Verificação

Ambas as linguagens usam execuções BTC/TON incluídas e fixtures explícitas de taxas, relatórios, pontuações, textos e aquecimento. Fixtures de calendário deslocam OHLC/volume e definem cotações; não são histórico plurianual. Fórmulas independentes, direção/tamanho das ordens, execuções reais, posições residuais e datas proibidas são verificadas. Dois grupos, K=1 e AssetDecile=1 são perfis declarados de mecânica, não prova do universo completo. Sem promessa de rentabilidade.
