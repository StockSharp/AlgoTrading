# Estrategia SpectrAnalysis Chaikin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia usa el oscilador Chaikin para detectar cambios de momentum. El oscilador se calcula a partir de la línea de Acumulación/Distribución suavizada por dos medias móviles ponderadas linealmente. Cuando la pendiente del oscilador gira hacia arriba y el último valor cruza por encima del valor anterior, se abre una posición larga. Por el contrario, cuando la pendiente gira hacia abajo y el último valor cruza por debajo del valor anterior, se abre una posición corta.

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| `FastMaPeriod` | Período de la media móvil ponderada lineal rápida utilizada en el oscilador Chaikin. |
| `SlowMaPeriod` | Período de la media móvil ponderada lineal lenta utilizada en el oscilador Chaikin. |
| `BuyPosOpen` | Habilitar apertura de posiciones largas. |
| `SellPosOpen` | Habilitar apertura de posiciones cortas. |
| `BuyPosClose` | Habilitar cierre de posiciones largas cuando se cumplan las condiciones. |
| `SellPosClose` | Habilitar cierre de posiciones cortas cuando se cumplan las condiciones. |
| `CandleType` | Marco temporal de las velas utilizadas para el cálculo. |

## Notas

- Se usan órdenes de mercado para entradas y salidas.
- La estrategia no establece órdenes de stop-loss ni take-profit.
- Solo se proporciona la versión en C#; no se incluye implementación en Python.
