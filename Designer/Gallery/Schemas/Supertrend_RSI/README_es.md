# Diagrama de la estrategia Supertrend + RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un diagrama seguidor de tendencia con un freno de oscilador. SuperTrend, una banda de ATR que se arrastra tras el precio y gira con él, decide la dirección, mientras que el RSI decide si al movimiento le queda recorrido: el largo solo se toma mientras el RSI sigue por debajo de su línea media y el corto solo mientras sigue por encima. La salida no es una señal, sino un take-profit y un stop-loss porcentuales colocados sobre la operación de entrada.

![schema](schema.svg)

## Resumen de la estrategia

- SuperTrend se construye con un ATR de diez periodos multiplicado por tres, de modo que la línea avanza por detrás del precio y solo gira cuando el cierre la atraviesa.
- El RSI actúa como freno y no como señal de giro: la entrada se permite mientras el oscilador está en el lado tranquilo del nivel cincuenta, lo que mantiene al diagrama fuera de movimientos ya estirados.
- Las entradas solo se producen desde posición plana, tanto por la comparación explícita de la posición con cero como por la condición de apertura de los bloques de orden.
- Toda la salida se delega en un bloque de protección con un take-profit del dos por ciento y un stop-loss del uno por ciento, justo el par que arranca la estrategia original.

## Reglas de entrada y salida

- **Entrada en largo**: El cierre está por encima de la línea SuperTrend, el RSI está por debajo de la media de cincuenta y la posición está plana. La orden compra el volumen compartido a mercado y el bloque de protección arma de inmediato el take-profit y el stop-loss sobre la operación resultante.
- **Entrada en corto**: El cierre está por debajo de la línea SuperTrend, el RSI está por encima de la media de cincuenta y la posición está plana. La orden vende el volumen compartido a mercado y el bloque de protección arma igualmente las dos salidas.
- **Salida**: No hay salida por señal ni vuelta de posición: la cierra la primera de las dos órdenes protectoras que se alcance, el take-profit del dos por ciento o el stop-loss del uno por ciento.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| SuperTrend ATR Period | 10 | Periodo del ATR dentro de SuperTrend; valores mayores ensanchan la banda y espacian los giros. |
| SuperTrend Multiplier | 3 | Multiplicador del ATR de SuperTrend, la distancia de la línea de arrastre respecto al precio mediano. |
| RSI Length | 14 | Periodo de suavizado del índice de fuerza relativa. |
| RSI Midline | 50 | Nivel del RSI contra el que se mide el filtro de entrada; el código original compara con cincuenta y no con los niveles de sobreventa y sobrecompra que declara. |
| Take Profit, % | 2 | Distancia del take-profit respecto al precio de entrada, en porcentaje. |
| Stop Loss, % | 1 | Distancia del stop-loss respecto al precio de entrada, en porcentaje. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta SuperTrend, el RSI y un conversor que lee el precio de cierre de la misma vela.
- La comparación del cierre con la salida de SuperTrend da el indicador de tendencia alcista; un NO lógico sobre él da el de tendencia bajista, por lo que las dos direcciones nunca disparan en la misma vela.
- Una única constante de cincuenta sirve a las dos comparaciones del RSI, así que mover la línea media mueve los dos filtros a la vez.
- Cada Y lógica une tres condiciones —tendencia, oscilador y posición plana— y dispara un bloque de modificación de posición que además lleva la condición de apertura.
- Ambos bloques de modificación entregan su operación al bloque de protección, que coloca las órdenes de take-profit y stop-loss tomando el precio del cierre de la vela en curso.
- La pausa de cien velas que el código original mantiene entre operaciones no se reproduce: los bloques disponibles no tienen contador de velas, así que las entradas se reanudan en cuanto la protección deja la posición plana.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
