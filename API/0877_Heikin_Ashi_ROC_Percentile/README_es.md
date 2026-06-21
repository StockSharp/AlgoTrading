# Estrategia Heikin Ashi ROC Percentil
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia convierte las velas a Heikin Ashi, suaviza el cierre con una SMA y mide su Rate of Change. Las bandas de percentil de máximos y mínimos recientes del ROC forman niveles de ruptura. Un cruce por encima de la banda inferior abre o revierte a largo, mientras que un cruce por debajo de la banda superior invierte a corto.

## Detalles

- **Criterios de entrada**:
  - Largo: el ROC cruza por encima de la línea de percentil inferior.
  - Corto: el ROC cruza por debajo de la línea de percentil superior.
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Stop porcentual.
- **Valores predeterminados**:
  - `RocLength` = 100
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
  - `StartDate` = new DateTimeOffset(2015, 3, 3, 0, 0, 0, TimeSpan.Zero)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Heikin Ashi, RateOfChange, Highest, Lowest
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
