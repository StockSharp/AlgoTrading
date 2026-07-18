# Estrategia de Máximos y Mínimos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera en los puntos medios de velas en relación con el rango de máximos y mínimos.

Compra cuando el punto medio de la vela actual está por debajo del promedio de los valores máximo y mínimo y la distancia normalizada está por debajo de LowThreshold. Cierra la posición larga cuando el punto medio sube por encima del promedio y la distancia normalizada está por encima de HighThreshold.

## Detalles

- **Criterios de entrada**: Punto medio por debajo del promedio y distancia normalizada por debajo de LowThreshold.
- **Largo/Corto**: Solo largo.
- **Criterios de salida**: Punto medio por encima del promedio y distancia normalizada por encima de HighThreshold.
- **Stops**: No.
- **Valores predeterminados**:
  - `Range` = 100
  - `LowThreshold` = 15m
  - `HighThreshold` = 85m
  - `CandleType` = TimeSpan.FromMinutes(240)
- **Filtros**:
  - Categoría: Rango
  - Dirección: Largo
  - Indicadores: Highest, Lowest
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (240m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
