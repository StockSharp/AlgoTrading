# Estrategia de Cuadrícula de Compra y Venta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia implementa un enfoque de cuadrícula simple que siempre mantiene abierta una posición larga y una corta. Cuando el mercado se mueve lo suficiente para alcanzar el take profit de un lado, el lado opuesto también se cierra y se abre el siguiente nivel de cuadrícula con un volumen mayor. El volumen crece geométricamente según el parámetro `VolumeMultiplier`.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `TakeProfitPoints` | Distancia del take profit medida en pasos de precio. |
| `InitialVolume` | Volumen utilizado para el primer par de órdenes. |
| `VolumeMultiplier` | Multiplicador aplicado al volumen para cada nuevo nivel de cuadrícula. |
| `MaxTrades` | Número máximo de niveles de cuadrícula permitidos. |
| `CandleType` | Tipo de datos de velas utilizado para activar la lógica de la estrategia. |

## Lógica de trading

1. **Inicio** – La estrategia se suscribe a la serie de velas especificada y abre el primer par de órdenes de mercado de compra y venta.
2. **Monitoreo** – En cada vela completada se comprueba el último precio frente a los precios de entrada. Si se alcanza el objetivo de beneficio en un lado, ambas posiciones se cierran.
3. **Progresión de la cuadrícula** – Tras cerrar todas las posiciones, se abre el siguiente nivel de cuadrícula con el volumen multiplicado por `VolumeMultiplier`.
4. **Límites** – El proceso se repite hasta que se abran `MaxTrades` niveles.

La estrategia no utiliza ningún indicador ni cálculos complejos, lo que la hace adecuada para demostrar la gestión de órdenes y posiciones dentro de StockSharp.

## Notas

- Todos los comentarios en el código están escritos en inglés según lo requerido.
- La estrategia utiliza la API de alto nivel con `SubscribeCandles` para los datos de mercado.
