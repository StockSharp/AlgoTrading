# Diagrama de la estrategia Tape Reader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una vela dice qué ocurrió durante una barra; la cinta dice cómo ocurrió. Este diagrama se suscribe al flujo de operaciones ejecutadas, mide cada impresión frente al tamaño medio de las últimas cien impresiones y trata una impresión de varias veces esa media como la huella de alguien con prisa. La lectura se toma una vez por cada vela terminada, de modo que un flujo que se dispara miles de veces al día sigue produciendo una decisión por barra.

![schema](schema.svg)

## Resumen de la estrategia

- El flujo de ticks lleva las dos mitades de la señal: un convertidor lee el tamaño de cada operación ejecutada y otro lee su precio.
- Una media móvil sobre los tamaños de las últimas cien impresiones da al diagrama una idea actualizada de cómo es una operación corriente en este instrumento, de modo que nada en él queda atado a un nivel de precio concreto ni a un tamaño de contrato concreto.
- Una fórmula divide el tamaño de cada impresión entre esa media y lo convierte en un múltiplo simple: uno es una impresión corriente, cinco es una impresión cinco veces mayor que la norma reciente.
- Una variable retiene ese múltiplo, y una segunda variable retiene el precio de la impresión, hasta que la vela cierra. La vela es su disparador, y eso es lo que pone una medición a velocidad de tick y una decisión a velocidad de vela en el mismo reloj.
- Una comparación contra el Size factor responde si la impresión fue grande. El cierre de la vela anterior, tomado con un bloque Previous value y un convertidor, responde hacia qué lado fue.
- El bloque Position se compara con cero tres veces, lo que da una comprobación de posición plana para las entradas y una comprobación larga y otra corta para las salidas.
- Strategy trades informa de cada ejecución propia y reinicia un contador de velas, lo que mantiene al diagrama fuera del mercado durante un número fijo de velas después de cualquier ejecución, de entrada o de salida.
- Ambas entradas son bloques Position modify configurados solo para abrir, y la salida es un tercero configurado para cerrar, de modo que se mantiene una sola posición a la vez y se cierra antes de poder tomar el lado contrario.

## Reglas de entrada y salida

- **Entrada en largo**: La impresión que pasó en último lugar antes del cierre de la vela fue al menos Size factor veces el tamaño medio de impresión, su precio está por encima del cierre de la vela anterior, la posición está plana y el enfriamiento ha transcurrido. Position modify compra el volumen de la orden a mercado.
- **Entrada en corto**: La misma impresión grande, pero con precio por debajo del cierre de la vela anterior, de nuevo desde posición plana y con el enfriamiento transcurrido. Position modify vende el volumen de la orden a mercado.
- **Salida**: No hay take-profit ni stop-loss. Una posición larga se cierra cuando llega una impresión grande por debajo del cierre anterior mientras la posición es larga, y una corta se cierra con una impresión grande por encima de él. Ambas condiciones de salida alimentan una misma Combination, que dispara el bloque Position modify configurado para cerrar; la ejecución que deja la posición plana reinicia también el enfriamiento, de modo que la siguiente entrada espera el mismo número de velas.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:05:00 | Marco temporal de las velas sobre las que trabaja todo el diagrama; cada lectura de la cinta se toma cuando una de ellas cierra. |
| Average Prints | 100 | Cuántas de las impresiones más recientes forman el tamaño medio contra el que se mide el múltiplo. Una ventana más corta sigue más rápido un cambio de actividad y hace que la propia media sea más nerviosa. |
| Size Factor | 5 | Cuántas veces el tamaño medio debe alcanzar una impresión para contar como grande. Súbelo para obtener señales más raras y selectivas; bájalo cuando la cinta está tranquila y nada califica. |
| Cooldown Bars | 3 | Cuántas velas terminadas deben pasar tras cualquier ejecución antes de permitir la siguiente entrada. |
| Order Volume | 1 | Tamaño de cada orden de entrada, en unidades del instrumento. La orden de cierre se dimensiona a partir de la posición abierta y no lee este valor. |

## Detalles del diagrama

- La medida es un ratio y no un número de contratos, así que el mismo Size factor se lee igual en un instrumento cotizado en fracciones de unidad que en uno cotizado en lotes enteros.
- La media incluye la impresión que se mide contra ella, así que una única impresión muy grande eleva un poco su propio patrón de referencia; con cien impresiones en la ventana, una impresión cinco veces la norma sigue leyéndose por encima de cuatro.
- La impresión que se lee es la última en llegar antes del cierre de la vela. La variable que la retiene es lo que se interpone entre un flujo que se dispara miles de veces al día y una decisión pensada para tomarse una vez por barra; sin ella, la comprobación de tamaño y la de precio responderían en relojes distintos y nunca coincidirían.
- El contador de enfriamiento se reinicia desde el bloque de ejecuciones de la estrategia y no desde los bloques de entrada, de modo que una ejecución de cierre también inicia la espera. Sin eso, la salida de una operación y la entrada de la siguiente caerían en velas contiguas.
- El panel del gráfico dibuja las velas, el tamaño medio de impresión, el múltiplo retenido y la línea del Size factor, las órdenes de entrada y de salida y cada ejecución propia, de modo que la impresión que produjo una operación puede encontrarse junto a la vela a la que pertenece.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
