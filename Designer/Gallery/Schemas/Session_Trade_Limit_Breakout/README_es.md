# Diagrama de la estrategia de ruptura con límite de operaciones por sesión
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una ruptura solo merece la pena cuando el mercado ha estado tranquilo, solo dentro de las horas en las que el diagrama tiene permitido operar y solo una vez, hasta que se devuelve el derecho a operar. El bloque Flag es el que impone esta última parte: deja pasar la primera ruptura válida como un único impulso y después permanece cerrado, de modo que una serie de velas fuertes produce una sola entrada en lugar de una orden por barra.

![schema](schema.svg)

## Resumen de la estrategia

- Todo lo que viene después funciona sobre velas de treinta minutos ya cerradas, así que ninguna decisión se toma sobre una barra que todavía se está formando.
- Un bloque Highest sobre veinte velas marca el techo del rango reciente, y un bloque Previous value toma ese nivel de una vela atrás: el nivel queda fijado antes de la vela que tiene que superarlo.
- Un Average Directional Index de período catorce se reduce a su propia línea mediante un conversor, y una comparación limita las entradas a un mercado que sigue tranquilo: la línea del ADX por debajo de su límite.
- Working time responde, vela a vela, si el momento cae dentro de la ventana de negociación, y un NOT lógico de esa misma respuesta es lo que marca el final de la sesión.
- Un AND lógico reúne cuatro respuestas en una única condición: el cierre está por encima del nivel de ruptura, la línea del ADX está por debajo del límite, el momento está dentro de la ventana y la posición está plana.
- El bloque Flag convierte esa condición en una ficha. La primera respuesta verdadera se transmite como un único impulso y dispara una compra a mercado a través de Position modify con la condición de apertura de posición; toda respuesta posterior se descarta mientras la ficha está gastada.
- Dos cosas devuelven la ficha: un bloque de retardo que cuenta quince velas cerradas desde la ejecución de la entrada, y la primera vela que se forma fuera de la ventana de negociación.
- La salida está construida con las dos mismas lecturas que permitieron la entrada —un límite de ADX ampliado y un nivel algo por debajo del nivel de ruptura—, unidas por un OR lógico en un único disparador de cierre, con Position protection trabajando por debajo como take-profit y stop-loss fijos.

## Reglas de entrada y salida

- **Entrada en largo**: La vela cierra por encima del máximo más alto de las veinte velas anteriores, la línea del ADX está por debajo del límite de mercado tranquilo, la vela pertenece a la ventana de negociación y la posición está plana. Las cuatro condiciones llegan en la misma vela, el AND lógico las convierte en una única respuesta verdadera y el Flag pasa la primera de esas respuestas a Position modify, que compra el volumen de la orden a mercado.
- **Entrada en corto**: No hay entradas cortas. El diagrama es deliberadamente unidireccional: la ruptura que busca es al alza, y una caída de vuelta por debajo del nivel de devolución se lee como un motivo para abandonar un largo, no como un motivo para vender.
- **Salida**: Dos lecturas terminan la operación, y el OR lógico hace que baste con la que llegue primero. La primera es un mercado que dejó de estar tranquilo: una fórmula multiplica el límite de mercado tranquilo por el multiplicador de tendencia y una comparación comprueba si la línea del ADX ha alcanzado ese nivel ampliado. La segunda es una ruptura que se ha devuelto a sí misma: una segunda fórmula rebaja el nivel de ruptura mediante el factor de devolución y una comparación comprueba si el cierre ha caído por debajo de él. Cualquiera de las dos dispara un Position modify configurado para cerrar la posición. Por debajo de ambas, Position protection vigila la ejecución de la entrada y cierra la operación con un dos por ciento de beneficio o un uno por ciento de pérdida.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:30:00 | Marco temporal sobre el que trabaja cada bloque del diagrama; solo se procesan velas cerradas. |
| Breakout Length | 20 | Número de velas que el bloque Highest mira hacia atrás para construir el nivel de ruptura. |
| ADX Length | 14 | Período del Average Directional Index cuya línea se usa como filtro de mercado tranquilo. |
| Calm Market Limit | 25 | Valor por debajo del cual tiene que mantenerse la línea del ADX para que una ruptura cuente como salida de un mercado tranquilo. |
| Session From | 12:00:00 | Inicio de la ventana de negociación; una vela anterior a él no puede abrir una posición. |
| Session Until | 21:00:00 | Fin de la ventana de negociación; la primera vela posterior devuelve la ficha de entrada. |
| Cooldown Candles | 15 | Velas cerradas que tienen que pasar tras la ejecución de una entrada antes de que se devuelva la ficha de entrada. |
| Order Volume | 1 | Tamaño de la orden, en lotes. |
| Trend Multiplier | 1.5 | Multiplicador aplicado al límite de mercado tranquilo para obtener el nivel de ADX que cierra la operación. |
| Give-Back Factor | 0.98 | Fracción del nivel de ruptura por debajo de la cual tiene que caer el cierre para la salida por devolución. |
| Take Profit, % | 2 | Distancia del take-profit, en porcentaje del precio de entrada. |
| Stop Loss, % | 1 | Distancia del stop-loss, en porcentaje del precio de entrada. |

## Detalles del diagrama

- La comprobación de posición plana dentro de la condición de entrada tiene un peso real. Sin ella, una ruptura que llegara con la operación ya abierta gastaría la ficha en una orden que la condición de apertura de posición rechazaría, y el diagrama se quedaría después esperando todo el enfriamiento para nada.
- El Flag emite solo en el momento en que se activa, por lo que su salida va directamente al disparador de la orden y nunca al AND lógico: es un impulso, no un nivel, y por eso todas las condiciones se reúnen antes de él y no a su lado.
- El retardo que mide el enfriamiento se arma con la ejecución de la entrada y se alimenta de la serie de velas, de modo que los quince valores que cuenta son quince barras cerradas.
- El bloque Highest lee el máximo de cada vela, así que el nivel que se rompe es el máximo más alto de las últimas veinte barras, no el cierre más alto, y la ruptura resulta correspondientemente más estricta.
- El take-profit y el stop-loss están junto a las dos comparaciones de salida, no en lugar de ellas: son el recurso de reserva para una operación que deriva sin que se active ninguna de las dos lecturas.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
