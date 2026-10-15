# Estrategia MPM Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión simplificada del experto MQL original `mpm-1_8.mq4`.
Espera una secuencia de velas progresivas y luego abre una posición en la misma
dirección. El Average True Range se utiliza para evaluar el tamaño de las velas y para
el trailing de los stops.

## Parámetros

| Nombre | Descripción |
| ------ | ----------- |
| `ProgressiveCandles` | Número de velas consecutivas requeridas para activar una operación. |
| `ProgressiveSize` | Tamaño mínimo del cuerpo de la vela en relación al ATR para contar como progresiva. |
| `StopRatio` | Proporción del ATR utilizada para seguir el nivel de stop. |
| `AtrPeriod` | Período del indicador Average True Range. |
| `CandleType` | Tipo de velas utilizadas por la estrategia. |
| `ProfitPerLot` | Objetivo de beneficio por lote. |
| `BreakEvenPerLot` | Beneficio requerido para salir en breakeven. |
| `LossPerLot` | Pérdida máxima tolerada por lote. |

## Lógica

1. En cada vela completada el tamaño del cuerpo se compara con el ATR.
2. Un contador alcista o bajista se incrementa cuando el cuerpo supera el
   umbral `ProgressiveSize`.
3. Después de `ProgressiveCandles` observadas en una dirección se envía una orden de mercado.
4. El nivel de stop se sigue por `StopRatio` del ATR.
5. Las posiciones se cierran cuando el stop es alcanzado o cuando se alcanzan los objetivos de beneficio/pérdida.
