# VWAP Pro V21
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia combina EMA rápida y lenta con VWAP y gestión de riesgo basada en ATR. Un filtro EMA de marco temporal superior (1h, longitud 50) confirma la tendencia. Las operaciones se abren cuando el precio se alinea con la tendencia y se cierran en los niveles de take profit o stop loss basados en ATR.

## Detalles

- **Criterios de entrada**: Precio por encima/debajo de la EMA rápida, VWAP y filtro de tendencia.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Take profit o stop loss basado en ATR.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `EmaFastPeriod` = 9
  - `EmaSlowPeriod` = 21
  - `AtrPeriod` = 14
  - `TakeProfitAtrMultiplier` = 0.7
  - `StopLossAtrMultiplier` = 1.4
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: EMA, VWAP, ATR
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
