# Maestría en estrategia de beneficio mínimo Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia replica el asesor experto MetaTrader 5 **MA en Momentum Min Profit.mq5** al negociar el cruce entre un indicador Momentum y un promedio móvil que se calcula sobre la serie de impulso. Aparece una señal alcista cuando el impulso cruza por encima de su promedio mientras que la barra anterior mantuvo el impulso por debajo del nivel neutral 100. Se genera una señal bajista cuando el impulso cruza por debajo del promedio con la barra anterior por encima de 100. La implementación mantiene el stop de acciones basado en dinero original y la distancia fija de obtención de beneficios medida en puntos.

## Lógica comercial
1. Solicite velas definidas por `CandleType` e introdúzcalas en el indicador Momentum.
2. Suaviza el flujo de impulso con una media móvil definida por `MomentumMovingAverageType` y `MomentumMovingAveragePeriod`.
3. Detecta cruces utilizando los valores de las barras anteriores para evitar señales dobles.
4. Funciones opcionales de la versión MQL:
   - Invertir la dirección de las señales generadas.
   - Cierre la exposición opuesta antes de iniciar una nueva operación u omita la entrada por completo.
   - Aplicar una única posición neta en cualquier momento.
   - Permita el disparo en la vela actual (en formación) en lugar de en la barra completamente cerrada.
5. Aplicar la gestión de riesgos:
   - Stop de equidad en dinero: `PnL + Position * (close - PositionPrice)` debe permanecer por encima de `StopLossMoney`.
   - Distancia de obtención de beneficios en puntos convertidos mediante `Security.PriceStep`.

## Parámetros
| Parámetro | Tipo | Predeterminado | Descripción |
|-----------|------|---------|-------------|
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(5).TimeFrame()` | Velas utilizadas para calcular el impulso. |
| `MomentumPeriod` | `int` | `14` | Período retrospectivo del indicador Momentum. |
| `MomentumMovingAveragePeriod` | `int` | `6` | Longitud de la media móvil aplicada al impulso. |
| `MomentumMovingAverageType` | `MomentumMovingAverageType` | `Smoothed` | Algoritmo de media móvil (Simple, Exponencial, Suavizado, Ponderado). |
| `ReverseSignals` | `bool` | `false` | Refleja señales de compra/venta de MetaTrader. |
| `CloseOpposite` | `bool` | `true` | Cierre la exposición opuesta antes de abrir una nueva posición. |
| `OnlyOnePosition` | `bool` | `true` | Mantener una única posición neta. |
| `UseCurrentCandle` | `bool` | `false` | Evalúe las señales en la vela que se está formando actualmente en lugar de en la barra cerrada. |
| `StopLossMoney` | `decimal` | `15` | Se permite la reducción de capital antes de cerrar todas las operaciones. |
| `TakeProfitPoints` | `decimal` | `460` | Objetivo de beneficio en puntos del instrumento (multiplicado por `PriceStep`). |
| `MomentumReference` | `decimal` | `100` | Nivel de impulso neutral copiado de la estrategia MQL. |

## Notas de implementación
- La media móvil se implementa con `LengthIndicator<decimal>` instancias para reutilizar StockSharp clases integradas SMA/EMA/SMMA/WMA.
- La cola de órdenes original y los filtros de números mágicos se asignan a StockSharp posiciones netas, por lo tanto, la estrategia envía una única orden de mercado de tamaño para aplanar el lado opuesto y abrir la nueva exposición cuando `CloseOpposite` está habilitado.
- La protección de acciones cierra todas las posiciones a través de `CloseAll()` una vez que la pérdida flotante supera el umbral, coincidiendo exactamente con el comportamiento de MetaTrader de monitorear la comisión, el swap y las ganancias combinados.
