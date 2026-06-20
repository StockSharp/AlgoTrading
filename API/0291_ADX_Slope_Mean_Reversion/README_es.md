# ADX Slope Estrategia de Reversión a la Media
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La estrategia ADX Slope de Reversión a la Media se centra en lecturas extremas del ADX para aprovechar la reversión. Las desviaciones amplias del nivel promedio raramente perduran.

Las operaciones se activan cuando el indicador se aleja mucho de su media y luego comienza a revertirse. Tanto las configuraciones largas como las cortas incluyen un stop de protección.

Adecuada para traders de swing que esperan oscilaciones, la estrategia cierra la posición una vez que el ADX regresa al equilibrio. Parámetro inicial `AdxPeriod` = 14.

## Detalles

- **Criterios de entrada**: El indicador cruza de vuelta hacia la media.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: El indicador revierte al promedio.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `AdxPeriod` = 14
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 1.0m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: ADX
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
