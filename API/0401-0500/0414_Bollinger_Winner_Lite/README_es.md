# Estrategia Bollinger Winner Lite
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Bollinger Winner Lite es un sistema de reversión simplificado que reacciona cuando
el precio se estira más allá de las Bollinger Bands. Observa velas grandes que
cierran fuera de una banda y anticipa un rápido rebote de regreso al interior.

El parámetro `CandlePercent` define qué tan grande debe ser la vela de ruptura en
relación con los movimientos recientes. Solo las velas que superan este umbral
activan operaciones, filtrando pequeñas fluctuaciones. Por defecto la estrategia
opera solo en largo, pero habilitar `ShowShort` permite configuraciones de corto
simétricas.

Las salidas se producen cuando el precio toca la banda opuesta o regresa a la
línea central. No se utiliza stop duro; el sistema se basa en la reversión a la media.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: Cierre por debajo de la banda inferior con tamaño de vela > `CandlePercent`.
  - **Corto**: Cierre por encima de la banda superior con tamaño de vela > `CandlePercent` (requiere `ShowShort`).
- **Criterios de salida**: Toque de la banda central o la banda opuesta.
- **Stops**: Ninguno por defecto.
- **Valores predeterminados**:
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `CandlePercent` = 30
  - `ShowShort` = false
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Solo largos por defecto
  - Indicadores: Bollinger Bands
  - Complejidad: Simple
  - Nivel de riesgo: Medio
