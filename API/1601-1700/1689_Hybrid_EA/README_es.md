# Estrategia Hybrid EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Hybrid EA utiliza el Índice de Vigor Relativo (RVI) y su línea de señal.
Abre una posición larga cuando el RVI sube por encima de la señal en una diferencia especificada, y abre una posición corta cuando cae por debajo en la misma cantidad. Las posiciones se protegen con niveles fijos de take profit y stop loss medidos en puntos de precio.

## Detalles

- **Criterios de entrada**: RVI menos señal supera el umbral
- **Largo/Corto**: Ambos
- **Criterios de salida**: cruce opuesto del umbral o take profit/stop loss
- **Stops**: Sí, distancia fija en puntos
- **Valores predeterminados**:
  - `Volume` = 1
  - `RviLength` = 10
  - `SignalLength` = 4
  - `DifferenceThreshold` = 0.05
  - `TakeProfit` = 18
  - `StopLoss` = 9
  - `CandleType` = 5 minute candles
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: RVI, SMA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
