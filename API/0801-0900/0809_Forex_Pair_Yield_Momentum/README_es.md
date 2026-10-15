# Estrategia de Momentum de Rendimiento de Par Forex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera un par de divisas seleccionado utilizando el momentum del diferencial de rendimiento a 2 años entre sus monedas. El momentum se mide como la diferencia entre el diferencial y su media móvil. Las Bandas de Bollinger aplicadas al momentum definen zonas de sobrecompra y sobreventa. Las posiciones se cierran después de un número fijo de barras.

## Características principales

- Usa el momentum del diferencial de rendimiento a 2 años para las señales.
- Las Bandas de Bollinger sobre el momentum identifican condiciones extremas.
- Inversión opcional de la lógica de entrada.
- Cierra las posiciones después de un número especificado de barras.

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| `YieldASecurity` | Primer activo de rendimiento. |
| `YieldBSecurity` | Segundo activo de rendimiento. |
| `CandleType` | Marco temporal de velas para el análisis. |
| `MomentumLength` | Período para la media del diferencial de rendimiento. |
| `BollingerLength` | Período para las Bandas de Bollinger. |
| `BollingerStdDev` | Multiplicador de desviación estándar para las bandas. |
| `HoldPeriods` | Barras para mantener una posición. |
| `ReverseLogic` | Invertir las condiciones de largo y corto. |

## Complejidad

Principiante

