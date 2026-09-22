# Diagrama de estrategia de gestión de cotizaciones de market maker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama convierte una señal de reversión a la media en un ciclo de vida completo de la cotización. Cuando el precio abandona una banda alrededor de una media lenta, publica una orden limitada en su lado del libro, la reemplaza siguiendo al mejor precio, cancela una orden caducada o contradicha y solo cruza el spread cuando expira el intento pasivo.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas cerradas de cinco minutos alimentan una SimpleMovingAverage de 100 periodos; dos fórmulas sitúan las bandas un 0,8% por encima y por debajo.
- Market depth entrega BestBid y BestAsk. Dos variables muestrean ambos precios con el reloj de las velas para evitar desfases con las actualizaciones del libro.
- El cierre actual debe estar fuera de una banda y el cierre anterior aún dentro. Las comparaciones de Position impiden aumentar una posición en el mismo sentido.
- Order registering publica una compra limitada en el BestBid muestreado o una venta limitada en el BestAsk, con un Quote Volume común.
- Combination conserva la referencia de la orden vigente. Order replacing devuelve cada reemplazo al mismo flujo y mueve la cotización cuando su deriva relativa supera el umbral.
- Order cancellation retira la cotización opuesta al romperse la otra banda y las órdenes caducadas tras 12 velas. Si el precio sigue fuera y la posición está plana, Modify position abre a mercado.
- Position protection cierra con un beneficio del 0,8% o una pérdida del 0,4%; su ejecución activa Mass order cancellation.

## Reglas de entrada y salida

- **Entrada en largo**: El cierre cae bajo la banda inferior después de que el cierre anterior estuviera en ella o por encima, Position no es largo y existe un BestBid muestreado. Se publica una compra limitada y se reemplaza si la deriva supera 0,001. Tras 12 velas, si Position sigue plano y el precio continúa bajo la banda, se cancela la cotización y se envía una compra a mercado OpenPosition.
- **Entrada en corto**: El cierre sube sobre la banda superior después de que el cierre anterior estuviera en ella o por debajo, Position no es corto y existe un BestAsk muestreado. Se publica una venta limitada que sigue al mercado mediante reemplazos. Tras 12 velas, si Position sigue plano y el precio continúa sobre la banda, se cancela y se vende a mercado con OpenPosition.
- **Salida**: Todas las ejecuciones de entrada procedentes del registro, los reemplazos o la entrada alternativa llegan a Position protection. Los cierres de vela controlan un take profit del 0,8% y un stop loss del 0,4%. Después de una ejecución protectora, Mass order cancellation elimina las cotizaciones activas restantes.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candle Time Frame | 00:05:00 | Marco temporal de la media, señales, muestreo del libro, edad de la cotización y protección. |
| SMA Length | 100 | Longitud de la SimpleMovingAverage central. |
| Band Deviation | 0.008 | Semiancho relativo de la banda; 0,008 equivale a 0,8% a cada lado. |
| Quote Volume | 1 | Cantidad de cotizaciones, reemplazos y entradas alternativas. |
| Re-quote Threshold | 0.001 | Distancia relativa respecto al mejor precio que activa un reemplazo. |
| Quote Life, candles | 12 | Número de velas cerradas antes de considerar caducado el intento pasivo. |
| Take Profit, % | 0.8 | Distancia favorable desde la ejecución de entrada, en porcentaje. |
| Stop Loss, % | 0.4 | Distancia adversa desde la ejecución de entrada, en porcentaje. |

## Detalles del diagrama

- Previous value guarda la vela anterior antes de convertir su cierre, de modo que la configuración es un evento de salida de la banda y no una condición repetida.
- BestBid, BestAsk y Position se retienen en variables y se liberan con la vela cerrada; todas las comparaciones y órdenes usan así la hora de la vela actual.
- Cada orden registrada o reemplazada entra en un bus Combination<Order>, que alimenta conversión, reemplazo, cancelación y gráfico con la orden más reciente.
- El contador se reinicia al abandonar una banda, avanza una vez por vela y se limita a una unidad por encima del plazo; la igualdad con 12 emite un único evento de caducidad.
- El registro y el reemplazo conservan el precio recibido. La alternativa a mercado usa OpenPosition, y la protección junto con la cancelación masiva limpian el estado al salir.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
