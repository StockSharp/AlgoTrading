# Estrategia ante desastres (MQL #7704)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

El asesor experto original de MetaTrader nombró `disaster.mq4` órdenes de parada en torno a una media móvil simple muy larga (SMA). Espera hasta que el precio actual se aleje lo suficiente del promedio, luego estaciona dos órdenes de parada pendientes que intentan capturar un retroceso de reversión a la media. Cada nuevo minuto recalcula el SMA y empuja las órdenes pendientes a la última línea de base. Las órdenes ejecutadas están protegidas con un límite de pérdidas fijo y una toma de ganancias adaptativa que se reduce después de que la operación anterior en el mismo lado cerró con pérdidas.

## Notas de portabilidad

* **Fuente de datos**: el script MQL utiliza barras de 1 minuto hasta `iMA(PERIOD_M1, 590)`. La versión StockSharp se suscribe a una serie de velas configurables (predeterminada `TimeSpan.FromMinutes(1)`) y alimenta un indicador `SMA` con la misma mirada retrospectiva.
* **Lógica de activación**: MetaTrader compara las cotizaciones de oferta/demanda con SMA y requiere una brecha de 20 pips antes de activar una orden pendiente. El puerto C# reproduce esto convirtiendo el parámetro `TriggerDistancePips` a una distancia de precio absoluta usando el instrumento `PriceStep`/`MinPriceStep` más el multiplicador de 10× para símbolos FX de 3/5 dígitos.
* **Tipos de órdenes**: EA registra órdenes suspendidas a través de `OrderSend(..., OP_BUYSTOP/OP_SELLSTOP, ...)`. Los equivalentes de StockSharp son `BuyStop` y `SellStop`. El puerto mantiene ambas órdenes independientes, permitiendo que cualquiera de ellas permanezca activa si las condiciones persisten.
* **Reubicación dinámica**: cada vez que llega una nueva vela, el código MQL llama a `OrderModify` para que las paradas pendientes rastreen la nueva SMA. StockSharp logra lo mismo llamando a `ReRegisterOrder` para mover pedidos activos sin cancelar/recrear abandono.
* **Niveles de detención**: MetaTrader aplica los niveles de detención del corredor (`MODE_STOPLEVEL`). La versión StockSharp respeta indirectamente el mismo margen de seguridad al redondear al paso del precio del instrumento y abortar la reubicación cuando el precio calculado no es válido (≤ 0).
* **Órdenes de protección**: en MT4, el stop-loss y el take-profit se adjuntan a la orden pendiente. StockSharp crea órdenes de protección de límite/stop separadas inmediatamente después de un cumplimiento de entrada, reflejando las compensaciones de precios exactas.
* **Obtención de beneficios adaptativa**: el EA reduce a la mitad la distancia de obtención de beneficios para la siguiente orden si la operación anterior de ese lado perdió dinero. El puerto mantiene banderas `_lastBuyWasLoss` / `_lastSellWasLoss` y ajusta la distancia de obtención de beneficios en consecuencia.
* **Gestión de dinero**: el script dimensiona los lotes con `0.4 * AccountFreeMargin / 1000`, limitado por los límites del corredor. El puerto StockSharp expone un parámetro `Volume` directo y lo alinea con `VolumeStep`, `MinVolume` y `MaxVolume`.

## Parámetros

| Parámetro | Predeterminado | Descripción |
| --- | --- | --- |
| `Volume` | `0.1` | Volumen de pedido alineado con el paso de volumen del instrumento. |
| `MaPeriod` | `590` | Longitud de media móvil simple utilizada como línea de base. |
| `StopLossPips` | `30` | Distancia entre el precio de entrada y el stop de protección. |
| `TakeProfitPips` | `70` | Distancia base de obtención de beneficios. Automáticamente se reduce a la mitad después de una operación perdedora del mismo lado. |
| `TriggerDistancePips` | `20` | Brecha requerida entre el precio y el SMA antes de activar las entradas de parada. |
| `CandleType` | `1-minute time frame` | Serie de velas utilizadas para alimentar el SMA. |

Todos los parámetros basados en pips se traducen a través del instrumento `PriceStep` o `MinPriceStep`. Para pares de divisas con 3 o 5 dígitos decimales, la conversión multiplica el paso por 10, coincidiendo con el comportamiento MetaTrader `Point`.

## Flujo de trabajo

1. Suscríbase a cotizaciones de nivel 1 y velas de minutos.
2. Actualice los precios de oferta y demanda almacenados en cada mensaje de Nivel 1.
3. En cada vela terminada, vuelva a calcular el SMA y mueva cualquier orden pendiente activa a la nueva línea de base.
4. Si no hay ninguna posición abierta y la brecha entre oferta y demanda excede la distancia de activación, coloque la orden stop correspondiente (venda por encima del SMA, compre por debajo cuando el precio esté infravalorado).
5. Cuando se ejecuta una orden stop, registre inmediatamente órdenes stop-loss y take-profit a las distancias solicitadas. Realice un seguimiento del último resultado comercial para adaptar la próxima toma de ganancias.
6. Cancele todas las órdenes pendientes/de protección cuando la estrategia se detenga.

## Diferencias versus la versión MQL

* El puerto se basa en StockSharp órdenes de protección en lugar de campos SL/TP adjuntos al corredor. El comportamiento es equivalente pero utiliza órdenes explícitas en la cuenta.
* MetaTrader impone el espaciado a nivel de parada con `MODE_STOPLEVEL`. StockSharp representa este requisito redondeando al paso de precio disponible y omitiendo las actualizaciones cuando el precio calculado no es válido. En la práctica, debería respetar las mismas restricciones una vez que el adaptador valida los precios de los pedidos.
* El código original recalcula el volumen comercial a partir del margen libre en cada tick. El puerto StockSharp deja el tamaño al usuario a través del parámetro `Volume` para mayor claridad y comportamiento predecible entre los corredores.

## Requisitos

* Los instrumentos deben exponer al menos `PriceStep` o `MinPriceStep`. Sin ellos, la conversión de pip a precio vuelve a ser `0.0001`, que es apropiado para los principales pares de divisas.
* Para imitar las reglas de nivel de parada de FX, la fuente de datos debe ofrecer las mejores actualizaciones de oferta/demanda (Nivel 1). La estrategia se degrada elegantemente utilizando el precio de cierre de la vela si faltan cotizaciones.
* Las órdenes de protección requieren corredores/bolsas que admitan órdenes stop y limit. Si no está disponible, ajuste el código para volver a las salidas del mercado.

## Consejos de uso

* Comience con microvolúmenes (`0.01`) en cuentas de demostración para validar las conversiones de precios.
* Ajuste `TriggerDistancePips` y `TakeProfitPips` juntos: los desencadenantes más pequeños generan operaciones más frecuentes, así que considere reducir la toma de ganancias en consecuencia.
* Supervise los indicadores `_lastBuyWasLoss` y `_lastSellWasLoss` a través de registros para confirmar que la lógica adaptativa de obtención de beneficios coincide con el historial de MetaTrader.
