# Estrategia Golden Ratio Cubes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Golden Ratio Cubes utiliza las matemáticas de Fibonacci para detectar
rupturas. Rastrea el máximo más alto y el mínimo más bajo durante una ventana de
referencia y calcula extensiones basadas en el ratio áureo (φ ≈ 1.618). Cuando el
precio cierra más allá de estas extensiones, la estrategia entra en la dirección del
rompimiento.

## Detalles

- **Criterios de entrada**:
  - Cierre por encima de la extensión del ratio áureo del rango reciente → Comprar.
  - Cierre por debajo de la extensión del ratio áureo del rango reciente → Vender.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Señal de rompimiento opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Lookback` = 34
  - `Phi` = 1.618
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Largo y Corto
  - Indicadores: Highest, Lowest
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
