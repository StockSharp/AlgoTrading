# Estrategia Cazadora de Doble Fondo y Doble Techo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia busca patrones de doble fondo y doble techo comparando mínimos y máximos recientes. Un doble fondo ocurre cuando el mínimo más bajo se alcanza dos veces dentro de una ventana de retrospectiva más amplia, mientras que el doble techo utiliza el máximo más alto. Las posiciones largas y cortas se abren en consecuencia y se cierran cuando el precio rompe el extremo opuesto tras formarse un nuevo extremo.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Doble fondo detectado.
  - **Corto**: Doble techo detectado.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**:
  - Largo: Nuevo máximo por encima del máximo anterior con el precio cayendo por debajo del mínimo anterior.
  - Corto: Nuevo mínimo por debajo del mínimo anterior con el precio subiendo por encima del máximo anterior.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Length` = 100
  - `Lookback` = 100
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: Highest, Lowest
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
