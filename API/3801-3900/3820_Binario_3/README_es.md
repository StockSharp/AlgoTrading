# Estrategia Binario 3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es una adaptación StockSharp del MetaTrader 4 expertos "Binario_3" de `MQL/7658/Binario_3.mq4`. El EA original rodea el mercado con dos promedios móviles exponenciales de 144 períodos calculados sobre los máximos y mínimos de las velas y negocia la ruptura de este canal adaptativo. Las órdenes stop pendientes se colocan por encima de la banda superior y por debajo de la banda inferior, mientras que las paradas protectoras, los objetivos de toma de ganancias y un trailing stop opcional emulan el comportamiento de MetaTrader.

La versión StockSharp mantiene las mismas reglas de decisión pero se implementa con el nivel alto API:

1. Se suscribe a la serie de velas configuradas y recalcula los dos sobres EMA cada vez que se completa una vela.
2. Cuando el último cierre permanece dentro del canal, coloca órdenes de compra y venta con el desplazamiento requerido de los valores EMA.
3. Registra los niveles de stop-loss y take-profit asociados con cada orden pendiente para que puedan aplicarse a la posición una vez que se completa la orden.
4. Realiza un seguimiento de las cotizaciones de Nivel 1 para gestionar las posiciones abiertas: cierra operaciones si el precio alcanza el stop-loss registrado, el objetivo o la distancia del trailing stop.
5. Cancela las órdenes pendientes si el precio abandona el canal o si la posición opuesta se activa, reflejando la lógica de limpieza en el script MQL.

## Parámetros

| Nombre | Predeterminado | Descripción |
|------|---------|-------------|
| `TakeProfit` | `850` puntos | Distancia adicional (en puntos) agregada al lado de ruptura al calcular la toma de ganancias. |
| `TrailingStop` | `850` puntos | Distancia en puntos utilizados para salidas finales. Establezca en `0` para deshabilitar el seguimiento. |
| `PipDifference` | `25` puntos | Compensación del canal EMA antes de realizar pedidos pendientes. |
| `Lots` | `0.1` | Volumen comercial base utilizado cuando no se puede derivar el tamaño basado en el riesgo. |
| `MaximumRisk` | `10` | Multiplicador de riesgo copiado del EA original. La estrategia estima el volumen como `max(Lots, Balance * MaximumRisk / 50000)`. |
| `EmaPeriod` | `144` | Período de medias móviles exponenciales basadas en precios altos y bajos. |
| `CandleType` | `1 hour` período de tiempo | Serie de velas que impulsa las actualizaciones de indicadores y la realización de pedidos. |

Todos los puntos se convierten en distancias de precios reales utilizando el `PriceStep` del instrumento. Si el símbolo no expone un paso, la estrategia vuelve a `1`.

## Lógica de trading

1. **Cálculos de indicadores**: dos instancias `ExponentialMovingAverage` procesan los precios máximos y mínimos de las velas. Las órdenes se generan sólo después de que ambos promedios estén completamente formados.
2. **Órdenes pendientes**: cuando el precio de cierre se encuentra dentro del canal, las órdenes de compra y venta se colocan en:
   - Stop de compra: EMA(alto) + spread + `PipDifference` * paso.
   - Parada de venta: EMA(baja) - `PipDifference` * paso.
Los valores de stop-loss y take-profit asociados con esas órdenes se almacenan hasta que la posición se activa.
3. **Gestión de posiciones**: tan pronto como se abre una posición, la estrategia cancela la orden pendiente opuesta y adopta los niveles de parada/objetivo almacenados. Las cotizaciones de nivel 1 se monitorean para cerrar la operación si el mercado alcanza el límite de pérdidas, la toma de ganancias o la distancia del límite dinámico (`TrailingStop` * paso).
4. **Trailing stop**: para posiciones largas, el nivel de seguimiento sigue la mejor oferta una vez que el beneficio supera la distancia configurada; para pantalones cortos el nivel sigue el mejor pedido. El nivel de seguimiento solo se mueve en la dirección de la operación, reproduciendo el comportamiento de seguimiento de MetaTrader.
5. **Limpieza de pedidos**: cuando el último cierre sale del canal EMA, ambos pedidos pendientes se cancelan para evitar entradas no deseadas, coincidiendo con las comprobaciones de seguridad del script original.

## Diferencias con la versión MQL

- El EA original modificó las órdenes de parada del lado del servidor con `OrderModify`; el puerto StockSharp simula el mismo efecto observando cotizaciones de nivel 1 y llamando a `ClosePosition()` cuando se alcanza una parada o un objetivo.
- Los stop dinámicos se implementan completamente dentro de la estrategia porque las órdenes StockSharp de alto nivel no admiten instrucciones de seguimiento en bolsa.
- El cálculo del volumen utiliza el saldo de la cartera (`Portfolio.CurrentValue` o `Portfolio.BeginValue`) cuando esté disponible. Si no se conoce el saldo, la estrategia vuelve al valor `Lots` configurado.
- Los precios se normalizan según el nivel de precio del instrumento antes de registrar las órdenes para mantenerlos alineados con los requisitos del intercambio.

## Notas de uso

- Habilite las suscripciones de Nivel 1 cuando ejecute la estrategia para que los trailingstops y las salidas protectoras puedan reaccionar a las actualizaciones de ofertas/demanda en vivo.
- La estrategia se basa en velas completadas. Si el período de tiempo seleccionado es demasiado largo, el tiempo de respuesta reflejará ese ritmo más lento.
- El seguimiento se puede desactivar configurando `TrailingStop` en `0`. En este modo sólo se utilizan los niveles fijos de stop-loss y take-profit.
