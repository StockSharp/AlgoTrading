# Diagrama de la estrategia Keltner RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un diagrama de reversión a la media construido en torno a la línea central de un canal de Keltner. El precio estirado por debajo de la EMA junto con un RSI débil se compra; el precio estirado por encima con un RSI fuerte se vende, y la operación se entrega cuando el precio vuelve a cruzar la media con el RSI pasado su punto medio. La estrategia original calcula las bandas del canal por ATR pero nunca las lee, así que este diagrama las omite y conserva solo lo que realmente decide una operación.

![schema](schema.svg)

## Resumen de la estrategia

- La ExponentialMovingAverage de 20 periodos es la línea central del canal de Keltner y la única referencia de precio de todo el diagrama.
- El RSI de 14 velas aporta la segunda opinión: una lectura por debajo de 45 confirma la caída que se compra y una por encima de 55 confirma el impulso que se vende.
- Ambas entradas exigen estar plano y ambas salidas son bloques de cierre, de modo que las cuatro ramas nunca se disputan la misma posición.
- Dos simplificaciones frente al original: se descartan las bandas ATR no utilizadas y la pausa de 120 barras posterior a cada ejecución no tiene bloque contador, así que este diagrama opera más a menudo.

## Reglas de entrada y salida

- **Entrada en largo**: El cierre está por debajo de la EMA, el RSI está por debajo del nivel de entrada larga y la posición es plana. La orden compra el volumen compartido a mercado y abre el largo.
- **Entrada en corto**: El cierre está por encima de la EMA, el RSI está por encima del nivel de entrada corta y la posición es plana. La orden vende el volumen compartido a mercado y abre el corto.
- **Salida**: El largo se cierra cuando el cierre vuelve por encima de la EMA y el RSI supera su punto medio; el corto se cierra cuando el cierre vuelve por debajo de la EMA y el RSI queda por debajo del punto medio. No hay stop ni objetivo, igual que en el código original, donde el porcentaje de stop declarado nunca se aplica.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| EMA Length | 20 | Periodo de la ExponentialMovingAverage que actúa como línea central del canal. |
| RSI Length | 14 | Periodo de suavizado del RelativeStrengthIndex. |
| RSI Long Entry | 45 | El RSI debe estar por debajo de este nivel para entrar en largo. |
| RSI Short Entry | 55 | El RSI debe estar por encima de este nivel para entrar en corto. |
| RSI Exit Level | 50 | Punto medio que el RSI debe superar para cerrar una posición. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta la EMA, el RSI y un conversor que toma el precio de cierre.
- Dos bloques de comparación enfrentan el cierre con la EMA y otros cuatro comprueban el RSI contra sus tres niveles; el bloque de posición se compara con una constante cero.
- Dos Y lógicas forman las entradas con una condición de precio, una de RSI y la comprobación de posición plana, y accionan bloques de modificación en modo apertura.
- Otras dos Y lógicas forman las salidas y accionan bloques de modificación en modo cierre, que no necesitan volumen y solo actúan sobre el lado que pueden cerrar.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
