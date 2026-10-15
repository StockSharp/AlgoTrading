# Diagrama de estrategia: plantilla de entrada filtrada por spread
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La señal es deliberadamente simple: una vela cierra por encima de su propia apertura mientras el precio cruza al alza una media móvil lenta. De lo que trata realmente el diagrama es de todo lo que se interpone entre esa señal y la orden: un spread leído del libro de órdenes, una espera que separa las entradas y un volumen de orden calculado a partir del capital en lugar de una cifra fija.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas de cuatro horas ya cerradas alimentan una media móvil simple de 50 periodos y dos conversores que extraen la apertura y el cierre de cada vela.
- El color de la vela son dos comparaciones: cierre por encima de la apertura es alcista, cierre por debajo de la apertura es bajista.
- Un bloque Previous value guarda la vela de un paso atrás y un segundo guarda la media, de modo que el cruce se lee como un par de comparaciones corrientes y no como un indicador aparte.
- Market depth aporta el mejor bid y el mejor ask, y una fórmula resta uno del otro. El resultado queda en una variable que libera la vela, así la condición de entrada compara un spread que pertenece al mismo momento que todo lo demás.
- Strategy P&L alimenta una variable que lleva el resultado realizado, una fórmula lo suma al capital inicial y una segunda fórmula convierte ese capital en un volumen de orden: capital por la fracción de riesgo, dividido entre el precio de cierre y redondeado a tres decimales.
- Un contador formado por una variable y una fórmula min(a + 1, n) mide las velas transcurridas desde la última ejecución y bloquea una nueva entrada hasta que hayan pasado ocho.
- Ambas entradas se toman únicamente desde posición plana, así que el diagrama mantiene una sola posición a la vez y nunca la incrementa.
- La salida es el color de vela contrario, y Position protection añade encima un take-profit del 0.7% y un stop-loss del 0.5%.

## Reglas de entrada y salida

- **Entrada en largo**: Una vela alcista cuyo cierre anterior quedó en la media anterior o por debajo de ella y cuyo cierre está por encima de la media actual, con el spread dentro de su límite, la posición plana y la espera cumplida. Position modify compra a mercado con el volumen calculado.
- **Entrada en corto**: Una vela bajista cuyo cierre anterior quedó en la media anterior o por encima de ella y cuyo cierre está por debajo de la media actual, con las mismas condiciones de spread, posición plana y espera. Position modify vende a mercado con el mismo volumen calculado.
- **Salida**: Una vela bajista cierra una posición larga y una vela alcista cierra una corta: ambas condiciones se juntan en un Combination que dispara un único Position modify configurado para cerrar la posición, así que ninguna dirección necesita su propio bloque de salida. Position protection vigila por su cuenta las ejecuciones de entrada y puede cerrar la operación antes, con un 0.7% de beneficio o un 0.5% de pérdida.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 04:00:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |
| SMA Length | 50 | Periodo de la media móvil simple contra la que se mide el cierre. |
| Spread Limit | 50 | Spread máximo del libro, en unidades de precio, que todavía permite una entrada. Súbelo en instrumentos cotizados con un spread amplio. |
| Start Capital | 1000000 | Tamaño de cuenta del que parte el cálculo del capital; ajústalo al tamaño real de la cuenta antes de operar. |
| Risk Fraction | 0.3 | Parte del capital comprometida en una posición, expresada como fracción: 0.3 es el treinta por ciento. |
| Cooldown Bars | 8 | Cuántas velas cerradas deben pasar tras una ejecución antes de permitir la siguiente entrada. |
| Take Profit, % | 0.7 | Distancia del take-profit, en porcentaje del precio de entrada. |
| Stop Loss, % | 0.5 | Distancia del stop-loss, en porcentaje del precio de entrada. |

## Detalles del diagrama

- El bloque de velas alimenta ocho consumidores: la media, ambos conversores, el bloque de vela anterior, la variable del spread, la variable del resultado realizado, el contador de espera y el panel del gráfico.
- El libro de órdenes se actualiza mucho más a menudo que las velas, por eso su spread no se compara directamente. Una variable toma el último valor y lo libera al cerrar la vela, lo que pone todos los términos de la condición de entrada en el mismo reloj.
- La variable del resultado realizado arranca en cero y solo toma valor cuando se cierra la primera operación, con lo que la fórmula del volumen tiene datos desde la primera vela.
- Ambos bloques de entrada comparten una misma fórmula de volumen, así que largo y corto se dimensionan con la misma regla.
- El contador de espera lo reinicia el bloque de ejecuciones de la estrategia, es decir, cualquier ejecución —una entrada o una salida de protección— vuelve a iniciar la espera de ocho velas.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
