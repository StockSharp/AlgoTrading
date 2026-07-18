# Estrategia de gestión de riesgos ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia de Gestión de riesgos ATR es una conversión StockSharp de los MetaTrader 5 expertos *Gestión de riesgos EA basada en la volatilidad ATR*. El EA original se centró en dimensionar automáticamente las posiciones de acuerdo con el saldo de la cuenta y la volatilidad actual del mercado medida por el rango verdadero promedio (ATR). El puerto StockSharp mantiene la misma filosofía: solo abre posiciones largas cuando una media móvil simple de 10 períodos cruza por encima de una media móvil simple de 20 períodos, y cada tamaño de entrada se calcula de modo que la pérdida potencial en el stop de protección coincida con el porcentaje de riesgo configurado.

La conversión sigue el nivel alto StockSharp API. Los cálculos de los indicadores se basan en los componentes `AverageTrueRange` y `SimpleMovingAverage` adjuntos a la suscripción de velas en lugar de llamadas directas al indicador. La gestión comercial reutiliza StockSharp métodos auxiliares, cancelando y recreando la parada de protección después de cada ejecución para que la posición neta y la orden de parada siempre coincidan.

## Lógica comercial
1. Suscríbase al período de tiempo definido por `CandleType` y espere a que las velas se cierren por completo para evitar decisiones prematuras.
2. Alimente un ATR de 14 períodos y dos promedios móviles simples (longitudes 10 y 20) con los datos de suscripción.
3. Cuando la media móvil rápida cierra por encima de la media móvil lenta y no hay ninguna posición abierta, calcule el tamaño de la posición según el modelo de riesgo seleccionado y envíe una orden de compra de mercado.
4. Después de cada llenado, calcule la distancia del límite de pérdidas: ya sea `ATR * AtrMultiplier` o un número fijo de pasos de precio cuando `UseAtrStopLoss` está deshabilitado.
5. Redondee el precio de parada al tick más cercano y coloque una orden `SellStop` con el tamaño de posición actual. Cualquier parada anterior se cancela antes de que se registre la nueva.
6. Cuando se ejecuta la orden de parada y la posición vuelve a cero, la estrategia borra su estado interno, lista para el próximo cruce.

## Gestión de riesgos
- `RiskPercentage` determina cuánto del valor de la cartera se puede perder en una sola operación. La estrategia lee `Portfolio.CurrentValue` (o `BeginValue` como alternativa) y lo multiplica por el porcentaje para obtener el riesgo monetario permitido.
- El riesgo permitido se divide por la distancia del stop-loss para obtener el volumen comercial. El redondeo de volumen respeta el paso de volumen del instrumento y las restricciones mínimas y máximas para que las órdenes generadas sigan siendo válidas en la bolsa.
- Si `RiskPercentage` se establece en `0`, la estrategia vuelve a la propiedad predeterminada `Volume` (1 lote por defecto) mientras mantiene la parada de protección automática.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | plazo de 1 minuto | Serie de velas primarias procesadas por la estrategia. |
| `AtrPeriod` | `int` | `14` | Número de velas utilizadas para suavizar el indicador ATR. |
| `AtrMultiplier` | `decimal` | `2.0` | Multiplicador aplicado al valor ATR para derivar la distancia de stop-loss. |
| `RiskPercentage` | `decimal` | `1.0` | Porcentaje del valor de la cartera arriesgado en cada operación. Establezca en cero para utilizar un volumen fijo. |
| `UseAtrStopLoss` | `bool` | `true` | Cuando está habilitado, la parada se coloca en `ATR * AtrMultiplier`; de lo contrario se utiliza una distancia fija. |
| `FixedStopLossPoints` | `int` | `50` | Número de pasos de precio utilizados para la parada de protección siempre que se deshabilite la ubicación basada en ATR. |

## Diferencias con el EA original
- StockSharp funciona con posiciones netas, por lo tanto, la conversión solo envía órdenes de compra de mercado. Las salidas se realizan a través de la protección `SellStop`, que reproduce el comportamiento EA de estar siempre plano después de una parada.
- MetaTrader expone la constante `_Point` para el tamaño del tick. El puerto consulta `Security.PriceStep` y recurre a una única unidad monetaria cuando el instrumento no proporciona una especificación de tick.
- El tamaño de la posición respeta los filtros de volumen de StockSharp (`VolumeStep`, `MinVolume`, `MaxVolume`) para garantizar que el libro de pedidos acepte los tamaños de pedidos generados.
- El procesamiento del indicador se basa en eventos a través de `Subscription.Bind(...)` en lugar de llamadas `iMA`/`iATR` sincrónicas.

## Consejos de uso
- Asegúrese de que la cartera conectada informe un `CurrentValue` correcto; de lo contrario, el tamaño de la posición basado en el riesgo volverá a caer a volumen cero.
- La propiedad `Volume` todavía actúa como una red de seguridad. Si desea un tamaño de lote fijo independientemente de los cálculos de ATR, establezca `RiskPercentage` en cero y ajuste `Volume` antes de comenzar la estrategia.
- Adjunte la estrategia a un gráfico para visualizar las velas, tanto las medias móviles como las operaciones ejecutadas. Ayuda a confirmar que las nuevas entradas solo aparecen cuando el promedio rápido cierra por encima del lento y que las paradas se ubican exactamente por debajo de la última oscilación del precio.
- Considere aumentar `AtrMultiplier` en instrumentos más volátiles para evitar paradas prematuras, o deshabilite la ubicación basada en ATR y proporcione una distancia fija personalizada a través de `FixedStopLossPoints`.

## Indicadores
- `AverageTrueRange` (longitud `AtrPeriod`).
- `SimpleMovingAverage` (longitud rápida `10`).
- `SimpleMovingAverage` (longitud lenta `20`).
