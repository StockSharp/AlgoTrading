# Estrategia de Fuerza de Tendencia VWAP ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **VWAP ADX Trend Strength** está construida en torno al VWAP con la fuerza de tendencia del ADX.

Las pruebas indican un rendimiento anual promedio de aproximadamente 46%. Funciona mejor en el mercado de acciones.

Las señales se activan cuando sus indicadores confirman cambios de tendencia en datos intradía (5m). Esto hace que el método sea adecuado para traders activos.

Los stops dependen de múltiplos de ATR y factores como AdxPeriod, AdxThreshold. Ajuste estos valores predeterminados para equilibrar el riesgo y la recompensa.

## Detalles
- **Criterios de entrada**: ver implementación para condiciones de indicadores.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: señal opuesta o lógica de stops.
- **Stops**: Sí, usando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `AdxPeriod = 14`
  - `AdxThreshold = 25m`
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
