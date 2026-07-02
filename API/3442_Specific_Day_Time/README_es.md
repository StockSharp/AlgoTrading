# Órdenes de días y horas específicas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el experto MetaTrader *"Día y hora específicos del Asesor Experto"*.
Coloca órdenes de compra y/o venta en una marca de tiempo programada y, opcionalmente, elimina cada exposición en otra marca de tiempo.
La versión StockSharp mantiene el comportamiento original de gestión de riesgos, incluidos trailingstops opcionales y movimientos de equilibrio.

## Lógica central

1. **Programación**
   - `OpenTime` – momento en el que se crean las órdenes.
   - `CloseTime` – momento en el que las posiciones se aplanan y las órdenes pendientes se pueden eliminar.
Ambas comprobaciones aceptan una ventana de un minuto, que coincide con la comparación `TimeToString(..., TIME_MINUTES)` utilizada en el código MT4.

2. **Realización de pedidos**
   - `OrderPlacement` elige entre órdenes de mercado, stop o límite.
   - `OpenBuyOrders` / `OpenSellOrders` habilita las direcciones deseadas.
   - `OrderDistancePoints` compensa el precio de las órdenes pendientes en una cantidad de puntos (pips).
   - `PendingExpireMinutes` cancela los pedidos pendientes automáticamente cuando finaliza su ventana de validez.

3. **Gestión de volumen**
   - `LotSizing = Manual` envía el `ManualVolume` fijo.
   - `LotSizing = Automatic` calcula el volumen a partir del valor actual de la cartera y el tamaño del contrato del instrumento:
`volume = (portfolio / contractSize) * RiskFactor`.
El resultado se alinea con `Security.VolumeStep` y se fija entre `MinVolume`/`MaxVolume` cuando esté disponible.

4. **Lógica de protección**
   - `StopLossPoints` y `TakeProfitPoints` traducen las distancias originales basadas en puntos a precios absolutos utilizando el tamaño del pip del instrumento.
   - `TrailingStopEnabled` + `TrailingStepPoints` y `BreakEvenEnabled` mueven la parada de protección exactamente como el script MQL, utilizando actualizaciones de oferta/demanda como activadores.
   - Cuando se alcanzan las condiciones de límite de pérdidas o toma de ganancias, la posición se cierra con una orden de mercado, reflejando el comportamiento de MT4 de modificar los límites a un nuevo precio.

5. **Fase de cierre**
   - Cuando `CloseOwnOrders` o `CloseAllOrders` está habilitado, la estrategia sale de cualquier posición abierta en la ventana de cierre.
   - `DeletePendingOrders` elimina todas las órdenes pendientes restantes al mismo tiempo.

## Parámetros

| Nombre | Descripción |
|------|-------------|
| `OpenTime`, `CloseTime` | Marcas de tiempo UTC para entrar y salir del mercado. |
| `OrderPlacement` | Colocación de órdenes de mercado, stop o límite. |
| `OpenBuyOrders`, `OpenSellOrders` | Instrucciones para activar. |
| `TakeProfitPoints`, `StopLossPoints` | Distancias de protección expresadas en puntos (0 inhabilitaciones). |
| `TrailingStopEnabled`, `TrailingStepPoints` | Habilite el trailing stop y defina el avance mínimo antes de moverlo. |
| `BreakEvenEnabled`, `BreakEvenAfterPoints` | Cambie el límite al punto de equilibrio una vez que la ganancia supere el umbral. |
| `OrderDistancePoints` | Compensación utilizada para órdenes pendientes. |
| `PendingExpireMinutes` | Ventana de vencimiento para órdenes pendientes. |
| `LotSizing` | Dimensionamiento de volumen manual o automático. |
| `RiskFactor`, `ManualVolume` | Entradas para los modos de dimensionamiento. |
| `CloseOwnOrders`, `CloseAllOrders`, `DeletePendingOrders` | Controla cómo se cierran al final las posiciones y órdenes pendientes. |

## Notas

- La clase reside en el espacio de nombres `StockSharp.Samples.Strategies` con sangría de tabulación como lo exigen las pautas del proyecto.
- Los datos de nivel 1 se utilizan para reproducir la lógica sensible a la oferta y la demanda de la versión MQL (parada dinámica, colocación de orden pendiente).
- La configuración de `MagicNumber` de MT4 no es necesaria porque StockSharp ya aísla las órdenes estratégicas.

## Uso

1. Compile el proyecto a través de `AlgoTrading.sln` y adjunte la estrategia a un par de valores/cartera.
2. Ajuste el cronograma, el tipo de orden y los parámetros de riesgo según sea necesario.
3. Inicie la estrategia antes de `OpenTime`; Los pedidos se enviarán automáticamente una vez que comience la ventana.
4. Mantenga la estrategia ejecutándose hasta después de `CloseTime` si desea que se active el paso de aplanamiento automático.
