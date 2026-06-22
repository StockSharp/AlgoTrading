# Estrategia SAR Automatizada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este ejemplo demuestra un enfoque de trading simple basado en el indicador **Parabolic SAR**.
La estrategia abre una posición larga cuando el precio actual está por encima del valor SAR y abre una posición corta cuando el precio está por debajo del SAR. Las características adicionales de gestión de riesgos incluyen stop-loss fijo, take-profit y un trailing stop opcional.

## Parámetros
- `SarStep` – factor de aceleración para el cálculo del SAR.
- `SarMax` – factor de aceleración máximo para el SAR.
- `StopLoss` – distancia del stop-loss en unidades de precio.
- `TakeProfit` – distancia del take-profit en unidades de precio.
- `TrailingStop` – distancia del trailing stop en unidades de precio.
- `CandleType` – tipo de velas utilizadas para los cálculos del indicador.

## Lógica de trading
1. Suscribirse a las velas y calcular los valores del Parabolic SAR.
2. **Entrada**:
   - Ir largo cuando SAR esté por debajo del precio de cierre y no exista posición.
   - Ir corto cuando SAR esté por encima del precio de cierre y no exista posición.
3. **Salida**:
   - Cerrar la posición cuando el precio alcance el nivel SAR opuesto.
   - Aplicar las reglas de stop-loss, take-profit y trailing stop.

Esta estrategia está destinada a fines educativos y muestra cómo usar indicadores y controles de riesgo con la API de alto nivel de StockSharp.
