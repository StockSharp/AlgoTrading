# Diagrama de interruptor por PnL flotante
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El diagrama envuelve una señal de retorno del CCI con una capa de emergencia monetaria. Las entradas aparecen como órdenes limitadas al cierre horario; cuando el resultado no realizado alcanza un límite, se cancelan las órdenes activas antes de cerrar la posición a mercado.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas horarias cerradas alimentan un CommodityChannelIndex de 30 periodos y su cierre fija el precio límite.
- El largo exige un CCI anterior inferior a −100 y uno actual de vuelta en −100 o más; el corto refleja el retorno desde encima de +100.
- La compra requiere Position <= 0 y la venta Position >= 0, de modo que una señal opuesta reduce la exposición existente.
- Cada ejecución reinicia un enfriamiento de cuatro velas; el contador limitado debe alcanzar de nuevo el umbral.
- Order registering publica la orden al cierre de la vela terminada sin ajustar el precio calculado.
- P&L change compara el resultado no realizado con +300 y −200; cualquier límite activa Mass order cancellation y ClosePosition.

## Reglas de entrada y salida

- **Entrada en largo**: El CCI anterior estaba bajo −100, el actual vuelve a −100 o más, Position no es largo y han pasado cuatro velas desde la última ejecución. Se publica una compra limitada al cierre.
- **Entrada en corto**: El CCI anterior estaba sobre +100, el actual vuelve a +100 o menos, Position no es corto y terminó el enfriamiento. Se publica una venta limitada al cierre.
- **Salida**: No hay objetivo ni stop por precio. Con beneficio no realizado de 300 o pérdida de −200, el interruptor solicita cancelar todas las órdenes activas y envía simultáneamente ClosePosition a mercado.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candle Time Frame | 01:00:00 | Marco temporal de las velas cerradas para CCI, enfriamiento y precios límite. |
| CCI Length | 30 | Número de valores horarios en CommodityChannelIndex. |
| CCI Level | 100 | Umbral absoluto de CCI usado como +Level y −Level. |
| Signal Cooldown, candles | 4 | Velas cerradas que deben pasar desde la última ejecución. |
| Order Volume | 1 | Cantidad de cada orden limitada de entrada. |
| Target Profit, money | 300 | Beneficio no realizado en moneda de cuenta que activa la liquidación. |
| Cut Loss, money | -200 | Límite de pérdida no realizada en moneda de cuenta, normalmente negativo. |

## Detalles del diagrama

- Previous value conserva el CCI anterior, por lo que la entrada es un retorno a través del umbral y no una condición repetida en la zona extrema.
- El enfriamiento comienza con una ejecución real, no con una señal o intento de registro, y queda limitado a cuatro.
- Las entradas limitadas al cierre muestran deliberadamente el ciclo de una orden pendiente y dan trabajo real a la cancelación masiva.
- Objetivo y pérdida son importes absolutos de PnL no realizado en moneda de cuenta, una capa de emergencia y no protección porcentual.
- Sin filtros Security o Portfolio conectados, la cancelación abarca todas las órdenes de la estrategia; Modify position cierra después el lado restante.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
