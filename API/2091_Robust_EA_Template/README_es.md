# Estrategia de Plantilla Robust EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que implementa la Plantilla Robust EA de MQL.
Utiliza el Commodity Channel Index (CCI) y el Relative Strength Index (RSI) para generar señales de entrada y aplica take profit y stop loss fijos.

## Lógica
- Comprar cuando CCI está en -200..-150 o -100..-50 y RSI está entre 0 y 25.
- Vender cuando CCI está entre 50 y 150 y RSI está entre 80 y 100.
- El stop loss y el take profit se definen en pips y se convierten a puntos de precio.

## Parámetros
- `Candle Type` – serie de datos de velas.
- `CCI Period` – período del indicador CCI.
- `RSI Period` – período del indicador RSI.
- `Take Profit (pips)` – distancia para el objetivo de beneficio.
- `Stop Loss (pips)` – distancia para el stop loss.
- `Volume` – volumen de la orden.
