# Diagrama de la estrategia de cruce del TSI con su línea de señal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El True Strength Index es momento suavizado dos veces, así que gira tarde pero rara vez miente. Leído frente a su propia línea de señal exponencial se comporta como un MACD lento: el cruce indica la dirección y la distancia entre las líneas dice cuán convincente es el giro. Este diagrama solo toma los cruces en los que esa distancia ya supera un mínimo, que es lo que separa un cambio real de control de dos líneas que apenas se rozan.

![schema](schema.svg)

## Resumen de la estrategia

- Un único bloque de True Strength Index contiene las dos líneas; dos conversores extraen del mismo valor la línea del índice y su línea de señal.
- Un bloque de cruce compara ambas líneas e informa de la dirección del cruce; un NO lógico convierte esa misma salida en el cruce a la baja.
- Una fórmula mide la separación absoluta entre las líneas y una comparación exige que alcance al menos la separación mínima antes de aceptar el cruce.
- El control de la posición decide si se permite la entrada, y el volumen de la orden es el volumen compartido más la posición absoluta, de modo que una señal contraria da la vuelta con una sola orden.

## Reglas de entrada y salida

- **Entrada en largo**: La línea del TSI cruza al alza su línea de señal, la separación entre ambas alcanza al menos el mínimo y la posición no es larga. La orden compra el volumen compartido más el tamaño del corto abierto, de forma que una sola orden a mercado cierra el corto y abre el largo.
- **Entrada en corto**: La línea del TSI cruza a la baja su línea de señal, la separación entre ambas alcanza al menos el mínimo y la posición no es corta. La orden vende el volumen compartido más el tamaño del largo abierto.
- **Salida**: No hay regla de salida propia ni stop de protección, igual que en el original: la posición se mantiene hasta que el cruce contrario la invierte. Se simplifican dos cosas. El original espera diez velas después de cada entrada antes de volver a mirar las señales, y ningún bloque guarda un contador de barras entre velas, así que esa pausa se elimina; el control de la posición sigue impidiendo una segunda entrada en el mismo sentido. El original también lanza dos órdenes a mercado al invertir, lo que duplica el tamaño por un instante; aquí la fórmula de volumen hace lo mismo con una sola orden.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| TSI First Length | 25 | Primer periodo de suavizado del True Strength Index. |
| TSI Second Length | 13 | Segundo periodo de suavizado del True Strength Index. |
| TSI Signal Length | 7 | Periodo de la línea de señal exponencial trazada sobre el índice. |
| Min spread | 2 | Separación absoluta mínima entre el índice y su línea de señal para que el cruce cuente. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 01:00:00 | Marco temporal de las velas con el que trabaja todo el diagrama. El original opera con velas de cuatro horas; en un mes de historial quedan demasiadas pocas barras cerradas para que un índice doblemente suavizado se forme y además opere, por lo que el diagrama se reduce a velas horarias. |

## Detalles del diagrama

- El bloque de velas alimenta el bloque del True Strength Index, cuyo valor complejo dividen dos conversores en el índice y su línea de señal.
- El bloque de cruce recibe el índice en la entrada superior y la línea de señal en la inferior, así que su salida es verdadera en el cruce al alza y falsa en el cruce a la baja.
- La fórmula de la separación y su comparación se calculan en cada vela, mientras que el bloque de cruce solo habla en los cruces, por lo que cada Y lógica se dispara justo en la barra donde ocurre un cruce filtrado.
- Ambos bloques de modificación de posición toman su volumen de una única fórmula que suma la posición absoluta a la constante de volumen compartida.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
