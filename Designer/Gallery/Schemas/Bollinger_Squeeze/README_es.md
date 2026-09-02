# Diagrama de la estrategia de ruptura Bollinger Squeeze
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un diagrama de ruptura sobre bandas de Bollinger: las bandas se trazan a 1,8 desviaciones típicas de una media de veinte periodos y un cierre fuera de ellas se interpreta como el comienzo de un movimiento, no como un exceso que convenga contrariar. El volumen de la orden siempre arrastra la posición abierta, de modo que cada señal da la vuelta al lado en lugar de aumentarlo.

![schema](schema.svg)

## Resumen de la estrategia

- Las bandas de Bollinger se calculan sobre velas cerradas de un solo instrumento y solo intervienen la banda superior y la inferior.
- Se trata de una ruptura y no de una reversión: compra la fuerza por encima de la banda superior y vende la debilidad por debajo de la inferior, al contrario que el ejemplo Bollinger_Bands de esta misma galería.
- El volumen de cada orden es el volumen base más el valor absoluto de la posición actual, así que una señal contraria a la posición abierta la cierra y abre el lado opuesto con una sola orden.
- Pese al nombre, no hay filtro de compresión: la estrategia original en C# calcula la anchura relativa de las bandas pero nunca la usa en ninguna condición, y el diagrama respeta lo que el código realmente hace.

## Reglas de entrada y salida

- **Entrada en largo**: La vela cierra por encima de la banda superior de Bollinger y la posición todavía no es larga. La orden compra el volumen base más el tamaño de la posición abierta: abre un largo desde plano o da la vuelta a un corto.
- **Entrada en corto**: La vela cierra por debajo de la banda inferior de Bollinger y la posición todavía no es corta. La orden vende el volumen base más el tamaño de la posición abierta: abre un corto desde plano o da la vuelta a un largo.
- **Salida**: No hay salida propia ni bloque de protección: solo se abandona una posición cuando el precio cierra más allá de la banda contraria y la orden de vuelta cambia de lado.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Bollinger Period | 20 | Número de velas sobre las que se promedian las bandas. |
| Bollinger Width | 1.8 | Multiplicador de la desviación típica que fija la distancia de las bandas respecto a la línea media. |
| Volume | 1 | Volumen base de la orden, en lotes; encima se suma el tamaño de la posición. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta el bloque de indicador con las bandas de Bollinger y un conversor que lee el precio de cierre de esa misma vela.
- Dos conversores tipados como valor de indicador extraen la banda superior y la inferior de la única salida del indicador.
- Dos bloques de comparación contrastan el cierre con las bandas, otros dos comparan la posición con una constante cero, y cada Y lógica une una condición de banda con una de posición.
- Un bloque de fórmula calcula el volumen base más la posición en valor absoluto y alimenta ambos bloques de modificación de posición, que es lo que convierte cada entrada en una vuelta.
- La pausa de diez velas que el código original mantiene tras cada entrada no se reproduce: los bloques disponibles no tienen contador de velas, así que solo las comprobaciones de posición contienen la frecuencia de operación.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
