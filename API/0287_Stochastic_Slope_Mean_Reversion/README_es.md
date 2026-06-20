# Stochastic Slope Estrategia de Reversión a la Media
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La estrategia Stochastic Slope de Reversión a la Media se centra en lecturas extremas del Stochastic para aprovechar la reversión. Las desviaciones amplias del nivel normal raramente perduran.

Las operaciones se activan cuando el indicador se aleja mucho de su media y luego comienza a revertirse. Tanto las configuraciones largas como las cortas incluyen un stop de protección.

Adecuada para traders de swing que esperan oscilaciones, la estrategia cierra la posición una vez que el Stochastic regresa al equilibrio. Parámetro inicial `StochPeriod` = 14.

## Detalles

- **Criterios de entrada**: El indicador cruza de vuelta hacia la media.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: El indicador revierte al promedio.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `StochPeriod` = 14
  - `StochKPeriod` = 3
  - `StochDPeriod` = 3
  - `SlopeLookback` = 20
  - `ThresholdMultiplier` = 2m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Stochastic
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
