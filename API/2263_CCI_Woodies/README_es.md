# Estrategia CCI Woodies
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia opera basándose en el cruce de dos líneas del Índice de Canal de Materias Primas (CCI) derivadas del método CCI de Woodies. Se calculan un CCI rápido y un CCI lento en el marco temporal especificado. Cuando la línea rápida cruza por debajo de la línea lenta, se abre una posición larga y se cierra cualquier posición corta. Cuando la línea rápida cruza por encima de la línea lenta, se abre una posición corta y se cierra cualquier posición larga.

## Parámetros
- **FastPeriod** – longitud del indicador CCI rápido.
- **SlowPeriod** – longitud del indicador CCI lento.
- **CandleType** – marco temporal de las velas utilizadas para los cálculos.
- **InvertSignals** – si está habilitado, las reglas de compra y venta se intercambian.
- **TakeProfitPoints** – objetivo de beneficio en puntos de precio.
- **StopLossPoints** – límite de pérdida en puntos de precio.

## Notas
La estrategia usa la API de alto nivel de StockSharp. Los indicadores se vinculan mediante `Bind`, y el control de riesgos se gestiona con `StartProtection` usando niveles de stop-loss y take-profit.
