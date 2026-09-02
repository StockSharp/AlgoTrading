# Diagrama de la estrategia de ruptura de N días
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El clásico de las tortugas reducido a su núcleo: dos indicadores, Highest y Lowest, guardan los extremos de las últimas N barras, y la vela que supera cualquiera de ellos se toma como el inicio de un movimiento. El diagrama está siempre en el mercado y gira con la ruptura contraria.

![schema](schema.svg)

## Resumen de la estrategia

- Highest lee el máximo de cada vela cerrada y Lowest el mínimo, de modo que juntos forman el canal de ruptura del periodo de observación.
- Ambas lecturas se desplazan una vela atrás, porque el valor actual ya incluye la vela que se está comprobando: sin ese desplazamiento el máximo, como mucho, igualaría el canal y nunca lo superaría.
- La posición actual filtra cada entrada y al volumen de la orden se le suma el valor absoluto de la posición, así una sola orden a mercado gira el lado.

## Reglas de entrada y salida

- **Entrada en largo**: El máximo de la vela supera el valor de Highest de la vela anterior y la posición no es larga. La orden compra el volumen base más el valor absoluto de la posición: gira un corto a largo o abre un largo desde plano.
- **Entrada en corto**: El mínimo de la vela cae por debajo del valor de Lowest de la vela anterior, la ruptura alcista no se ha disparado en la misma vela y la posición no es corta. La orden vende el volumen base más el valor absoluto de la posición.
- **Salida**: Sin stop, sin objetivo y sin salida propia: la posición vive hasta que la ruptura contraria la gira, igual que en el código original.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Lookback period | 20 | Número de barras con las que se construye el canal de ruptura; la misma longitud sirve para Highest y Lowest. |
| Volume | 1 | Volumen base de la orden, en lotes; al girar se añade el valor absoluto de la posición. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta ambos indicadores y, mediante dos conversores, el máximo y el mínimo de la vela actual.
- Dos bloques de valor anterior retrasan las lecturas de Highest y Lowest una vela, que es todo el truco de esta estrategia.
- Los bloques de comparación generan las dos banderas de ruptura y otros dos comparan la posición con cero; un NO lógico da prioridad a la ruptura alcista sobre la bajista, igual que la rama else-if del original.
- Un bloque de fórmula calcula el volumen de giro como volumen base más el valor absoluto de la posición y alimenta los dos bloques de modificación de posición.
- El original declara una media móvil y un porcentaje de stop que su propio código nunca usa, y toma por defecto un canal de 1500 barras de un minuto; el diagrama omite esos parámetros muertos y usa un canal de 20 barras de cinco minutos, tal como sugieren el README de la estrategia y su rango de optimización.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
