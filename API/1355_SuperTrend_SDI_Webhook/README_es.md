# Estrategia SuperTrend SDI Webhook
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en SuperTrend y el Indicador Direccional Suavizado (SDI). Entra largo cuando +DI está por encima de -DI y SuperTrend indica una tendencia alcista. Las posiciones cortas se abren cuando -DI está por encima de +DI y SuperTrend apunta hacia abajo. La estrategia aplica take profit, stop-loss y stop móvil en porcentaje.

## Detalles

- **Criterios de entrada**:
  - Largo: `+DI > -DI && SuperTrend up`
  - Corto: `-DI > +DI && SuperTrend down`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Take profit, stop-loss o stop móvil
- **Indicadores**: SuperTrend, AverageDirectionalIndex
- **Stops**: Take profit, stop-loss y stop móvil en porcentaje
- **Valores predeterminados**:
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 1.8m
  - `DiLength` = 3
  - `DiSmooth` = 7
  - `TakeProfitPercent` = 25m
  - `StopLossPercent` = 4.8m
  - `TrailingPercent` = 1.9m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: SuperTrend, SDI
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
