# Estrategia de 21 horas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia **21hora** reproduce el comportamiento del MQL4 asesor experto `21hour.mq4`. Opera alrededor de una ventana de tiempo diaria: las órdenes de ruptura pendientes se crean en una hora de inicio configurable y toda la exposición se elimina en una hora de finalización configurable. La versión StockSharp mantiene la misma idea de "dos órdenes stop alrededor del precio" mientras aprovecha el nivel alto API para la gestión de órdenes, suscripciones a datos de mercado y manejo protector de toma de ganancias.

## Lógica de trading
- Al comienzo de cada día de negociación, cuando la hora del servidor coincide con `StartHour:00`, la estrategia lee las últimas cotizaciones de oferta/demanda y coloca una orden de compra y de venta.
  - La distancia desde el precio de venta actual hasta el activador de parada de compra es `StepPoints * PriceStep`.
  - La distancia desde el precio de oferta actual hasta el desencadenante de la parada de venta es la misma cantidad por debajo del mercado.
  - `TakeProfitPoints` se convierte en precio distancia a través del paso de precio del instrumento y se pasa a `StartProtection`, por lo que tanto las posiciones largas como las cortas reciben una toma de ganancias protectora inmediatamente después de la ejecución.
- Sólo se permite una configuración pendiente por día. Si solo una de las dos órdenes stop permanece activa (por ejemplo, después de que se llenó un lado), la estrategia cancela la orden pendiente sobreviviente para reflejar la lógica EA original.
- Cuando el reloj llega a `StopHour:00`, la estrategia cierra cualquier posición abierta en el mercado y cancela todas las órdenes pendientes pendientes. Esto se aplica incluso si no se produjo ninguna ruptura.
- El flujo de velas predeterminado son datos de un minuto. Se utiliza únicamente para activar las comprobaciones horarias de las velas terminadas, lo que imita la protección `prevtime` de la versión MQL.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `Volume` | Volumen de órdenes en lotes para ambas órdenes pendientes. | `0.1` |
| `StartHour` | Hora (0–23) en la que se crea el par de órdenes pendientes. | `10` |
| `StopHour` | Hora (0–23) en la que la estrategia cierra posiciones y elimina todas las órdenes pendientes. | `22` |
| `StepPoints` | Distancia en puntos del instrumento entre el precio de oferta/demanda actual y cada entrada de stop. Convertido a precio multiplicando por `PriceStep`. | `15` |
| `TakeProfitPoints` | Distancia en puntos desde el precio de entrada hasta el objetivo de obtención de beneficios gestionado por `StartProtection`. Establezca en `0` para desactivar el objetivo. | `200` |
| `CandleType` | Tipo de datos de vela utilizado para el seguimiento del tiempo. El valor predeterminado es un período de tiempo de un minuto (`TimeSpan.FromMinutes(1).TimeFrame()`). | `1 minute` |

## Notas de implementación
- Utiliza `SubscribeCandles` para obtener velas terminadas y evaluar el cronograma horario solo una vez por minuto.
- Suscríbase a cotizaciones de nivel 1 a través de `SubscribeLevel1()` para mantener los valores de oferta y demanda más recientes para una colocación de paradas precisa.
- Se basa en `StartProtection` con una unidad de obtención de beneficios para emular la obtención de beneficios de la orden pendiente del EA original en lugar de adjuntar órdenes manualmente.
- Realiza un seguimiento de las órdenes stop activas de compra y venta y llama a `CancelOrder` si solo queda un lado, lo que garantiza que el sistema nunca se ejecute con una orden pendiente no emparejada.
- Invoca ayudantes `BuyMarket` / `SellMarket` para estabilizar posiciones en la hora de parada, utilizando estrictamente la estrategia de alto nivel API.

## Notas de comportamiento
- La estrategia espera que la conexión del corredor proporcione información sobre el paso del precio. Si `PriceStep` está ausente, los precios no se redondean.
- Los pedidos pendientes se generan solo una vez por día calendario. Se volverán a crear el siguiente día de negociación a la hora de inicio configurada, incluso si la ruptura del día anterior no se activó.
- Cuando `TakeProfitPoints` es cero, la estrategia aún coloca órdenes pendientes pero no se gestiona ninguna toma de ganancias protectora.
