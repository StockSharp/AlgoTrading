# Z-Score Estrategia con Filtro de Volumen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La estrategia Z-Score con Filtro de Volumen utiliza el Z-Score junto con filtros de volatilidad. Entra en operaciones solo cuando las condiciones especificadas se alinean.

Las señales requieren que el indicador supere un umbral mientras la volatilidad cumple criterios predefinidos. Las posiciones pueden ser largas o cortas con stops integrados.

Diseñada para traders que valoran el control del riesgo, la estrategia sale en cuanto el indicador revierte a la media o la volatilidad cambia. Configuración inicial `LookbackPeriod` = 20.

## Detalles

- **Criterios de entrada**: El indicador cruza de vuelta hacia la media.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: El indicador revierte al promedio.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `LookbackPeriod` = 20
  - `ZScoreThreshold` = 2.0m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Z-Score
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
