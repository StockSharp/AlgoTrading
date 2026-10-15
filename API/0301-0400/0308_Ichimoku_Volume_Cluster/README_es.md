# Estrategia de Clúster de Volumen con Ichimoku
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Ichimoku Volume Cluster** está construida alrededor de la Nube Ichimoku con confirmación por clúster de volumen.

Las señales se disparan cuando sus indicadores confirman cambios de tendencia en datos intradía (1h). Esto hace que el método sea adecuado para traders activos.

Los stops se basan en múltiplos de ATR y factores como TenkanPeriod, KijunPeriod. Ajuste estos valores predeterminados para equilibrar el riesgo y la recompensa.

## Detalles
- **Criterios de entrada**: ver implementación para las condiciones del indicador.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: señal opuesta o lógica de stop.
- **Stops**: Sí, usando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `TenkanPeriod = 9`
  - `KijunPeriod = 26`
  - `SenkouSpanBPeriod = 52`
  - `VolumeAvgPeriod = 20`
  - `VolumeStdDevMultiplier = 2.0m`
  - `CandleType = TimeSpan.FromHours(1).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: múltiples indicadores
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (1h)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
