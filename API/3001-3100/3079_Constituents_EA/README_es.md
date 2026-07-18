# Estrategia Constituents EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el **Constituents EA** de `MQL/22595` a la API de alto nivel de StockSharp. Recrea la lógica original
de colocar dos órdenes pendientes alrededor del rango más reciente a una hora específica mientras mantiene el flujo de trabajo
compatible con el manejo de órdenes y los helpers de protección de riesgo de StockSharp.

## Cómo funciona la estrategia

1. **Activación programada** – al final de cada vela la estrategia verifica si la siguiente barra comenzará en `StartHour`. Solo
   en ese momento se consideran nuevas órdenes pendientes, lo que replica el código de MetaTrader que reaccionaba al nacimiento
   de la barra cuyo tiempo de apertura coincide con la hora configurada.
2. **Detección de rango** – el máximo más alto y el mínimo más bajo entre las `SearchDepth` velas completadas anteriores se
   rastrean con indicadores `Highest`/`Lowest`. Estos dos precios definen los niveles de ruptura/reversión a la media usados para
   la colocación de órdenes.
3. **Filtros de distancia de precio** – las mejores cotizaciones bid/ask actuales se reciben del feed del libro de órdenes. Las
   órdenes se colocan solo si la distancia entre la cotización y el precio candidato es mayor o igual a `MinOrderDistancePips`
   (convertido a precio absoluto usando `PointValue`). Esto reimplementa la validación del nivel de congelación original y
   previene órdenes pendientes inválidas.
4. **Selección de estilo de orden** – `PendingOrderMode` elige entre órdenes limitadas (buy limit en el mínimo, sell limit en el
   máximo) u órdenes stop (buy stop encima del máximo, sell stop debajo del mínimo). Ambas órdenes se envían simultáneamente,
   igual que en el script de MetaTrader.
5. **Protección de riesgo** – el helper integrado `StartProtection` adjunta niveles de stop-loss y take-profit expresados en pasos
   de precio absolutos (`StopLossPips`/`TakeProfitPips`). Las verificaciones de distancia mínima contra `MinStopDistancePips`
   replican el requisito de MT5 de que las órdenes protectoras deben respetar el nivel de stop del símbolo.
6. **Gestión de órdenes** – si una orden pendiente se ejecuta, la orden opuesta se cancela inmediatamente. Durante el intervalo
   de la barra la estrategia nunca coloca órdenes adicionales mientras existen órdenes activas, coincidiendo con el comportamiento
   del EA de origen.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `StartHour` | Hora (0-23) cuando se crea el nuevo par de órdenes pendientes. |
| `SearchDepth` | Número de velas completadas previas usadas para calcular el rango máximo/mínimo. |
| `PendingOrderMode` | `Limit` replica la variante de reversión a la media, `Stop` coloca órdenes de ruptura. |
| `StopLossPips` | Distancia de stop-loss medida en pips (convertido con `PointValue`). Establecer en 0 para deshabilitar. |
| `TakeProfitPips` | Distancia de take-profit en pips. Establecer en 0 para deshabilitar. |
| `PointValue` | Valor del pip en unidades de precio. Establecer en 0 para auto-detectar desde `Security.PriceStep`/`MinStep`. |
| `MinOrderDistancePips` | Distancia mínima permitida entre bid/ask actual y el precio pendiente, modelando verificaciones de freeze-level. |
| `MinStopDistancePips` | Distancia mínima permitida para stop/take, reflejando verificaciones de `StopsLevel`. |
| `CandleType` | Marco temporal usado para el cálculo de rango y lógica de programación. |

`Strategy.Volume` controla el tamaño de la orden; mantenerlo positivo para que `BuyLimit`, `SellLimit`, `BuyStop` y `SellStop`
puedan enviar órdenes.

## Uso

1. Adjuntar la estrategia a un instrumento y establecer `CandleType` al marco temporal que se desea operar.
2. Configurar `StartHour` y `SearchDepth` exactamente como en las entradas de MT5. Ajustar los umbrales `Min*Pips` si el broker
   aplica distancias mínimas entre órdenes y el precio de mercado.
3. Calibrar `PointValue` cuando la auto-detección de los metadatos del instrumento no sea posible (por ejemplo, en instrumentos
   sintéticos).
4. Establecer `StopLossPips` y `TakeProfitPips` para que coincidan con el EA original. El módulo de protección adjuntará
   automáticamente stops y objetivos una vez que se ejecute una orden.
5. Proporcionar un `Volume` positivo e iniciar la estrategia. Se suscribirá a velas y datos del libro de órdenes, colocará ambas
   órdenes pendientes en la barra programada y cancelará la orden opuesta cuando se ejecute una operación.

## Diferencias respecto al EA original

- El modo de riesgo `MoneyFixedMargin` de MetaTrader (dimensionamiento basado en porcentaje) no está portado. Los usuarios de
  StockSharp deben configurar `Strategy.Volume` directamente o envolver la estrategia con un módulo de dimensionamiento de
  posición externo.
- Las verificaciones de freeze-level y stop-level se expresan a través de los parámetros configurables `MinOrderDistancePips` y
  `MinStopDistancePips` porque los metadatos equivalentes del exchange no siempre están disponibles a través de StockSharp.
- La colocación de órdenes ocurre cuando la vela anterior cierra y la próxima barra comienza en `StartHour`. Esto es
  funcionalmente idéntico a la implementación MT5 que se activaba en el nacimiento de la nueva barra.
- Todos los comentarios dentro del código fuente han sido traducidos al inglés, mientras que la documentación externa está
  disponible en varios idiomas por conveniencia.

Ajustar las distancias y la hora de trading para que coincidan con el instrumento que se planea operar. En mercados con spreads
amplios puede ser necesario aumentar `MinOrderDistancePips` o los valores de pip para evitar el rechazo inmediato por parte del
broker.
