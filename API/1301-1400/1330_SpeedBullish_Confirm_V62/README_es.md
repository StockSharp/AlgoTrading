# Estrategia SpeedBullish Strategy Confirm V6.2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Estrategia que combina filtro de tendencia EMA, cruce del histograma MACD y umbral RSI. Los filtros opcionales de ATR y volumen mejoran la calidad de las señales.

## Condiciones de entrada
- Precio por encima de EMA10 o EMA15 para largos, por debajo para cortos.
- Histograma MACD cruzando por encima de cero para largos, por debajo de cero para cortos.
- RSI mayor o menor que el nivel especificado.
- Opcional: el ATR debe superar su media móvil por un multiplicador.
- Opcional: el volumen debe superar la SMA por un multiplicador.

## Condiciones de salida
- Señal de entrada opuesta.
- Take profit y trailing stop en puntos.
- Stop loss manual en puntos.
