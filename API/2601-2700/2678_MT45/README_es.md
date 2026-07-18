# Estrategia MT45
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia MT45 es una conversión directa del asesor experto original de MetaTrader. Alterna entre posiciones largas y cortas de mercado en cada barra completada, mientras protege cada operación con las mismas distancias fijas de take-profit y stop-loss que se usaban en la implementación MQL. El dimensionamiento de la posición sigue una regla de recuperación estilo martingala, de modo que la siguiente operación aumenta su volumen sólo después de un resultado perdedor.

## Lógica de trading
1. La estrategia se suscribe a una única serie de velas definida por el parámetro **Candle Type** y espera las velas completadas para evitar el ruido intrabarra.
2. Cuando no hay posición abierta y la orden de entrada anterior ha sido completamente procesada, el algoritmo envía una orden de mercado en la dirección programada para este turno (compra, luego venta, luego compra, ...).
3. La dirección alterna sólo después de que la orden correspondiente sea ejecutada, asegurando que la alternación coincida con el comportamiento del experto MQL donde cada operación completada cambia el lado para la siguiente señal.
4. Las órdenes protectoras de stop-loss y take-profit se gestionan automáticamente a través de `StartProtection`, de modo que la estrategia abandona el mercado cuando se alcanza cualquiera de las distancias.

## Dimensionamiento de la posición
* **Base Volume** establece el tamaño de lote inicial. Se restaura después de cada operación rentable o de equilibrio.
* Después de una operación perdedora, el volumen de la siguiente entrada se multiplica por **Martingale Multiplier**. Si el valor escalado excedería **Max Volume**, la estrategia regresa al volumen base para evitar un crecimiento descontrolado.
* El beneficio o pérdida realizada se mide comparando el precio de salida con el precio de entrada almacenado, lo que reproduce la función `Lot()` del asesor experto original.

## Gestión de riesgo
* **Stop Points** y **Take Points** se expresan en pasos de precio, reflejando el multiplicador `_Point` que se usaba en MetaTrader. La estrategia convierte esos valores a distancias de precio absolutas mediante el `PriceStep` del instrumento antes de habilitar `StartProtection`.
* Las órdenes protectoras se adjuntan automáticamente a cada posición y se colocan simétricamente tanto para operaciones largas como cortas.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| Stop Points | Distancia al stop protector en pasos de precio del instrumento. | 600 |
| Take Points | Distancia al objetivo de take-profit en pasos de precio del instrumento. | 700 |
| Base Volume | Volumen base usado para nuevas posiciones después de victorias. | 0.01 |
| Martingale Multiplier | Multiplicador de volumen aplicado después de pérdidas. | 2 |
| Max Volume | Volumen máximo permitido para el escalado martingala. | 10 |
| Candle Type | Serie de velas usada para detectar la finalización de barras (por defecto: 1 minuto). | 1 minuto |

## Notas de uso
* Elige el marco temporal de velas que coincida con el marco temporal del gráfico del experto original. La lógica opera estrictamente en velas completadas.
* La estrategia no encola otra entrada mientras haya una orden pendiente o una posición activa; siempre espera a que la operación existente se cierre mediante stop-loss o take-profit.
* No hay una versión de Python separada para esta estrategia en este momento, siguiendo las directrices del proyecto.
