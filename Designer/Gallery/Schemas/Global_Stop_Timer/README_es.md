# Diagrama de la estrategia Global Stop Timer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Las entradas las decide el precio; las salidas, el dinero. El cruce del momentum sobre su nivel neutro abre una posición en la dirección que ya señala la media móvil, y desde ese instante el resultado abierto de la estrategia manda sobre la operación: el bloque de P&L vigila cada cambio de la cifra no realizada y cierra la posición en cuanto esta cae al stop monetario o alcanza el objetivo monetario, digan lo que digan los indicadores.

![schema](schema.svg)

## Resumen de la estrategia

- Una única serie de velas, de cinco minutos y solo con barras terminadas, alimenta todo: los dos indicadores, el conversor de precio de cierre y los disparadores de las variables constantes.
- El momentum mide cuánto ha recorrido el precio a lo largo de su propia longitud; el bloque Crossing lo compara con un nivel guardado en una variable y solo se dispara en el momento en que ambos intercambian sus lados: verdadero para un cruce al alza y falso para uno a la baja.
- Un NOT lógico convierte ese mismo cruce en el evento a la baja, de modo que un solo bloque Crossing sirve para las dos direcciones y ambas nunca pueden dispararse en la misma barra.
- Un conversor extrae el precio de cierre de la vela y dos comparaciones lo sitúan por encima o por debajo de la media móvil, que es el filtro de tendencia que ambas entradas deben superar.
- El bloque Position se compara con cero tres veces: la posición plana protege las entradas, y las condiciones de largo y de corto habilitan las dos salidas por señal.
- Cada puerta de entrada es un AND lógico de tres respuestas —el cruce, el lado de la media móvil y una posición plana— y la entrada en sí es una orden a mercado que solo se toma estando plano, así que una señal que llega mientras hay una operación abierta no cambia nada.
- El bloque Strategy P&L no tiene entradas ni ritmo de velas propio: emite el resultado no realizado en cada cambio, y dos comparaciones miden esa cifra frente al stop monetario y al objetivo monetario guardados en variables.
- El veredicto del dinero y los dos veredictos de cruce contrario llegan a un único bloque Combination, que acciona un solo Position modify configurado para cerrar: sea cual sea la razón que llegue primero, la posición se cierra con la misma orden a mercado.

## Reglas de entrada y salida

- **Entrada en largo**: El momentum cruza su nivel al alza, la vela cierra por encima de la media móvil y la posición está plana. Position modify compra a mercado el volumen de la orden.
- **Entrada en corto**: El momentum cruza su nivel a la baja —el mismo bloque Crossing leído a través del NOT lógico—, la vela cierra por debajo de la media móvil y la posición está plana. Position modify vende a mercado el volumen de la orden.
- **Salida**: Tres razones cierran una operación y las tres terminan en el mismo bloque. El resultado no realizado de la estrategia cae hasta el stop monetario; o alcanza el objetivo monetario; o el momentum vuelve a cruzar su nivel mientras la posición está abierta en la dirección contraria. Las dos primeras se comprueban en cada cambio del P&L y no al ritmo de las velas, de modo que un movimiento rápido recibe respuesta dentro de la barra; la tercera se comprueba una vez por cada vela terminada. La orden de cierre se dimensiona a partir de la posición actual, así que siempre deja la estrategia plana y nunca invierte la posición en un solo paso.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:05:00 | Marco temporal de la serie de velas sobre la que funciona todo el diagrama; solo se usan velas terminadas. |
| Momentum Length | 10 | Longitud del indicador de momentum: hasta dónde atrás se mide el movimiento del precio. |
| EMA Length | 20 | Longitud de la media móvil que decide en qué lado de la tendencia se permite una entrada. |
| Momentum Level | 0 | Nivel con el que se compara el momentum. Cero es su valor neutro: por encima, el precio es más alto que hace una longitud; por debajo, más bajo. |
| Order Volume | 1 | Tamaño de la orden, en lotes, para ambas entradas. La orden de cierre lo ignora y se dimensiona a partir de la posición. |
| Money Stop | -250 | Pérdida sobre la posición abierta, en la divisa de la cuenta, a la que se cierra la posición. Negativa. |
| Money Target | 500 | Beneficio sobre la posición abierta, en la divisa de la cuenta, al que se cierra la posición. |

## Detalles del diagrama

- Los dos indicadores están configurados para emitir solo valores formados y definitivos, de modo que una vela sin terminar no puede producir un cruce.
- El nivel con el que se compara el momentum vive en una variable y no dentro de la comparación, que es lo que lo convierte en un parámetro del esquema que puede cambiarse y optimizarse sin abrir el diagrama.
- Todas las variables constantes están disparadas —las tres que dependen de las velas, por la serie de velas; los dos umbrales monetarios, por el P&L no realizado— porque una variable conserva su valor pero solo lo emite cuando algo se lo pide.
- Las entradas llevan la condición de apertura de posición, por lo que solo se disparan estando planas. Sin ella se enviaría una orden a mercado en cada cambio de la posición y la ejecución se llenaría de operaciones no deseadas.
- El stop monetario y el objetivo monetario son importes en la divisa de la cuenta, no distancias en precio, así que hay que reescalarlos junto con el volumen de la orden cuando el diagrama se traslada a otro instrumento.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
