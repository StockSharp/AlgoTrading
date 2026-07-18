# Estrategia C-Factor HLH4 Solo Compra
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una traducción en C# del asesor experto MQL original **C_Factor_HLH4_buy_only**. Demuestra cómo portar estrategias de MetaTrader a la API de alto nivel de StockSharp.

## Lógica de la estrategia

- Utiliza velas de marco temporal de cuatro horas.
- Abre una posición larga cuando la vela actual cierra por encima del máximo de la vela anterior.
- Sale de la posición larga cuando el precio de cierre:
  - supera el mínimo de la vela anterior en 100 ticks, o
  - cae por debajo del máximo de la vela anterior en 20 ticks.
- La gestión del riesgo se maneja con distancias de stop-loss y take-profit configurables.
- El volumen de la orden se calcula a partir del porcentaje del capital de la cuenta arriesgado por operación.

## Parámetros

| Nombre | Descripción |
| ---- | ----------- |
| `StopLoss` | Distancia en ticks para el stop de protección. |
| `TakeProfit` | Distancia en ticks para el objetivo de beneficio. |
| `RiskPercent` | Porcentaje del capital de la cuenta arriesgado en cada operación. |
| `CandleType` | Tipo y marco temporal de la vela para el análisis (por defecto: velas de 4 horas). |

## Notas

La estrategia opera solo en largo y está diseñada con fines educativos. Ajuste los parámetros y la configuración de riesgo antes de usarla en trading real.
