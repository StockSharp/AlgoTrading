# Estrategia Limits Bot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Coloca órdenes límite simétricas alrededor del precio de apertura de cada vela y protege las posiciones con stop-loss, take-profit y trailing opcional.

## Detalles

- **Entrada**:
  - Compra límite en `Open - StopOrderDistance * PriceStep` si el trading largo está habilitado.
  - Venta límite en `Open + StopOrderDistance * PriceStep` si el trading corto está habilitado.
- **Salida**: Cierre a mercado al activarse el stop-loss, take-profit o trailing stop.
- **Largo/Corto**: Ambos.
- **Stops**: Stop-loss fijo con opción de trailing.
- **Valores predeterminados**:
  - `StopOrderDistance` = 5
  - `TakeProfit` = 35
  - `StopLoss` = 8
  - `TrailingStart` = 40
  - `TrailingDistance` = 30
  - `TrailingStep` = 1
  - `CandleType` = 1 minuto
- **Sesión**: Opera solo entre `StartTime` y `EndTime`.
- **Filtros**:
  - Categoría: Price action
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
