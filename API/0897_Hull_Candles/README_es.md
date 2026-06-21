# Estrategia Hull Candles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Hull Candles es una estrategia de seguimiento de tendencia simple que utiliza una Hull Moving Average del precio promedio (OHLC4). Cuando el HMA sube y el cierre está por encima de su SMA, abre posiciones largas; cuando el HMA cae y el cierre está por debajo de su SMA, abre posiciones cortas.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: HMA sube y cierre > SMA.
  - **Corto**: HMA cae y cierre < SMA.
- **Criterios de salida**: Señal inversa.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `BodyLength` = 10
  - `SmaLength` = 1
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo/Corto
  - Indicadores: HMA, SMA
  - Complejidad: Bajo
  - Nivel de riesgo: Alto
