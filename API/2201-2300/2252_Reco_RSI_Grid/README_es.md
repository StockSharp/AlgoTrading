# Estrategia de Cuadrícula RSI Reco
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia reproduce el comportamiento del asesor experto original "Reco" de MetaTrader utilizando la API de alto nivel de StockSharp. El algoritmo abre una posición inicial basada en el Índice de Fuerza Relativa (RSI) y luego coloca posiciones contrarias formando una cuadrícula. La distancia entre órdenes de cuadrícula y su volumen crecen geométricamente. Todas las posiciones abiertas se cierran juntas cuando el beneficio o pérdida acumulada alcanza umbrales predefinidos.

## Lógica de trading
- **Señal inicial** – el RSI supera las zonas de sobrecompra o sobreventa configuradas. Se abre una posición corta cuando el RSI está por encima del nivel de venta y una posición larga cuando está por debajo del nivel de compra.
- **Expansión de la cuadrícula** – tras la primera orden, la estrategia observa el movimiento del precio respecto a la última operación. Cuando el precio se mueve una distancia calculada, se envía una orden de mercado opuesta. La distancia aumenta con el *Distance Multiplier* en cada nuevo paso y puede estar limitada por *Max Distance* y *Min Distance*.
- **Escala de volumen** – el tamaño de cada nueva orden es igual al *Lot* inicial multiplicado por *Lot Multiplier* elevado al número de órdenes ya abiertas. También se admiten límites de volumen máximo y mínimo.
- **Reglas de salida** – si *Use Close Profit* está habilitado, todas las posiciones se cierran cuando el beneficio agregado es mayor que *Profit First Order* multiplicado por *Profit Multiplier* para cada orden adicional. Si *Use Close Lose* está habilitado, la misma lógica se aplica a las pérdidas usando *Lose First Order* y *Lose Multiplier*.

## Parámetros
| Nombre | Descripción |
|--------|-------------|
| `RsiPeriod` | Período del indicador RSI. |
| `RsiSellZone` | Nivel de RSI que desencadena una señal de venta. |
| `RsiBuyZone` | Nivel de RSI que desencadena una señal de compra. |
| `StartDistance` | Distancia inicial desde la última orden expresada en puntos. |
| `DistanceMultiplier` | Multiplicador aplicado a la distancia para cada orden adicional. |
| `MaxDistance` | Límite superior para el crecimiento de la distancia (0 deshabilita). |
| `MinDistance` | Límite inferior para el crecimiento de la distancia (0 deshabilita). |
| `MaxOrders` | Número máximo de órdenes abiertas simultáneamente (0 significa sin límite). |
| `Lot` | Volumen base de la orden. |
| `LotMultiplier` | Multiplicador para el escalado de volumen. |
| `MaxLot` | Volumen máximo permitido por orden (0 deshabilita). |
| `MinLot` | Volumen mínimo permitido por orden (0 deshabilita). |
| `UseCloseProfit` | Habilitar el cierre de todas las posiciones por objetivo de beneficio. |
| `ProfitFirstOrder` | Objetivo de beneficio para la primera orden. |
| `ProfitMultiplier` | Multiplicador de beneficio para órdenes posteriores. |
| `UseCloseLose` | Habilitar el cierre de todas las posiciones por umbral de pérdida. |
| `LoseFirstOrder` | Umbral de pérdida para la primera orden. |
| `LoseMultiplier` | Multiplicador de pérdida para órdenes posteriores. |
| `PointMultiplier` | Multiplicador aplicado al paso de precio del instrumento para calcular un punto. |
| `CandleType` | Tipo de velas utilizadas para los cálculos del indicador. |

## Notas
- La estrategia trabaja con órdenes de mercado y asume ejecución inmediata.
- Las posiciones se netean: abrir una orden opuesta puede reducir o revertir la posición actual.
- La estrategia usa tabulaciones para sangría y comentarios en inglés según las convenciones del proyecto.
