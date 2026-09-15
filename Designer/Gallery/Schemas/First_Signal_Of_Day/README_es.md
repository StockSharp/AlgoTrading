# Diagrama de la estrategia Primera señal del día
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El cruce de dos medias exponenciales en velas de cinco minutos es una señal corriente y, en un marco temporal rápido, se repite muchas veces al día. Este diagrama opera solo la primera señal de cada dirección por día natural y deja pasar sin tocar todos los cruces posteriores. La memoria que lo hace posible son dos bloques Flag, y lo que borra esa memoria es el reloj de la estrategia al detectar que la fecha ha cambiado.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas de cinco minutos ya cerradas alimentan una media móvil exponencial rápida y otra lenta, y un bloque Crossing convierte el par en un único evento: emite true cuando la media rápida cruza por encima de la lenta y false cuando la cruza por debajo.
- Un Not lógico convierte ese mismo evento en una señal independiente de cruce a la baja, de modo que un solo bloque Crossing sirve para ambas direcciones sin duplicar las medias.
- El bloque Current time envía el reloj de la estrategia a un conversor que extrae de él el día natural, así el diagrama dispone de un número de día que no debe nada a la serie de velas.
- Una variable guarda el número de día y lo libera cuando se cierra una vela, lo que significa que siempre lleva el día al que pertenecía la vela anterior; por eso una comparación NotEqual contra el número de día actual resulta verdadera exactamente una vez, en la primera barra después de la medianoche.
- Cada dirección tiene su propio Flag: la señal de entrada es su disparador y ese pulso de cambio de día es su reinicio. Un Flag deja pasar un disparador solo la primera vez que se activa, de modo que todo lo que llega tras la primera señal aceptada del día se descarta en silencio hasta que cambia la fecha.
- Las entradas son bloques Position modify configurados para abrir solo desde posición plana, así los dos cerrojos gastan como mucho una posición larga y una corta al día y nunca acumulan tamaño.
- El cruce contrario cierra lo que esté abierto: dos condiciones lógicas se reúnen en un Combination que dispara un único Position modify configurado para cerrar, de modo que ninguno de los dos lados necesita su propio bloque de salida.
- Position protection vigila las ejecuciones de entrada y puede terminar una operación antes, a una distancia fija de take-profit o de stop-loss, así una posición nunca se queda esperando un cruce que no llega.

## Reglas de entrada y salida

- **Entrada en largo**: La media rápida cruza por encima de la lenta mientras la posición está plana. Esa combinación dispara el Flag largo y, como un Flag se dispara solo en su primera activación, el cruce se ejecuta únicamente si no se ha tomado ninguna posición larga desde el último cambio de fecha. Position modify compra entonces a mercado con el volumen configurado, y rechaza la orden de plano si ya hay algo abierto.
- **Entrada en corto**: La media rápida cruza por debajo de la lenta mientras la posición está plana. La señal pasa por el Flag corto, que igualmente deja pasar solo su primera activación del día, y Position modify vende a mercado con el mismo volumen. Los dos Flag son independientes, así que un día puede contener una posición larga y una corta, en cualquier secuencia.
- **Salida**: Un cruce a la baja estando largo y un cruce al alza estando corto los recoge un Combination que dispara un único Position modify configurado para cerrar la posición, el cual calcula por sí mismo el volumen de cierre. Position protection funciona en paralelo sobre las ejecuciones de entrada y puede cerrar la operación antes, a la distancia de take-profit o de stop-loss. Nada se invierte en un solo paso: la posición vuelve primero a plana y el lado contrario se abre después, en un cruce que encuentre la cuenta vacía.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:05:00 | Marco temporal de la serie de velas sobre la que funciona todo el diagrama. Una serie más lenta significa menos cruces al día y un límite diario que rara vez se alcanza. |
| Fast EMA Length | 14 | Longitud de la media móvil exponencial rápida. |
| Slow EMA Length | 40 | Longitud de la media móvil exponencial lenta. Mantenla holgadamente por encima de la rápida; de lo contrario, las dos medias se cruzan constantemente y, de todos modos, solo el primer cruce de cada día sobrevive al cerrojo. |
| Volume | 1 | Tamaño de una entrada, en unidades del instrumento. Ambas direcciones lo utilizan. |
| Take Profit, % | 1.5 | Distancia del take-profit, en porcentaje del precio de entrada. |
| Stop Loss, % | 1 | Distancia del stop-loss, en porcentaje del precio de entrada. |

## Detalles del diagrama

- El número de día procede del reloj y no de las velas, así el reinicio sigue funcionando en una sesión con huecos y no depende de que exista una barra en el momento en que cambia la fecha.
- El reloj avanza con su propio tick, mucho más a menudo de lo que cierran las velas. Por eso su valor no se compara directamente con uno almacenado: una variable lo pone primero al ritmo de las velas, que es lo que hace que la comparación signifique «esta vela pertenece a un día distinto que la vela anterior».
- Un Flag ignora un false que llegue por cualquiera de sus dos conectores, de modo que la comparación de cambio de día puede emitir false durante todo el día sin alterar el cerrojo, y una condición lógica que se evalúa como falsa no cuesta nada.
- La puerta de entrada exige posición plana y el propio bloque de entrada se niega a trabajar desde cualquier otra situación, y eso es deliberado: la señal gasta el cerrojo, así que una señal que no pudiera ejecutarse quemaría el día para esa dirección.
- Ambas direcciones comparten una única variable de volumen y un único bloque de protección, así que la posición larga y la corta se dimensionan y se protegen exactamente con la misma regla.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
