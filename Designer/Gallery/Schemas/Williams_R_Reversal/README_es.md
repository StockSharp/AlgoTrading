# Diagrama de la estrategia de cruce de niveles del Williams %R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El Williams %R indica dónde queda el último cierre dentro del máximo y el mínimo de la ventana reciente, en una escala que va de -100 en el fondo a 0 en el techo. Este diagrama no opera el tiempo que el indicador pasa en una zona extrema, sino el instante en que sale de ella: la vuelta por encima de -80 compra y la vuelta por debajo de -20 vende.

![schema](schema.svg)

## Resumen de la estrategia

- El Williams %R se calcula sobre velas cerradas de un solo instrumento y equivale por completo a la fórmula de máximo y mínimo que la estrategia original programa a mano.
- Dos niveles dividen la escala: por debajo de -80 el mercado se considera sobrevendido y por encima de -20, sobrecomprado.
- Un bloque de valor anterior guarda la lectura de la vela precedente, así cada nivel se comprueba dos veces y solo la vela del cruce genera la señal.
- La posición actual participa en ambas decisiones, de modo que ninguna orden aumenta una posición ya abierta.

## Reglas de entrada y salida

- **Entrada en largo**: La lectura anterior del %R estaba por debajo del nivel inferior, la actual está en él o por encima y la posición no es larga. La orden compra un lote: abre un largo desde plano o devuelve un corto existente a cero.
- **Entrada en corto**: La lectura anterior del %R estaba por encima del nivel superior, la actual está en él o por debajo y la posición no es corta. La orden vende un lote: abre un corto desde plano o devuelve un largo existente a cero.
- **Salida**: No hay bloque de salida propio: el cruce contrario envía una orden a mercado del mismo volumen y deja la posición en cero igual que la estrategia original. Esta además se aparta durante cincuenta velas después de cada operación; aquí no existe un bloque contador de barras, así que el cruce de nivel asume esa función en solitario y el diagrama opera algo más a menudo que el código de origen.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Williams %R Length | 14 | Ventana del máximo y del mínimo sobre la que se mide el Williams %R. |
| Lower Level | -80 | Nivel que el indicador debe volver a superar al alza para dar una señal de compra. |
| Upper Level | -20 | Nivel que el indicador debe volver a perder a la baja para dar una señal de venta. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta el bloque de indicador Williams %R, cuya salida va a los bloques de comparación y al bloque de valor anterior.
- Cuatro bloques de comparación arman los dos cruces: la lectura previa contra un nivel y la lectura actual contra ese mismo nivel.
- El bloque de posición se compara dos veces con una constante cero, lo que da la protección «no largo» para la compra y «no corto» para la venta.
- Cada Y lógica une las dos mitades de un cruce con su protección de posición y dispara un bloque de modificación de posición; ambos toman el volumen de una constante compartida.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
