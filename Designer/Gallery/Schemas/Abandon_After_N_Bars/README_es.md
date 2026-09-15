# Diagrama de la estrategia de abandono tras N barras
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dos medias móviles cortas y un RSI de dos periodos deciden cuándo abrir una operación, pero el objetivo de este diagrama es la regla que la termina. Una operación que no ha alcanzado ni su objetivo ni su stop dentro de un número fijo de velas se abandona y se cierra a mercado. La cuenta atrás la mide un bloque N values y arranca con la ejecución que realmente abrió la posición, no con la señal que la pidió.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas de cinco minutos finalizadas alimentan una media móvil exponencial rápida y otra lenta, un RSI de dos periodos y un conversor que extrae el precio de cierre de cada vela.
- Dos comparaciones leen la tendencia a partir de las medias: la rápida por encima de la lenta y la rápida por debajo de la lenta. Otras dos leen el momentum frente a una variable de umbral: el RSI por debajo de ella y el RSI por encima de ella.
- El bloque Position se compara con cero dos veces, una por igualdad y otra por desigualdad, de modo que el diagrama dispone tanto de una prueba de posición plana para las entradas como de una prueba de posición abierta para la regla de abandono.
- Cada entrada es un AND lógico de tres señales — tendencia, momentum y posición plana — y ambos bloques de entrada están configurados solo para abrir, por lo que el diagrama mantiene una única posición a la vez y nunca la incrementa.
- Position protection vigila las ejecuciones de entrada y gestiona la operación con un take-profit porcentual y un stop-loss dinámico (trailing) que sigue al precio en cuanto este se mueve a favor de la posición.
- Strategy trades informa de cada ejecución propia. Un bloque Flag, reiniciado mientras la posición está plana, deja pasar solo la primera ejecución de una operación, que es la que la abrió.
- Ese único pulso arma el bloque N values, que a partir de ahí cuenta velas finalizadas y emite una señal cuando se alcanza la cuenta.
- La señal de abandono pasa por un segundo AND junto con la prueba de posición abierta antes de llegar a un bloque Position modify configurado para cerrar, de modo que la cuenta atrás solo puede terminar una operación que siga viva.

## Reglas de entrada y salida

- **Entrada en largo**: La media rápida está por encima de la lenta, el RSI está por debajo de su umbral y la posición está plana. La combinación compra la debilidad dentro de una tendencia alcista de corto plazo. Position modify compra el volumen de la orden a mercado, solo en apertura.
- **Entrada en corto**: La media rápida está por debajo de la lenta, el RSI está por encima de su umbral y la posición está plana: fortaleza dentro de una tendencia bajista de corto plazo. Position modify vende el volumen de la orden a mercado, solo en apertura.
- **Salida**: Hay dos salidas independientes. Position protection puede cerrar la operación primero, con un 1.2% de beneficio o por un trailing stop situado un 0.6% por detrás del mejor precio alcanzado. Si no ocurre ninguna de las dos, toma el relevo la regla de abandono: doce velas finalizadas después de la ejecución de apertura, el bloque N values se dispara, la prueba de posición abierta confirma que todavía hay algo que cerrar y un bloque Position modify configurado para cerrar liquida el lado que se mantenga.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:05:00 | Marco temporal de las velas con las que trabaja todo el diagrama; la cuenta atrás de abandono se mide en estas velas. |
| Fast EMA Length | 3 | Periodo de la media móvil exponencial rápida. |
| Slow EMA Length | 7 | Periodo de la media móvil exponencial lenta; manténgalo mayor que el de la rápida o la prueba de tendencia pierde su sentido. |
| RSI Length | 2 | Periodo del RSI. Un periodo muy corto hace que cruce el umbral a menudo, que es lo que produce entradas frecuentes. |
| RSI Threshold | 50 | Nivel con el que se compara el RSI. Un largo compra por debajo de él y un corto vende por encima, así que subirlo hace más frecuentes las entradas largas y más raras las cortas. |
| Order Volume | 1 | Tamaño de cada orden de entrada, en unidades del instrumento. |
| Take Profit, % | 1.2 | Distancia del take-profit, en porcentaje del precio de entrada. |
| Trailing Stop, % | 0.6 | Distancia del trailing stop, en porcentaje: el stop parte a esa distancia de la entrada y sigue al mejor precio alcanzado, sin retroceder nunca. |
| Bars Before Abandon | 12 | Cuántas velas finalizadas puede durar una operación antes de abandonarla y cerrarla a mercado. Redúzcalo para abandonar antes; auméntelo para dejar la salida al take-profit y al trailing stop. |

## Detalles del diagrama

- La cuenta atrás la inicia una ejecución y no una señal, de modo que el reloj mide cuánto tiempo lleva existiendo realmente la operación y no cuánto hace que el diagrama la quiso.
- El bloque de ejecuciones de la estrategia informa también de las salidas, que de otro modo reiniciarían la cuenta atrás en cada ejecución de cierre. El bloque Flag lo impide: solo se reinicia mientras la posición está plana, así que, una vez que la operación está en marcha, sus ejecuciones posteriores se ignoran.
- La prueba de posición abierta en el segundo AND es lo que vuelve inofensiva una cuenta atrás que vence sin posición abierta: el bloque de cierre solo se dispara cuando hay una posición que cerrar.
- Ambos bloques de entrada envían sus ejecuciones a un Combination, de modo que Position protection queda armado por una única entrada sea cual sea el lado abierto, y el precio de cierre alimenta ese mismo bloque, dando al take-profit y al trailing stop un valor con el que trabajar en cada vela finalizada.
- El panel del gráfico dibuja las velas, ambas medias, el RSI, las órdenes de entrada y de abandono, las órdenes de protección y cada ejecución, de modo que una operación abandonada se distingue con facilidad de otra que alcanzó su objetivo.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
