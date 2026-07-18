# Panel de Rompimiento RSI Híbrido
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia combina la reversión a la media del RSI con entradas de ruptura filtradas por ADX y una EMA de 200.

El sistema compra cuando el mercado está en rango y el RSI cae por debajo de `RsiBuy` en tendencia alcista de la EMA. Vende en corto cuando el RSI sube por encima de `RsiSell` en tendencia bajista. En régimen tendencial, entra en rupturas por encima/debajo de cierres recientes y rastrea la posición usando ATR.

Incluye un filtro de fecha de inicio y variables simples de panel para el último tipo de trade y dirección.

## Detalles

- **Criterios de entrada**: Señales de RSI en régimen de rango con sesgo de EMA, o rupturas por encima/debajo de los `BreakoutLength` cierres anteriores cuando ADX > `AdxThreshold`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Los trades de RSI salen en `RsiExit`. Los trades de ruptura usan trailing stop ATR.
- **Stops**: Trailing stop ATR para trades de ruptura.
- **Valores predeterminados**:
  - `AdxLength` = 14
  - `AdxThreshold` = 20m
  - `EmaLength` = 200
  - `RsiLength` = 14
  - `RsiBuy` = 40m
  - `RsiSell` = 60m
  - `RsiExit` = 50m
  - `BreakoutLength` = 20
  - `AtrLength` = 14
  - `AtrMultiplier` = 2m
  - `StartDate` = 2017-01-01
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia, Reversión a la media
  - Dirección: Ambos
  - Indicadores: ADX, EMA, RSI, ATR, Highest/Lowest
  - Stops: Trailing
  - Complejidad: Moderado
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
