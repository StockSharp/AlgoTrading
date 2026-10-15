# Diagrama de estrategia de orden pendiente por pin bar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama, solo largo, busca una sombra inferior profunda dentro de un abanico ascendente de medias. En vez de comprar al cierre de la señal, coloca una orden limitada dentro de la sombra, cancela la orden no ejecutada tras varias velas cerradas, protege la ejecución con salidas porcentuales y también cierra cuando la EMA rápida cae bajo la EMA media.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas cerradas de treinta minutos alimentan conversores de apertura, máximo, mínimo y cierre, además de EMA 6, EMA 18 y SMA 50 ya formadas.
- Formula calcula (min(open, close) - low) / (high - low). La sombra inferior debe superar 0,45 del rango total.
- El filtro exige EMA 6 > EMA 18 > SMA 50. El mínimo debe perforar EMA 6 y el cierre terminar de nuevo por encima.
- La puerta de entrada reúne el patrón, Position plana y una pausa de seis velas desde la última ejecución de la estrategia.
- Order registering coloca una compra limitada de Order Volume en low * (1 + 0,25 / 100), sin redondear el precio.
- N values cuenta seis velas cerradas desde el registro y activa Order cancellation si sigue vigente; Trades for order entrega las ejecuciones a la protección.
- Position protection coloca take profit de 1,4% y stop loss de 0,7%; EMA 6 bajo EMA 18 activa además un ClosePosition a mercado.

## Reglas de entrada y salida

- **Entrada en largo**: La vela cumple cuando su sombra inferior supera 0,45, EMA 6 > EMA 18 > SMA 50, el mínimo queda bajo EMA 6, el cierre vuelve sobre EMA 6, Position está plana y han pasado seis velas desde la última ejecución. Se registra una compra limitada un 0,25% sobre el mínimo; solo hay entrada si el precio posterior la ejecuta.
- **Entrada en corto**: No existe entrada corta. Un abanico debilitado es una condición de salida, no una señal para abrir cortos.
- **Salida**: Una limitada no ejecutada se cancela tras seis velas. Un largo ejecutado sale mediante Position protection a +1,4% o -0,7%, o mediante ClosePosition a mercado cuando EMA 6 cae bajo EMA 18. La ejecución de esta salida vuelve a la protección para limpiar su estado.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:30:00 | Marco temporal de las velas cerradas para patrón, indicadores, vida, pausa y salidas. |
| Fast EMA Length | 6 | Longitud de la ExponentialMovingAverage rápida que la sombra perfora y el cierre recupera. |
| Medium EMA Length | 18 | Longitud de la ExponentialMovingAverage media del abanico. |
| Slow SMA Length | 50 | Longitud de la SimpleMovingAverage lenta en la base del abanico. |
| Wick Share | 0.45 | Participación mínima de la sombra inferior en el rango completo. |
| Entry Offset, % | 0.25 | Porcentaje sobre el mínimo de señal usado para la compra limitada. |
| Order Volume | 1 | Cantidad de cada compra pendiente. |
| Order Life, candles | 6 | Velas cerradas antes de cancelar una limitada no ejecutada. |
| Take Profit, % | 1.4 | Distancia favorable desde la ejecución, en porcentaje. |
| Stop Loss, % | 0.7 | Distancia adversa desde la ejecución, en porcentaje. |
| Cooldown, candles | 6 | Velas cerradas mínimas desde la última ejecución antes de permitir otra entrada. |

## Detalles del diagrama

- Precios, indicadores, estados y contadores usan una sola serie cerrada de treinta minutos, manteniendo las órdenes en el reloj de negociación.
- La puerta AND combina sombra, dos comparaciones del abanico, perforación y recuperación de EMA rápida, Position plana y pausa lista.
- Order registration entrega la orden a N values, Order cancellation, Trades for order y el gráfico; el contador arranca con el registro y avanza con velas cerradas.
- Strategy trades reinicia la pausa a cero con cada ejecución propia; cada vela la incrementa hasta el límite y un valor inicial grande permite la primera configuración.
- La posición plana bloquea nuevas entradas después de una ejecución. Mientras una limitada anterior siga pendiente, otra vela válida constituye un intento independiente con la misma cancelación a seis velas.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
