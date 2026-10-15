# Reversión Last Price con límites Level1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El ejemplo trata ante todo de Order registering y ejecución Level1: expresa la conocida reversión por desviación de EMA con límites negociables. Pese al nombre histórico, cada decisión procede de una vela terminada de cuatro horas; no hay señal por ticks.

![schema](schema.svg)

## Resumen de la estrategia

- Cierres terminados de cuatro horas alimentan EMA(20) y se comparan con límites 0,5% inferior y superior.
- Sin posición, cerrar bajo el límite inferior pide compra y sobre el superior pide venta.
- El largo sale al volver el cierre a EMA o por encima; el corto sale al volver a EMA o por debajo.
- Cada vela captura best ask para compras y best bid para ventas; puertas de precio positivo evitan registrar sin cotización.
- El mismo volumen uno alimenta entradas y salidas, por lo que una posición propia se aplana con una ejecución opuesta.

## Reglas de entrada y salida

- **Entrada en largo**: Con Position == 0 y Close < EMA × (1 − 0,5/100), registra compra en best ask. Cruzar el spread busca ejecutar como BuyMarket.
- **Entrada en corto**: Con Position == 0 y Close > EMA × (1 + 0,5/100), registra venta en best bid. Cruzar el spread busca ejecutar como SellMarket.
- **Salida**: Con Position > 0 y Close >= EMA, el bloque sell vende una unidad en best bid; con Position < 0 y Close <= EMA, buy compra una unidad en best ask.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candle Time Frame | 04:00:00 | Intervalo terminado para EMA y decisiones, igual al valor C#. |
| EMA Period | 20 | Cantidad de cierres en ExponentialMovingAverage. |
| Entry Distance, % | 0.5 | Desviación porcentual de EMA requerida para entrar sin posición. |
| Shared Entry/Exit Volume | 1 | Volumen común de límites de entrada y salida. |

## Detalles del diagrama

- C# usa órdenes de mercado. El diagrama conserva Order registering y usa límites negociables: compra en best ask y venta en best bid.
- Una versión pasiva compraría en best bid y vendería en best ask, pero necesitaría Order cancellation o replacement para órdenes sin ejecutar.
- Close y EMA pertenecen a la misma vela. Una variable libera el cierre tras actualizar EMA y evita compararlo con la media anterior.
- El volumen compartido refleja Strategy.Volume. Una posición externa o de otro tamaño no queda necesariamente plana con salida fija de una unidad.
- La idea se solapa con MA_Deviation; aquí la lección distinta es muestreo Level1 y ejecución mediante límites negociables.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
