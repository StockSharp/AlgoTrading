# Diagrama de la estrategia Color Schaff Trend Cycle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El Schaff Trend Cycle es un estocástico calculado sobre el histograma del MACD, así que reacciona más rápido que un oscilador corriente y sigue moviéndose entre cero y cien. El diagrama opera el momento en que el ciclo sale de la parte central de esa banda y deja que una simple línea MACD decida si merece la pena seguirlo: solo las roturas al alza con MACD positivo y las roturas a la baja con MACD negativo se convierten en órdenes.

![schema](schema.svg)

## Resumen de la estrategia

- El Schaff Trend Cycle se calcula sobre velas cerradas y un bloque de valor anterior guarda su lectura de una vela antes, para distinguir una rotura del nivel de estar simplemente por encima de él.
- Dos niveles enmarcan el centro de la banda: cruzar el superior desde abajo es la señal larga, cruzar el inferior desde arriba es la señal corta.
- La línea MACD, la diferencia entre una media móvil exponencial rápida y una lenta, es solo un filtro de signo: positiva permite largos, negativa permite cortos.
- Tras la primera operación la estrategia siempre está en el mercado: cada señal gira la posición, porque el volumen de la orden es el volumen base más lo que ya se mantiene.

## Reglas de entrada y salida

- **Entrada en largo**: En la vela anterior el ciclo estaba en el nivel superior o por debajo y ahora está por encima, la línea MACD es positiva y la posición no es larga. La orden compra el volumen base más el valor absoluto de la posición: gira un corto a largo o abre un largo desde plano.
- **Entrada en corto**: En la vela anterior el ciclo estaba en el nivel inferior o por encima y ahora está por debajo, la línea MACD es negativa y la posición no es corta. La orden vende el volumen base más el valor absoluto de la posición: gira un largo a corto o abre un corto desde plano.
- **Salida**: No hay salida propia ni órdenes de protección, igual que en la estrategia original: solo se abandona la posición cuando llega la rotura contraria del nivel y la gira.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| STC smoothing length | 10 | Periodo de suavizado del Schaff Trend Cycle; valores mayores lo vuelven más lento y las roturas más escasas. |
| MACD fast EMA | 12 | Media móvil exponencial rápida dentro del filtro MACD. |
| MACD slow EMA | 26 | Media móvil exponencial lenta dentro del filtro MACD. |
| Upper level | 60 | Nivel que el ciclo debe romper al alza para dar señal larga. |
| Lower level | 40 | Nivel que el ciclo debe romper a la baja para dar señal corta. |
| Volume | 1 | Volumen base de la orden, en lotes; al girar se añade el valor absoluto de la posición. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta el Schaff Trend Cycle y el MACD; un bloque de valor anterior lee el ciclo una vela atrás.
- Cuatro bloques de comparación construyen las dos roturas: el valor anterior frente a un nivel y el actual frente al mismo nivel, lo que en conjunto significa que la línea lo atravesó en esta vela.
- Otras dos comparaciones dan el signo de la línea MACD y dos más contrastan la posición con la constante cero compartida, para que una señal no aumente una posición ya abierta.
- Cada Y lógica reúne cuatro condiciones —dónde estaba el ciclo, dónde está, el signo del MACD y la posición— y dispara un bloque de modificación de posición.
- Un bloque de fórmula calcula el tamaño del giro como volumen base más el valor absoluto de la posición, de modo que una sola orden a mercado cierra el lado antiguo y abre el nuevo, igual que el par de órdenes que envía el código en C#.
- Conviene conocer dos diferencias con el original en C#. El original lleva el nombre del Schaff Trend Cycle pero en realidad calcula un RSI de diez periodos en su lugar; este diagrama usa el indicador Schaff Trend Cycle real, así que las señales son las que promete el nombre y no las que produce el código.
- Además el original trabaja con velas de cuatro horas, que dejan muy pocas barras en el mes de histórico que acompaña a la galería; el diagrama usa velas de cinco minutos.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
