# Estrategia de Eficiencia Fractal Polarizada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera basándose en el indicador **Polarized Fractal Efficiency (PFE)**. El PFE mide la eficiencia del movimiento de precios y cambia de signo cuando el momentum cambia.

## Lógica de operación

1. Suscribirse a las velas del marco temporal seleccionado y calcular el PFE.
2. Si el PFE en la barra anterior es menor que dos barras atrás y el valor actual es mayor que el anterior, se abre una posición larga.
3. Si el PFE en la barra anterior es mayor que dos barras atrás y el valor actual es menor que el anterior, se abre una posición corta.
4. Las posiciones opuestas se cierran antes de abrir nuevas.
5. Se habilita protección opcional de stop loss y take profit.

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| `CandleType` | Serie de velas utilizada para el análisis. |
| `PfePeriod` | Período para calcular el indicador PFE. |
| `SignalBar` | Desplazamiento de la barra utilizada para generar señales. |
| `TakeProfit` | Take profit en pasos de precio. |
| `StopLoss` | Stop loss en pasos de precio. |

