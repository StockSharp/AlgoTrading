# Estrategia Liquidex V1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Liquidex V1 es una estrategia de scalping por ruptura convertida del asesor experto MQL original. Combina un **filtro de rango** y una **media móvil ponderada (WMA)** para identificar oportunidades a corto plazo.

## Lógica de trading
1. Para cada vela completada, la estrategia mide su rango (`high - low`).
2. Si el rango de la vela es menor que `RangeFilter`, la vela se ignora.
3. Se calcula una WMA con período `MaPeriod` usando precios de cierre.
4. Cuando la vela abre por debajo de la WMA y cierra por encima, se envía una orden de **compra** a mercado.
5. Cuando la vela abre por encima de la WMA y cierra por debajo, se envía una orden de **venta** a mercado.
6. Cada posición está protegida por un stop-loss definido en `StopLoss`.

## Parámetros
- `RangeFilter` – rango mínimo de la vela en unidades de precio requerido para operar.
- `MaPeriod` – número de períodos para la media móvil ponderada.
- `StopLoss` – stop-loss de protección en puntos.
- `CandleType` – serie de velas utilizada para el análisis.

La estrategia usa `Strategy.Volume` como tamaño de la orden e invierte la posición cuando aparece una señal opuesta.
