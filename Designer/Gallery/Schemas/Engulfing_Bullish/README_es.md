# Diagrama de la estrategia de envolvente alcista con filtro SMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una vela envolvente indica que el bando que dominaba la barra anterior acaba de ser arrollado. Por sí solo esto ocurre demasiado a menudo, así que una media móvil simple decide dónde se toma la señal: la envolvente alcista solo se compra por debajo de la media y la bajista solo se vende por encima. Esa misma media es el objetivo donde se cierra la operación.

![schema](schema.svg)

## Resumen de la estrategia

- Dos bloques de indicador de patrones de vela llevan las figuras integradas Bullish Engulfing y Bearish Engulfing, así que la forma se reconoce sin escribir una fórmula.
- Una media móvil simple del precio de cierre parte el gráfico en una mitad barata y otra cara.
- El patrón solo se compra en la mitad barata y solo se vende en la cara, lo que convierte el diagrama en un ejemplo de reversión a la media y no de ruptura.
- El control de posición asegura que solo se actúa sobre un patrón cuando no hay posición abierta.

## Reglas de entrada y salida

- **Entrada en largo**: El bloque de patrón informa de una envolvente alcista, la vela cerró por debajo de la media móvil y no hay posición. La orden compra un lote y abre un largo.
- **Entrada en corto**: El bloque de patrón informa de una envolvente bajista, la vela cerró por encima de la media móvil y no hay posición. La orden vende un lote y abre un corto.
- **Salida**: El largo se cierra cuando una vela cierra por encima de la media móvil y el corto cuando cierra por debajo, ambos mediante bloques de modificación de posición en modo cierre. La estrategia original sale por el mismo lado de la media por el que entró y sostiene la operación con una pausa de varios cientos de barras; aquí no existe un bloque contador de barras, así que la salida es el regreso a la media, la regla más cercana que sigue operando con sentido.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| SMA Length | 20 | Periodo de la media móvil simple que filtra los patrones y cierra las operaciones. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta cuatro ramas: los dos indicadores de patrón, la media móvil y un conversor que lee el precio de cierre.
- Dos bloques de comparación sitúan el cierre a un lado u otro de la media; esas mismas dos señales sirven de filtro de entrada y de disparo de salida.
- El bloque de posición se compara con una constante cero y el resultado protege ambas entradas.
- Cada Y lógica une un patrón, un filtro y el control de posición, y dispara un bloque de modificación de posición; las dos órdenes de entrada toman el volumen de una constante compartida y los dos bloques de cierre no lo necesitan.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
