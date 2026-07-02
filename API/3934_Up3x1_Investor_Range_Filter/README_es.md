# Estrategia de filtro de rango de inversores Up3x1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una adaptación directa del asesor experto MetaTrader 4 **up3x1_Investor**. Opera con un solo instrumento utilizando velas completadas desde un período de tiempo configurable (H1 por defecto). El puerto replica la lógica original con StockSharp API de alto nivel y agrega parámetros claros de gestión de riesgos.

## Lógica de trading
- La estrategia evalúa la última vela completamente cerrada y comprueba que:
  - El rango de velas (máximo menos mínimo) supera las `0.0060` unidades de precio.
  - El cuerpo de la vela (diferencia absoluta entre apertura y cierre) supera las `0.0050` unidades de precio.
- Si la vela cerró alcista y se cumplen las condiciones anteriores, la estrategia abre una posición de mercado **larga**.
- Si la vela cerró bajista y se cumplen las condiciones, la estrategia abre una posición de mercado **corta**.
- El comercio está completamente deshabilitado los lunes (para reflejar la protección `DayOfWeek()==1` del código MQL).

## Gestión de Puestos
- Al ingresar, la estrategia establece objetivos internos utilizando las distancias configuradas basadas en pasos:
  - `TakeProfitPoints` → distancia al objetivo de ganancias.
  - `StopLossPoints` → distancia de parada de protección.
  - `TrailingStopPoints` → distancia utilizada para rastrear el stop una vez que el precio se mueve a favor.
- Las paradas y los objetivos se evalúan en cada vela terminada:
  - Si el precio alcanza el objetivo, la posición se cierra al precio objetivo.
  - Si el precio alcanza el tope, la posición se cierra para limitar la pérdida.
  - Una vez que el precio avanza más allá de la distancia de seguimiento, el stop se acerca al precio de mercado para asegurar las ganancias.
- Además, si las medias móviles simples de 24 y 60 períodos calculadas sobre las mismas velas se igualan (dentro de un paso del precio), la posición se cierra inmediatamente. Esto imita la lógica MQL donde la orden se cierra cuando ambos promedios coinciden exactamente.

## Gestión de volumen y riesgos
- `BaseVolume` define el tamaño del lote alternativo cuando no se puede calcular ningún ajuste basado en la cuenta.
- `MaximumRisk` replica la fórmula original `AccountFreeMargin()*MaximumRisk/1000`. Si el valor de la cartera está disponible, la estrategia dimensiona la posición como `value * MaximumRisk / 1000`, redondeada a un decimal.
- `DecreaseFactor` imita la reducción de la racha de pérdidas: después de más de una pérdida consecutiva, el volumen disminuye proporcionalmente a `losses / DecreaseFactor`.
- `MinimumVolume` garantiza que el volumen nunca caiga por debajo del tamaño negociable más pequeño utilizado en el script MQL (0,1 lotes).

## Parámetros
| Nombre | Predeterminado | Descripción |
| ---- | ------- | ----------- |
| `BaseVolume` | `0.1` | Tamaño de la posición base en lotes cuando no se aplica ningún ajuste de riesgo. |
| `MaximumRisk` | `0.2` | Factor de riesgo utilizado para derivar el volumen del capital de la cuenta (igual que el EA original). |
| `DecreaseFactor` | `3` | Reduce el tamaño de la posición después de pérdidas consecutivas. |
| `MinimumVolume` | `0.1` | Volumen mínimo permitido. |
| `TakeProfitPoints` | `20` | Distancia objetivo de beneficio medida en pasos de precio. |
| `StopLossPoints` | `50` | Distancia de stop-loss medida en pasos de precio. |
| `TrailingStopPoints` | `10` | Distancia del trailing stop medida en incrementos de precio. |
| `SkipMondays` | `true` | Desactive toda la actividad comercial los lunes. |
| `CandleType` | `1 hour` | Plazo de suscripción de velas. |

## Notas
- La estrategia solo mantiene una posición abierta a la vez, igualando la guardia original `CalculateCurrentOrders`.
- El seguimiento de pérdidas consecutivas es puramente interno porque los corredores StockSharp no exponen el historial de pedidos MetaTrader.
- No se utilizan órdenes pendientes; todas las operaciones se envían como órdenes de mercado a través de `BuyMarket` y `SellMarket`.
