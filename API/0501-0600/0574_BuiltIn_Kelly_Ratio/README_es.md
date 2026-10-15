# Ratio Kelly Integrado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de ruptura de canal usando una media móvil y bandas ATR con dimensionamiento de posición basado en el ratio Kelly.

## Detalles

- **Criterios de entrada**: Precio que cruza por encima o por debajo de las bandas basadas en ATR.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Take-profit y stop-loss opcionales.
- **Stops**: Opcional.
- **Valores predeterminados**:
  - `Length` = 20
  - `Multiplier` = 1
  - `AtrLength` = 10
  - `UseEma` = true
  - `UseKelly` = true
  - `UseTakeProfit` = false
  - `UseStopLoss` = false
  - `TakeProfit` = 10
  - `StopLoss` = 1
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: MA, ATR
  - Stops: Opcional
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
