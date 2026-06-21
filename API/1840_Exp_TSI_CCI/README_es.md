# Estrategia Exp TSI CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia calcula el True Strength Index (TSI) basado en el Commodity Channel Index (CCI) y opera en cruces con una línea de señal.

## Lógica
- Calcular el CCI usando el período especificado.
- Introducir los valores del CCI en el True Strength Index con longitudes de suavizado corto y largo.
- Suavizar el TSI resultante con una EMA para obtener una línea de señal.
- Entrar largo cuando el TSI cruza por encima de la línea de señal.
- Entrar corto cuando el TSI cruza por debajo de la línea de señal.

## Parámetros
- `Candle Type` – marco temporal de las velas utilizadas para el análisis.
- `CCI Period` – período para el Commodity Channel Index.
- `TSI Short Length` – longitud de suavizado corto del TSI.
- `TSI Long Length` – longitud de suavizado largo del TSI.
- `Signal Length` – longitud EMA para la línea de señal del TSI.

## Indicadores
- Commodity Channel Index
- True Strength Index
- Exponential Moving Average

## Descargo de responsabilidad
Esta estrategia se proporciona únicamente con fines educativos y no constituye asesoramiento de inversión.
