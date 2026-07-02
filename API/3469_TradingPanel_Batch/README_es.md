# Estrategia por lotes del panel comercial
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
`TradingPanelBatchStrategy` es una versión StockSharp del asesor experto MetaTrader 4 **EA_TradingPanel**. El script original exponía un panel manual donde el comerciante configuraba el número de operaciones simultáneas, el tamaño del lote y las distancias de protección antes de presionar **COMPRAR** o **VENDER**. En la versión StockSharp, el mismo comportamiento está automatizado: una vez que el operador establece el parámetro `Direction`, la estrategia dispara un lote de órdenes de mercado en la siguiente vela terminada y restablece instantáneamente la dirección a `None`.

La lógica es intencionalmente simple para que el módulo pueda combinarse con señales externas o supervisión manual. Todas las órdenes heredan distancias opcionales de stop-loss y take-profit medidas en pips, lo que refleja los controles de riesgo disponibles en la implementación MQL.

## Flujo de trabajo
1. Cuando comienza la estrategia, calcula el tamaño del pip desde `Security.PriceStep`. Para símbolos Forex de 1/3/5 dígitos, el valor se multiplica por diez, lo que coincide con la conversión MetaTrader entre puntos y pips.
2. Si las compensaciones de stop-loss o take-profit son distintas de cero, la estrategia permite a `StartProtection` gestionar salidas con órdenes de mercado.
3. La estrategia se suscribe a la serie de velas especificada por `CandleType`. Después de cada vela terminada, verifica el parámetro `Direction`.
4. Si se solicita una dirección y el motor permite operar, la estrategia envía `NumberOfOrders` órdenes de mercado usando `OrderVolume` para cada ticket.
5. Una vez enviado el lote, la estrategia registra la acción y automáticamente establece `Direction` de nuevo en `None`, listo para la siguiente activación manual.

Este diseño mantiene el módulo sin estado entre ejecuciones. Los comerciantes pueden configurar repetidamente `Direction` en `Buy` o `Sell` cada vez que requieran un nuevo lote de pedidos; la ejecución siempre ocurre en la siguiente vela completa para evitar actuar sobre datos de mercado parcialmente formados.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| ---- | ---- | ------- | ----------- |
| `NumberOfOrders` | `int` | `1` | Número de órdenes de mercado enviadas en el siguiente lote. |
| `OrderVolume` | `decimal` | `0.01` | Volumen aplicado a cada orden de mercado. |
| `StopLossPips` | `decimal` | `2` | Distancia de stop-loss convertida de pips a precio absoluto utilizando los metadatos del instrumento actual. Establezca en `0` para desactivar. |
| `TakeProfitPips` | `decimal` | `10` | Distancia de toma de ganancias en pips. Establezca en `0` para desactivar. |
| `Direction` | `TradeDirection` | `None` | Dirección solicitada para la próxima ejecución. La estrategia restablece el valor después de realizar las órdenes. |
| `CandleType` | `DataType` | `TimeFrameCandle(1m)` | Serie de velas utilizadas para desencadenar la ejecución. |

## Notas
- La estrategia requiere un `Security` válido con un `PriceStep` configurado correctamente (y opcionalmente `Decimals`). Sin estos metadatos, los cálculos de pips vuelven a `1`.
- `StartProtection` utiliza órdenes de mercado para salidas para imitar cómo el panel MQL cerró posiciones en niveles de stop-loss o take-profit.
- Debido a que la ejecución ocurre en velas terminadas, los operadores pueden sincronizar lotes de órdenes con análisis personalizados o señales externas actualizando `Direction` antes de que se cierre la vela.
