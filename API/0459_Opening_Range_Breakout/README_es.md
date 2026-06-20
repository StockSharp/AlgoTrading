# Estrategia de Ruptura del Rango de Apertura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Ruptura del Rango de Apertura rastrea los precios más altos y más bajos durante los primeros minutos de una sesión de trading. Después de que el rango termina, se colocan órdenes de ruptura más allá del rango con un buffer configurable. Los objetivos se derivan de una proporción recompensa-riesgo mientras que los stops se establecen en el lado opuesto del rango.

## Detalles

- **Criterios de entrada**:
  - Después del rango de apertura, ir largo cuando el precio cierra por encima del máximo más el buffer.
  - Ir corto cuando el precio cierra por debajo del mínimo menos el buffer.
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Stop y objetivo basados en el rango y la proporción recompensa-riesgo.
- **Stops**: Sí
- **Valores predeterminados**:
  - `RangeMinutes` = 15
  - `RewardRisk` = 2.0
  - `EntryBuffer` = 0.0001
  - `SessionStart` = 08:00
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
