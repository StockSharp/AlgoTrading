# Estrategia de Bollinger Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Bollinger Breakout busca capturar movimientos que empujan más allá de las Bollinger
Bands y continúan en esa dirección. Cuando el precio cierra por encima de la banda
superior o por debajo de la banda inferior, la estrategia entra en la dirección del
rompimiento si las confirmaciones opcionales apoyan la operación.

Los filtros de RSI, Aroon y media móvil pueden habilitarse para validar el impulso
y la tendencia. Un stop-loss opcional ayuda a controlar el riesgo. Las posiciones se
cierran cuando el precio alcanza la banda opuesta o se activa el stop.

Este enfoque favorece mercados propensos a tendencias fuertes donde las rupturas de
banda conducen a continuación en lugar de reversión a la media.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: Cierre por encima de la banda superior y todos los filtros habilitados confirman.
  - **Corto**: Cierre por debajo de la banda inferior y todos los filtros habilitados confirman.
- **Criterios de salida**: Toque de la banda opuesta o stop-loss si `UseSL`.
- **Stops**: Stop-loss opcional (`UseSL`).
- **Valores predeterminados**:
  - `UseRSI` = True
  - `UseAroon` = False
  - `UseMA` = True
  - `UseSL` = True
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Largo/Corto
  - Indicadores: Bollinger Bands, RSI, Aroon, Moving Average
  - Complejidad: Moderado
  - Nivel de riesgo: Alto
