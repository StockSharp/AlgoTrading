# Estrategia ADX CCI MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia combina ADX, CCI y una media móvil configurable para operar tendencias fuertes.

El sistema compra cuando +DI cruza por encima de -DI, CCI > 100 y ADX supera el umbral (opcionalmente el cierre está por encima de la MA). Vende en corto cuando -DI cruza por encima de +DI, CCI < -100 y ADX supera el umbral (cierre por debajo de la MA).

Incluye stop-loss y take-profit basados en porcentaje más gestión de riesgo opcional con MA que sale tras varias velas cerrando contra la media móvil.

## Detalles

- **Criterios de entrada**: Cruce de +DI/-DI con CCI extremo y ADX > `AdxThreshold`, cierre vs MA opcional.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop-loss o take-profit alcanzado, gestión de riesgo opcional con MA.
- **Stops**: Sí, take profit y stop loss.
- **Valores predeterminados**:
  - `EnableLong` = true
  - `EnableShort` = true
  - `TakeProfitPercent` = 2m
  - `StopLossPercent` = 1m
  - `CciPeriod` = 15
  - `AdxLength` = 10
  - `AdxThreshold` = 20m
  - `UseMaTrend` = true
  - `MaType` = MovingAverageTypeEnum.Simple
  - `MaLength` = 200
  - `UseMaRiskManagement` = false
  - `MaRiskExitCandles` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: ADX, CCI, MA
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
