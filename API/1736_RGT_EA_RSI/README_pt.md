# Estratégia RGT EA RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina o **Índice de Força Relativa (RSI)** com as **Bandas de Bollinger** para identificar movimentos de preço extremos e operar potenciais reversões. As posições são abertas quando o RSI entra em zonas de sobrevendido ou sobrecomprado e o preço cruza as Bandas de Bollinger. Um stop loss e trailing stop gerenciam o risco e asseguram os lucros.

## Como Funciona

1. RSI e Bandas de Bollinger são calculados para as velas recebidas.
2. **Comprar** quando o RSI está abaixo do nível de sobrevendido e o preço de fechamento está abaixo da banda inferior.
3. **Vender** quando o RSI está acima do nível de sobrecomprado e o preço de fechamento está acima da banda superior.
4. Após a entrada, um stop loss fixo é colocado. Assim que a posição atinge o lucro mínimo, o stop loss rastreia o preço.

## Parâmetros

| Nome | Descrição |
|------|-----------|
| `Volume` | Volume da ordem. |
| `RsiPeriod` | Período de cálculo do RSI. |
| `RsiHigh` | Limite de sobrecomprado do RSI. |
| `RsiLow` | Limite de sobrevendido do RSI. |
| `StopLoss` | Distância do stop loss inicial em unidades de preço. |
| `TrailingStop` | Distância do trailing stop em unidades de preço. |
| `MinProfit` | Lucro mínimo antes de o trailing ser ativado. |
| `CandleType` | Tipo de vela usado para cálculos. |

## Notas

- Funciona com qualquer instrumento e período suportado pelo StockSharp.
- Usa ordens a mercado para entradas e saídas.
- O trailing stop é atualizado a cada vela concluída.
