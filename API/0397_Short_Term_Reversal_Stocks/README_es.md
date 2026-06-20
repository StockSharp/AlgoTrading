# Estrategia de Reversión a Corto Plazo en Acciones
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Reversión a Corto Plazo en Acciones** aplica los principios de reversión a la media en valores de renta variable. Cada día se compran las acciones con las mayores pérdidas de la semana anterior mientras se venden en corto los ganadores recientes, apostando a una reversión de corta duración.

Las posiciones se mantienen solo unos días antes de reevaluar.

## Detalles
- **Criterios de entrada**: Clasificación diaria por retorno de una semana.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Posiciones cerradas tras varios días o cuando los rankings se actualizan.
- **Stops**: Se puede usar un stop basado en volatilidad.
- **Valores predeterminados**:
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Basados en precio
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
