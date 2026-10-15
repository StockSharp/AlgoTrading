# Estrategia de MACD Volume Cluster
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La estrategia **MACD Volume Cluster** se basa en el análisis de clústeres de volumen MACD.

Las señales se activan cuando los indicadores confirman cambios de tendencia en datos intradía (5m). Esto hace que el método sea adecuado para traders activos.

Los stops se basan en múltiplos de ATR y factores como FastMacdPeriod, SlowMacdPeriod. Ajuste estos valores predeterminados para equilibrar el riesgo y la recompensa.

## Detalles
- **Criterios de entrada**: consulte la implementación para conocer las condiciones de los indicadores.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: señal opuesta o lógica de stop.
- **Stops**: Sí, utilizando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `FastMacdPeriod = 12`
  - `SlowMacdPeriod = 26`
  - `MacdSignalPeriod = 9`
  - `VolumePeriod = 20`
  - `VolumeDeviationFactor = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Múltiples indicadores
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
