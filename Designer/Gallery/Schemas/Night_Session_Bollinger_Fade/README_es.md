# Diagrama de la estrategia Night Session Bollinger Fade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una banda indica dónde el precio ha dejado de comportarse con normalidad; un reloj indica cuándo merece la pena actuar en consecuencia. Este diagrama junta ambas cosas sobre velas horarias terminadas: opera en contra de un toque de una banda de Bollinger, pero solo durante las horas vespertinas y solo mientras el propio canal es estrecho. La salida es la línea media, y esa mitad no está acotada por el reloj: una posición abierta a última hora de la tarde se cierra en cuanto el precio vuelve, sea cual sea la hora a la que ocurra.

![schema](schema.svg)

## Resumen de la estrategia

- Una única serie de velas lo gobierna todo. Solo se emiten velas horarias terminadas, de modo que cada comparación, cada filtro y cada orden se deciden sobre una barra cerrada.
- Las bandas de Bollinger sobre 20 velas con una desviación de 2.0 solo emiten valores una vez formadas. Tres bloques Converter separan el valor del indicador en la banda superior, la banda inferior y la línea media.
- Un bloque Formula resta la banda inferior de la superior para obtener la anchura del canal, y un bloque Comparison la contrasta con el umbral de anchura. Esa única respuesta es el filtro de mercado tranquilo que comparten ambas entradas.
- El bloque Working time lee la marca de tiempo que lleva cada vela, su hora de apertura, y responde verdadero desde las 19:00:00 hasta las 23:59:59. Solo lo consultan los dos filtros de entrada.
- Una entrada larga se acepta cuando cuatro respuestas coinciden en la misma vela: el mínimo alcanzó la banda inferior, el canal está dentro del umbral, la vela abrió dentro de la sesión y la posición está plana.
- Una entrada corta es la imagen especular: el máximo alcanzó la banda superior, con las mismas condiciones de anchura, de sesión y de posición plana.
- Ambas entradas son órdenes a mercado enviadas por Position modify bajo la condición Open position, de modo que una señal que llega con una posición ya abierta la rechaza el propio bloque de órdenes y no una comparación adicional.
- Las salidas comparan el cierre con la línea media y se envían como órdenes Reduce only en ambas direcciones. El panel del gráfico dibuja las velas, las tres líneas de las bandas, y las órdenes y ejecuciones de los cuatro bloques de órdenes.

## Reglas de entrada y salida

- **Entrada en largo**: Dentro de la ventana vespertina, en una vela terminada cuyo mínimo alcanzó o cruzó la banda inferior, con una anchura de canal no mayor que el umbral y la posición plana, Position modify compra a mercado el volumen de la orden bajo la condición Open position.
- **Entrada en corto**: Dentro de esa misma ventana, en una vela terminada cuyo máximo alcanzó o cruzó la banda superior, con una anchura de canal no mayor que el umbral y la posición plana, Position modify vende a mercado el volumen de la orden bajo la condición Open position.
- **Salida**: La línea media es el objetivo de ambos lados. En cualquier vela terminada que cierre en ella o por encima se envía una venta Reduce only, y en cualquier vela que cierre en ella o por debajo se envía una compra Reduce only. Un bloque Reduce only al que se le pasa la dirección que la posición ya mantiene rechaza la orden por sí mismo, de modo que la ruta de venta permanece en silencio mientras se está corto y la de compra permanece en silencio mientras se está largo. Ninguna de las dos salidas está limitada por la sesión ni por la anchura del canal: funcionan en cada vela terminada, a cualquier hora. No hay stop-loss ni take-profit: el regreso a la línea media es la única forma de salir de una operación.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 01:00:00 | Marco temporal de la única serie de velas sobre la que funciona todo el diagrama; de ella solo salen velas terminadas. |
| Bollinger Period | 20 | Número de velas sobre las que se promedian las bandas. |
| Bollinger Deviation | 2.0 | Multiplicador de la desviación estándar que fija a qué distancia de la línea media se sitúan ambas bandas. |
| Width Threshold | 3000 | Canal más ancho, en las unidades de precio del instrumento, que todavía se considera lo bastante tranquilo como para entrar. |
| Session From | 19:00:00 | Inicio de la ventana en la que se permiten las entradas, contrastado con la hora de apertura de la vela. |
| Session Until | 23:59:59 | Fin de esa ventana; una vela que abre en ese momento o antes sigue contando como dentro. |
| Order Volume | 0.01 | Tamaño de la orden que envían tanto las dos entradas como las dos salidas. |

## Detalles del diagrama

- La entrada lee los extremos de la vela y no su cierre. Una barra que perforó una banda dentro de la propia barra y cerró de nuevo por dentro sigue contando como un toque, y eso es lo que convierte esto en una operación contra la excursión en lugar de una ruptura del precio de cierre.
- El filtro de anchura se mide en las unidades de precio del instrumento, no en porcentaje. En un instrumento cotizado en unidades casi todas las velas lo superan y el filtro queda de hecho abierto; en uno cotizado en decenas de miles se convierte en el filtro selectivo que pretende ser. El umbral está expuesto para poder ajustarlo al instrumento.
- Ambas salidas llevan una dirección aunque Reduce only decida el lado por sí mismo: la dirección es lo que hace que cada ruta rechace la posición equivocada. Por eso en el lado de las salidas del diagrama no aparece ninguna comparación de largo o corto: esa comprobación la realizan los dos bloques de órdenes.
- Una única variable Volume alimenta los cuatro bloques de órdenes. En las salidas, Reduce only recorta la orden hasta lo que la posición mantiene realmente, de modo que incluso una entrada ejecutada parcialmente se cierra con exactitud en lugar de invertirse.
- La sesión se evalúa por vela y no a partir de un reloj libre, de modo que cambia de valor al mismo compás que las respuestas de la banda y de la anchura, y las cuatro entradas de un filtro de entrada describen siempre la misma barra.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
