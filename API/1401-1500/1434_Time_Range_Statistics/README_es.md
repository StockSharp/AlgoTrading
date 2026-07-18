# Estrategia de Estadísticas de Rango Temporal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Recopila estadísticas simples entre índices de barras seleccionados.
Registra el precio medio, el rango normalizado, el cambio porcentual, el volumen promedio y el recuento de gaps.
Opera en largo si el periodo termina positivo y en corto si es negativo.

## Detalles

- **Criterios de entrada**: el cambio porcentual en `EndIndex` determina la dirección
- **Largo/Corto**: Ambos
- **Criterios de salida**: ninguno
- **Stops**: No
- **Valores predeterminados**:
  - `StartIndex` = 9000
  - `EndIndex` = 10000
- **Filtros**:
  - Categoría: Estadísticas
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
