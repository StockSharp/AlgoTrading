# Estrategia PROfeta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una versión StockSharp del MetaTrader 4 asesor experto "PROphet". El EA original evalúa la ejecución comercial reciente
recorre cuatro velas históricas y utiliza esos rangos ponderados para activar nuevas operaciones. Mantiene posiciones abiertas sólo entre los
sesiones europeas y estadounidenses y sigue el stop-loss cada vez que el precio se mueve una distancia fija a favor de la operación. El StockSharp
La implementación mantiene todos esos mecanismos mientras los adapta al modelo de compensación utilizado por las carteras StockSharp.

## Lógica comercial
- Suscríbase al período de tiempo configurado (`CandleType`, predeterminado M5) y procese solo velas terminadas.
- Mantenga las tres velas completadas más recientes para reproducir la indexación `High[i]` y `Low[i]` utilizada por la versión MQL.
- Calcule el disparador largo `Qu(X1, X2, X3, X4)` y el disparador corto `Qu(Y1, Y2, Y3, Y4)` en cada barra. Cada término multiplica un
rango ponderado (por ejemplo `|High[1] - Low[2]|`) por el peso correspondiente menos cien, exactamente como en el código original.
- Permita nuevas entradas solo cuando la hora actual caiga entre `TradeStartHour` y `TradeEndHour` (inclusive). Esto imita al hombre.
ventana de negociación dual del experto MQL (de 10:00 a 18:00 de forma predeterminada).
- Utilice una orden de mercado única cuyo volumen neutralice cualquier exposición opuesta antes de abrir la nueva posición. Esto refleja el Mag
ic Número de filtros de la implementación MetaTrader.

## Gestión de riesgos y seguimiento
- La estrategia convierte las MetaTrader distancias de parada basadas en puntos en unidades de precio a través del instrumento `PriceStep`. Los valores predeterminados (`B
uyStopLossPoints = 68`, `SellStopLossPoints = 72`) coinciden con las MQL variables externas.
- Una vez que la oferta (para operaciones largas) o la demanda (para operaciones cortas) supera el límite existente en `spread + 2 * stopDistance`,
El trailing stop avanza hasta `currentPrice ± stopDistance`, utilizando datos de Nivel 1 en vivo cuando estén disponibles.
- Las operaciones abiertas se cierran a la fuerza después de `ExitHour`. El valor predeterminado (18) reproduce el comportamiento original de cerrar la posición.
s después de las 18:00 hora del servidor.
- Las salidas protectoras utilizan órdenes de mercado porque el nivel alto de StockSharp API no genera automáticamente órdenes de parada. Esto mantiene
comportamiento determinista entre corredores.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `AllowBuy` | Permite operaciones largas. |
| `AllowSell` | Permite operaciones cortas. |
| `X1`, `X2`, `X3`, `X4` | Pesos aplicados a los componentes del rango del lado largo dentro de la fórmula `Qu`. |
| `BuyStopLossPoints` | Distancia de stop-loss para operaciones largas expresada en MetaTrader puntos. |
| `Y1`, `Y2`, `Y3`, `Y4` | Pesos aplicados a los componentes del rango del lado corto dentro de la fórmula `Qu`. |
| `SellStopLossPoints` | Distancia de stop-loss para operaciones cortas expresada en MetaTrader puntos. |
| `TradeVolume` | Volumen base (lotes) utilizado para nuevas entradas. Se agrega volumen adicional automáticamente para cerrar la exposición opuesta. |
| `TradeStartHour` | Primera hora de la ventana de negociación (inclusive). |
| `TradeEndHour` | Última hora de la ventana de negociación (inclusive). |
| `ExitHour` | Hora tras la cual se cierran todas las operaciones abiertas. |
| `CandleType` | Periodo de tiempo de las velas utilizadas para el análisis. |

## Notas
- Las carteras StockSharp se compensan de forma predeterminada. Cuando aparece una nueva señal, la estrategia agrega el volumen necesario para aplanar el ex.
posición actual antes de abrir la nueva operación, que reproduce el diseño de posición única por dirección de la experiencia MetaTrader
rt.
- El script MQL utilizó la extensión de símbolo informada por `MarketInfo`. El puerto recupera el diferencial de los datos de Nivel 1 cuando están disponibles
y, de lo contrario, vuelve a caer a un único escalón de precio.
- Debido a que el trailing stop se evalúa al cierre de cada vela terminada, puede ocurrir un deslizamiento en comparación con el stop a nivel de tick.
actualizaciones realizadas por el EA original.
