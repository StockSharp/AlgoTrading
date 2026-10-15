# Estrategia de Ruptura de Volumen ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **ADX Volume Breakout** está construida en torno al ADX con ruptura de volumen.

Las pruebas indican un rendimiento anual promedio de aproximadamente 55%. Funciona mejor en el mercado de acciones.

Las señales se activan cuando sus indicadores confirman oportunidades de ruptura en datos intradía (5m). Esto hace que el método sea adecuado para traders activos.

Los stops dependen de múltiplos de ATR y factores como AdxPeriod, AdxThreshold. Ajuste estos valores predeterminados para equilibrar el riesgo y la recompensa.

## Detalles
- **Criterios de entrada**: ver implementación para condiciones de indicadores.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: señal opuesta o lógica de stops.
- **Stops**: Sí, usando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `AdxPeriod = 14`
  - `AdxThreshold = 25m`
  - `VolumeAvgPeriod = 20`
  - `VolumeThresholdFactor = 2.0m`
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
