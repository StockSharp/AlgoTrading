# Diagrama de la estrategia de reversión del RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El diagrama opera contra los extremos del RSI, pero solo en el momento en que el índice se da la vuelta: compra cuando el RSI vuelve a subir por encima del nivel de sobreventa y vende cuando cae por debajo del nivel de sobrecompra. Una sola orden lleva el volumen necesario para dar la vuelta a la posición, de modo que la estrategia está plana o en un único sentido.

![schema](schema.svg)

## Resumen de la estrategia

- El índice de fuerza relativa se calcula sobre velas cerradas y un bloque de valor anterior guarda la lectura de la vela previa, con lo que el par detecta la vela exacta en la que el índice regresa al rango normal.
- La media SimpleMovingAverage de 50 velas se conserva de la estrategia original: no elige dirección, solo retrasa la operativa hasta que está formada.
- La posición actual interviene en ambas decisiones y el volumen de la orden es el volumen base más la posición abierta, así que una sola orden a mercado cierra y gira en un paso.

## Reglas de entrada y salida

- **Entrada en largo**: La lectura anterior del RSI está por debajo del nivel de sobreventa, la actual está en ese nivel o por encima, la SMA 50 está formada y la posición no es larga. La orden compra el volumen base más el tamaño de un corto abierto, convirtiendo un corto en largo o abriendo un largo desde plano.
- **Entrada en corto**: La lectura anterior del RSI está por encima del nivel de sobrecompra, la actual está en ese nivel o por debajo, la SMA 50 está formada y la posición no es corta. La orden vende el volumen base más el tamaño de un largo abierto, convirtiendo un largo en corto o abriendo un corto desde plano.
- **Salida**: No hay bloque de salida propio: la señal de reversión contraria cierra la posición y abre el otro lado con la misma orden. La estrategia original tampoco tiene stop ni take profit, y su pausa de diez velas tras cada operación no se traslada, porque los bloques del diagrama no mantienen un nivel entre velas.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| RSI Length | 14 | Periodo de suavizado del índice de fuerza relativa. |
| SMA Length | 50 | Periodo de la media simple que controla el calentamiento. |
| Oversold | 30 | Nivel al que el índice debe volver por encima para comprar. |
| Overbought | 70 | Nivel al que el índice debe volver por debajo para vender. |
| Volume | 1 | Volumen base de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta ambos indicadores y el bloque de valor anterior sobre la salida del RSI aporta la lectura de la vela previa.
- Cada lado usa dos bloques de comparación que contrastan la lectura anterior y la actual con la constante de nivel, reproduciendo literalmente la condición del código fuente.
- La comparación de la SMA con cero equivale a la comprobación del código original; como el bloque de indicador solo emite valores formados, la operativa comienza tras cincuenta velas.
- Un bloque de fórmula suma el valor absoluto de la posición a la constante de volumen, y ambos bloques de modificación de posición envían órdenes a mercado con ese volumen.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
