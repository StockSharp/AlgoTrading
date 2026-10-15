# Estrategia de Trailing Stop Trigger Manager
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Trailing Stop Trigger Manager** es una StockSharp adaptación del MetaTrader asesor experto `Trailing Sl.mq5`. El EA original
no abrió operaciones por sí solo. En lugar de ello, supervisó las posiciones ya abiertas con un *número mágico* coincidente y reforzó sus
niveles de stop-loss cuando el mercado se movía en la dirección deseada. Esta implementación de C# reproduce ese comportamiento usando
La estrategia de alto nivel de StockSharp API, que ofrece una gestión transparente de trailing-stop que funciona con cualquier instrumento compatible con
StockSharp.

## Lógica final
1. Suscríbete al libro de pedidos para leer las últimas mejores ofertas y mejores cotizaciones.
2. Detecta si la estrategia mantiene actualmente una posición neta larga o corta.
3. Calcula el beneficio flotante utilizando el lado apropiado del mercado (mejor oferta para posiciones largas, mejor demanda para posiciones cortas).
4. Activa el modo de seguimiento una vez que la ganancia excede `TriggerPoints` (convertida a unidades de precio hasta `PriceStep`).
5. Establece el trailing stop a la distancia configurada `TrailingPoints` de la cotización de mercado actual.
6. Mueve el trailing stop solo hacia el mercado para seguir obteniendo ganancias adicionales.
7. Envía una orden de mercado para aplanar la posición tan pronto como la mejor cotización toque el nivel de trailing stop calculado.

## Gestión de pedidos y riesgos.
- La estrategia **no** envía órdenes de entrada iniciales. Solo gestiona una posición existente que puede haber sido abierta manualmente.
o por otra estrategia.
- Las salidas de mercado se colocan con `BuyMarket`/`SellMarket`, reflejando las llamadas `PositionModify` del código MetaTrader original.
- La distancia de parada escala automáticamente con el `PriceStep` del instrumento, lo que preserva la configuración basada en puntos de
el EA.
- Una vez que se cierra la posición, el estado final se restablece para que las nuevas posiciones comiencen desde cero.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `TrailingPoints` | `int` | `1000` | Distancia entre el precio actual y el trailing stop, medida en incrementos de precio. |
| `TriggerPoints` | `int` | `1500` | Beneficio mínimo en los pasos de precio requeridos para comenzar a rastrear la posición. |

## Notas de uso
- Adjunte la estrategia al valor cuya posición desea supervisar. Inmediatamente comenzará a rastrear los existentes.
exposición.
- Configure el `Volume` inicial de la estrategia para que coincida con el tamaño de su posición abierta. StockSharp utiliza posiciones netas, por lo que el
La estrategia saldrá de todo el lote cuando se active el trailing stop.
- Si el corredor ofrece incrementos de precios aproximados, ajuste `TrailingPoints` y `TriggerPoints` en consecuencia para evitar salidas prematuras.
- La estrategia mantiene su estado completamente dentro de StockSharp, por lo que se puede combinar con cualquier sistema discrecional o automatizado que
deja la ejecución real de la orden en StockSharp.

## Diferencias con el experto MetaTrader original
- MetaTrader gestionó posiciones separadas por ticket y las filtró por *número mágico*. StockSharp trabaja con una posición neta por
seguridad, eliminando la necesidad de filtrar tickets.
- Las entradas `Setloss`, `TakeProfit` y `Lots` no se usaron en el EA original. Por lo tanto, se omiten en el StockSharp
versión para mantener la configuración centrada en el comportamiento de seguimiento.
- Las modificaciones de órdenes se reemplazan por salidas directas del mercado, que es el enfoque idiomático para compensar cuentas en StockSharp.
