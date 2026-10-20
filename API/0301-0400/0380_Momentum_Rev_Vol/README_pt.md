# Momentum Rev Vol

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Regras de negociação

Usar fechamentos diários completos distintos P: M = P[t]/P[t−Lookback12]−1, R = P[t]/P[t−Lookback1]−1. V é o desvio padrão amostral dos últimos VolWindow retornos diários simples (divisor N−1). Pontuação = WM×M − WR×R − WV×V, sem normalização transversal. Comprar floor(n/Decile) líderes e vender a descoberto igual número de últimos; exigir pelo menos Decile ativos aptos. Manter quantidades entre revisões mensais.

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
| `CandleType` | TimeSpan.FromMinutes(1440).TimeFrame() |
| `PriceHistoryFile` | não definido |
| `Lookback12` | 252 |
| `Lookback1` | 21 |
| `VolWindow` | 60 |
| `WM` | 1 |
| `WR` | 1 |
| `WV` | 1 |
| `Decile` | 10 |

## Dados de entrada

CSV opcional `PriceHistoryFile`: `SecurityId,Date,Close`, preço positivo, par ativo/data único, data `yyyy-MM-dd`. Intradiário usa datas UTC anteriores; vela diária encerrada fornece seu fechamento. Linhas futuras não antecipam decisões. Velas substituem seu valor diário.

## Verificação

Ambas as linguagens são verificadas com BTC/TON incluído, fórmulas independentes, direção/tamanho das ordens, execuções, posições e saídas. Dados datados de preços/fatores e calendário verificam mecânica; OHLC/volumes deslocados não são história plurianual. Dois grupos, TopN=1 e SMA=2 são perfis pequenos explícitos. Janelas longas e valores originais são verificados separadamente. Sem promessa de rentabilidade.
