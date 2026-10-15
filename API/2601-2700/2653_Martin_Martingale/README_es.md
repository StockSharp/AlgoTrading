# Estrategia Martin Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia reproduce el comportamiento del Asesor Experto original "Martin" de MQL ejecutando una cuadrícula martingala con cobertura alrededor del precio actual. Alterna continuamente posiciones largas y cortas, duplicando el volumen operado en cada reversión hasta que la ganancia acumulada de toda la cesta alcance el objetivo configurado. Las velas solo se usan como impulsor para la lógica de decisión, mientras que las ejecuciones reales dependen de órdenes de mercado y órdenes stop expuestas por la API de alto nivel de StockSharp.

## Cómo funciona
1. Al iniciarse, la estrategia lee el `PriceStep` del instrumento para convertir los parámetros `EntryOffsetPoints` y `StepPoints` en distancias de precio absolutas. Si el paso de precio no está disponible, se asume el valor 1.
2. Cuando no hay posición abierta ni ciclo martingala activo, la estrategia coloca una orden stop de compra y una orden stop de venta alrededor del último cierre. Los offsets son `EntryOffsetPoints * PriceStep`, lo que coincide con la distancia de 10 puntos usada en el código MQL original.
3. Cuando se ejecuta una de las órdenes stop, la orden pendiente opuesta se cancela. El relleno define el primer trade de la secuencia martingala: la estrategia almacena su precio, volumen y dirección, y establece el contador de nivel interno en 1.
4. En cada cierre de vela posterior, el precio de cierre actual se compara con el precio del último orden ejecutado. Si el mercado se ha movido contra esa orden al menos `martingaleLevel * StepPoints * PriceStep`, se envía una orden de mercado en la dirección opuesta con volumen duplicado respecto al trade anterior. La información del último trade se actualiza tras cada ejecución.
5. La ganancia no realizada se evalúa como `PnL + Position * (closePrice - PositionPrice)`. Cuando esta ganancia agregada supera el parámetro `ProfitTarget`, la estrategia envía `CloseAll()` para aplanar cada posición en la cesta, cancela todas las órdenes restantes y reinicia el ciclo para que pueda colocarse un nuevo par de órdenes stop.
6. El mismo reinicio ocurre automáticamente cuando todas las posiciones se cierran manualmente: los contadores internos se borran y se crearán nuevas órdenes stop en la siguiente vela.

Este flujo de trabajo refleja la lógica de compra/venta alternante del Asesor Experto original mientras mantiene la implementación completamente dentro de la API de alto nivel de StockSharp.

## Parámetros
- `StepPoints` – número de pasos de precio usados para calcular el umbral de reversión para la siguiente orden de promediado. Por defecto 10 y puede optimizarse.
- `EntryOffsetPoints` – offset para las órdenes stop iniciales de compra/venta en pasos de precio. También por defecto 10 puntos como la versión MQL.
- `ProfitTarget` – ganancia absoluta en divisa requerida para cerrar toda la cesta martingala. Una vez que el PnL combinado realizado y no realizado supera este valor, todas las posiciones se liquidan.
- `CandleType` – suscripción de velas usada para impulsar la lógica de la estrategia. El valor predeterminado es el marco temporal de un minuto, pero puede seleccionarse cualquier `DataType` soportado por el mercado.

El tamaño base del trade se toma de la propiedad `Volume` de la estrategia. Cada nueva reversión multiplica esta base por potencias de dos de la manera martingala clásica.

## Notas prácticas
- Siempre configure `Volume` para que coincida con el tamaño mínimo de lote del broker. El esquema de duplicación incrementa rápidamente la exposición, por lo que los límites de riesgo deben aplicarse externamente.
- Dado que la colocación de órdenes está impulsada por los cierres de velas, los movimientos de precio rápidos dentro de la vela pueden activar entradas ligeramente más tarde que la versión MQL basada en ticks. Sin embargo, las órdenes stop mantienen los precios de entrada alineados con la lógica original.
- La estrategia dibuja velas de precio y trades propios en el área de gráfico predeterminada para facilitar el seguimiento visual.
- No se usa stop-loss automático. La única condición de salida es el `ProfitTarget`, por lo que el instrumento y el marco temporal deben elegirse cuidadosamente para controlar el riesgo de grandes tendencias adversas.

## Diferencias respecto al Experto MQL
- StockSharp usa posiciones netas, por lo tanto cada reversión se ejecuta con una orden de mercado que cierra la exposición anterior y abre la nueva en un solo trade. El PnL acumulado de la cesta sigue siendo idéntico a la implementación con cobertura.
- La lógica tick a tick fue reemplazada por cierres de velas para la evaluación de señales con el fin de mantenerse dentro del uso recomendado de la API de alto nivel.
- Los identificadores de órdenes son rastreados para evitar procesar ejecuciones parciales múltiples veces, asegurando que la lógica de duplicación de volumen permanezca consistente.

Estos cambios mantienen el comportamiento de trading fiel a la estrategia fuente mientras la adaptan al framework de StockSharp.
