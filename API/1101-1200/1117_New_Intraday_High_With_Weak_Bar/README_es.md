# Estrategia de Nuevo Máximo Intradía con Barra Débil
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Entra largo en un nuevo máximo de `HighestLength` barras cuando la vela cierra cerca de su mínimo. Sale cuando el precio cierra por encima del máximo de la barra anterior.

## Detalles

- **Criterios de entrada**:
  - Sin posición, el máximo es igual al máximo de las últimas `HighestLength` barras y `(close - low)/(high - low) < WeakRatio`.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Cierre por encima del máximo de la barra anterior.
- **Stops**: No.
- **Valores predeterminados**:
  - `HighestLength` = 10
  - `WeakRatio` = 0.15
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Solo largos
  - Indicadores: Highest
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
