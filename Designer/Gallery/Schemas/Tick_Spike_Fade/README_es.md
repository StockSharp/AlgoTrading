# Diagrama de la estrategia Tick Spike Fade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un pico no se ve en el precio de cierre. El precio puede alejarse medio por ciento de donde estaba hace veinte minutos y volver antes de que termine el minuto, y la vela que lo registra parece igual que cualquier otra. Este diagrama observa en su lugar la cinta: cada operación ejecutada se mide contra el cierre de veinte barras atrás, y la primera que se aleja lo suficiente arma uno de los dos lados de la entrada. La orden se envía entonces en sentido contrario — una subida se vende, una caída se compra — y sale al cierre de la barra en la que apareció el pico.

![schema](schema.svg)

## Resumen de la estrategia

- Una serie de velas de un minuto es el reloj del diagrama. Solo se transmiten las velas terminadas, de modo que el precio de referencia, el temporizador de liberación y el momento en que una señal armada se convierte en orden se cuentan todos en barras cerradas.
- La cinta se suscribe junto con las velas, y un conversor lee el precio de cada operación ejecutada. Ese precio, y no el cierre de una vela, es el precio actual de todo el diagrama.
- Un bloque Previous value guarda la vela de veinte barras atrás y un conversor toma su cierre. Esta es la referencia contra la que se mide el precio actual, lo bastante lejana como para que un solo minuto de ruido no pueda alcanzar el umbral.
- Una variable mantiene esa referencia y la libera en cada operación ejecutada, de modo que la fórmula posterior recibe ambas entradas actualizadas al ritmo de la cinta. Calcula en porcentaje la distancia entre la última operación y el cierre de referencia, y está protegida para que una referencia igual a cero no produzca ningún resultado.
- Una segunda fórmula niega ese porcentaje, lo que permite que una sola constante de umbral sirva para ambas direcciones: la comparación contra ella responde por una subida, y la misma comparación sobre el valor negado responde por una caída.
- Cada dirección tiene su propio bloque Flag. La primera operación que cruza el umbral activa su Flag y este emite un único pulso; todas las operaciones posteriores del mismo movimiento se ignoran, de modo que un pico produce una decisión y no cien.
- Ese pulso arma un bloque N values configurado en un valor, que lo libera en la siguiente vela terminada. La decisión se toma entre velas, sobre la cinta, y la orden se envía en una vela.
- Position modify abre a mercado bajo la condición de apertura de posición, de modo que un diagrama que ya mantiene algo nunca añade a esa posición. Position protection se hace cargo entonces de la operación, y su conector Price también se alimenta desde la cinta.

## Reglas de entrada y salida

- **Entrada en largo**: La última operación ejecutada está a una distancia igual o mayor que el umbral por debajo del cierre de veinte barras atrás, y el Flag de caída está libre. El Flag emite su único pulso, la retención de un valor lo libera al cierre de la barra en curso, y Position modify compra a mercado el volumen de la orden.
- **Entrada en corto**: La última operación ejecutada está a una distancia igual o mayor que el umbral por encima del cierre de veinte barras atrás, y el Flag de subida está libre. El Flag emite su único pulso y, al cierre de la barra en la que apareció el pico, Position modify vende a mercado el volumen de la orden.
- **Salida**: No hay señal de salida ni regla de cierre propia. Position protection toma la ejecución de entrada y coloca un take-profit al 0.6% y un stop-loss al 0.3% del precio de ejecución; como su conector Price se alimenta desde la cinta, ambos niveles se comprueban en cada operación ejecutada en lugar de una vez por minuto. El primero que se toque cierra la posición con una orden a mercado, y el diagrama vuelve a quedar plano mucho antes de que se agote el temporizador de liberación.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:01:00 | Marco temporal de la serie de velas. Es el reloj del diagrama: el cierre de referencia, el temporizador de liberación y el momento en que una señal armada se convierte en orden se cuentan todos en estas velas. |
| Lookback Bars | 20 | A cuántas velas atrás se toma el cierre de referencia. Un número mayor mide el movimiento sobre una ventana más larga y hace que el mismo umbral sea más difícil de alcanzar. |
| Spike Threshold, % | 0.3 | A qué distancia, en porcentaje, debe estar la última operación del cierre de referencia para que el movimiento cuente como pico. Un solo valor sirve para ambas direcciones. |
| Cooldown Bars | 30 | Cuántas velas terminadas pasan tras un pico antes de que los Flag se liberen y el diagrama pueda volver a reaccionar. |
| Order Volume | 1 | Tamaño de la orden, en lotes. |
| Take Profit, % | 0.6 | Distancia del take-profit respecto al precio de ejecución, en porcentaje, comprobada en cada operación ejecutada. |
| Stop Loss, % | 0.3 | Distancia del stop-loss respecto al precio de ejecución, en porcentaje, comprobada en cada operación ejecutada. |

## Detalles del diagrama

- Leer el precio actual desde la cinta es lo que convierte la comparación en un detector de picos. Un movimiento que se aleja un tercio de por ciento y vuelve dentro del mismo minuto casi no deja rastro en el precio de cierre, pero cada una de sus operaciones pasa por la comparación, y la primera que cruza la línea activa el Flag.
- La referencia es un cierre de veinte barras atrás y no el cierre anterior al último. En un minuto incluso un movimiento violento es pequeño, y un umbral lo bastante bajo como para reaccionar a él saltaría con el ruido corriente.
- Los Flag son lo que hace que un pico sea una sola operación. Un Flag permanece activo hasta que el temporizador de liberación ha contado treinta velas terminadas, de modo que un movimiento que sigue estirándose no puede venderse tres veces mientras sube.
- La orden se cronometra con una vela aunque la decisión se tomara sobre una operación de la cinta: la retención de un valor pasa la señal armada a la primera vela que termina después de ella, y la orden de entrada lleva la hora de esa vela.
- Ambas distancias de protección son porcentajes del precio de ejecución y no pasos fijos, por lo que las mismas cifras significan lo mismo en un instrumento cotizado cerca de 65 000 y en otro cotizado cerca de 5.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
