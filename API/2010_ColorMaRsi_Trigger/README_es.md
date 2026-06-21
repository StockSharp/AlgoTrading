# Estrategia de Activación ColorMaRsi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port de StockSharp del experto MQL5 original `exp_colormarsi-trigger.mq5`. Compara las EMAs rápida y lenta y los valores RSI rápido y lento. La señal combinada toma los valores -1, 0 o +1. Se abre una posición cuando la señal anterior tiene un signo opuesto al actual.

## Cómo funciona

- Cuando la señal pasa de positiva a cero o negativa, se abre una posición larga y se cierra cualquier posición corta.
- Cuando la señal pasa de negativa a cero o positiva, se abre una posición corta y se cierra cualquier posición larga.

## Parámetros

- **Fast EMA** – período para la media móvil exponencial rápida.
- **Slow EMA** – período para la media móvil exponencial lenta.
- **Fast RSI** – período para el RSI rápido.
- **Slow RSI** – período para el RSI lento.
- **Candle Type** – marco temporal de las velas usadas para el cálculo.

## Indicadores

- Media Móvil Exponencial (rápida y lenta)
- Índice de Fuerza Relativa (rápido y lento)

Solo se procesan velas terminadas. Las órdenes se colocan usando `BuyMarket` y `SellMarket`.
