# Estrategia de cuadrícula de pisada pendiente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de cuadrícula pendiente** es una fiel StockSharp versión del MetaTrader 4 asesor experto `Pending_tread.mq4`. El EA original reconstruye constantemente dos escaleras de órdenes pendientes: una escalera por encima del mercado y otra por debajo. Cada escalera se puede configurar para utilizar órdenes de compra o venta, y el espaciado se define en pips. La implementación StockSharp reproduce el mismo comportamiento a través del API de alto nivel sin introducir indicadores o colecciones adicionales.

## Lógica de trading
1. **Mantenimiento basado en oferta/demanda**: la estrategia se suscribe a cotizaciones de nivel 1 (`SubscribeLevel1`) y mantiene los precios de oferta y demanda más recientes. Cada vez que llegan nuevos datos, se ejecuta la rutina de mantenimiento (con un acelerador configurable) y compara las órdenes pendientes existentes con el tamaño de la red configurada.
2. **Escalera por encima del mercado**: dependiendo de `AboveMarketSide`, el algoritmo coloca órdenes de límite de compra o de venta en incrementos de `PipStep` pips por encima del mercado. Cada nuevo pedido recibe su propio nivel de toma de ganancias, compensado por `TakeProfitPips` pips.
3. **Escalera por debajo del mercado**: el parámetro `BelowMarketSide` selecciona entre órdenes de límite de compra y órdenes de parada de venta apiladas por debajo del mercado. Se aplica la misma lógica de separación de pips y obtención de beneficios.
4. **Guardia de nivel de parada**: el parámetro `MinStopDistancePoints` emula la comprobación MetaTrader `MODE_STOPLEVEL`. Las órdenes se omiten cuando la distancia entre el precio y el ancla de oferta/demanda relevante es menor que el límite proporcionado.
5. **Acelerador**: `ThrottleSeconds` refleja el acelerador original de cinco segundos que evitó errores `TRADE_CONTEXT_BUSY`. Sólo se ejecuta un ciclo de mantenimiento durante ese intervalo, independientemente de cuántos ticks lleguen.

Todas las entradas basadas en pips (`PipStep`, `TakeProfitPips`) se convierten en compensaciones de precios absolutos utilizando el instrumento `PriceStep` y `Decimals`. Las comillas de cinco dígitos multiplican automáticamente el paso por diez para que coincida con la lógica de "punto ajustado" MetaTrader.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `OrderVolume` | 0,01 | Volumen utilizado al realizar cada orden pendiente. Redondeado al paso de volumen del instrumento antes del registro. |
| `PipStep` | 12 | Espaciado entre órdenes consecutivas en el ladder, expresado en pips. |
| `TakeProfitPips` | 10 | Distancia en pips utilizada para realizar la toma de ganancias de cada orden pendiente. |
| `OrdersPerSide` | 10 | Número máximo de órdenes activas mantenidas por encima y por debajo del mercado. |
| `AboveMarketSide` | comprar | Tipo de orden utilizada por encima del mercado. `Buy` crea órdenes de parada de compra, `Sell` crea órdenes de límite de venta. |
| `BelowMarketSide` | Vender | Tipo de orden utilizada por debajo del mercado. `Buy` crea órdenes de límite de compra, `Sell` crea órdenes de parada de venta. |
| `MinStopDistancePoints` | 0 | Distancia mínima (en puntos brutos) permitida entre la oferta/demanda y el precio pendiente. Establezca esto en el corredor `MODE_STOPLEVEL` si es necesario. |
| `ThrottleSeconds` | 5 | Periodo de enfriamiento entre ciclos de mantenimiento de la red. |
| `SlippagePoints` | 3 | Preservado para la paridad de documentación; StockSharp órdenes pendientes no utilizan este valor. |

## Notas de implementación
- Utiliza solo los ayudantes de alto nivel StockSharp (`SubscribeLevel1`, `BuyLimit`, `SellLimit`, `BuyStop`, `SellStop`).
- Los precios se normalizan a través de `Security.ShrinkPrice` para que el corredor reciba valores válidos alineados con ticks.
- El volumen se ajusta para respetar `VolumeStep`, `MinVolume` y `MaxVolume` antes de enviar cada pedido.
- Todos los mensajes de diagnóstico se enrutan a través de `AddInfoLog`/`AddWarningLog`, reflejando la salida detallada del script MetaTrader.
- La implementación de Python se omite intencionalmente, según lo solicitado.

## Consejos de uso
1. Asigne un instrumento líquido y una cartera, luego comience la estrategia. Las escaleras pendientes aparecerán instantáneamente después de la primera actualización de nivel 1.
2. Aumente `OrdersPerSide` con precaución: cada escalón adicional da como resultado otra orden pendiente activa del lado del corredor.
3. Para imitar el EA original con precisión, mantenga el acelerador predeterminado en cinco segundos y configure `MinStopDistancePoints` con el requisito de nivel de parada del corredor.
4. Recuerde que StockSharp maneja posiciones netas; Si se activan escaleras opuestas simultáneamente, los rellenos resultantes se compensarán parcialmente entre sí en lugar de crear subposiciones cubiertas.
