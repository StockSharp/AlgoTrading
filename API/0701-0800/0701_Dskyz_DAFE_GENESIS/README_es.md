# Estrategia Dskyz (DAFE) GENESIS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Versión simplificada de la estrategia Dskyz (DAFE) GENESIS. El sistema opera cuando el momentum a corto plazo se alinea con un filtro de tendencia y el RSI.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `SMA(9) > SMA(30)` y `RSI > 55` y `EMA(8) > EMA(21)`.
  - **Corto**: `SMA(9) < SMA(30)` y `RSI < 45` y `EMA(8) < EMA(21)`.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**:
  - **Largo**: `EMA(8) < EMA(21)`.
  - **Corto**: `EMA(8) > EMA(21)`.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `RSI Length` = 9.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: RSI, EMA, SMA
  - Stops: No
  - Complejidad: Simple
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
