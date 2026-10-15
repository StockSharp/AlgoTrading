# Estrategia de reversión a la media Donchian
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una adaptación del MetaTrader asesor experto `MeanReversion.mq5`. Opera con un patrón simple de reversión a la media: cada vez que el precio registra un nuevo mínimo dentro de la ventana retrospectiva seleccionada, la estrategia abre una posición larga, apuntando al punto medio del rango reciente. Cuando aparece un nuevo máximo, la estrategia refleja la lógica del lado corto. El tamaño de la posición se calcula a partir del porcentaje de riesgo y la distancia de parada, replicando fielmente el cálculo del lote que realiza el EA original.

## Lógica de trading
1. Cree un canal Donchian utilizando el tipo de vela configurado y el período retrospectivo. La banda superior marca el máximo más alto y la banda inferior el mínimo más bajo sobre la ventana. El punto medio `(upper + lower) / 2` actúa como objetivo de reversión a la media.
2. Si la vela finalizada actual alcanza un nuevo mínimo (`Low <= LowerBand`) y no hay ninguna posición abierta, la estrategia compra en el mercado. La parada de protección se refleja alrededor del precio de entrada, de modo que el punto medio se convierte en el objetivo de ganancias, coincidiendo con el cálculo MetaTrader `sl = 2 * Ask - tp`.
3. Si la vela alcanza un nuevo máximo (`High >= UpperBand`) y no hay ninguna posición abierta, la estrategia vende en el mercado con un stop simétrico por encima del precio. El punto medio vuelve a actuar como nivel de obtención de beneficios.
4. El stop-loss y el take-profit se controlan en cada vela terminada. Una ruptura más allá del stop cierra la posición inmediatamente, mientras que al tocar el punto medio se sale de la operación en el objetivo previsto. El estado interno se reinicia automáticamente cada vez que la posición es plana.

## Dimensionamiento de posiciones
* El riesgo por operación es igual a `Portfolio.CurrentValue * (RiskPercent / 100)`. Si los datos de la cartera no están disponibles, la estrategia vuelve al volumen mínimo negociable.
* El riesgo del contrato se mide como `|EntryPrice - StopPrice|`. El volumen bruto es `RiskAmount / perUnitRisk` y está normalizado al paso de volumen del instrumento. Se respetan las restricciones cambiarias mínimas y máximas. Cuando el volumen normalizado es menor que el tamaño mínimo comercializable, se utiliza el mínimo.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Tipo de vela y período de tiempo utilizados para crear el canal Donchian. | plazo de 15 minutos |
| `LookbackPeriod` | Número de velas utilizadas para calcular el máximo más alto y el mínimo más bajo. | 200 |
| `RiskPercent` | Porcentaje del capital de la cartera arriesgado por operación. | 1% |

Todos los parámetros admiten la optimización a través del optimizador incorporado.

## Notas adicionales
* La estrategia solo opera una posición a la vez, replicando la guardia `PositionsTotal()>0` de la versión MQL.
* Los precios de stop-loss y take-profit se mantienen internamente en lugar de enviar órdenes separadas, lo que mantiene la lógica cercana al Asesor Experto original sin dejar de ser compatible con el nivel alto API.
* Cuando falta información sobre el capital de la cartera o el volumen del instrumento, la estrategia aún se negocia utilizando el volumen más pequeño posible para mantener el comportamiento determinista.
