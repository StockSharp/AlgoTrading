# Bollinger RSI Estrategia de MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia MA Bollinger RSI transfiere el experto MetaTrader *BolRSIMAs* al API de alto nivel de StockSharp. El sistema combina un
Bollinger Ruptura de banda, un filtro RSI y un promedio móvil exponencial de período de tiempo más alto (EMA) para identificar operaciones de retroceso en
la dirección de la tendencia dominante. Se conserva el tamaño del lote automático: cuando está habilitada, la estrategia convierte el riesgo configurado
fracción del capital de la cartera en volumen utilizando el precio actual, la distancia de parada Bollinger y el tamaño del contrato del instrumento.

## Lógica comercial
1. Suscríbase a la serie de velas principal (predeterminada: 1 hora) y calcule Bollinger Bandas y RSI en el mismo período de tiempo.
2. Suscríbase a velas diarias e introduzca sus precios de cierre en un EMA de 200 períodos para reproducir el filtro de marco temporal más alto utilizado.
en el original EA.
3. Genere una configuración **larga** cuando la última vela cierre por debajo de la banda inferior, el valor RSI esté por debajo del umbral de sobreventa
y el cierre se mantiene por encima del EMA diario. Una configuración **corta** se desencadena por un cierre por encima de la banda superior, RSI por encima de la
umbral de sobrecompra y precio por debajo del EMA diario.
4. Abra posiciones solo cuando no haya ninguna exposición activa. Cada nueva operación almacena niveles de stop-loss y take-profit derivados de la
valores anteriores de Bollinger: los largos usan `lowerBand - StopLossOffset` y apuntan a la banda media; uso de pantalones cortos
`upperBand + StopLossOffset` y apuntar también a la banda media.
5. En cada vela terminada, la estrategia compara los extremos de la vela con los niveles de protección. Si el bajo/alto toca el
stop o target, la posición se cierra inmediatamente, emulando las órdenes de protección colocadas por la versión MetaTrader.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `CandleType` | velas de 1 hora | Periodo de tiempo principal procesado por Bollinger Bandas y RSI. |
| `DailyCandleType` | velas de 1 dia | Periodo de tiempo más alto que alimenta el filtro de tendencias EMA. |
| `BollingerPeriod` | `20` | Número de velas utilizadas para construir Bollinger Bandas. |
| `BollingerDeviation` | `2` | Multiplicador de ancho de banda. |
| `RsiPeriod` | `13` | RSI longitud de suavizado. |
| `RsiUpperLevel` | `70` | Umbral de sobrecompra requerido para operaciones cortas. |
| `RsiLowerLevel` | `30` | Umbral de sobreventa requerido para operaciones largas. |
| `MaPeriod` | `200` | Duración del plazo superior EMA. |
| `StopLossOffset` | `0.0238` | Se agregó un buffer adicional fuera de la banda antes de colocar el stop-loss. |
| `UseAutoLot` | `true` | Permite dimensionar las posiciones en función del riesgo. |
| `RiskPerTrade` | `0.05` | Fracción del capital asignado a cada operación cuando el lote automático está activo. |
| `FixedVolume` | `0.1` | Tamaño del pedido cuando el tamaño de lote automático está deshabilitado. |

## gestión del dinero
- Cuando `UseAutoLot` es `true`, el volumen es igual a `(equity * RiskPerTrade) / (StopLossOffset * price * contractSize)` redondeado al
límites de cambio. Esto refleja la rutina de autolot MetaTrader, que divide el monto del riesgo por la distancia de parada en efectivo y
el tamaño del contrato.
- Si la información sobre el capital o el precio no están disponibles, la estrategia vuelve a `FixedVolume` respetando al mismo tiempo el
Restricciones de volumen del instrumento.

## Diferencias con el experto MetaTrader
- Las órdenes de stop-loss y take-profit se simulan a través de máximos y mínimos de velas en lugar de órdenes del lado del servidor, coincidiendo con el
resultado del EA original sin depender del envío sincrónico del pedido.
- El filtro EMA utiliza las suscripciones de velas de StockSharp; no hay dependencia de llamadas de datos diarias específicas de MetaTrader.
- El tamaño del riesgo respeta los límites de seguridad StockSharp (`MinVolume`, `MaxVolume`, `VolumeStep`) para evitar pedidos rechazados en los intercambios.

## Consejos de uso
- Ajuste `StopLossOffset` al intercambiar símbolos con diferentes escalas de precios para que la distancia refleje los EA originales
Amortiguador del 2,38 % más allá de la banda Bollinger.
- Si el instrumento utiliza un período de tiempo diario diferente (por ejemplo, intercambios de cifrado), cambie `DailyCandleType` en consecuencia para que EMA
refleja el filtro de tendencia previsto.
- Combine la estrategia con trailingstops externos si prefiere salidas dinámicas una vez que se alcance el objetivo de la banda media.
