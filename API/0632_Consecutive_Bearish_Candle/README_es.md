# Estrategia de Velas Bajistas Consecutivas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Entra en largo después de una serie de velas bajistas y sale cuando el precio rompe por encima del máximo anterior.

Este enfoque de reversión a la media compra tras una presión bajista excesiva, buscando un rebote una vez que los vendedores se agotan.

## Detalles

- **Criterios de entrada**: `N` velas bajistas consecutivas dentro de la ventana temporal.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Cierre por encima del máximo anterior.
- **Stops**: No.
- **Valores predeterminados**:
  - `Lookback` = 3
  - `CandleType` = TimeSpan.FromDays(1)
  - `StartTime` = 2014-01-01
  - `EndTime` = 2099-01-01
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Largo
  - Indicadores: Price Action
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
