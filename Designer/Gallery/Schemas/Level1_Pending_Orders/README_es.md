# Diagrama de órdenes pendientes por retorno del CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El diagrama opera el regreso del CCI desde zonas extremas mediante órdenes limitadas de corta vida. El precio ejecutable procede del cierre terminado y no del bid o ask de Level 1, por lo que el ciclo es reproducible sin esos campos.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas horarias alimentan CCI(30); Previous value distingue el retorno sobre −100 o bajo +100 de la permanencia en la zona extrema.
- Dirección de Position, enfriamiento de cuatro velas tras una ejecución y un bloqueo global de orden pendiente filtran ambos lados.
- Order registering publica una sola orden limitada al cierre sin ajustar el precio; solo puede existir una pendiente.
- La Order registrada arma N values y las velas cuentan; después de cuatro, Order cancellation elimina una orden no ejecutada.

## Reglas de entrada y salida

- **Entrada en largo**: CCI anterior <= −100, CCI actual > −100, Position no largo, enfriamiento cumplido y ninguna orden pendiente: se publica una compra limitada al cierre.
- **Entrada en corto**: CCI anterior >= +100, CCI actual < +100, Position no corto, enfriamiento cumplido y ninguna orden pendiente: se publica una venta limitada al cierre.
- **Salida**: No hay stop ni objetivo fijo. Un retorno opuesto del CCI puede enviar una orden contraria que lleve Position hacia cero. Una orden sin ejecutar se cancela al vencer.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candle Time Frame | 01:00:00 | Marco temporal para CCI, enfriamiento, precio y vida de la orden. |
| CCI Length | 30 | Número de valores de CommodityChannelIndex. |
| CCI Level | 100 | Frontera simétrica de sobrecompra y sobreventa como +Level y −Level. |
| Signal Cooldown, candles | 4 | Velas cerradas requeridas tras la última ejecución. |
| Order Volume | 1 | Cantidad de cada orden limitada pendiente. |
| Pending Lifetime, candles | 4 | Máximo de velas cerradas que puede vivir una orden no ejecutada. |

## Detalles del diagrama

- La carpeta conserva el nombre de su posición, pero no conecta Level 1: close es el precio pendiente explícito y reproducible.
- El estado pendiente lo activa una Order registrada y lo limpia Finished, incluyendo ejecución, cancelación y fallo.
- Enfriamiento y vida pendiente son parámetros distintos: uno mide desde una ejecución y el otro limita una orden sin ejecutar.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
