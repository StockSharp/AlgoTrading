# Estrategia de Ruptura (BreakThrough)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia BreakThrough ejecuta operaciones cuando el precio cruza niveles de tendencia definidos por el usuario.
Se utilizan dos niveles principales:
- **Buy Line** – nivel de precio para activar una posición larga.
- **Sell Line** – nivel de precio para activar una posición corta.

Una vez que se cruza una línea desde el lado opuesto, la estrategia entra al mercado en esa dirección.
Líneas adicionales opcionales permiten cerrar una posición cuando el precio toca un nivel específico.
Las distancias protectoras de stop-loss, take-profit y trailing stop se miden en pips desde el precio de entrada.

## Detalles

- **Criterios de entrada**:
  - **Largo**: el precio cruza por encima o por debajo de la Buy Line dependiendo de su posición inicial.
  - **Corto**: el precio cruza por encima o por debajo de la Sell Line dependiendo de su posición inicial.
- **Largo/Corto**: ambos lados.
- **Criterios de salida**:
  - El precio alcanza una línea opcional de take-profit o stop-loss.
  - El precio alcanza la distancia de take-profit o stop-loss en pips.
  - Se activa el trailing stop.
- **Stops**: sí, usando `StopLossPips`, `TakeProfitPips` y `TrailingStopPips`.
- **Valores predeterminados**:
  - `BuyLinePrice` = 0 (desactivado)
  - `SellLinePrice` = 0 (desactivado)
  - `TakeProfitPips` = 100
  - `StopLossPips` = 30
  - `TrailingStopPips` = 20
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Simple
  - Marco temporal: Cualquiera (predeterminado 1 minuto)
  - Nivel de riesgo: Medio
