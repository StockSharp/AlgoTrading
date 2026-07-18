# Estrategia del Factor Semanal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementa el patrón de Factor Semanal descrito por Andrea Unger. La estrategia opera rupturas del máximo o mínimo de la sesión cuando el rango de cinco días muestra compresión.

## Detalles
- **Criterios de entrada**: Tras el inicio de sesión, si se cumple la condición del Factor Semanal y el precio rompe el máximo de la sesión -> largo; rompe el mínimo de la sesión -> corto.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Cerrar en nueva sesión o tras dos días con posición rentable.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `RangeFilter` = 0.5
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: Weekly factor
  - Stops: No
  - Complejidad: Medio
  - Marco temporal: 15m
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
