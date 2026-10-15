# Estrategia de Reversión a la Media por Correlación
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Reversión a la Media por Correlación se centra en lecturas extremas de la Correlación para explotar la reversión. Las desviaciones amplias respecto al nivel típico raramente se sostienen.

Las operaciones se disparan cuando el indicador se aleja mucho de su media y luego comienza a revertirse. Tanto las configuraciones largas como las cortas incluyen un stop de protección.

Adecuada para traders de swing que esperan oscilaciones, la estrategia cierra una vez que la Correlación regresa hacia el equilibrio. Parámetro inicial `CorrelationPeriod` = 20.

## Detalles

- **Criterios de entrada**: El indicador cruza de vuelta hacia la media.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: El indicador revierte al promedio.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `CorrelationPeriod` = 20
  - `LookbackPeriod` = 20
  - `DeviationThreshold` = 2.0m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Correlation
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
