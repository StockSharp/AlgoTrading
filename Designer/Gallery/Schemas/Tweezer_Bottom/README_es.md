# Diagrama de la estrategia de pinzas en el suelo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Unas pinzas son dos velas contiguas que se dan la vuelta una contra otra en el mismo nivel: tras una vela bajista, una alcista se detiene casi en el mismo mínimo y el par marca un suelo. La imagen especular sobre los máximos marca un techo. Como dos mínimos casi nunca coinciden hasta el último tick, el diagrama mide la distancia entre ellos en porcentaje y los da por iguales mientras esa distancia no supere la tolerancia.

![schema](schema.svg)

## Resumen de la estrategia

- Un bloque de patrón de velas reconoce únicamente el cambio de color del par: vela bajista seguida de alcista para el suelo, alcista seguida de bajista para el techo.
- La igualdad de los extremos la mide aparte una fórmula, de modo que la tolerancia sigue siendo un parámetro optimizable del esquema y no queda congelada dentro del texto del patrón.
- La media móvil simple no interviene en la entrada; solo decide cuándo termina la operación.
- Cada entrada está protegida por la posición, así que unas pinzas son un intento de giro y nunca una forma de aumentar una operación en curso.

## Reglas de entrada y salida

- **Entrada en largo**: El bloque de patrón informa de una vela bajista seguida de una alcista, la distancia entre los dos mínimos no supera el porcentaje de tolerancia del mínimo anterior y la posición está plana. La orden compra el volumen compartido a mercado.
- **Entrada en corto**: El bloque de patrón informa de una vela alcista seguida de una bajista, la distancia entre los dos máximos no supera el porcentaje de tolerancia del máximo anterior y la posición está plana. La orden vende el volumen compartido a mercado.
- **Salida**: La primera vela que cierra por debajo de la media móvil simple cierra un largo, y la primera que cierra por encima cierra un corto; ambas salidas son bloques de modificación de posición en modo cierre y nunca abren nada. El original no tiene stop loss ni take profit, y este diagrama tampoco. Dos cosas del original no se pudieron expresar con los bloques disponibles: la pausa de quinientas barras después de cada operación, porque ningún bloque guarda un contador entre velas, y el marco de un minuto, escalado a las velas de cinco minutos del historial incluido.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Tolerance, % | 0.1 | Cuánto pueden separarse los dos extremos, en porcentaje del nivel de la vela anterior. |
| SMA Length | 20 | Periodo de suavizado de la media móvil simple que cierra las operaciones. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta los dos bloques de patrón, la media móvil y tres conversores que leen el mínimo, el máximo y el cierre.
- Dos bloques de valor anterior guardan el mínimo y el máximo de la vela previa, y dos fórmulas convierten cada par en la distancia porcentual entre los extremos.
- Dos comparaciones contrastan esas distancias con la constante de tolerancia compartida, y otra comparación contrasta la posición con cero.
- Cada Y lógica une el patrón, la coincidencia de los extremos y la comprobación de posición plana, y luego dispara un bloque de entrada que toma su volumen de la constante compartida.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
