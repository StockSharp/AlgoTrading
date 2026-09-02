# Diagrama de la estrategia de cruce del cero con el CMO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El Chande Momentum Oscillator oscila entre -100 y +100 y cambia de signo justo cuando la presión compradora y la vendedora intercambian sus papeles. Este diagrama opera ese cambio de signo, pero solo cuando la nueva lectura ya está suficientemente lejos del cero como para merecer una orden, de modo que el vaivén plano alrededor de la línea cero se ignora.

![schema](schema.svg)

## Resumen de la estrategia

- El Chande Momentum Oscillator se calcula sobre velas horarias cerradas de un solo instrumento.
- El cruce se lee a partir de dos valores, el oscilador de la vela anterior y el actual, en lugar de un bloque de cruce, lo que hace explícita la dirección del movimiento en el dibujo.
- Un filtro de fuerza exige que la nueva lectura se aleje del cero al menos una distancia mínima, lo que descarta los cruces superficiales que ocurren cuando el mercado no va a ninguna parte.
- La posición interviene en cada decisión y además fija el tamaño de la orden, así que una señal contraria a una operación abierta la da la vuelta con una sola orden a mercado.

## Reglas de entrada y salida

- **Entrada en largo**: El oscilador estaba por debajo de cero en la vela anterior y ahora se sitúa en el nivel positivo mínimo o por encima, y la posición no es larga. La orden compra el volumen compartido más el tamaño del corto abierto, de forma que una sola orden a mercado cierra el corto y abre el largo.
- **Entrada en corto**: El oscilador estaba en cero o por encima en la vela anterior y ahora se sitúa en el nivel negativo mínimo o por debajo, y la posición no es corta. La orden vende el volumen compartido más el tamaño del largo abierto.
- **Salida**: No hay bloque de salida propio: la posición se abandona por el cruce contrario del cero, que la invierte, o por el bloque de protección. El original emplea un take profit absoluto de 2000 y un stop loss de 1000 pasos de precio; unos niveles absolutos calibrados para otro instrumento nunca se alcanzarían en este historial, así que aquí se escriben como un objetivo del dos por ciento y un stop del uno por ciento, manteniendo la proporción de dos a uno. El original también hace una pausa de cuatro velas tras cada cambio de posición; no existe ningún bloque que guarde un contador de barras entre velas, por lo que la pausa se elimina y el control de la posición basta para impedir una segunda entrada en el mismo sentido.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| CMO Length | 14 | Periodo de suavizado del Chande Momentum Oscillator. |
| Min |CMO| | 5 | Distancia mínima al cero que debe alcanzar el oscilador para que el cruce cuente. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Take profit, % | 2 | Distancia del take profit respecto al precio de entrada, en porcentaje. |
| Stop loss, % | 1 | Distancia del stop loss respecto al precio de entrada, en porcentaje. |
| Candles | 01:00:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta el bloque de indicador con el Chande Momentum Oscillator y un conversor que toma el precio de cierre para el bloque de protección.
- Un bloque de valor anterior guarda el oscilador de la vela previa y dos bloques de comparación deciden en qué lado del cero estaba.
- La constante de fuerza entra directamente en la comparación del largo y, mediante una pequeña fórmula que la niega, en la del corto, de modo que un único parámetro gobierna ambos lados.
- Cada Y lógica une el lado anterior, el filtro de fuerza y el control de la posición y dispara un bloque de modificación de posición cuyo volumen procede de la fórmula que suma la posición absoluta al volumen compartido.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
