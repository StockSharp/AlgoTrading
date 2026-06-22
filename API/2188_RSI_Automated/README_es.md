# Estrategia RSI Automatizada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de momentum que utiliza el Índice de Fuerza Relativa (RSI) para operar en condiciones extremas de sobreventa y sobrecompra.
El sistema abre una posición larga cuando el RSI cae por debajo del nivel de sobreventa y una posición corta cuando el RSI sube por encima del nivel de sobrecompra.
Las posiciones se cierran cuando el RSI regresa a un umbral medio o cuando se activan los niveles de stop-loss, take profit o trailing stop.

## Detalles

- **Criterios de entrada**: RSI cruzando por debajo de `Oversold` para largos o por encima de `Overbought` para cortos.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: RSI cruzando `ExitLevel`, stop-loss, take profit o trailing stop.
- **Stops**: Sí, stop-loss fijo, take profit y trailing stop opcional.
- **Valores predeterminados**:
  - `RsiPeriod` = 14
  - `Overbought` = 75
  - `Oversold` = 25
  - `ExitLevel` = 50
  - `StopLossPoints` = 50
  - `TakeProfitPoints` = 150
  - `TrailingStopPoints` = 25
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: RSI
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
