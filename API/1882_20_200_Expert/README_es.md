# Estrategia 20/200 Expert
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia abre operaciones basándose en la diferencia entre los precios de apertura de dos barras pasadas. Entra largo cuando la apertura en shift2 menos la apertura en shift1 supera un umbral y entra corto en la condición opuesta. Las posiciones se abren solo en una hora especificada y se cierran por take profit, stop loss o después de un tiempo máximo de mantenimiento.

## Detalles

- **Criterios de entrada:**
  - Largo: open[Shift2] - open[Shift1] > DeltaLong puntos.
  - Corto: open[Shift1] - open[Shift2] > DeltaShort puntos.
- **Largo/Corto:** Ambos.
- **Criterios de salida:** take profit, stop loss o tiempo máximo de mantenimiento.
- **Stops:** Stop loss y take profit fijos en puntos.
- **Valores predeterminados:**
  - Shift1 = 6
  - Shift2 = 2
  - DeltaLong = 6 puntos
  - DeltaShort = 21 puntos
  - TakeProfitLong = 390 puntos
  - StopLossLong = 1470 puntos
  - TakeProfitShort = 320 puntos
  - StopLossShort = 2670 puntos
  - TradeHour = 14
  - MaxOpenTime = 504 horas
  - Volume = 0.1
  - Marco temporal de velas = 1 hora
- **Filtros:**
  - Categoría: Momentum
  - Dirección: Largo y Corto
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Por hora
  - Estacionalidad: Basada en tiempo
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
