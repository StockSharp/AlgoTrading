# Estrategia MACross
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia replica el comportamiento del asesor experto `MQL/34176/MACross.mq4` original utilizando la API de alto nivel de StockSharp. Negocia un único instrumento en un cruce de media móvil y mantiene todos los controles de riesgo expresados ​​en pips y capital de la cuenta.

## Lógica comercial

1. Se construyen dos promedios móviles simples (SMA) sobre el tipo de vela configurado:
   - `FastPeriod` reacciona rápidamente a los cambios de precios.
   - `SlowPeriod` suaviza la tendencia a largo plazo.
2. Al cierre de cada vela terminada se comparan los promedios rápido y lento:
   - Un cruce alcista (cruce rápido por encima del lento) abre una posición larga. Cualquier corto activo se aplana primero.
   - Un cruce bajista (cruce rápido por debajo del lento) abre una posición corta después de cerrar una posición larga existente.
3. Cada entrada utiliza un volumen de mercado fijo derivado de `LotSize` y alineado con los límites del instrumento (`VolumeStep`, `MinVolume`, `MaxVolume`).
4. Una vez abierta la posición, la estrategia rastrea dos objetivos de riesgo medidos en pips. El tamaño del pip se infiere automáticamente de `Security.Decimals` (o `PriceStep` como alternativa):
   - `TakeProfitPips` define la distancia al objetivo de ganancias. Golpearlo implica una salida del mercado en la dirección actual.
   - `StopLossPips` define la distancia de parada de protección. Su incumplimiento cierra la posición inmediatamente.
5. El guardia `MinEquity` puede pausar las operaciones. Cuando el valor actual de la cartera está por debajo del umbral, la estrategia sigue gestionando la posición activa pero no permite nuevas entradas.

Todos los cálculos funcionan únicamente con velas terminadas, coincidiendo completamente con el asesor experto original que esperó una nueva barra antes de evaluar las medias móviles.

## Visualización

Cuando hay un panel de gráfico disponible, la estrategia se traza:

- Ingrese velas de la serie suscrita.
- Las SMA rápidas y lentas.
- Operaciones propias para resaltar las entradas y salidas activadas por las reglas de cruce.

## Parámetros

| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `FastPeriod` | `int` | `8` | Longitud del rápido SMA que genera señales cruzadas. |
| `SlowPeriod` | `int` | `20` | Longitud del SMA lento utilizado como línea de tendencia de referencia. |
| `TakeProfitPips` | `decimal` | `20` | Distancia objetivo de ganancias expresada en pips. El tamaño del pip se deduce de los decimales del instrumento. |
| `StopLossPips` | `decimal` | `20` | Distancia de parada de protección en pips. Utiliza el mismo cálculo del tamaño del pip que el objetivo de ganancias. |
| `LotSize` | `decimal` | `1` | Volumen base de pedidos. La estrategia lo redondea al tamaño permitido más cercano antes de enviar órdenes de mercado. |
| `MinEquity` | `decimal` | `100` | Patrimonio mínimo de la cuenta. Las nuevas operaciones se bloquean mientras el valor de la cartera esté por debajo de este nivel. |
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(1).TimeFrame()` | Serie de velas utilizada para cálculos SMA y evaluación de señales. |

## Diferencias vs. versión MQL

- El experto original de MQL pasó los precios de límite de pérdidas y obtención de ganancias a `OrderSend` como cero. El puerto StockSharp emula el mismo comportamiento con salidas manuales que monitorean el precio de cierre de cada vela terminada.
- La validación de equidad (`cekMinEquity`) ahora lee `Portfolio.CurrentValue` y `Portfolio.BeginValue` en lugar de `AccountEquity()` pero conserva la lógica del umbral.
- La detección del tamaño del pip refleja el ayudante `GetPipPoint`: las cotizaciones de 2 o 3 dígitos usan 0,01, las cotizaciones de 4 o 5 dígitos usan 0,0001; de lo contrario, se toma `PriceStep`.

La estrategia resultante se puede optimizar a través de todos los parámetros expuestos y se combina perfectamente con la infraestructura de gestión de riesgos y gráficos de StockSharp.
