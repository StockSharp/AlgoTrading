# Estrategia Malr de Ruptura de Canal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera rupturas de un canal MALR (Moving Average Linear Regression) personalizado. El indicador MALR combina una media móvil simple y una media móvil lineal ponderada para formar una línea central. La desviación estándar del precio relativa a esta línea crea dos bandas exteriores.

Se abre una posición larga cuando la banda superior de ruptura cruza por debajo del precio de cierre, indicando una ruptura al alza. Se abre una posición corta cuando la banda inferior de ruptura cruza por encima del precio de cierre, señalando una ruptura a la baja.

## Parámetros

- `MaPeriod` – período para las medias móviles y la desviación estándar.
- `ChannelReversal` – anchura del canal MALR interior medida en desviaciones estándar.
- `ChannelBreakout` – anchura adicional para el canal exterior de ruptura.
- `CandleType` – tipo de velas utilizado para los cálculos.

## Cómo funciona

1. Calcular SMA y LWMA del precio de cierre.
2. Computar la línea MALR `FF = 3 * LWMA - 2 * SMA`.
3. Medir la desviación estándar de `close - FF` sobre el mismo período.
4. Derivar bandas de ruptura: `FF ± StdDev * (ChannelReversal + ChannelBreakout)`.
5. Entrar largo cuando la banda superior cruza de arriba a abajo el cierre.
6. Entrar corto cuando la banda inferior cruza de abajo a arriba el cierre.

La estrategia siempre cierra la posición opuesta antes de abrir una nueva.
