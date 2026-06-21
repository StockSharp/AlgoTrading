# Estrategia de Barras Equivolumen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en picos de volumen relativos a la suma de volúmenes durante un período de retrovisión.

## Lógica
- Calcular la proporción del volumen actual con respecto a la suma de volúmenes anteriores.
- Ir largo cuando la proporción supera el umbral y la vela es alcista.
- Ir corto cuando la proporción supera el umbral y la vela es bajista.
- Cerrar la posición cuando la proporción cae por debajo del umbral o la vela revierte.

## Parámetros
- `Lookback` – número de barras para la suma de volúmenes.
- `Volume Threshold` – umbral de proporción para volumen alto.
- `Candle Type` – tipo de velas a utilizar.
