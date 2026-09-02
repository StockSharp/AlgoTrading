# Diagrama de la estrategia de retroceso con media móvil y estocástico
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dos bloques deciden juntos: la SimpleMovingAverage indica de qué lado del mercado puede ponerse el diagrama y la StochasticK espera un movimiento contrario a ese lado antes de enviar la orden. La posición se devuelve en cuanto el precio cierra al otro lado de la misma media.

![schema](schema.svg)

## Resumen de la estrategia

- La dirección la marca el cierre frente a la SimpleMovingAverage: por encima solo se consideran largos, por debajo solo cortos.
- La entrada es contraria: la línea %K debe estar en zona de sobreventa para comprar y en zona de sobrecompra para vender, de modo que el diagrama compra retrocesos dentro de una subida y vende rebotes dentro de una bajada.
- StochasticK es exactamente el %K que la estrategia original calculaba a mano: 100 * (Close - mínimo Low) / (máximo High - mínimo Low) sobre las últimas N velas.
- La misma media móvil sirve de línea de salida y en el diagrama no hay stop de pérdidas ni toma de beneficios.

## Reglas de entrada y salida

- **Entrada en largo**: El cierre está por encima de la SimpleMovingAverage, la StochasticK por debajo del nivel de sobreventa y la posición está plana. La orden compra un lote a mercado.
- **Entrada en corto**: El cierre está por debajo de la SimpleMovingAverage, la StochasticK por encima del nivel de sobrecompra y la posición está plana. La orden vende un lote a mercado.
- **Salida**: El largo se cierra en la primera vela que cierra por debajo de la media y el corto en la primera que cierra por encima; los dos bloques de cierre toman el volumen de la posición abierta.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| SMA Length | 20 | Periodo de suavizado de la SimpleMovingAverage que filtra la tendencia y cierra la posición. |
| %K Length | 14 | Número de velas que mira hacia atrás la línea %K. |
| %K Oversold | 20 | Nivel por debajo del cual %K se considera sobrevendido y se permite comprar. |
| %K Overbought | 80 | Nivel por encima del cual %K se considera sobrecomprado y se permite vender. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta tres ramas: el convertidor que lee el cierre, la SimpleMovingAverage y el indicador StochasticK.
- Dos comparaciones sitúan el cierre respecto a la media, otras dos sitúan %K frente a las constantes de umbral y una compara la posición con cero.
- Cada Y lógica une una condición de tendencia, una del estocástico y la comprobación de posición plana, y después dispara un bloque de modificación que solo abre desde plano.
- Las comparaciones de tendencia se reutilizan en la salida: la misma señal que permite el corto cierra el largo, lo que mantiene compacto el diagrama. El contador que detenía la estrategia original durante 100 velas tras cada operación no tiene bloque propio y se omite.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
