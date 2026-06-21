# Estrategia de Regresión Estadística Multi-Capa EUR/USD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que utiliza múltiples capas de regresión lineal para estimar la dirección de la tendencia en EUR/USD. Calcula regresiones cortas, medias y largas, las valida mediante umbrales de R² y pendiente, y opera en la dirección del conjunto ponderado.

## Detalles

- **Criterios de entrada**:
  - Largo: pendiente ponderada > 0 y fiabilidad > 0.5
  - Corto: pendiente ponderada < 0 y fiabilidad > 0.5
- **Largo/Corto**: Ambos
- **Criterios de salida**: Revertir cuando aparece la señal opuesta
- **Stops**: Protección por pérdida diaria
- **Valores predeterminados**:
  - `ShortLength` = 20
  - `MediumLength` = 50
  - `LongLength` = 100
  - `MinRSquared` = 0.45m
  - `SlopeThreshold` = 0.00005m
  - `WeightShort` = 0.4m
  - `WeightMedium` = 0.35m
  - `WeightLong` = 0.25m
  - `PositionSizePct` = 50m
  - `MaxDailyLossPct` = 12m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Linear Regression
  - Stops: Sí
  - Complejidad: Avanzado
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Nivel de riesgo: Medio
