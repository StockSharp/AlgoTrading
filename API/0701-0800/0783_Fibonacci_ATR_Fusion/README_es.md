# Estrategia Fibonacci ATR Fusion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina ratios de presión compradora en múltiples períodos Fibonacci con ATR y utiliza cruces de umbrales para entradas y salidas. Take-profit en capas basado en ATR opcional.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La media ponderada cruza por encima de `LongEntryThreshold`.
  - **Corto**: La media ponderada cruza por debajo de `ShortEntryThreshold`.
- **Criterios de salida**:
  - La media ponderada cruza los umbrales de salida opuestos o reversión de posición.
- **Indicadores**:
  - Ratios ponderados de presión compradora sobre ATR.
  - ATR para take-profit opcional.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `LongEntryThreshold` = 58
  - `ShortEntryThreshold` = 42
  - `LongExitThreshold` = 42
  - `ShortExitThreshold` = 58
  - `Tp1Atr` = 3
  - `Tp2Atr` = 8
  - `Tp3Atr` = 14
  - `Tp1Percent` = 12
  - `Tp2Percent` = 12
  - `Tp3Percent` = 12
- **Filtros**:
  - Seguimiento de tendencia
  - Marco temporal único
  - Indicadores: ATR
  - Stops: ninguno
  - Complejidad: Moderado
