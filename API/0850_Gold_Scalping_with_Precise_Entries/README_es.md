# Estrategia de Scalping del Oro con Entradas Precisas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de scalping para el oro que utiliza filtro de tendencia EMA, rango RSI y patrones envolventes.

## Detalles

- **Criterios de entrada**: Filtro de tendencia EMA con RSI entre 45 y 55 más patrón envolvente alcista/bajista cerca de EMA50.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Take profit o stop loss.
- **Stops**: Stop loss basado en ATR y objetivo fijo en pips.
- **Valores predeterminados**:
  - `EmaFastPeriod` = 50
  - `EmaSlowPeriod` = 200
  - `RsiPeriod` = 14
  - `AtrPeriod` = 14
  - `RsiLower` = 45
  - `RsiUpper` = 55
  - `PipTarget` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Scalping
  - Dirección: Ambos
  - Indicadores: EMA, RSI, ATR
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
