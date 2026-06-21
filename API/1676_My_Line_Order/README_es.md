# My Line Order
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia dispara órdenes de mercado cuando el precio cruza niveles horizontales predefinidos. El usuario especifica niveles separados para entradas largas y cortas y parámetros de riesgo en pips. Tras abrir una posición, la estrategia rastrea el stop-loss, take-profit y el trailing stop opcional.

El sistema es adecuado para configuraciones discrecionales donde los niveles de entrada se conocen de antemano. Funciona con cualquier instrumento y marco temporal porque solo depende de niveles de precio.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El precio de cierre cruza por encima de `BuyPrice`.
  - **Corto**: El precio de cierre cruza por debajo de `SellPrice`.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Stop-loss en `StopLossPips`.
  - Take-profit en `TakeProfitPips`.
  - Trailing stop si `TrailingStopPips` > 0.
- **Stops**: Sí, en pips.
- **Valores predeterminados**:
  - `BuyPrice` = 0 (desactivado)
  - `SellPrice` = 0 (desactivado)
  - `TakeProfitPips` = 30
  - `StopLossPips` = 20
  - `TrailingStopPips` = 0
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Manual
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
