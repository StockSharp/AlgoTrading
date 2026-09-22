# Entradas limitadas por cruce EMA desde Level 1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El diagrama conserva la señal EMA(14)/EMA(50) de TwoDLimitsStrategy y hace visible la gestión de órdenes. Cada vela de cinco minutos terminada captura Level 1, coloca un límite detrás de la mejor cotización y cancela la orden pendiente si las medias se cruzan al revés.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas terminadas alimentan EMA rápida(14) y lenta(50); dos bloques Crossing detectan ambas direcciones.
- BestBidPrice y BestAskPrice llegan de forma asíncrona desde Level 1 y se fijan en cada vela antes de calcular precios.
- La compra queda 0,02% bajo el mejor bid y la venta 0,02% sobre el mejor ask, dando una función observable a Order cancellation.
- Cada ejecución inicia un enfriamiento N values de 100 velas; hasta terminar bloquea entradas y precios de Position protection.
- Tras el enfriamiento, Position protection aplica stop de 0,3% y objetivo de 0,6%.

## Reglas de entrada y salida

- **Entrada en largo**: EMA(14) cruza por encima de EMA(50), Position es cero o corta, existe un bid positivo y terminó el enfriamiento. Se coloca una compra bajo el bid; el volumen es abs(Position) más el volumen base para cerrar el corto y abrir largo con una ejecución.
- **Entrada en corto**: EMA(14) cruza por debajo de EMA(50), Position es cero o larga, existe un ask positivo y terminó el enfriamiento. Se coloca una venta sobre el ask con la misma regla de reversión neta.
- **Salida**: El cruce inverso cancela el último límite opuesto aún activo. Tras la ejecución y 100 velas, Position protection sale con stop de 0,3% u objetivo de 0,6%; un límite contrario ejecutado también puede invertir Position.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candle Time Frame | 00:05:00 | Intervalo terminado para EMA, captura de cotizaciones, enfriamiento y protección. |
| Fast EMA Length | 14 | Cantidad de valores de cinco minutos en la EMA rápida. |
| Slow EMA Length | 50 | Cantidad de valores de cinco minutos en la EMA lenta. |
| Quote Offset | 0.02% | Porcentaje alejado de la mejor cotización: bajo bid para comprar y sobre ask para vender. |
| Base Volume | 1 | Tamaño de la nueva posición después de compensar la exposición contraria. |
| Cooldown, candles | 100 | Velas terminadas tras cada ejecución antes de reactivar entradas y protección. |
| Stop Loss | 0.3% | Distancia porcentual del stop respecto de la ejecución de entrada. |
| Take Profit | 0.6% | Distancia porcentual del objetivo respecto de la ejecución de entrada. |

## Detalles del diagrama

- El C# entra a mercado; el diagrama usa límites de Level 1 deliberadamente para mostrar registro y cancelación.
- Las distancias originales de 200/400 pasos se representan como aproximadamente 0,3%/0,6% al precio BTCUSDT del replay, conservando 1:2.
- El código no revisa stop ni objetivo durante las primeras 100 velas tras una ejecución; filtrar el precio reproduce ese orden.
- Los retenedores alinean Level 1 asíncrono con la decisión EMA por vela. Se desactiva el ajuste de precio porque el instrumento puede no declarar paso.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
