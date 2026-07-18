# Estrategia HistoryInfoEaStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
**HistoryInfoEaStrategy** replica la utilidad MT4 "HistoryInfo" sobre StockSharp. En lugar de dibujar texto en el gráfico de MetaTrader, la estrategia escucha el flujo `OnNewMyTrade` y agrega estadísticas de operaciones que coinciden con un filtro elegido. Los valores agregados se exponen mediante la propiedad `LastSnapshot` y se reflejan en el log de la estrategia para que una GUI o un script de automatización pueda mostrar el resumen en el formato preferido.

La estrategia nunca registra sus propias órdenes. Está diseñada para ejecutarse junto con otras estrategias automáticas o manuales mientras estas envían órdenes al bróker. Cada operación ejecutada que satisface el filtro contribuye a los totales.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `FilterType` | Modo de selección que determina cómo se emparejan las operaciones. Valores admitidos: `CountByUserOrderId`, `CountByComment`, `CountBySecurity`. |
| `MagicNumber` | `Order.UserOrderId` esperado. Se usa solo cuando `FilterType` es igual a `CountByUserOrderId`. Dejar vacío para desactivar este filtro. |
| `OrderComment` | Prefijo que debe coincidir con `Order.Comment`. Solo es relevante para el modo `CountByComment`. El valor predeterminado (`\"OrdersComment\"`) imita el placeholder del script MT4 y normalmente no coincide con ninguna orden hasta reemplazarlo. |
| `SecurityId` | Identificador del instrumento (`Security.Id`) que debe coincidir cuando `FilterType` es igual a `CountBySecurity`. El valor predeterminado (`\"OrdersSymbol\"`) es un placeholder. |

## Métricas agregadas
`LastSnapshot` se actualiza después de cada operación coincidente. Contiene:

- `FirstTrade` / `LastTrade` - marcas temporales de las operaciones procesadas más antigua y más reciente.
- `TotalVolume` - volumen ejecutado acumulado expresado en las unidades de volumen de la operación (lotes, contratos, etc.).
- `TotalProfit` - suma de `MyTrade.PnL` menos la comisión reportada, lo que da la ganancia realizada en la divisa de la cuenta.
- `TotalPips` - ganancia convertida a pips usando `Security.PriceStep`, `Security.StepPrice` y manejo de dígitos similar a MT4 (5/3 dígitos multiplican el punto por 10).
- `TradeCount` - número de operaciones que pasaron el filtro.

La misma información se imprime en el log de la estrategia en una sola línea, emulando la salida `Comment()` de MT4 para inspección rápida.

## Uso
1. Adjunte la estrategia a la misma cartera y valor que otras estrategias usan para enviar órdenes.
2. Elija el `FilterType` deseado y complete el parámetro asociado (magic number, prefijo de comentario o identificador de valor).
3. Inicie la estrategia. Tan pronto como se ejecute la primera operación que coincide con los criterios, los totales estarán disponibles mediante `LastSnapshot` y el log.
4. Los contadores se reinician automáticamente en cada reinicio de estrategia o reinicio manual.

> **Nota:** Para calcular totales de pips, la estrategia depende de metadatos correctos del instrumento. Asegúrese de que `Security.PriceStep` y `Security.StepPrice` estén configurados en la definición del board. Si falta cualquiera de los valores, el contador de pips permanece en cero mientras la ganancia sigue acumulándose.

## Notas de conversión
- El código MT4 iteraba sobre `OrdersHistoryTotal()` en cada tick. En StockSharp, la estrategia reacciona a notificaciones `MyTrade` en tiempo real, por lo que no hay polling y los cálculos se actualizan inmediatamente cuando llega una ejecución.
- MT4 guardaba la ganancia como `OrderProfit + OrderCommission + OrderSwap`. StockSharp entrega la ganancia realizada mediante `MyTrade.PnL` y la comisión por separado; el swap normalmente ya está incluido en el PnL. La adaptación resta la comisión de `PnL` para mantener consistencia con el reporte original.
- Los placeholders de cadena (`\"OrdersComment\"`, `\"OrdersSymbol\"`) se conservan para parecerse a los valores predeterminados originales. Reemplácelos por valores reales antes de iniciar la estrategia si espera coincidencias.
- La salida visual de gráfico de MT4 se sustituye por datos estructurados (`LastSnapshot`) y líneas de log para que los integradores decidan cómo representar la información.
- La estrategia evita deliberadamente crear órdenes nuevas, por lo que puede iniciarse en modo solo lectura para analizar flujos de operaciones de terceros sin interferir con ellos.

## Ideas de extensibilidad
- Suscríbase a las actualizaciones de `LastSnapshot` y reenvíe la información a un dashboard o colector de telemetría.
- Amplíe la clase con filtros adicionales (por ejemplo, por cartera o etiquetas de estrategia personalizadas) si el conector proporciona los metadatos relevantes.
- Combine la estrategia con un temporizador periódico para exportar resúmenes históricos a un reporte CSV/JSON.
