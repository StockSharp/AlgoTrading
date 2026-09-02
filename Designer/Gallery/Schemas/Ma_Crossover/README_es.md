# Diagrama de la estrategia de cruce de medias móviles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El diagrama de tendencia más antiguo que existe: una media móvil exponencial rápida frente a una lenta, con la posición invertida cada vez que se cruzan. Un bloque de protección aporta lo que el cruce por sí solo no da: un stop porcentual que cierra la posición cuando el movimiento va en contra.

![schema](schema.svg)

## Resumen de la estrategia

- Dos medias móviles exponenciales, una rápida y otra lenta, se calculan sobre velas cerradas de un solo instrumento.
- El bloque de cruce dispara solo en la vela en la que la media rápida cruza realmente a la lenta, y la dirección del cruce distingue el largo del corto.
- El bloque de protección de la posición vigila el cierre de cada vela terminada y cierra la posición en cuanto el precio se aleja un porcentaje dado del precio de entrada.

## Reglas de entrada y salida

- **Entrada en largo**: La EMA rápida cruza por encima de la lenta y la posición todavía no es larga. La orden compra Volume más el valor absoluto de la posición actual: abre un largo desde plano o convierte un corto directamente en largo.
- **Entrada en corto**: La EMA rápida cruza por debajo de la lenta y la posición todavía no es corta. La orden vende Volume más el valor absoluto de la posición actual: abre un corto desde plano o convierte un largo directamente en corto.
- **Salida**: O bien el cruce contrario invierte la posición con una sola orden, o bien el stop de protección la cierra cuando el cierre de la vela empeora el precio medio de entrada en el porcentaje indicado.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Fast EMA length | 20 | Periodo de la media móvil exponencial rápida. |
| Slow EMA length | 80 | Periodo de la media móvil exponencial lenta. |
| Stop loss, % | 2 | Distancia del stop de protección respecto al precio de entrada, en porcentaje. |
| Volume | 1 | Volumen base de la orden, en lotes; en una inversión se le suma la posición abierta. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta ambos bloques de indicador y sus salidas se encuentran en el bloque de cruce.
- La salida del cruce es la señal larga, un NO lógico sobre ella es la corta, y cada una se une mediante una Y lógica a la comparación de la posición con cero.
- Un bloque de fórmula calcula Volume más la posición en valor absoluto y alimenta la entrada de volumen de ambos bloques de modificación, de modo que una orden a mercado invierte la posición.
- Ambos bloques de modificación envían sus propias operaciones al bloque de protección y un conversor lleva el precio de cierre de cada vela terminada a su entrada de precio, así el stop se comprueba en los cierres.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
