# Estrategia ASCTrendND
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia está inspirada en el asesor experto ASCTrendND de MQL5. Utiliza una Media Móvil Simple como señal principal de tendencia, un filtro RSI para confirmar la fuerza y un stop trailing basado en ATR para salir de las operaciones. El enfoque intenta replicar la lógica ASCTrend + NRTR + TrendStrength de forma simplificada en la API de alto nivel de StockSharp.

## Detalles

- **Criterios de entrada:**
  - **Largo:** El precio de cierre está por encima de la SMA y RSI > 50.
  - **Corto:** El precio de cierre está por debajo de la SMA y RSI < 50.
- **Criterios de salida:**
  - Stop trailing basado en ATR * multiplicador o señal contraria.
- **Stops:** Solo stop trailing basado en ATR.
- **Valores predeterminados:**
  - `SmaPeriod` = 50
  - `RsiPeriod` = 14
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0
  - `CandleType` = velas de 5 minutos
- **Filtros:**
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo/Corto
  - Indicadores: SMA, RSI, ATR
  - Stops: Trailing
  - Complejidad: Bajo
  - Marco temporal: 5m
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
