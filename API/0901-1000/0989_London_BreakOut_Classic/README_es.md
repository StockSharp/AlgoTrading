# Estrategia Clásica de Rompimiento de London
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera rompimientos de la sesión de London usando el rango asiático. El máximo y mínimo entre las 00:00 y las 06:55 UTC forman una caja. Después de las 07:00 UTC, un rompimiento por encima del máximo abre una posición larga y un rompimiento por debajo del mínimo abre una posición corta. El stop loss se coloca en el punto medio de la caja y el take profit usa un factor configurable de riesgo-recompensa.

## Detalles

- **Criterios de entrada**:
  - Largo: el precio cruza por encima del máximo de la sesión asiática.
  - Corto: el precio cruza por debajo del mínimo de la sesión asiática.
- **Criterios de salida**:
  - Stop loss o take profit.
  - Fin de la ventana de operación.
- **Stops**: Sí.
- **Valores predeterminados**:
  - Sesión asiática: 00:00–06:55 UTC.
  - Sesión de operación: 07:00–16:00 UTC.
  - CRV = 1.
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
