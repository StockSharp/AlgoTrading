# Estrategia de Filtro de Momentum Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Supertrend Momentum Filter** está construida en torno a los indicadores Supertrend y Momentum.

Las señales se activan cuando sus indicadores confirman entradas filtradas en datos intradía (5m). Esto hace que el método sea adecuado para traders activos.

Los stops dependen de múltiplos de ATR y factores como SupertrendPeriod, SupertrendMultiplier. Ajuste estos valores predeterminados para equilibrar el riesgo y la recompensa.

## Detalles
- **Criterios de entrada**: ver implementación para condiciones de indicadores.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: señal opuesta o lógica de stops.
- **Stops**: Sí, usando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `SupertrendPeriod = 10`
  - `SupertrendMultiplier = 3.0m`
  - `MomentumPeriod = 14`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
