# Estrategia de Cruce Fractal AMA MBK
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia de cruce Fractal AMA MBK utiliza la **Media Móvil Adaptativa Fractal (FRAMA)** junto con una línea de activación de **Media Móvil Exponencial (EMA)**. Las señales de operación se generan cuando la línea FRAMA cruza la línea EMA.

## Cómo funciona
- FRAMA adapta su factor de suavizado basándose en la dimensión fractal del movimiento reciente del precio.
- La EMA actúa como línea de activación que suaviza los datos del precio.
- **Entrada larga:** cuando FRAMA cruza hacia arriba la EMA y no hay posición larga abierta.
- **Entrada corta:** cuando FRAMA cruza hacia abajo la EMA y no hay posición corta abierta.
- Las posiciones existentes pueden protegerse con niveles opcionales de stop-loss y take-profit.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `CandleType` | Tipo y marco temporal de las velas usadas para los cálculos (predeterminado: velas de 4 horas). |
| `FramaPeriod` | Período del indicador FRAMA. |
| `SignalPeriod` | Período de la línea EMA de activación. |
| `StopLoss` | Distancia del stop-loss desde el precio de entrada en unidades de precio absolutas (0 lo deshabilita). |
| `TakeProfit` | Distancia del take-profit desde el precio de entrada en unidades de precio absolutas (0 lo deshabilita). |
| `Volume` | Volumen de operación en lotes. |

## Notas
- Solo se procesan las velas completadas.
- Las operaciones se ejecutan con órdenes de mercado (`BuyMarket`/`SellMarket`).
- Los parámetros `FramaPeriod` y `SignalPeriod` admiten optimización.
