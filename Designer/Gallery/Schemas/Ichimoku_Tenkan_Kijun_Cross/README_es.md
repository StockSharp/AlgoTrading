# Diagrama de la estrategia de cruce Tenkan/Kijun del Ichimoku
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Aquí el sistema Ichimoku se usa por completo: el par de líneas rápidas da la señal y la nube decide si esa señal está permitida. El cruce de Tenkan-sen con Kijun-sen es el disparador, y la posición solo se abre cuando el cierre está en el mismo lado de la nube Kumo hacia el que apunta el cruce.

![schema](schema.svg)

## Resumen de la estrategia

- Un único bloque Ichimoku construye todas las líneas, y cuatro conversores leen Tenkan-sen, Kijun-sen, Senkou Span A y Senkou Span B de su valor compuesto.
- Dos bloques de fórmula pliegan las dos líneas Senkou en el techo y el suelo de la nube, así que basta una comparación por lado para situar el cierre respecto a la nube.
- Solo se entra desde plano, y eso se comprueba dos veces: comparando la posición con cero y mediante la condición de apertura del propio bloque de orden.
- Las salidas son bloques aparte: el cruce contrario o un cierre que vuelve a caer dentro de la nube devuelven la posición a plano, y los bloques de cierre toman su tamaño de la posición abierta.
- El original ignora toda señal durante 500 velas tras una ejecución, lo que también retrasa sus salidas; con estos bloques no se puede construir un contador de barras, así que esa pausa se omite y el diagrama opera más a menudo que el original.

## Reglas de entrada y salida

- **Entrada en largo**: Tenkan-sen cruza por encima de Kijun-sen, el cierre está por encima del techo de la nube y la posición es plana. La orden compra el volumen fijo y abre el largo.
- **Entrada en corto**: Tenkan-sen cruza por debajo de Kijun-sen, el cierre está por debajo del suelo de la nube y la posición es plana. La orden vende el volumen fijo y abre el corto.
- **Salida**: El largo se cierra cuando Tenkan-sen vuelve a cruzar por debajo de Kijun-sen o el cierre cae por debajo del suelo de la nube; el corto, en la imagen especular. La orden de cierre se dimensiona con la posición, de modo que el diagrama vuelve a plano en lugar de invertir, y no hay stop de pérdidas ni toma de beneficios, igual que en el original.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Tenkan period | 9 | Periodo de Tenkan-sen, el punto medio entre el máximo y el mínimo de ese número de velas. |
| Kijun period | 26 | Periodo de Kijun-sen, construido igual pero sobre una ventana más larga. |
| Senkou Span B period | 52 | Periodo de Senkou Span B, el más lento de los dos bordes de la nube. |
| Volume | 1 | Volumen de la orden, en lotes, con el que se abre la posición; las salidas cierran el tamaño que haya abierto. |
| Candles | 00:01:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta el indicador Ichimoku y un conversor para el precio de cierre.
- Tenkan-sen y Kijun-sen se encuentran en un bloque de cruce cuya salida es el cruce alcista; un NO lógico sobre ella da el cruce bajista.
- Las dos comparaciones con la nube se comparten entre entradas y salidas: por encima de la nube se abre un largo y se cierra un corto, por debajo ocurre lo contrario.
- Cada entrada pasa por una Y lógica junto con la comprobación de posición plana, mientras que cada salida pasa por un O lógico, de modo que basta el cruce o la rotura de la nube para disparar un bloque de cierre.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
