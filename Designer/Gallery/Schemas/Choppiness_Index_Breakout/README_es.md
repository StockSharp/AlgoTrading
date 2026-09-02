# Diagrama de la estrategia de ruptura con Choppiness Index
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El Choppiness Index no dice hacia dónde va el mercado, solo si va a alguna parte. El diagrama lo utiliza como interruptor: mientras el índice está bajo el mercado tiene tendencia y se abre posición del lado en que el cierre queda respecto de una media móvil simple; cuando el índice vuelve a la zona lateral, la posición se cierra valga lo que valga.

![schema](schema.svg)

## Resumen de la estrategia

- El Choppiness Index se calcula sobre catorce velas cerradas y se lee como porcentaje: valores bajos indican mercado direccional, valores altos un rango.
- La media móvil simple de veinte periodos aporta únicamente la dirección; no filtra por sí misma, porque el permiso para operar ya lo dio la prueba de régimen.
- Solo se entra estando plano, de modo que un tramo con tendencia produce una operación y no un montón creciente de ellas.
- No hay stop ni objetivo: el mismo índice que abrió la operación es el que la termina.

## Reglas de entrada y salida

- **Entrada en largo**: El Choppiness Index está por debajo del umbral de tendencia, la vela cerró por encima de la media móvil simple y la posición está plana. La orden compra un lote y abre un largo.
- **Entrada en corto**: El Choppiness Index está por debajo del umbral de tendencia, la vela cerró por debajo de la media móvil simple y la posición está plana. La orden vende un lote y abre un corto.
- **Salida**: En cuanto el Choppiness Index supera el umbral lateral, la posición abierta se cierra: el largo con una venta en modo cierre y el corto con una compra en modo cierre. El código original tampoco lleva stop loss ni take profit. Dos cosas se apartan a propósito de ese código. Sus umbrales son 99 y 99.5, lo que dejaría el filtro de entrada abierto para siempre y la condición de salida fuera de alcance, así que el diagrama usa los valores canónicos 38.2 y 61.8 de la documentación del indicador, que son además los que describe el propio README de la estrategia. Su pausa de quinientas barras entre operaciones también se omite, porque un contador así no tiene equivalente fiel en bloques.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| SMA Length | 20 | Periodo de suavizado de la media móvil simple que da la dirección a la entrada. |
| Choppiness Length | 14 | Periodo de suavizado del Choppiness Index. |
| Trending Threshold | 38.2 | Valor del índice por debajo del cual se permite entrar. |
| Choppy Threshold | 61.8 | Valor del índice por encima del cual el mercado se considera lateral y la posición se cierra. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama; el original usa velas de un minuto y este diagrama las de cinco minutos del histórico incluido. |

## Detalles del diagrama

- El bloque de velas alimenta el Choppiness Index, la media móvil y un convertidor que extrae el precio de cierre de la vela.
- Dos comparaciones convierten el índice en dos indicadores de régimen —tendencia por debajo de un umbral, lateral por encima del otro— y otras dos comparan el cierre con la media móvil.
- El bloque de posición se compara tres veces con una constante cero: da la guarda de plano para las entradas y las guardas de largo y de corto para las salidas.
- Cuatro Y lógicas alimentan cuatro bloques de modificación de posición: dos abren posición y toman el volumen de la constante compartida, dos solo cierran lo que ya existe.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
