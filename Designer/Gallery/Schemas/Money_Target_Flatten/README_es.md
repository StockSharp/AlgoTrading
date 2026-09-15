# Diagrama de la estrategia Money Target Flatten
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un objetivo en dinero no mira el precio en absoluto: cierra la posición cuando el dinero acumulado en ella alcanza una cifra. Este diagrama coloca esa regla sobre un motor sencillo de dos medias. El cruce decide cuándo estar en el mercado; el objetivo de beneficio y el límite de pérdida deciden cuándo es suficiente, y en el momento en que se alcanza cualquiera de los dos la posición se cierra y toda orden que siga activa se retira detrás de ella.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas de cinco minutos ya cerradas alimentan una media móvil exponencial rápida y otra lenta, y un bloque de cruce convierte ese par en un único evento: verdadero cuando la línea rápida sube atravesando a la lenta, falso cuando la atraviesa a la baja.
- Un NOT lógico da al cruce bajista una señal propia, de modo que cada dirección es una compuerta por derecho propio.
- El bloque de posición comparado con cero indica si el diagrama está sin posición, largo o corto, y cada compuerta es un AND lógico de un cruce y un estado de la posición.
- Ambas entradas son órdenes a mercado de volumen fijo y llevan la condición de apertura de posición, de modo que un cruce que llegue mientras ya hay una posición en marcha no puede aumentarla.
- El bloque Strategy P&L emite el resultado abierto de la posición en curso a un ritmo propio, varias veces por hora en lugar de una vez por barra.
- Dos comparaciones contrastan ese número con el objetivo de beneficio y con el límite de pérdida, y cada umbral es una variable disparada por el mismo valor de P&L, de modo que una comparación siempre ve sus dos operandos del mismo instante.
- Un bloque Combination une las dos respuestas en una única línea de salida, que hace dos cosas a la vez: Position modify cierra la posición a mercado y Mass order cancellation barre todo lo que siga activo.
- Un cruce que va en contra de una posición abierta también la cierra, de modo que el diagrama nunca permanece en una operación contra la que se han girado las medias.

## Reglas de entrada y salida

- **Entrada en largo**: La media rápida cruza al alza la lenta en una vela cerrada mientras no hay posición: Position modify compra el volumen de la orden a mercado.
- **Entrada en corto**: La media rápida cruza a la baja la lenta en una vela cerrada mientras no hay posición: Position modify vende el volumen de la orden a mercado.
- **Salida**: Dos salidas independientes. La salida por dinero se dispara en cuanto el resultado abierto de la posición alcanza el objetivo de beneficio o cae hasta el límite de pérdida: la combinación transmite la señal, la posición se cierra a mercado y toda orden restante se retira. Un cruce en contra de la posición abierta también la cierra. Lo que ocurra primero deja el diagrama sin posición y a la espera, y el siguiente cruce que lo encuentre sin posición abre la siguiente posición, larga o corta.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:05:00 | Marco temporal de la serie de velas sobre la que se construyen ambas medias. |
| Fast EMA Length | 12 | Longitud de la media móvil exponencial rápida. |
| Slow EMA Length | 26 | Longitud de la media móvil exponencial lenta. |
| Volume | 1 | Tamaño de la orden, en lotes, que envía cada entrada. |
| Profit To Close | 300 | Resultado abierto, en dinero, al que la posición se cierra como ganancia. |
| Loss To Close | -600 | Resultado abierto, en dinero, al que la posición se cierra como pérdida; se escribe como número negativo porque se compara directamente con el resultado. |

## Detalles del diagrama

- Los umbrales se comparan con el resultado no realizado, el dinero de la posición que está en curso, así que se comportan como un take-profit y un stop-loss en dinero sobre cada posición por turno. Esas mismas dos comparaciones conectadas a la salida realizada convierten el par en un interruptor de un solo sentido: en cuanto la cuenta alcanza la cifra, la ejecución ha terminado.
- La salida por dinero no está atada al ritmo de las velas. Actúa sobre la actualización del P&L, de modo que un objetivo puede tomarse en mitad de una barra en lugar de en el siguiente cierre.
- Todas las órdenes que envía el diagrama son órdenes a mercado, así que sobre el histórico incluido el barrido no encuentra nada que retirar. Está conectado porque una salida por dinero que deja órdenes activas detrás es solo media salida, y empieza a importar en cuanto una orden en reposo se suma al diagrama.
- Ambos bloques de cierre llevan la condición de cierre de posición y por tanto no necesitan ni lado ni volumen: el bloque lee la posición abierta y envía la orden contraria exactamente por ese tamaño.
- El ritmo de cinco minutos es lo que hace visible la capa de dinero. Sobre una vela mucho más larga este par de medias se cruza solo unas pocas veces al mes, las posiciones son escasas, y los umbrales están dimensionados para el recorrido que produce una serie de cinco minutos.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
