# Diagrama de la estrategia de emulación Renko sin repintado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Los operadores recurren a los gráficos de ladrillos porque un ladrillo, una vez impreso, ya no cambia: una señal tomada sobre él no puede retirarse un minuto después. Este diagrama obtiene esa misma propiedad a partir de velas de tiempo corrientes. La fuente de velas se suscribe deliberadamente con actualizaciones intermedias, y un bloque Final value se interpone entre ella y todos los bloques que deciden, de modo que una media, un cruce y una entrada se resuelven siempre sobre una barra ya cerrada. El flujo en vivo se conserva, pero va únicamente al gráfico, que es donde corresponde una vela en formación.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas de cinco minutos se suscriben con las actualizaciones intermedias activadas, por lo que la fuente emite la barra muchas veces mientras se forma y una vez más cuando cierra.
- Un bloque Final value de tipo vela es la única puerta entre la fuente y la lógica: solo deja pasar un valor cuando ese valor es definitivo, así que nada aguas abajo llega a ver un precio que todavía puede moverse.
- Ese bloque es estructural, no decorativo. Un bloque indicador marca como definitivo todo valor que recibe, de modo que una media alimentada con una barra en formación reescribiría la lectura de esa misma barra en cada actualización, y un cruce construido sobre dos medias así aparecería y desaparecería dentro de la barra.
- Una media exponencial rápida, una media exponencial lenta y un índice de fuerza relativa se construyen sobre el flujo cerrado, y los tres están configurados para hablar solo una vez formados, de modo que las primeras decisiones esperan a una ventana lenta completa.
- Dos bloques Crossing llevan las dos caras del mismo evento: uno recibe la media rápida en la entrada up y la lenta en la entrada down, el otro las recibe intercambiadas, así que cada uno se activa en el cruce que le da nombre.
- El índice de fuerza relativa se compara con una variable de línea media en ambas direcciones, lo que da un filtro de impulso que debe coincidir con el cruce antes de que se envíe nada.
- La posición se lee en una variable de tenencia que se dispara con cada vela cerrada y se compara con cero dos veces, de modo que la entrada puede exigir una posición que no sea larga y la salida puede exigir que sí lo sea.
- Dos bloques lógicos AND reúnen cruce, impulso y posición; uno acciona una entrada a mercado con la condición Open position, el otro una salida a mercado con la condición Close position, y el panel del gráfico dibuja las velas en vivo, los tres indicadores, las órdenes y las ejecuciones.

## Reglas de entrada y salida

- **Entrada en largo**: En una vela cerrada la media rápida cruza por encima de la lenta, el índice de fuerza relativa se sitúa por encima de su línea media y la posición no es larga. El bloque AND reúne las tres respuestas y el bloque de entrada compra a mercado el volumen de la orden. Su condición Open position significa que la orden solo se envía desde una posición plana, así que un segundo cruce mientras hay una operación abierta no cambia nada.
- **Entrada en corto**: No hay entradas cortas. Por debajo de la línea media, o con la media rápida por debajo de la lenta, el diagrama simplemente se aparta; la única orden que llega a enviar en dirección de venta es la que cierra una posición larga.
- **Salida**: En una vela cerrada la media rápida vuelve a cruzar por debajo de la lenta, el índice de fuerza relativa ha caído por debajo de su línea media y la posición es larga. El segundo bloque AND se dispara y el bloque de cierre vende toda la posición a mercado, con un volumen calculado a partir de la posición abierta por la condición Close position. No hay bloque de stop-loss ni de take-profit: el cruce que abrió la operación es lo mismo que la termina, y cada una de esas decisiones se toma sobre una barra que ya ha concluido.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:05:00 | Marco temporal de la serie de velas sobre la que funciona todo el diagrama; la serie se suscribe con actualizaciones intermedias y el bloque Final value separa de ella las barras cerradas. |
| Fast EMA Length | 14 | Período de la media exponencial rápida, la más veloz de las dos líneas cuyos cruces son la señal. |
| Slow EMA Length | 40 | Período de la media exponencial lenta, la línea de referencia frente a la que se mide la rápida. |
| RSI Length | 14 | Período del índice de fuerza relativa utilizado como filtro de impulso en ambos lados. |
| RSI Midline | 50 | Nivel que divide el filtro de impulso: por encima de él el diagrama puede abrir una posición larga, por debajo de él puede cerrarse una larga. |
| Order Volume | 1 | Tamaño de la orden que envía la entrada; la salida, en cambio, toma su tamaño de la posición abierta. |

## Detalles del diagrama

- La puerta se dispara exactamente en la barra del cruce. Un bloque Crossing emite solo en el momento en que las dos líneas intercambian su posición, mientras que las comparaciones emiten en cada vela cerrada; una condición lógica retiene cada entrada hasta que todas han llegado desde su última activación, así que la pieza que falta es siempre el cruce y las respuestas con las que se combina son las de esa misma barra.
- Los dos bloques Crossing son el mismo bloque con las entradas intercambiadas. Cada uno emite true cuando su propia entrada up alcanza o supera a su entrada down, y false en el evento contrario, y por eso uno lleva el nombre del cruce alcista y el otro el del bajista, en lugar de que un único bloque alimente ambas puertas.
- El bloque de posición solo habla cuando la posición cambia, lo que en una semana tranquila puede no ocurrir nunca. La variable de tenencia que hay detrás parte de cero, conserva el último número que recibió y lo vuelve a emitir en cada vela cerrada, de modo que ambas comparaciones tienen siempre un lado izquierdo fresco con el que trabajar.
- Nada espacia las señales en el tiempo. Se toma todo cruce que cumpla las condiciones, y lo único que impide que una entrada caiga sobre otra es la exigencia de que la posición no sea larga, respaldada por la condición Open position en el propio bloque de la orden.
- La vela en formación no se descarta, solo se mantiene apartada de las decisiones: el flujo bruto se dibuja en el gráfico junto a los indicadores, que se trazan a partir del flujo cerrado, de modo que ambos pueden leerse uno frente al otro.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
