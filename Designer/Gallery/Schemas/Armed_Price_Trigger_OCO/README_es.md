# Diagrama de estrategia OCO con disparadores de precio armados
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama mantiene dos disparadores virtuales de ruptura alrededor de un canal de Donchian calculado con velas de cinco minutos finalizadas. El mejor precio de venta y el mejor precio de compra del libro en vivo se comparan con los últimos límites del canal; el primer lado válido abre una posición a mercado, que después se gestiona con protección porcentual de beneficio y pérdida.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas de cinco minutos finalizadas alimentan Donchian Channels(20), que genera los límites superior e inferior de ruptura.
- Cada actualización de profundidad de mercado toma los últimos valores formados del canal y lee `BestAsk.Price` y `BestBid.Price` del libro de órdenes.
- Una ruptura superior puede comprar y una ruptura inferior puede vender solo mientras `Armed` esté activado y la posición sea cero.
- Un bloque Flag deja pasar el primer pulso válido de cada lado y suprime repeticiones hasta que un cambio de posición restablece ambas puertas de un solo uso.
- La protección de posición cierra la entrada ejecutada con un beneficio del 1% o una pérdida del 0,6%, y el gráfico muestra velas, ambos límites del canal y todas las ejecuciones.

## Reglas de entrada y salida

- **Entrada en largo**: Cuando `Armed` es true, la posición es cero y el mejor ask es mayor o igual que el último límite superior de Donchian, se envía una compra a mercado por `Volume`.
- **Entrada en corto**: Cuando `Armed` es true, la posición es cero y el mejor bid es menor o igual que el último límite inferior de Donchian, se envía una venta a mercado por `Volume`.
- **Salida**: La posición abierta se cierra cuando el libro en vivo alcanza el objetivo de beneficio del 1% o el umbral de pérdida del 0,6%, medidos desde la ejecución de entrada.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Channel Length | 20 | Número de velas finalizadas que utiliza Donchian Channels. |
| Armed | true | Interruptor general que habilita las dos rutas de entrada por ruptura. |
| Take Profit, % | 1 | Distancia porcentual desde la ejecución de entrada hasta el objetivo de beneficio. |
| Stop Loss, % | 0.6 | Distancia porcentual desde la ejecución de entrada hasta el umbral de pérdida. |
| Volume | 1 | Volumen de la orden a mercado para cualquiera de los dos sentidos de entrada. |
| Candles | 00:05:00 | Marco temporal de las velas finalizadas utilizadas para calcular el canal. |

## Detalles del diagrama

- Los convertidores UpperBand y LowerBand extraen ambos límites de Donchian. Dos bloques Variable los conservan y emiten sus últimos valores dentro del contexto causal de cada actualización de profundidad.
- Antes de que Donchian Channels(20) esté formado, las variables de los límites no contienen ningún valor y las comparaciones de entrada permanecen inactivas.
- Las rutas `BestAsk.Price` y `BestBid.Price` de los convertidores del libro proporcionan los precios en vivo utilizados por las dos comparaciones.
- Cada puerta de entrada combina tres valores booleanos: la comparación de precio correspondiente, `position = 0` y el parámetro expuesto `Armed`.
- Los dos bloques Flag implementan un comportamiento OCO virtual de un solo uso sin dejar órdenes pendientes en el mercado. Tras ejecutarse un lado, la posición distinta de cero bloquea ambas entradas hasta que la protección la cierra.
- Ambos bloques Modify position utilizan órdenes a mercado. Sus ejecuciones de entrada alimentan directamente Position protection y la profundidad de mercado aporta los precios con los que se evalúan sus niveles de salida.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
