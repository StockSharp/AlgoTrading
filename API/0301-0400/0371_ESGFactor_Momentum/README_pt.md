# ESG Factor Momentum

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Regras de negociação

Universe deve conter ativos previamente selecionados por ESG; nenhum rating ESG é fabricado. Momentum = P[d] / P[d−LookbackDays] − 1 com LookbackDays + 1 fechamentos diários completos. Comprar mensalmente apenas o vencedor, mesmo com todos os retornos negativos. Liquidar todas as posições anteriores e aguardar suas execuções antes da compra, mesmo quando o vencedor não muda. O orçamento é limitado pelo valor informado da carteira; sem alavancagem ou posições vendidas.

Somente velas encerradas sincronizadas são avaliadas. As estatísticas contam datas UTC distintas; novas velas intradiárias substituem o valor da mesma data. Há um rebalanceamento na primeira vela conjunta apta de cada mês; dados incompletos adiam a decisão. Empates são ordenados por Security.Id.

Observações devem ser publicadas até o fechamento da vela de sinal. No intradiário contam apenas datas UTC anteriores; uma vela diária completa contribui sua própria data. Datas CSV futuras não antecipam decisões.

## Dados de entrada

PriceHistoryFile é um CSV opcional de fechamentos diários positivos: SecurityId,Date,Close; data yyyy-MM-dd. Datas atuais e futuras não antecipam o aquecimento. Fechamentos ao vivo substituem o valor da sua data UTC. Duplicatas são rejeitadas. Permite preparar janelas longas com um arquivo de execução curto.

## Parâmetros

| Parâmetro | Padrão |
|---|---|
| `Universe` | obrigatório |
| `Capital` | `100000` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `PriceHistoryFile` | opcional — não definido |
| `LookbackDays` | `252` |

## Execução

Definir Universe explicitamente. Preços e Capital em USD por unidade negociada. Exposição comprada limitada a min(Capital, Portfolio.CurrentValue informado); sem valor positivo não há orçamento de compra. Sem alavancagem, stop-loss ou realização de lucro.

Arredondar objetivos para zero em VolumeStep. Aguardar execuções da liquidação completa antes de abrir novas posições; conciliar posições reais, respeitar MinVolume/MaxVolume e dividir ajustes grandes entre velas posteriores. MinTradeUsd filtra ajustes de objetivos não nulos; liquidação completa ignora o filtro.

## Verificação

Ambas as linguagens usam execuções BTC/TON incluídas e fixtures explícitas de taxas, relatórios, pontuações, textos e aquecimento. Fixtures de calendário deslocam OHLC/volume e definem cotações; não são histórico plurianual. Fórmulas independentes, direção/tamanho das ordens, execuções reais, posições residuais e datas proibidas são verificadas. Dois grupos, K=1 e AssetDecile=1 são perfis declarados de mecânica, não prova do universo completo. Sem promessa de rentabilidade.
