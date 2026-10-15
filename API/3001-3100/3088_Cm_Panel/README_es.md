# Estrategia CM Panel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia CM Panel** es un asistente manual de órdenes pendientes que recrea el comportamiento del script original de MetaTrader 5 "cm panel". En lugar de dibujar controles en pantalla, el puerto a StockSharp expone parámetros interactivos que funcionan como botones: establecer un indicador en `true` coloca o cancela órdenes stop pendientes y el indicador se reinicia inmediatamente a `false`, imitando el flujo de trabajo de botón pulsable del panel. La estrategia mantiene configuración separada para órdenes de compra y venta, incluyendo distancias, volúmenes y objetivos de protección expresados en puntos.

La conversión se basa completamente en la API de alto nivel de StockSharp. Las órdenes pendientes se envían con los helpers `BuyStop` y `SellStop`, mientras que la protección posterior a la ejecución se implementa registrando órdenes de stop-loss y take-profit independientes. Los valores de precio y volumen se adaptan automáticamente al tamaño de tick y paso de lote del instrumento, por lo que la estrategia respeta las restricciones de la bolsa sin requerir normalización manual.

## Lógica de trading
1. Cuando el usuario activa `PlaceBuyStop` en `true`, la estrategia lee el mejor ask (con respaldo al último precio negociado si es necesario) y lo desplaza por `BuyStopOffsetPoints` convertidos a unidades de precio. Se envía una orden stop de compra con volumen `BuyVolume` al nivel resultante. Los precios deseados de stop-loss y take-profit se calculan inmediatamente y se almacenan como objetivos de protección pendientes.
2. Cuando el usuario activa `PlaceSellStop` en `true`, el mejor bid (o última operación) se desplaza hacia abajo por `SellStopOffsetPoints`. Se coloca una orden stop de venta con volumen `SellVolume` a ese precio y los niveles de protección correspondientes se registran.
3. Después de que una orden stop pendiente se ejecuta, la estrategia coloca automáticamente las órdenes de protección registradas:
   - Las posiciones largas reciben un `SellStop` de stop-loss por debajo del precio de entrada y un `SellLimit` de take-profit por encima.
   - Las posiciones cortas reciben un `BuyStop` de stop-loss por encima del precio de entrada y un `BuyLimit` de take-profit por debajo.
   Cada orden de protección se envía solo una vez; si una se ejecuta, la otra se cancela para emular el par SL/TP único de MetaTrader.
4. Cuando se activa el indicador `CancelPendingOrders`, se cancelan todas las órdenes stop de compra o venta activas creadas por la estrategia. Las órdenes de protección que ya guardan posiciones abiertas se dejan intencionalmente intactas para que las operaciones en curso permanezcan protegidas.
5. Los volúmenes se ajustan al `VolumeStep`, `MinVolume` y `MaxVolume` del instrumento. Si el tamaño resultante se vuelve inválido (por ejemplo, inferior al lote mínimo), la operación se cancela y se registra una advertencia en lugar de enviar una orden.
6. Todas las distancias de precio se expresan en puntos y se convierten usando el `PriceStep` del instrumento. Si el paso es desconocido, se aplica un respaldo conservador de `0.0001` para que el panel siga siendo utilizable en símbolos sin metadatos de tick.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `BuyVolume` | `decimal` | `0.10` | Volumen enviado con cada orden stop de compra después de respetar el paso de lote del instrumento. |
| `SellVolume` | `decimal` | `0.10` | Volumen enviado con cada orden stop de venta después de respetar el paso de lote del instrumento. |
| `BuyStopOffsetPoints` | `int` | `100` | Distancia en puntos añadida por encima del ask actual para posicionar el stop de compra pendiente. |
| `SellStopOffsetPoints` | `int` | `100` | Distancia en puntos restada del bid actual para posicionar el stop de venta pendiente. |
| `BuyStopLossPoints` | `int` | `100` | Distancia del stop-loss (en puntos) para posiciones largas activadas por el stop de compra. Establecer en cero para omitir la orden de protección. |
| `SellStopLossPoints` | `int` | `100` | Distancia del stop-loss (en puntos) para posiciones cortas activadas por el stop de venta. Establecer en cero para omitir la orden de protección. |
| `BuyTakeProfitPoints` | `int` | `150` | Distancia del take-profit (en puntos) para posiciones largas activadas por el stop de compra. Establecer en cero para omitir la orden de protección. |
| `SellTakeProfitPoints` | `int` | `150` | Distancia del take-profit (en puntos) para posiciones cortas activadas por el stop de venta. Establecer en cero para omitir la orden de protección. |
| `PlaceBuyStop` | `bool` | `false` | Indicador que coloca una orden stop de compra una vez. El valor se reinicia a `false` automáticamente después del procesamiento. |
| `PlaceSellStop` | `bool` | `false` | Indicador que coloca una orden stop de venta una vez. El valor se reinicia a `false` automáticamente después del procesamiento. |
| `CancelPendingOrders` | `bool` | `false` | Indicador que cancela todas las órdenes stop pendientes activas creadas por el panel. |

## Diferencias respecto a la versión MetaTrader
- MetaTrader adjunta niveles de stop-loss y take-profit directamente a las órdenes pendientes. StockSharp mantiene el mismo comportamiento generando órdenes de protección dedicadas inmediatamente después de que se ejecuta una entrada.
- La implementación de StockSharp adapta transparentemente los volúmenes y precios a los metadatos del instrumento, eliminando la necesidad de normalización manual con `_Point`, `_Digits` o redondeo de volumen.
- Las limitaciones de nivel de stop de la sede de negociación no se consultan automáticamente. Los usuarios deben configurar desplazamientos que respeten la distancia mínima del bróker, como lo harían en MetaTrader.
- El indicador de eliminación (`CancelPendingOrders`) cancela solo los stops pendientes. Las órdenes de protección existentes para posiciones abiertas permanecen activas para que las operaciones en curso estén protegidas.

## Consejos de uso
- Asignar un portafolio e instrumento antes de activar cualquier indicador de acción; de lo contrario, la estrategia registra una advertencia e ignora la solicitud.
- Para emular el flujo de trabajo del panel original, agregar la estrategia a la interfaz de Designer o Runner, exponer los parámetros en la cuadrícula de propiedades y cambiar los booleanos cuando se quiera enviar o cancelar órdenes.
- Como la lógica depende de las mejores cotizaciones bid/ask, asegurarse de que los datos Level 1 estén siendo transmitidos. Si los mejores precios faltan, el código recurre al último precio negociado, pero las órdenes pendientes pueden quedar más cerca del mercado de lo previsto.
- Ajustar las distancias en puntos para respetar el nivel de stop mínimo del instrumento. El helper no aplica automáticamente buffers específicos del bróker.
- Establecer las distancias de protección en cero cuando se quieran colocar órdenes stop desnudas sin niveles de SL/TP adjuntos.
