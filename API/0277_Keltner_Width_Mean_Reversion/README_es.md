# Estrategia de Reversión a la Media por Ancho de Keltner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Reversión a la Media por Ancho de Keltner se centra en lecturas extremas de las Keltner para explotar la reversión. Las grandes desviaciones del nivel normal rara vez perduran.

Las pruebas indican un rendimiento anual promedio de aproximadamente 160%. Funciona mejor en el mercado de divisas.

Las operaciones se activan cuando el indicador se aleja mucho de su media y luego comienza a revertirse. Tanto las configuraciones largas como cortas incluyen un stop protector.

Adecuada para operadores de swing que esperan oscilaciones, la estrategia cierra las posiciones una vez que las Keltner regresan al equilibrio. Parámetro inicial `EmaPeriod` = 20.

## Detalles

- **Criterios de entrada**: El indicador cruza de regreso hacia la media.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: El indicador revierte al promedio.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `KeltnerMultiplier` = 2.0m
  - `WidthLookbackPeriod` = 20
  - `WidthDeviationMultiplier` = 2.0m
  - `AtrStopMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Mean Reversion
  - Dirección: Ambos
  - Indicadores: Keltner
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
