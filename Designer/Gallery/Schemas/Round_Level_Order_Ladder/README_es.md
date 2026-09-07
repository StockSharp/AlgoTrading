# Diagrama de estrategia de escalera de órdenes en niveles redondos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El diagrama gestiona todo el ciclo de vida de entradas pasivas en niveles redondos. Las velas terminadas de cinco minutos controlan una media adaptativa de Kaufman, los cruces de dirección, los niveles calculados de compra y venta, la sustitución de órdenes, la cancelación temporizada y un stop móvil de distancia absoluta.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas terminadas de cinco minutos alimentan KAMA(15), con periodo rápido 2 y periodo lento 30; solo los valores formados del indicador continúan por la ruta de señales.
- Un bloque Previous value desplaza la KAMA formada una actualización, y Crossing compara cada cierre con ese valor anterior de la media adaptativa.
- El centro redondeado más cercano se calcula con floor(close / step + 0.5). El límite de compra está un paso de 200 unidades por debajo y el de venta un paso por encima.
- Los buses separados de compra y venta conservan la orden activa. Cuando cambia su nivel calculado, Order replacing desplaza el límite activo al nuevo nivel.
- Cada lado tiene su propio indicador de ciclo y temporizador de doce velas. Un cruce contrario o el temporizador cancela un límite aún activo, mientras una ejecución activa la protección móvil.

## Reglas de entrada y salida

- **Entrada en largo**: Cuando el cierre cruza al alza la KAMA formada anterior, la posición muestreada es cero y el indicador del ciclo de compra está libre, registrar una compra limitada de 0.1 unidades en (floor(close / step + 0.5) - 1) × step.
- **Entrada en corto**: Cuando el cierre cruza a la baja la KAMA formada anterior, la posición muestreada es cero y el indicador del ciclo de venta está libre, registrar una venta limitada de 0.1 unidades en (floor(close / step + 0.5) + 1) × step.
- **Salida**: Una entrada ejecutada activa un stop móvil de distancia absoluta 10, evaluado con cierres de velas terminadas y ejecutado mediante una orden de mercado. El take-profit está desactivado. Las entradas pendientes se cancelan con el cruce contrario o tras doce velas terminadas.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles Series | 00:05:00 | Marco temporal de las velas terminadas usadas para señales, precios, temporizadores, actualización de la protección y gráfico. |
| KAMA Fast SC Period | 2 | Periodo de suavizado rápido de la media móvil adaptativa de Kaufman. |
| KAMA Slow SC Period | 30 | Periodo de suavizado lento de la media móvil adaptativa de Kaufman. |
| KAMA Length | 15 | Longitud de cálculo de la media móvil adaptativa de Kaufman. |
| KAMA Source | Not set | No se selecciona otro campo de entrada del indicador; las velas se conectan directamente. |
| Round Level Step | 200 | Distancia entre niveles redondos adyacentes, expresada en unidades de precio. |
| Order Volume | 0.1 | Volumen fijo de cada límite de entrada registrado y sustituido. |
| Buy Order Life (N) | 12 | Cantidad de velas terminadas del ciclo de compra antes de su cancelación temporizada y el reinicio del indicador. |
| Sell Order Life (N) | 12 | Cantidad de velas terminadas del ciclo de venta antes de su cancelación temporizada y el reinicio del indicador. |
| Take Profit | 0 | El valor absoluto cero desactiva la protección por beneficio. |
| Stop Loss | 10 | Distancia absoluta del stop móvil respecto al mejor precio protegido observado. |
| Trailing Stop Loss | true | Desplaza el límite del stop cuando el precio avanza a favor de la posición. |
| Use Market Orders | true | Envía la salida activada por el stop móvil como orden de mercado. |

## Detalles del diagrama

- El cierre de la vela llega a Crossing antes de desplazar el nuevo valor formado de KAMA. Así, Crossing recibe el cierre actual y el valor de la media adaptativa de la actualización formada anterior.
- Las dos fórmulas de nivel comparten el mismo cierre y paso. Los bloques Previous value conservan los niveles previos de compra y venta, y las comparaciones NotEqual emiten un pulso de sustitución solo cuando cambia un nivel.
- Cada bloque Combination recibe la orden emitida por el registro y todas las órdenes emitidas por las sustituciones. Su salida entrega la orden más reciente tanto a Order replacing como a Order cancellation.
- Los bloques N values se arman tras un registro correcto y cuentan doce velas terminadas. Sus salidas solicitan la cancelación y liberan el indicador de ciclo correspondiente para una configuración posterior.
- El gráfico muestra velas de cinco minutos, KAMA, ambos niveles redondos, los límites actuales de compra y venta, las órdenes del stop móvil y todas las ejecuciones.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
