# Diagrama de la estrategia de ruptura de Bandas de Bollinger con ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una ruptura solo merece la pena cuando el mercado va realmente a alguna parte. Este diagrama espera un cierre fuera de una banda de Bollinger, señal de que el movimiento es inusualmente grande para la volatilidad reciente, y pregunta al ADX si hay una tendencia detrás. Si ambos coinciden, se abre la posición en el sentido de la ruptura y se abandona en cuanto el precio vuelve a la banda central.

![schema](schema.svg)

## Resumen de la estrategia

- Las Bandas de Bollinger se calculan sobre velas cerradas de un solo instrumento: la superior y la inferior marcan los niveles de ruptura y la central, que es la media móvil del mismo periodo, marca la salida.
- El ADX mide la fuerza de la tendencia sin decir nada de su dirección, así que se usa solo como filtro: por debajo del umbral toda ruptura se ignora.
- La posición actual interviene en ambas entradas, y los dos bloques de cierre están en modo cierre en lugar de apertura, de modo que cada uno solo puede actuar sobre su lado.
- La estrategia de origen se bloquea cien barras tras cualquier operación, salidas incluidas. Ese contador no tiene equivalente entre los bloques, así que el diagrama lo omite: la salida en la banda central funciona siempre, que además es lo más sensato.

## Reglas de entrada y salida

- **Entrada en largo**: El cierre está por encima de la banda superior, el ADX supera su umbral y la posición está plana. Se compra un lote a mercado.
- **Entrada en corto**: El cierre está por debajo de la banda inferior, el ADX supera su umbral y la posición está plana. Se vende un lote a mercado.
- **Salida**: Un largo se cierra en el primer cierre por debajo de la banda central y un corto en el primero por encima. No hay stop ni objetivo, igual que en la estrategia de origen.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Bollinger Length | 20 | Periodo de suavizado de las Bandas de Bollinger y de su línea central. |
| Bollinger Width | 2.0 | Multiplicador de la desviación típica que fija la anchura de las bandas. |
| ADX Length | 14 | Periodo del Índice Direccional Medio (ADX). |
| ADX Threshold | 25 | Nivel por encima del cual se considera que el ADX es bastante fuerte para operar la ruptura. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta dos bloques de indicador y un convertidor del precio de cierre; otros tres convertidores extraen la banda superior, la inferior y la central de un mismo valor de Bollinger, y uno más extrae la línea del ADX.
- Cinco bloques de comparación hacen el trabajo: dos para la ruptura, dos para el regreso a la banda central y uno para el filtro de tendencia contra una constante de umbral.
- Cada Y lógica une una condición de ruptura, el filtro de tendencia y la comprobación de la posición, y dispara un bloque de modificación en modo apertura que toma su volumen de la constante compartida.
- Las dos comparaciones de salida accionan bloques de modificación en modo cierre, que no necesitan volumen propio porque el bloque cierra lo que haya abierto.
- El código original calcula la fuerza de la tendencia a mano como un DX sin suavizar. El diagrama emplea el ADX estándar, la versión suavizada por Wilder de esa misma cifra, así que los momentos en que se cruza el umbral difieren ligeramente.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
