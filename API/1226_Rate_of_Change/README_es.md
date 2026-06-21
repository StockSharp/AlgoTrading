# Estrategia de Tasa de Cambio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia utiliza el indicador de Tasa de Cambio para detectar condiciones de burbuja y operar cruces de la línea cero con dimensionamiento dinámico de posición.

Las pruebas retrospectivas muestran un rendimiento estable en datos diarios para activos principales.

## Detalles

- **Criterios de entrada**: ROC cruza por encima o por debajo de cero; corto opcional al reventar la burbuja.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `RocLength` = 365
  - `BubbleThreshold` = 180m
  - `StopLossPercent` = 6m
  - `FixedRatioValue` = 400m
  - `IncreasingOrderAmount` = 200m
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: RateOfChange
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
