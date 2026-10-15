# Estrategia Target EA Manager
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **estrategia Target EA Manager** es una adaptación fiel a StockSharp del experto de MetaTrader *TargetEA_v1.5*. La estrategia no abre operaciones nuevas por sí misma. En su lugar, monitoriza constantemente la ganancia y pérdida flotante de las órdenes que ya pertenecen a la estrategia y, si es necesario, liquida posiciones y cancela órdenes pendientes cuando se alcanzan umbrales definidos por el usuario. El comportamiento reproduce la lógica de gestión de "cesta" del experto original: las órdenes de compra y venta pueden evaluarse de forma independiente o como una sola cesta combinada.

La estrategia se suscribe a datos Level1 (mejor bid y ask) y usa la API de alto nivel para cierres de posición y cancelación de órdenes. Las cotizaciones bid y ask en tiempo real se traducen en métricas de ganancia no realizada para la exposición abierta.

## Funciones clave
- **Cestas independientes o combinadas** - elija si las órdenes largas y cortas se tratan por separado o juntas mediante `ManageBuySellOrders`.
- **Múltiples tipos de objetivo** - los umbrales pueden expresarse en pips, en divisa de la cuenta por lote o como porcentaje del balance de la cartera, coincidiendo con el flag `TypeTargetUse` de la versión MQL.
- **Disparadores de ambos lados** - interruptores separados para reaccionar a ganancias flotantes (`CloseInProfit`) y pérdidas flotantes (`CloseInLoss`).
- **Limpieza de órdenes pendientes** - cancelación opcional de órdenes pendientes de compra y/o venta cada vez que se cierra una cesta.
- **Operaciones de alto nivel** - las salidas de mercado se ejecutan con `BuyMarket` / `SellMarket`, y las órdenes pendientes se cancelan mediante la colección de órdenes de la estrategia.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `ManageBuySellOrders` | `Separate` emula dos cestas (larga y corta), `Combined` fusiona ambos lados. |
| `CloseBuyOrders` / `CloseSellOrders` | Habilita la liquidación del lado correspondiente. |
| `DeleteBuyPendingPositions` / `DeleteSellPendingPositions` | Cancela órdenes pendientes activas después de que se cierre una cesta. |
| `TypeTargetUse` | `Pips`, `CurrencyPerLot` o `PercentageOfBalance` seleccionan la medición aplicada al PnL abierto. |
| `CloseInProfit` / `CloseInLoss` | Activa disparadores de ganancia o pérdida. |
| `TargetProfitInPips`, `TargetLossInPips` | Umbrales en pips. Cuando el instrumento proporciona `PriceStep`, el valor de pip se calcula como `priceDifference / PriceStep * (volume / VolumeStep)`. |
| `TargetProfitInCurrency`, `TargetLossInCurrency` | Ganancia o pérdida flotante por lote, multiplicada por el volumen actual antes de la comparación. |
| `TargetProfitInPercentage`, `TargetLossInPercentage` | Porcentaje del balance de la cartera que debe alcanzarse antes del cierre. El experto original compara la ganancia flotante bruta con `Balance ± Balance * Percentage / 100`, y esta adaptación conserva intacta esa convención. |

## Comportamiento
1. **Seguimiento de estado** - las operaciones ejecutadas actualizan los totales internos de volumen largo y corto y sus precios medios ponderados. Por tanto, las posiciones cubiertas (largas y cortas) se manejan correctamente.
2. **Cálculo de PnL** - cada actualización Level1 refresca los valores bid/ask, a partir de los cuales se calculan ganancias en pips y divisa para ambos lados.
3. **Evaluación de objetivos** - según el modo de objetivo y el modo de cesta, se comprueban los umbrales correspondientes. Las comprobaciones de ganancia requieren valores *mayores o iguales* a los objetivos configurados, mientras que las comprobaciones de pérdida usan comparaciones *menores o iguales*, coincidiendo con la lógica MQL.
4. **Liquidación de cesta** - cuando una condición se satisface, la estrategia cancela opcionalmente órdenes pendientes de ese lado y envía la orden de mercado necesaria para aplanar la exposición abierta.

La implementación evita intencionalmente colecciones adicionales o almacenamiento de indicadores, y se apoya en la API de alto nivel de StockSharp, igual que el EA original.
