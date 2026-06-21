# Estrategia Flex ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Flex ATR selecciona dinámicamente los períodos de EMA, RSI y ATR según el marco temporal actual. Se abre una operación larga cuando la EMA rápida cruza por encima de la lenta y el RSI supera 50. Una operación corta se activa en el cruce inverso con RSI por debajo de 50. Las salidas utilizan stops basados en ATR o un trailing stop opcional.

## Detalles

- **Criterios de entrada**: Cruce de EMA rápida vs lenta con filtro RSI.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop o objetivo basado en ATR, trailing stop opcional.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `AtrStopMult` = 3
  - `AtrProfitMult` = 1.5
  - `EnableTrailingStop` = true
  - `AtrTrailMult` = 1
  - `CandleType` = TimeSpan.FromMinutes(30)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: EMA, RSI, ATR
  - Stops: Sí
  - Complejidad: Avanzado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
