# Estrategia de comerciante Elite eFibo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Elite eFibo Trader reproduce el asesor experto en promedios que abre una progresión Fibonacci de órdenes mientras monitorea un cruce de promedio móvil y un filtro RSI opcional. El puerto StockSharp mantiene la lógica de la cesta original: una entrada al mercado activa una pila de órdenes stop pendientes espaciadas por distancias de pips configurables, y cada llenado adicional aumenta la exposición siguiendo la secuencia Fibonacci. La estrategia aplana automáticamente la cesta una vez que el beneficio flotante alcanza un objetivo de efectivo o cuando el filtro de tendencia se vuelve contra la exposición actual.

## Datos de mercado
- Se suscribe a un único tipo de vela configurable (predeterminado: velas de 15 minutos).
- Utiliza el cierre de la vela para los valores del indicador y para evaluar las condiciones de seguimiento/parada.

## Lógica de entrada
1. La dirección se determina mediante el cruce de media móvil (habilitado de forma predeterminada) o mediante los cambios manuales `ManualOpenBuy`/`ManualOpenSell`.
2. Cuando la lógica MA está activa, un cruce alcista (`fast` por encima de `slow`) compra cestas y un cruce bajista vende cestas. Se aplica una única señal por vela.
3. Si el filtro RSI está habilitado, las cestas largas requieren `RSI > RsiHigh` mientras que las cestas cortas requieren `RSI < RsiLow`.
4. Se abre una nueva escalera solo cuando no hay órdenes o posiciones activas de la estrategia y se permite el comercio (`TradeAgainAfterProfit`).
5. El primer nivel se abre con una orden de mercado, mientras que los niveles restantes se envían como órdenes stop compensadas por `LevelDistancePips`. Los volúmenes siguen la secuencia Fibonacci y se pueden ajustar nivel por nivel.

## Lógica de salida
- Cada nivel llenado recibe una parada inicial calculada a partir de `StopLossPips` y participa en una actualización final cuando la lógica MA detecta un cruce adverso.
- Las paradas se arrastran hasta `close - TrailingStopPips` para cestas largas y hasta `close + TrailingStopPips` para cestas cortas, y nunca se alejan más que la parada actual.
- Cuando el precio toca un nivel stop (basado en el máximo/mínimo de la vela), la estrategia cierra el volumen restante de ese nivel con una orden de mercado.
- Si el beneficio flotante de la cesta (calculado a partir del instrumento `PriceStep` y `StepPrice`) alcanza `MoneyTakeProfit`, se cierran todas las posiciones y se cancelan las órdenes pendientes.
- Una vez que la cesta está plana, cualquier orden stop pendiente se cancela automáticamente. Si `TradeAgainAfterProfit` es `false`, la estrategia permanece inactiva hasta que se restablece.

## Parámetros
| Nombre | Descripción |
| ---- | ----------- |
| `UseMaLogic` | Habilite o deshabilite la lógica de cruce de media móvil que establece la dirección comercial. |
| `MaSlowPeriod`, `MaFastPeriod` | Períodos de las SMA lentas y rápidas. |
| `TrailingStopPips` | Distancia de pip utilizada por el trailing stop protector cuando el filtro de tendencia se vuelve adverso. |
| `UseRsiFilter`, `RsiPeriod`, `RsiHigh`, `RsiLow` | RSI configuración del filtro. El filtro permite posiciones largas por encima de `RsiHigh` y cortas por debajo de `RsiLow`. |
| `ManualOpenBuy`, `ManualOpenSell` | Conmutaciones manuales utilizadas cuando la lógica MA está deshabilitada. |
| `TradeAgainAfterProfit` | Reanude las operaciones después de alcanzar la obtención de beneficios del dinero. |
| `LevelDistancePips` | Distancia en pips entre órdenes pendientes consecutivas. |
| `StopLossPips` | Desplazamiento de parada inicial para cada nivel. |
| `MoneyTakeProfit` | Objetivo de beneficio en efectivo evaluado en el PnL abierto de la cesta. |
| `Level1Volume` … `Level14Volume` | Volumen de cada nivel Fibonacci. Establezca en cero para desactivar un nivel. |
| `CandleType` | Plazo/tipo de datos utilizados para los indicadores. |

## Notas de implementación
- Las distancias de pip se convierten a partir de puntos estilo MetaTrader multiplicando el instrumento `PriceStep` por diez cuando el valor tiene 3 o 5 decimales. Esto refleja el ajuste original `MyPoint` para cotizaciones FX de 5 dígitos.
- Cada nivel se rastrea de forma independiente. La estrategia almacena el precio de entrada, el volumen restante y el nivel de parada, de modo que los llenados parciales y las paradas individuales se manejan de la misma manera que el experto MQL.
- El beneficio flotante se calcula a partir de `PriceStep` y `StepPrice`. Asegúrese de que las propiedades del instrumento estén configuradas; de lo contrario, la toma de ganancias del dinero no se activará correctamente.
- `StartProtection()` se invoca una vez durante el inicio para habilitar las comprobaciones de seguridad integradas de la clase base de estrategia StockSharp.
- Cuando no queda ningún volumen abierto, se llama automáticamente a `CancelAllPendingOrders()`, replicando las llamadas repetidas a `subCloseAllPending()` del script original.

## Consejos de uso
- Verifique la configuración del corredor para `PriceStep`, `StepPrice`, `VolumeStep` y el tamaño mínimo de lote para garantizar que los volúmenes de Fibonacci se traduzcan en pedidos válidos.
- La estrategia se basa en datos de velas; asegúrese de que el período de tiempo seleccionado coincida con el período del gráfico MetaTrader previsto.
- Considere ejecutar la estrategia primero en feeds de demostración: los sistemas promedio pueden acumular una gran exposición durante las tendencias adversas.
- Deshabilite `UseMaLogic` para reproducir el sesgo manual utilizado en las entradas originales EA o manténgalo habilitado para la detección automática de tendencias.
