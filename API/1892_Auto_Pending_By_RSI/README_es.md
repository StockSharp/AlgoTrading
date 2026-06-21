# Órdenes Pendientes Automáticas por RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia coloca órdenes límite pendientes después de que el Índice de Fuerza Relativa (RSI) permanece en zonas extremas durante varias velas consecutivas.

Cuando el RSI permanece por debajo del nivel de sobreventa durante `MatchCount` velas, se registra una orden de compra límite por debajo del cierre de la vela en `PendingOffset` puntos de precio. Cuando el RSI permanece por encima del nivel de sobrecompra durante el mismo número de velas, se coloca una orden de venta límite por encima del cierre con el mismo desplazamiento.

## Parámetros
- `RsiPeriod` – período de cálculo del RSI.
- `RsiOverbought` – nivel que define la zona de sobrecompra.
- `RsiOversold` – nivel que define la zona de sobreventa.
- `PendingOffset` – distancia desde el precio de cierre para colocar las órdenes pendientes (puntos de precio).
- `MatchCount` – número de velas consecutivas requeridas antes de colocar las órdenes.
- `CandleType` – marco temporal de velas utilizado para el análisis.

Los valores predeterminados emulan el script MQL original y utilizan velas de 4 horas.
