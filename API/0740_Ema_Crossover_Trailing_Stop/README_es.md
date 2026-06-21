# Estrategia de Cruce EMA con Stop Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de Cruce EMA con Stop Trailing** abre una posición larga cuando la EMA corta cruza por encima de la EMA larga y abre una posición corta cuando cruza por debajo. Un stop trailing basado en el precio más alto o más bajo después de la entrada cierra la posición cuando el precio retrocede un porcentaje establecido.

## Detalles
- **Criterios de entrada**: cruce de la EMA corta sobre la EMA larga.
- **Largo/Corto**: ambas direcciones.
- **Criterios de salida**: cruce opuesto o stop trailing.
- **Stops**: stop trailing usando el precio máximo/mínimo desde la entrada.
- **Valores predeterminados**:
  - `ShortLength = 9`
  - `LongLength = 21`
  - `TrailStopPercent = 1`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: EMA
  - Stops: Stop trailing
  - Complejidad: Básico
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
