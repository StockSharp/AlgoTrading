# Estrategia de Modelos de Crecimiento Automático Simplista
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia forma bandas de promedio acumulado de máximos y mínimos y opera cuando el precio rompe esos niveles.

## Detalles

- **Criterios de entrada**:
  - El precio de cierre por encima de la banda superior abre un largo.
  - El precio de cierre por debajo de la banda inferior abre un corto.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - La señal opuesta cierra la posición.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Length` = 10
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Highest, Lowest
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
