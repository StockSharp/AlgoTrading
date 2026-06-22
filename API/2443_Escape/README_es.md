# Estrategia de Escape
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera basándose en medias móviles simples de los precios de apertura de las velas. Compara el cierre más reciente de 5 minutos con dos medias móviles calculadas sobre el precio de apertura:

- **SMA rápida (4 períodos)** – utilizada como umbral para entradas cortas.
- **SMA lenta (5 períodos)** – utilizada como umbral para entradas largas.

## Cómo funciona

1. En cada vela de 5 minutos completada, la estrategia actualiza dos SMA del precio de apertura de las velas.
2. Si no hay posición activa:
   - Entrar en **largo** cuando el último cierre está por debajo de la SMA lenta.
   - Entrar en **corto** cuando el último cierre está por encima de la SMA rápida.
3. Tras entrar en una posición, la estrategia establece niveles fijos de stop-loss y take-profit medidos en unidades de precio.
4. La posición se cierra cuando se alcanza el take-profit o el stop-loss.

La lógica usa la API de alto nivel de StockSharp y está destinada a propósitos educativos.

## Parámetros

| Nombre | Descripción | Predeterminado |
|--------|-------------|----------------|
| `FastLength` | Período de la SMA rápida. | `4` |
| `SlowLength` | Período de la SMA lenta. | `5` |
| `TakeProfitLong` | Distancia de take-profit para operaciones largas en unidades de precio. | `25` |
| `TakeProfitShort` | Distancia de take-profit para operaciones cortas en unidades de precio. | `26` |
| `StopLossLong` | Distancia de stop-loss para operaciones largas en unidades de precio. | `25` |
| `StopLossShort` | Distancia de stop-loss para operaciones cortas en unidades de precio. | `3` |
| `CandleType` | Tipo de vela utilizado para el análisis. | `TimeFrame(5m)` |

Todos los parámetros pueden optimizarse mediante el optimizador de StockSharp.
