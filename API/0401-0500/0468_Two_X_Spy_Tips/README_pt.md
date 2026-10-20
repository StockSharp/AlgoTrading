# Two X Spy Tips

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Regras de negociação

SpySecurity e TipsSecurity são sinais explícitos distintos de Universe; Security também pertence ao conjunto. Na primeira vela encerrada sincronizada do mês, ambos fechamentos diários completos devem superar sua SMA de SmaLength dias incluindo o fechamento atual. Filtro formado reprovado consome a revisão mensal; histórico insuficiente a adia. Sem posição comprar Capital×Leverage/preço arredondado a VolumeStep. Manter compra sem saídas, vendas a descoberto ou acumulação mensal adicional. Usar USD e respeitar MinTradeUsd e limites nativos.

## Parâmetros

| Parâmetro | Padrão |
|---|---|
| `Universe` | obrigatório |
| `Capital` | 100000 |
| `Leverage` | 2 |
| `MinTradeUsd` | 200 |
| `CandleType` | TimeSpan.FromMinutes(1440).TimeFrame() |
| `PriceHistoryFile` | não definido |
| `SpySecurity` | obrigatório |
| `TipsSecurity` | obrigatório |
| `SmaLength` | 200 |

## Dados de entrada

CSV opcional `PriceHistoryFile`: `SecurityId,Date,Close`, preço positivo, par ativo/data único, data `yyyy-MM-dd`. Intradiário usa datas UTC anteriores; vela diária encerrada fornece seu fechamento. Linhas futuras não antecipam decisões. Velas substituem seu valor diário.

## Verificação

Ambas as linguagens são verificadas com BTC/TON incluído, fórmulas independentes, direção/tamanho das ordens, execuções, posições e saídas. Dados datados de preços/fatores e calendário verificam mecânica; OHLC/volumes deslocados não são história plurianual. Dois grupos, TopN=1 e SMA=2 são perfis pequenos explícitos. Janelas longas e valores originais são verificados separadamente. Sem promessa de rentabilidade.
