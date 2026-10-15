# Estrategia Rentable SuperTrend + MA + Stoch
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina SuperTrend, cruce de medias móviles y el oscilador Stochastic.

Busca capturar tendencias identificadas por SuperTrend y confirmar las entradas con el cruce de EMA y los niveles de Stochastic. Incluye objetivos opcionales de take profit y stop loss.

## Detalles

- **Criterios de entrada**: Tendencia por SuperTrend, cruce de EMA, umbrales de Stochastic.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Cruce de EMA opuesto o TP/SL.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `MaFastPeriod` = 9
  - `MaSlowPeriod` = 21
  - `StochKPeriod` = 14
  - `StochDPeriod` = 3
  - `TakeProfitPercent` = 2m
  - `StopLossPercent` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: SuperTrend, EMA, Stochastic
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
