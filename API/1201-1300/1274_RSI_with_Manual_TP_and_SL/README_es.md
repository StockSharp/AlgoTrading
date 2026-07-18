# Estrategia RSI con TP y SL Manuales
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementa una estrategia RSI que entra largo cuando el RSI cruza por encima del nivel de sobreventa y el cierre está por encima del 70% del cierre más alto en las últimas 50 velas. Entra corto cuando el RSI cruza por debajo del nivel de sobrecompra y el cierre está por debajo del 130% del cierre más bajo en las últimas 50 velas. Las posiciones están protegidas con take profit y stop loss porcentuales.

## Parámetros

- **Candle Type** – marco temporal de las velas.
- **RSI Length** – período del RSI.
- **Oversold Level** – umbral de RSI para entradas largas.
- **Overbought Level** – umbral de RSI para entradas cortas.
- **Lookback** – período para el cálculo de máximos/mínimos.
- **Take Profit %** – porcentaje de take profit.
- **Stop Loss %** – porcentaje de stop loss.
