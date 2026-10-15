# Estrategia PS de Prueba Retrospectiva del Barómetro de Enero
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementa el Barómetro de Enero, donde se toma una posición larga cuando el cierre de febrero a junio supera el máximo de enero. Los filtros opcionales requieren un resultado positivo del Santa Claus Rally y/o de los primeros cinco días del año.

## Detalles

- **Criterios de entrada**: Cierre de febrero a junio por encima del máximo de enero con filtros estacionales opcionales
- **Largo/Corto**: Largo
- **Criterios de salida**: Cerrar la posición en diciembre
- **Stops**: No
- **Valores predeterminados**:
  - `CandleType` = 1 month
  - `UseSantaClausRally` = false
  - `UseFirstFiveDays` = false
- **Filtros**:
  - Categoría: Estacionalidad
  - Dirección: Solo largos
  - Indicadores: Estacionalidad
  - Stops: No
  - Complejidad: Principiante
  - Marco temporal: Mensual
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
