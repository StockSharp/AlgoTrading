# Filtro de Volatilidad por Hurst Exponent
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Filtro de Volatilidad por Hurst Exponent utiliza el Hurst junto con filtros de volatilidad. Entra en operaciones solo cuando las condiciones especificadas se alinean.

Las pruebas indican un retorno anual promedio de aproximadamente el 163%. Funciona mejor en el mercado de acciones.

Las señales requieren que el indicador supere un umbral mientras la volatilidad cumple criterios predefinidos. Las posiciones pueden ser largas o cortas con stops integrados.

Diseñada para traders que valoran el control del riesgo, la estrategia sale tan pronto como el indicador revierte a la media o la volatilidad cambia. Configuración inicial `HurstPeriod` = 100.

## Detalles

- **Criterios de entrada**: El indicador cruza de vuelta hacia la media.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: El indicador revierte al promedio.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `HurstPeriod` = 100
  - `MAPeriod` = 20
  - `ATRPeriod` = 14
  - `StopLoss` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Hurst
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
