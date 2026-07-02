# Estrategia FiveMinuteRsiCci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

`FiveMinuteRsiCciStrategy` es un puerto StockSharp del asesor experto MetaTrader 4 **5Mins Rsi Cci EA.mq4**. El script original intercambia velas de cinco minutos combinando un cruce de umbral RSI con un filtro de media móvil suavizado/EMA y la polaridad de dos indicadores CCI. La versión C# mantiene la misma lógica de decisión mientras utiliza el nivel alto API de StockSharp para suscripciones de datos, vinculación de indicadores y gestión de riesgos.

## Lógica comercial

1. Suscríbase al tipo de vela configurado (período de tiempo de cinco minutos de forma predeterminada) y actualice cinco indicadores en tiempo real: RSI, un MA suavizado del precio de apertura, un EMA del precio de apertura, además de CCI rápidos y lentos calculados a partir de precios típicos.
2. Cada vela terminada se evalúa solo cuando no hay ninguna posición abierta y el diferencial de oferta/demanda actual es inferior a `MaxSpreadPoints` (convertido a unidades de precio).
3. Una señal larga requiere:
   - el MA suavizado por encima del EMA,
   - el RSI cruzando hacia arriba a través de `BullishRsiLevel` entre la vela anterior y la actual,
   - ambos valores CCI superiores a cero.
4. Una señal corta requiere condiciones inversas (MA suavizada por debajo de EMA, RSI cruzando hacia abajo a través de `BearishRsiLevel`, ambos CCI por debajo de cero).
5. El volumen de la orden reproduce el tamaño de posición dinámica del EA: `LotCoefficient × sqrt(Equity / EquityDivisor)` redondeado al paso de volumen del instrumento y restringido por `VolumeMin`/`VolumeMax`.
6. La lógica de protección es manejada por `StartProtection`, que adjunta distancias de stop-loss, take-profit y trailing-stop convertidas de MetaTrader puntos a compensaciones de precios absolutos.

## Parámetros

| Parámetro | Predeterminado | Descripción |
| --- | --- | --- |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` | Plazo utilizado para las actualizaciones de indicadores y la evaluación de señales. |
| `RsiPeriod` | `14` | Número de velas utilizadas en el cálculo RSI. |
| `FastSmmaPeriod` | `2` | Período de media móvil suavizada rápida aplicada a los precios de apertura. |
| `SlowEmaPeriod` | `6` | Período de la EMA lenta aplicada a los precios de apertura. |
| `FastCciPeriod` | `34` | Periodo del rápido CCI computado a partir del precio típico `(H+L+C)/3`. |
| `SlowCciPeriod` | `175` | Periodo del CCI lento calculado a partir del precio típico. |
| `BullishRsiLevel` | `55` | RSI umbral que se debe cruzar hacia arriba para armar una entrada larga. |
| `BearishRsiLevel` | `45` | RSI umbral que se debe cruzar hacia abajo para armar una entrada corta. |
| `StopLossPoints` | `60` | Distancia de stop-loss en MetaTrader puntos (convertida a precio absoluto). Establezca en `0` para desactivar. |
| `TakeProfitPoints` | `0` | Distancia de obtención de beneficios en MetaTrader puntos. Zero mantiene el comportamiento original EA (sin TP). |
| `TrailingStopPoints` | `20` | Distancia del trailing-stop en MetaTrader puntos. Zero desactiva el seguimiento. |
| `LotCoefficient` | `0.01` | Coeficiente base utilizado en la fórmula de dimensionamiento de posición dinámica. |
| `EquityDivisor` | `10` | Divisor dentro de la raíz cuadrada para el tamaño basado en acciones (`sqrt(Equity / EquityDivisor)`). |
| `MaxSpreadPoints` | `18` | Spread máximo permitido (en MetaTrader puntos). Las órdenes se omiten hasta que el diferencial se reduce. |

## Notas

- El filtro de dispersión se basa en datos de nivel 1; Si las mejores cotizaciones de oferta/demanda no están disponibles, la estrategia espera antes de abrir nuevas posiciones.
- La conversión de punto a precio escala automáticamente en `PriceStep` y la precisión del instrumento (5/3 instrumentos decimales multiplican el paso por 10) para reflejar el valor `Point` de MetaTrader.
- Las paradas y el seguimiento se gestionan a través del motor de protección integrado de StockSharp con salidas de mercado, lo que coincide con el uso de órdenes de mercado por parte de EA para las actualizaciones de las paradas de seguimiento.
