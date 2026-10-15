# Estrategia de Factor de Momentum Residual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Factor de Momentum Residual** clasifica los valores por una puntuación externa de momentum residual.
Cada mes, en el primer día de negociación, toma posiciones largas en el decil superior y cortas en el decil inferior.

## Detalles
- **Criterios de entrada**: fuente de datos externa de momentum residual.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Rebalanceo mensual.
- **Stops**: Sin lógica de stop explícita.
- **Valores predeterminados**:
  - `Decile = 10`
  - `MinTradeUsd = 200`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Fundamental
  - Dirección: Ambos
  - Indicadores: Fundamentales
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Diario
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
