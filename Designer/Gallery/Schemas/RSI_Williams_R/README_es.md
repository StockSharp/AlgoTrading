# Diagrama de la estrategia de doble cruce RSI + Williams %R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Los dos osciladores tienen que coincidir en la misma vela. El diagrama compra solo cuando el RSI cae por debajo de 30 mientras el Williams %R cae por debajo de -80 a la vez, y vende solo cuando el RSI sube por encima de 70 mientras el Williams %R sube por encima de -20. No basta con estar dentro de la zona: en la vela anterior ambos debían seguir fuera de ella, y por eso cada oscilador se guarda también una vela atrás. El descanso de 180 barras del código original no se reproduce, porque en velas de cinco minutos silenciaría la estrategia durante quince horas después de cada operación.

![schema](schema.svg)

## Resumen de la estrategia

- El RSI 14 y el Williams %R 14 se calculan sobre las mismas velas de cinco minutos de un solo instrumento.
- Los bloques de valor anterior guardan ambos osciladores una vela atrás, de modo que una entrada reciente en la zona se distingue de un valor que lleva horas allí.
- Solo se entra desde posición plana, y la línea media del RSI en 50 es la que devuelve la posición a plano.

## Reglas de entrada y salida

- **Entrada en largo**: El RSI está por debajo del nivel de sobreventa y en la vela anterior estaba en él o por encima, y el Williams %R está por debajo de su nivel de sobreventa y en la vela anterior estaba en él o por encima; la posición es plana. Se compra un lote a mercado.
- **Entrada en corto**: El RSI está por encima del nivel de sobrecompra y en la vela anterior estaba en él o por debajo, y el Williams %R está por encima de su nivel de sobrecompra y en la vela anterior estaba en él o por debajo; la posición es plana. Se vende un lote a mercado.
- **Salida**: Un largo se cierra en cuanto el RSI vuelve por encima de la línea media de 50, y un corto en cuanto el RSI cae por debajo de ella; ambas salidas son bloques de cierre, así que cada una toca solo el lado realmente abierto.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| RSI Length | 14 | Periodo de suavizado del índice de fuerza relativa. |
| RSI Oversold | 30 | Nivel que el RSI debe atravesar a la baja para dar señal de compra. |
| RSI Overbought | 70 | Nivel que el RSI debe atravesar al alza para dar señal de venta. |
| Williams %R Length | 14 | Periodo de observación del Williams %R. |
| Williams %R Oversold | -80 | Nivel que el Williams %R debe atravesar a la baja para la compra; el indicador va de -100 a 0. |
| Williams %R Overbought | -20 | Nivel que el Williams %R debe atravesar al alza para la venta. |
| RSI Midline | 50 | Nivel neutro del RSI en el que se abandona la posición abierta. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- Cada oscilador alimenta un par de comparaciones, una con su valor actual y otra con el anterior, de modo que la ruptura de un nivel se describe sin bloque de cruce, que permitiría que las dos rupturas llegasen de velas distintas.
- Cada Y lógica reúne cinco señales: las dos comparaciones del RSI, las dos del Williams %R y la posición plana obtenida al comparar el bloque de posición con cero.
- Ambos bloques de entrada abren posición solo cuando no hay ninguna y toman el volumen de una constante compartida.
- Otras dos comparaciones vigilan el RSI frente a su línea media y accionan los bloques de cierre, la única salida del diagrama.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
