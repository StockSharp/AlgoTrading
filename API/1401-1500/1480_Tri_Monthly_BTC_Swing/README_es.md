# Estrategia Tri-Monthly BTC Swing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Tri-Monthly BTC Swing opera con EMA200, cruce de MACD y filtro RSI.
La estrategia permite solo una operación cada 90 días.

## Detalles

- **Criterios de entrada**: cierre por encima de EMA200, línea MACD por encima de la señal, RSI por encima del umbral y al menos 90 días desde la última operación
- **Largo/Corto**: Largo
- **Criterios de salida**: línea MACD por debajo de la señal o RSI por debajo del umbral
- **Stops**: No
- **Valores predeterminados**:
  - `EmaLength` = 200
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `RsiLength` = 14
  - `RsiThreshold` = 50
  - `TradeInterval` = 90 días
  - `CandleType` = 1 día
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo
  - Indicadores: EMA, MACD, RSI
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
