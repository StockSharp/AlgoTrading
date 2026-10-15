# Estrategia de niveles de stock
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de niveles de Stoch** es una conversión directa del MetaTrader 4 asesor experto `Stoch.mq4`. El script original se basa en los límites de la sesión diaria, calcula niveles de precios personalizados a partir de la vela anterior y coloca dos órdenes pendientes para la siguiente sesión. Esta versión de C# mantiene la misma idea comercial y la implementa con la estrategia de alto nivel de StockSharp API.

La estrategia calcula un rango de negociación sintético ampliando el diferencial máximo/mínimo de la vela anterior mediante un multiplicador configurable (predeterminado `1.1`). Luego posiciona:

- Una orden de **límite de venta** por encima del cierre anterior en la mitad del rango ampliado.
- Una orden de **límite de compra** por debajo del cierre anterior en la mitad del rango ampliado.

Cada vez que se ejecuta una orden pendiente, la estrategia adjunta inmediatamente salidas de grupo (stop-loss y take-profit) utilizando las distancias definidas en los pasos de precio. Todas las exposiciones pendientes y órdenes pendientes se borran al comienzo de cada nuevo día de negociación, reflejando el bloque de reinicio de medianoche del script MQL.

## Lógica de trading
1. Suscríbase a la serie de velas configuradas (diariamente de forma predeterminada) y espere hasta que las velas estén completamente terminadas.
2. Cuando llega una nueva sesión:
   - Cierre cualquier posición abierta y cancele todas las órdenes de entrada o de protección.
   - Calcule el rango ampliado `range * RangeMultiplier` usando la vela anterior.
   - Realice nuevas órdenes limitadas de compra y venta en `Close + range / 2` y `Close - range / 2` respectivamente.
3. Al ejecutar la orden, cree órdenes coincidentes de stop-loss y take-profit utilizando las compensaciones de incremento de precio solicitadas.
4. Si se activa alguna de las órdenes de protección, cancele la orden de protección del hermano y espere hasta que se reinicie la siguiente sesión.

## Parámetros
| Nombre | Descripción | Predeterminado | Notas |
| --- | --- | --- | --- |
| `TakeProfitPoints` | Distancia de obtención de beneficios medida en incrementos de precios. | `20` | Equivalente a la entrada `TakeProfit` en el script MQL. Establezca en `0` para deshabilitar la orden de obtención de ganancias. |
| `StopLossPoints` | Distancia de stop-loss medida en pasos de precio. | `40` | Equivalente a la entrada `StopLoss` en el script MQL. Establezca en `0` para deshabilitar la orden de límite de pérdidas. |
| `RangeMultiplier` | Multiplicador aplicado al rango de velas anterior (`High - Low`). | `1.1` | Coincide con el factor de expansión `1.1` codificado en MQL. |
| `OrderVolume` | Volumen de cada orden pendiente. | `1` | Refleja el parámetro `Lots`. |
| `CandleType` | Serie de velas que define la sesión de negociación. | `Daily` | Personalice si la estrategia debe operar en otros plazos. |

Todos los parámetros se configuran a través de `Param()` para admitir la optimización y el enlace de la interfaz de usuario.

## Gestión del riesgo
- Las entradas largas reciben un soporte protector de **parada de venta** y **límite de venta**; los cortos obtienen las salidas reflejadas **parada de compra** y **límite de compra**.
- Los pedidos se dimensionan usando `OrderVolume`. Cuando se ejecuta un lado del corchete, la orden de protección restante se cancela para evitar salidas duplicadas.
- Se produce un reinicio completo en cada nueva vela, lo que garantiza que la estrategia no lleve exposición más allá de la sesión actual.

## Notas de conversión
- La implementación de MQL utilizó MetaTrader variables globales para evitar pedidos duplicados; la versión C# rastrea la última sesión procesada internamente (`_lastProcessedDay`).
- El ciclo de cierre nocturno se ha traducido al asistente `ResetOrders()` que cancela todas las órdenes pendientes y envía un comando de aplanamiento del mercado si permanece una posición.
- Los niveles de stop-loss y take-profit se recrean explícitamente mediante métodos de orden StockSharp en lugar de integrarse en los parámetros `OrderSend`.
- Las entradas de trailing stop, administración de dinero y riesgo presentes en el script MQL no se usaron allí y siguen sin ser compatibles en este puerto.

## Consejos de uso
1. Adjunte la estrategia a un valor y establezca `OrderVolume`, distancias de parada y tipo de vela para que coincidan con el instrumento negociado.
2. Asegúrese de que la seguridad exponga un `PriceStep` adecuado; de lo contrario, la estrategia vuelve a `1` y registra una advertencia.
3. Debido a que las órdenes se recalculan solo una vez por vela completa, mantenga el período de tiempo diario predeterminado para alinearse con el comportamiento original.
4. Revise los registros para confirmar el flujo de trabajo de reinicio diario, realización de pedidos y adjunto de órdenes de protección.

## Archivos
- `CS/StochLevelsStrategy.cs` – implementación de la estrategia principal.
- `README.md`, `README_zh.md`, `README_ru.md`: documentación multilingüe para la estrategia convertida.
