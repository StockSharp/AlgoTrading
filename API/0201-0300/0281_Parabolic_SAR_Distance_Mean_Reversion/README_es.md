# Estrategia de Reversión a la Media por Distancia del Parabolic SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Reversión a la Media por Distancia del Parabolic SAR se centra en lecturas extremas del indicador Parabolic SAR para explotar la reversión. Las grandes desviaciones del nivel normal rara vez perduran.

Las operaciones se activan cuando el indicador se aleja mucho de su media y luego comienza a revertirse. Tanto las configuraciones largas como cortas incluyen un stop protector.

Adecuada para operadores de swing que esperan oscilaciones, la estrategia cierra las posiciones una vez que el Parabolic SAR regresa al equilibrio. Parámetro inicial `AccelerationFactor` = 0.02m.

## Detalles

- **Criterios de entrada**: El indicador cruza de regreso hacia la media.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: El indicador revierte al promedio.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `AccelerationFactor` = 0.02m
  - `AccelerationLimit` = 0.2m
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Mean Reversion
  - Dirección: Ambos
  - Indicadores: Parabolic SAR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
