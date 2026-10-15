# Estrategia Brandy (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
La estrategia Brandy es una traducción directa del Expert Advisor de MetaTrader 5 *Brandy (edición de barabashkakvn)*. Combina dos medias móviles configurables y evalúa sus posiciones relativas en velas cerradas para decidir si abrir una posición larga o corta. La lógica original también aplica controles opcionales de stop loss, take profit y trailing stop expresados en pips. Esta versión en C# reproduce fielmente esos comportamientos sobre la API de estrategia de alto nivel de StockSharp.

La estrategia calcula una media móvil "rápida" sobre el flujo de precios de apertura y una media móvil "lenta" sobre el flujo de precios de cierre. Ambos indicadores tienen parámetros independientes de período, método de suavizado, fuente de precio, referencia de barra de señal y desplazamiento. Las señales se generan cuando los valores de MA de la barra anterior están en el mismo lado de los valores de señal respectivos. La lógica protectora verifica la media móvil basada en la apertura en cada vela y sale inmediatamente de la operación si la condición de tendencia ya no se cumple. La gestión de riesgo adicional se implementa con distancias opcionales de stop loss, take profit y trailing stop, todas medidas en pips y convertidas a precios absolutos mediante el tamaño del tick del instrumento con un ajuste de pip de cinco dígitos.

## Lógica de Trading
1. En cada vela terminada, la estrategia actualiza las medias móviles de precio de apertura y cierre usando el método de suavizado configurado y el precio aplicado. Los valores históricos de MA se almacenan en búfer para que el código pueda emular el comportamiento de desplazamiento de `iMA` del Expert Advisor original.
2. Cuando no hay posición activa, se abre una operación larga si:
   - El valor de MA basado en apertura de la barra anterior es mayor que el valor de señal configurado (posiblemente desplazado);
   - El valor de MA basado en cierre de la barra anterior también es mayor que su referencia de señal (nótese que el EA original compara contra el indicador basado en apertura para esta verificación, y la traducción mantiene esa peculiaridad por compatibilidad).
3. Se abre una operación corta cuando ambas medias móviles están por debajo de sus referencias de señal respectivas.
4. Mientras hay una posición activa, la estrategia evalúa las salidas en cada vela terminada en el siguiente orden:
   - Reversión de tendencia: si la MA basada en apertura cae por debajo del valor de señal (para largos) o sube por encima (para cortos), la posición se cierra inmediatamente a mercado.
   - Actualización del trailing stop: cuando está habilitado y el movimiento a favor de la operación excede *trailing stop + trailing step* (convertido a precios absolutos), el nivel de stop se ajusta para mantener una distancia de *trailing stop* desde el último cierre.
   - Take profit: si el rango de la vela toca el objetivo de beneficio, la operación se cierra a mercado.
   - Stop loss: si el rango de la vela viola el nivel de stop protector, la operación se cierra.
5. Todo el volumen es fijo y está determinado por el parámetro `TradeVolume`. El valor por defecto replica la configuración de 0.1 lotes de la versión MT5.

## Referencia de Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `TradeVolume` | Tamaño de la orden de mercado en lotes.
| `StopLossPips` | Distancia del stop protector, medida en pips (0 lo deshabilita).
| `TakeProfitPips` | Distancia del objetivo de beneficio en pips (0 lo deshabilita).
| `TrailingStopPips` | Distancia del trailing stop en pips. Requiere que `TrailingStepPips` sea positivo.
| `TrailingStepPips` | Movimiento adicional de pips requerido antes de avanzar el trailing stop. Debe ser distinto de cero cuando el trailing stop está activo.
| `MaClosePeriod`, `MaOpenPeriod` | Longitudes de media móvil para las series de cierre y apertura respectivamente.
| `MaCloseShift`, `MaOpenShift` | Desplazamientos aplicados a los búferes de MA (número de barras).
| `MaCloseSignalBar`, `MaOpenSignalBar` | Índices de barras usados como referencias de comparación. Cero coincide con el valor más reciente, uno se refiere a la barra anterior, y así sucesivamente.
| `MaCloseMethod`, `MaOpenMethod` | Métodos de suavizado de media móvil (SMA, EMA, SMMA, LWMA).
| `MaCloseAppliedPrice`, `MaOpenAppliedPrice` | Fuente de precio de vela para cada indicador (cierre, apertura, máximo, mínimo, mediana, típico, ponderado).
| `CandleType` | Marco temporal de las velas solicitadas desde la fuente de datos.

## Notas de Implementación
- El tamaño del pip se calcula desde `Security.PriceStep` y se multiplica por 10 cuando el instrumento expone 3 o 5 decimales, reflejando el ajuste de MetaTrader entre puntos y pips.
- El historial del indicador se retiene usando colas acotadas para que la estrategia pueda reproducir llamadas a `iMA` con índices de barra de señal arbitrarios y desplazamientos positivos sin depender de accesores de indicadores prohibidos.
- La condición de cierre para la media móvil basada en cierre compara intencionalmente contra el búfer de MA de **apertura** porque el código fuente original invocaba `iMAGet(handle_iMAOpen, MaClose_SignalBar)`. Esta traducción mantiene el comportamiento para preservar la compatibilidad con configuraciones heredadas.
- Los stops y la lógica de trailing se ejecutan en velas terminadas y aproximan las modificaciones de órdenes realizadas por el Expert Advisor mientras respetan la API de alto nivel de StockSharp.

## Consejos de Uso
- Configure el parámetro `CandleType` para que coincida con el marco temporal usado por el EA original (típicamente un único marco temporal de instrumento).
- Mantenga `TrailingStopPips` en cero si no se desea comportamiento de trailing; de lo contrario, asegúrese de que `TrailingStepPips` sea estrictamente positivo para evitar el error de inicialización aplicado por la estrategia.
- Al hacer backtesting en StockSharp, asegúrese de que `PriceStep` y `Decimals` del instrumento reflejen la definición de pip prevista para que las distancias de riesgo se conviertan correctamente.
