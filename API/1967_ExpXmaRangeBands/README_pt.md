# Estratégia ExpXmaRangeBands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica a lógica do exemplo do MetaTrader "Exp_XMA_Range_Bands" usando a API de alto nível do StockSharp. Ela emprega um Canal de Keltner para definir suporte e resistência dinâmicos baseados em uma média móvel e o range verdadeiro médio. As operações são acionadas quando o preço re-entra no canal após ter saído.

## Como funciona

1. Construir um Canal de Keltner usando:
   - Período de EMA `MaLength`
   - Período de ATR `RangeLength`
   - Multiplicador do ATR `Deviation`
2. Quando um candle fecha acima da banda superior anterior, qualquer posição vendida é fechada. Se o próximo candle fechar de volta dentro do canal (fechamento ≤ banda superior atual), uma posição comprada é aberta.
3. Quando um candle fecha abaixo da banda inferior anterior, qualquer posição comprada é fechada. Se o próximo candle fechar de volta dentro do canal (fechamento ≥ banda inferior atual), uma posição vendida é aberta.
4. Os níveis de stop-loss e take-profit são expressos em pontos e aplicados assim que uma posição é aberta.

## Parâmetros

- `MaLength` – Período de EMA para o centro do canal.
- `RangeLength` – Período de ATR usado para a largura do canal.
- `Deviation` – Multiplicador aplicado ao ATR para calcular as bandas.
- `StopLoss` – Stop-loss em pontos (convertido em preço por `Security.PriceStep`).
- `TakeProfit` – Take-profit em pontos (convertido em preço por `Security.PriceStep`).
- `CandleType` – Série de candles usada nos cálculos.

## Indicadores

- KeltnerChannels (EMA + ATR)
