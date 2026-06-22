# Estrategia de Velas Anchored Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia convierte el experto MQL5 "AnchoredMomentumCandle" en un ejemplo de StockSharp en C#. Calcula el momentum anclado para los precios de apertura y cierre de las velas utilizando medias móviles exponenciales y simples. El indicador dibuja una vela sintética cuyo color refleja la dirección del momentum.

Un cambio a una vela **azul** abre una posición larga y cierra cualquier corta. Un cambio a una vela **rosa** abre una posición corta y cierra cualquier larga.

## Parámetros
- **Momentum Period** – longitud de las medias móviles simples.
- **Smooth Period** – longitud de las medias móviles exponenciales.
- **Candle Type** – marco temporal de las velas utilizadas para los cálculos.

La estrategia se suscribe a las velas especificadas, calcula el indicador y emite órdenes de mercado en las transiciones de color.
