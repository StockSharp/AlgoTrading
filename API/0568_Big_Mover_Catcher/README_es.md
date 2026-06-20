# Estrategia Cazadora de Grandes Movimientos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra en largo cuando el precio cierra por encima de la Banda de Bollinger superior y todos los filtros habilitados confirman el movimiento. También puede ir en corto cuando el precio cierra por debajo de la banda inferior. Los filtros incluyen RSI, ADX, ATR, dirección de tendencia EMA y MACD. Se aplica un stop loss de porcentaje fijo, las posiciones se cierran cuando el precio regresa a la banda media y una toma de beneficios forzada opcional sale en velas inusualmente grandes.

## Detalles
- **Criterios de entrada:**
  - **Largo:** cierre > Banda de Bollinger superior y todos los filtros activos pasan.
  - **Corto:** cierre < Banda de Bollinger inferior y todos los filtros activos pasan.
- **Largo/Corto:** Ambos (configurable).
- **Criterios de salida:**
  - El precio cruza la Banda de Bollinger media.
  - Toma de beneficios forzada opcional en velas grandes.
- **Stops:** Stop loss de porcentaje fijo.
- **Valores predeterminados:** Longitud Bollinger = 40, stop loss = 2%, umbral de TP forzado = 5%.
- **Filtros:** RSI (14), ADX (28), ATR (14), EMA (350), MACD (12,26,9).
