# Estrategia de Reversión IBS de Rango Promedio Alto-Bajo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia busca la reversión a la media después de que el precio se ha mantenido por debajo de un umbral dinámico derivado del rango promedio alto-bajo. Calcula la media móvil del rango de la barra, el máximo más alto y el mínimo más bajo durante el período de referencia. Un umbral de compra se define como el máximo más alto menos 2.5 veces el rango promedio. Cuando el precio permanece por debajo de este nivel durante un número especificado de barras y la fuerza intrabarra (IBS) está por debajo de un límite dado dentro de la ventana de trading, se abre una posición larga. La posición se cierra si el cierre supera el máximo de la barra anterior.

## Detalles

- **Criterios de entrada**:
  - El precio se ha mantenido por debajo del umbral de compra durante `BarsBelowThreshold` barras.
  - IBS < `IbsBuyThreshold`.
  - Hora entre `StartTime` y `EndTime`.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - El precio de cierre supera el máximo de la barra anterior.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Length` = 20
  - `BarsBelowThreshold` = 2
  - `IbsBuyThreshold` = 0.2
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Largo
  - Indicadores: SMA, Highest, Lowest
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
