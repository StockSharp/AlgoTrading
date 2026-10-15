# Estratégia RSI com TP e SL Manuais
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementa uma estratégia RSI que entra comprado quando o RSI cruza acima do nível de sobrevenda e o fechamento está acima de 70% do fechamento mais alto dos últimos 50 candles. Entra vendido quando o RSI cruza abaixo do nível de sobrecompra e o fechamento está abaixo de 130% do fechamento mais baixo dos últimos 50 candles. As posições são protegidas com take profit e stop loss percentuais.

## Parâmetros

- **Candle Type** – período do candle.
- **RSI Length** – período do RSI.
- **Oversold Level** – limiar do RSI para entradas compradas.
- **Overbought Level** – limiar do RSI para entradas vendidas.
- **Lookback** – período para o cálculo de máximas/mínimas.
- **Take Profit %** – percentual de take profit.
- **Stop Loss %** – percentual de stop loss.
