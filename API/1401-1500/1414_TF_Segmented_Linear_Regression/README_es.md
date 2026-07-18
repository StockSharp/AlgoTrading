# TF Estrategia de Regresión Lineal Segmentada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia aplica un canal de regresión lineal dentro de cada segmento temporal. Se abre una posición larga cuando el precio cruza por encima de la banda superior y una corta cuando cruza por debajo de la banda inferior.

## Detalles
- **Criterios de entrada**: El precio cruza el canal de regresión.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Cruce de la banda opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `Segment` = TimeSpan.FromDays(1)
  - `Multiplier` = 2
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Linear Regression
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
