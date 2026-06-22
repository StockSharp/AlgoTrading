# Vela Volume Weighted MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia construye medias móviles ponderadas por volumen (VWMA) para los precios de apertura y cierre de las velas. La posición relativa de estas VWMA define un "color" de vela.

## Lógica de Trading
1. Una vela es **alcista** cuando VWMA(apertura) está por debajo de VWMA(cierre).
2. Una vela es **bajista** cuando VWMA(apertura) está por encima de VWMA(cierre).
3. Cuando la vela anterior es alcista y la actual se vuelve neutral o bajista, la estrategia abre una posición larga y cierra cualquier posición corta.
4. Cuando la vela anterior es bajista y la actual se vuelve neutral o alcista, la estrategia abre una posición corta y cierra cualquier posición larga.

## Parámetros
- `VWMA Period` – longitud utilizada para calcular ambas medias móviles ponderadas por volumen.
- `Candle Type` – marco temporal de las velas utilizadas para los cálculos.

Un bloque de protección está habilitado por defecto: take‑profit del 2% y stop‑loss del 1%.
