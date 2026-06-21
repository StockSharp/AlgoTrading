# Estrategia de Swing Trader Coensio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de ruptura de líneas de tendencia definidas por el usuario. La estrategia calcula proyecciones lineales a partir de los parámetros de pendiente e intercepto tanto para las líneas alcistas como bajistas. Cuando el precio de cierre supera la línea de compra proyectada por un umbral, se abre una posición larga. Cuando el precio cae por debajo de la línea de venta menos el umbral, se entra en una posición corta.

Las posiciones están protegidas por valores de take profit y stop loss en ticks. Un stop trailing opcional actualiza el stop de protección conforme el precio se mueve a favor. Una opción adicional cierra la operación si la ruptura falla en la siguiente vela.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close > BuyLine + EntryThreshold`
  - Corto: `Close < SellLine - EntryThreshold`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Stop loss, take profit, stop trailing o señal opuesta
- **Stops**:
  - Take profit en ticks
  - Stop loss en ticks
  - Stop trailing opcional en ticks
  - Cierre opcional por ruptura falsa en la siguiente vela
- **Valores predeterminados**:
  - `EntryThreshold` = 15m
  - `StopLossTicks` = 50
  - `TakeProfitTicks` = 100
  - `EnableTrailing` = false
  - `TrailingStepTicks` = 5
  - `FalseBreakClose` = true
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `BuyLineSlope` = 0m
  - `BuyLineIntercept` = 0m
  - `SellLineSlope` = 0m
  - `SellLineIntercept` = 0m
- **Filtros**:
  - Categoría: Ruptura de línea de tendencia
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Medio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
