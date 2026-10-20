# Return Asymmetry Commodity

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Regras de negociação

Com WindowDays retornos diários completos r: U = Σmax(r,0), D = Σmax(−r,0). Ordenar por (U−D)/(U+D), zero quando U+D=0. Preserva a ordem de U/D inclusive em janelas só de altas ou baixas sem piso artificial no denominador. É balanço de retornos, não assimetria estatística. Comprar TopN líderes e vender TopN últimos distintos; exigir 2×TopN ativos aptos.

Universe é definido explicitamente. Preços e Capital usam USD por unidade negociada. Capital é um orçamento fixo; Leverage define a exposição bruta. Não são adicionados stop loss nem take profit.

Somente velas encerradas sincronizadas são avaliadas. As estatísticas contam datas UTC distintas; novas velas intradiárias substituem o valor da mesma data. Há um rebalanceamento na primeira vela conjunta apta de cada mês; dados incompletos adiam a decisão. Empates são ordenados por Security.Id.

Cada cesta recebe metade de Capital × Leverage, dividida igualmente. As unidades são calculadas pelo fechamento do sinal e arredondadas para zero conforme VolumeStep. Usam-se posições realmente executadas, aguardam-se ordens pendentes e respeitam-se MinVolume/MaxVolume, dividindo grandes ajustes entre velas posteriores. MinTradeUsd filtra pequenos ajustes, mas não liquidações completas.

## Parâmetros

| Parâmetro | Padrão |
|---|---|
| `Universe` | obrigatório |
| `Capital` | 100000 |
| `Leverage` | 1 |
| `MinTradeUsd` | 200 |
| `CandleType` | TimeSpan.FromMinutes(5).TimeFrame() |
| `PriceHistoryFile` | não definido |
| `WindowDays` | 120 |
| `TopN` | 5 |

## Dados de entrada

CSV opcional `PriceHistoryFile`: `SecurityId,Date,Close`, preço positivo, par ativo/data único, data `yyyy-MM-dd`. Intradiário usa datas UTC anteriores; vela diária encerrada fornece seu fechamento. Linhas futuras não antecipam decisões. Velas substituem seu valor diário.

## Verificação

Ambas as linguagens são verificadas com BTC/TON incluído, fórmulas independentes, direção/tamanho das ordens, execuções, posições e saídas. Dados datados de preços/fatores e calendário verificam mecânica; OHLC/volumes deslocados não são história plurianual. Dois grupos, TopN=1 e SMA=2 são perfis pequenos explícitos. Janelas longas e valores originais são verificados separadamente. Sem promessa de rentabilidade.
