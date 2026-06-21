# Correlación de Volatilidad Crypto con Bitcoin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra en posición larga cuando la volatilidad de Bitcoin sube junto con el índice BVOL7D y el precio cotiza por encima de su EMA. Sale cuando el precio cae de nuevo por debajo de la EMA.

## Detalles

- **Criterios de entrada**: VIXFix mayor que el valor anterior, BVOL7D mayor que el valor anterior, cierre por encima de la EMA.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Cierre por debajo de la EMA.
- **Stops**: No.
- **Valores predeterminados**:
  - `VixFixLength` = 22
  - `EmaLength` = 50
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Volatilidad
  - Dirección: Solo largos
  - Indicadores: Highest, EMA
  - Stops: No
  - Complejidad: Principiante
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
