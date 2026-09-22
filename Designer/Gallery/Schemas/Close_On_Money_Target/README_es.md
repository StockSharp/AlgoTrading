# Diagrama de cierre por objetivo monetario
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El diagrama añade una salida monetaria de emergencia a la dirección SMA(10)/SMA(30). Las entradas son límites pendientes para que, al alcanzar un beneficio o pérdida no realizada, Mass order cancellation tenga órdenes reales que retirar antes de ClosePosition.

![schema](schema.svg)

## Resumen de la estrategia

- Velas terminadas de cinco minutos alimentan las SMA; Greater y Less evalúan su estado en cada vela, no un evento de cruce.
- Estado alcista con Position <= 0 coloca compra al cierre; estado bajista con Position >= 0 coloca venta.
- El volumen es abs(Position) más volumen base, conservando la reversión con una orden neta.
- P&L change compara el resultado no realizado con +300 y -150 en moneda de cuenta.
- Cualquier límite activa simultáneamente Mass order cancellation y ClosePosition a mercado.

## Reglas de entrada y salida

- **Entrada en largo**: SMA rápida sobre SMA lenta y Position plana o corta: una compra limitada al cierre cubre el corto y deja una unidad base larga.
- **Entrada en corto**: SMA rápida bajo SMA lenta y Position plana o larga: una venta limitada al cierre cubre el largo y deja una unidad base corta.
- **Salida**: PnLUnreal >= 300 o <= -150 cancela todas las órdenes activas y cierra Position a mercado. Al volver P&L a cero, la estrategia puede operar de nuevo.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candle Time Frame | 00:05:00 | Intervalo terminado usado por ambas medias y decisiones de entrada. |
| Fast SMA Length | 10 | Cantidad de valores en la SimpleMovingAverage rápida. |
| Slow SMA Length | 30 | Cantidad de valores en la SimpleMovingAverage lenta. |
| Base Volume | 1 | Tamaño de posición después de entrada plana o reversión neta. |
| Profit Target, money | 300 | Beneficio no realizado en moneda de cuenta que activa la liquidación. |
| Loss Limit, money | -150 | Límite de pérdida no realizada en moneda de cuenta; debe ser negativo. |

## Detalles del diagrama

- RequestCloseAll nunca se llama en C#; la ruta activa solo opera el estado SMA a mercado. El diagrama implementa el cierre monetario anunciado.
- Los parámetros fuente son niveles de equity y valen cero por defecto; aquí se usa PnLUnreal con +300 y -150.
- Los límites al cierre sustituyen entradas a mercado para dar trabajo a Mass order cancellation; se desactiva el ajuste por paso para el replay.
- El código llamaría Stop después de liquidar. El diagrama sigue activo para mostrar ciclos repetidos durante el mes.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
