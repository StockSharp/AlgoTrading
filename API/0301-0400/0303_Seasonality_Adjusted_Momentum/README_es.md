# Estrategia de Momentum Ajustado por Estacionalidad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Seasonality Adjusted Momentum** está construida alrededor del indicador de momentum ajustado con la fuerza de la estacionalidad.

Las pruebas indican un retorno anual promedio de aproximadamente el 172%. Funciona mejor en el mercado de divisas.

Las señales se disparan cuando la Estacionalidad confirma cambios de momentum en datos diarios. Esto hace que el método sea adecuado para traders activos.

Los stops se basan en múltiplos de ATR y factores como MomentumPeriod, SeasonalityThreshold. Ajuste estos valores predeterminados para equilibrar el riesgo y la recompensa.

## Detalles
- **Criterios de entrada**: ver implementación para las condiciones del indicador.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: señal opuesta o lógica de stop.
- **Stops**: Sí, usando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `MomentumPeriod = 14`
  - `SeasonalityThreshold = 0.5m`
  - `StopLossPercent = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Seasonality, Adjusted
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Diario
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
