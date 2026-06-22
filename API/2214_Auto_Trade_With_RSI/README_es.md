# Estrategia de Comercio Automático con RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia promedia los últimos valores de RSI para generar señales de trading. Calcula un Índice de Fuerza Relativa (RSI) estándar sobre un período configurable y luego aplica una media móvil simple al propio RSI. Las operaciones se abren cuando el RSI promediado cruza umbrales predefinidos y se cierran cuando se alcanza el umbral opuesto.

## Lógica de trading

1. **Cálculo del RSI**
   - El indicador utiliza `RsiPeriod` para calcular el RSI basado en precios de cierre de velas.
2. **Promediado del RSI**
   - Los últimos `AveragePeriod` valores de RSI se suavizan con una media móvil simple.
3. **Reglas de entrada**
   - Si `BuyEnabled` es `true` y no hay posición abierta, se envía una orden de **compra** cuando el RSI promediado supera `BuyThreshold` (por defecto 55).
   - Si `SellEnabled` es `true` y no hay posición abierta, se envía una orden de **venta** cuando el RSI promediado cae por debajo de `SellThreshold` (por defecto 45).
4. **Reglas de salida**
   - Cuando `CloseBySignal` es `true`, las posiciones abiertas se cierran en señales opuestas:
     - Las posiciones largas se cierran cuando el RSI promediado cae por debajo de `CloseBuyThreshold` (por defecto 47).
     - Las posiciones cortas se cierran cuando el RSI promediado sube por encima de `CloseSellThreshold` (por defecto 52).

## Parámetros

- `BuyEnabled` – habilitar o deshabilitar entradas largas.
- `SellEnabled` – habilitar o deshabilitar entradas cortas.
- `CloseBySignal` – permitir salidas en señales RSI opuestas.
- `RsiPeriod` – longitud del cálculo del RSI.
- `AveragePeriod` – número de valores de RSI usados para el promediado.
- `BuyThreshold` – valor del RSI promediado por encima del cual se abre una posición larga.
- `SellThreshold` – valor del RSI promediado por debajo del cual se abre una posición corta.
- `CloseBuyThreshold` – valor del RSI promediado por debajo del cual se cierra una posición larga.
- `CloseSellThreshold` – valor del RSI promediado por encima del cual se cierra una posición corta.
- `CandleType` – tipo de vela para las suscripciones.

## Notas

Esta estrategia demuestra cómo los valores de indicadores pueden combinarse mediante vinculación en la API de alto nivel de StockSharp. Las funciones de trailing stop y gestión del dinero de la versión MQL original se omiten por simplicidad.

