# Diagrama de la estrategia de alineación de marcos temporales con MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un MACD es una opinión; dos MACD que coinciden en marcos temporales distintos son una señal. El diagrama mide cuánto se separa el MACD de su propia línea de señal en velas de media hora y en velas de cuatro horas, y abre una posición solo cuando ambas lecturas apuntan en la misma dirección y el libro de órdenes está lo bastante estrecho como para operar en él.

![schema](schema.svg)

## Resumen de la estrategia

- Dos bloques de velas trabajan sobre el mismo instrumento en dos marcos temporales: media hora para operar y cuatro horas para confirmar.
- Cada serie alimenta su propio MACD, y dos convertidores extraen de cada indicador la línea MACD y la línea de señal.
- Una fórmula resta la línea de señal a la línea MACD, de modo que cada marco temporal queda reducido a un solo número: positivo significa que la parte rápida va por delante, negativo que va por detrás.
- Market depth se limita a su mejor nivel y dos convertidores leen la mejor oferta (ask) y la mejor demanda (bid); una tercera fórmula las convierte en el spread.
- Sync retiene los tres números y los libera juntos al compás de las cuatro horas. Eso es lo que hace honesta la comparación: el libro de órdenes se actualiza cientos de veces por vela y, sin ese bloque, las lecturas nunca corresponderían al mismo instante.
- Después de Sync, las dos diferencias se comparan con cero y el spread con su límite, y una condición lógica reúne las tres respuestas junto con la ausencia de posición abierta.
- Ambas entradas son órdenes a mercado de volumen fijo y solo se toman estando sin posición.
- Position protection se hace cargo de la operación: vigila las ejecuciones de entrada, toma del libro de órdenes el precio actual y cierra en un take-profit o un stop-loss expresados en porcentaje.

## Reglas de entrada y salida

- **Entrada en largo**: En el compás de cuatro horas ambas diferencias son positivas —el MACD está por encima de su línea de señal tanto en el marco temporal de trading como en el de confirmación—, el spread está dentro de su límite y no hay posición abierta. Position modify compra a mercado el volumen de la orden.
- **Entrada en corto**: En ese mismo compás ambas diferencias son negativas, con las mismas condiciones de spread y de posición plana. Position modify vende a mercado el volumen de la orden.
- **Salida**: En el diagrama no hay señal de salida: una vez abierta la posición, Position protection se queda con ella y la cierra con un 1.5% de beneficio o un 1% de pérdida respecto al precio de entrada. Mientras la posición sigue viva se ignoran las lecturas contrarias, de modo que una operación nunca se da la vuelta a mitad de camino.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Trading Candles | 00:30:00 | Marco temporal en el que trabaja el MACD de trading. |
| Confirming Candles | 04:00:00 | Marco temporal del MACD de confirmación y compás con el que Sync libera todos los valores. |
| Maximum Spread | 50 | Spread máximo, en unidades de precio, que todavía permite una entrada. |
| Order Volume | 1 | Tamaño de la orden, en lotes. |
| Take Profit, % | 1.5 | Distancia del take-profit, en porcentaje del precio de entrada. |
| Stop Loss, % | 1 | Distancia del stop-loss, en porcentaje del precio de entrada. |

## Detalles del diagrama

- Ambos bloques MACD están configurados para emitir solo valores formados y definitivos, de modo que una vela sin cerrar no puede alterar la decisión.
- El MACD de confirmación es deliberadamente más corto que el de trading: en un mes de historial hay pocas velas de cuatro horas, y el clásico 12/26/9 consumiría casi todo ese tiempo en su periodo de formación.
- Sync da nombre a cada línea que retiene, y ambos extremos de cada línea están conectados: un valor que entra y nunca se extrae dejaría el bloque a la espera y la estrategia no arrancaría.
- El spread se compara después de Sync y no en el punto donde llega, y esa es la única razón por la que un filtro gobernado por el libro de órdenes puede formar parte de una condición gobernada por velas.
- Position protection se alimenta del libro de órdenes en lugar de un precio de vela, de modo que calcula la salida a partir del mejor nivel actual.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
