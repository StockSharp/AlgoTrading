# Estrategia de Trading de Pares Ajustada por Beta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Trading de Pares Ajustada por Beta utiliza el Beta junto con filtros de volatilidad. Entra en operaciones solo cuando las condiciones especificadas se alinean.

Las señales requieren que el indicador supere un umbral mientras la volatilidad cumple criterios predefinidos. Las posiciones pueden ser largas o cortas con stops integrados.

Diseñada para traders que valoran el control del riesgo, la estrategia sale tan pronto como el indicador revierte a la media o la volatilidad cambia. Configuración inicial `Asset2` = (Security.

## Detalles

- **Criterios de entrada**: El indicador cruza de vuelta hacia la media.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: El indicador revierte al promedio.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `Asset2` = (Security
  - `Asset2Portfolio` = (Portfolio
  - `BetaAsset1` = 1.0m
  - `BetaAsset2` = 1.0m
  - `LookbackPeriod` = 20
  - `EntryThreshold` = 2.0m
  - `StopLoss` = 2.0m
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Beta
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
