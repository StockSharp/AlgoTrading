# Estrategia CloseDeleteEA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia CloseDeleteEA reproduce la utilidad de MetaTrader que cierra posiciones en masa y elimina órdenes pendientes. Escanea periódicamente la cartera seleccionada y envía órdenes de mercado o solicitudes de cancelación según filtros definidos por el usuario. Esto la hace útil para liquidación de emergencia o escenarios de limpieza cuando la gestión manual de órdenes es demasiado lenta.

## Funciones clave
- Cierra exposición larga y/o corta con órdenes de mercado.
- Cancela órdenes pendientes que coinciden con los filtros configurados.
- Filtros opcionales de ganancia/pérdida para evitar tocar posiciones específicas.
- Restringe el escaneo al valor actual o procesa toda la cartera.
- Filtra posiciones y órdenes por identificador de estrategia.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `CloseBuyPositions` | Cierra exposición larga que coincide con los filtros. |
| `CloseSellPositions` | Cierra exposición corta que coincide con los filtros. |
| `CloseMarketPositions` | Habilita el módulo de cierre de posiciones de mercado. |
| `CancelPendingOrders` | Habilita la cancelación de órdenes pendientes. |
| `CloseOnlyProfitable` | Cierra posiciones solo cuando el PnL actual es no negativo. |
| `CloseOnlyLosing` | Cierra posiciones solo cuando el PnL actual es no positivo. |
| `ApplyToCurrentSecurity` | Cuando es true, solo se escanea el valor de la estrategia. De lo contrario se procesan todos los valores de la cartera. |
| `TargetStrategyId` | Filtro opcional de identificador de estrategia (valor vacío coincide con todo). |
| `TimerInterval` | Frecuencia del temporizador usada para el bucle de gestión. |

## Notas de uso
1. Adjunte la estrategia a un conector con una cartera asignada.
2. Configure filtros opcionalmente antes de iniciar la estrategia.
3. Inicie la estrategia para activar el ciclo close/delete. La estrategia se detiene automáticamente cuando no quedan posiciones u órdenes coincidentes.
4. Tenga en cuenta que las solicitudes de cancelación solo pueden dirigirse a órdenes visibles para la estrategia a través del conector.

## Diferencias frente a la versión MQL
- StockSharp trabaja con posiciones agregadas, por lo que el control individual a nivel de ticket se sustituye por gestión de exposición neta basada en volumen.
- El filtrado por id de estrategia imita el concepto original de magic number.
- Los elementos visuales de limpieza del gráfico de MetaTrader no se reproducen.
