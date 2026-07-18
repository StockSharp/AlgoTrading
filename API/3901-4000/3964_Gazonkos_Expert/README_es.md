# Estrategia experta de Gazonkos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una StockSharp versión del MetaTrader 4 asesores expertos "gazonkos expert" que fue diseñado para el gráfico EUR/USD H1. El EA espera un fuerte movimiento de impulso de una hora y luego ingresa en la dirección de ese movimiento después de un retroceso configurable. Los niveles protectores de stop loss y takeprofit se aplican como distancias fijas medidas en pips.

## Lógica original MQL4
- El EA monitorea continuamente la diferencia entre dos cierres históricos (`Close[t2] - Close[t1]`). Los valores predeterminados son `t1 = 3` y `t2 = 2`, que corresponden a los cierres de las velas que terminaron hace dos y tres horas.
- Se detecta un impulso alcista cuando `Close[t2] - Close[t1]` supera los `delta` puntos. Se detecta un impulso bajista cuando `Close[t1] - Close[t2]` supera el mismo umbral.
- Una vez que se detecta un impulso, el EA registra la oferta más alta (para alcistas) o más baja (para bajistas) que se produce antes de que comience la siguiente hora. Si el precio retrocede `Otkat` puntos desde ese extremo en la misma hora, se envía una orden de mercado en la dirección de impulso.
- Las operaciones se bloquean cuando ya existe una posición abierta con el mismo número mágico o cuando ya se abrió una operación durante la hora actual.
- Cada orden se envía con una distancia fija de takeprofit (`TakeProfit`) y stop loss (`StopLoss`) expresada en puntos.

## Máquina de estados en la versión C#
La implementación StockSharp recrea la máquina de estado original:
1. **WaitingForSlot**: verifica que no se haya abierto ninguna operación reciente en la hora actual y que no se haya alcanzado el número máximo configurado de operaciones simultáneas.
2. **WaitingForImpulse**: verifica los cierres históricos para detectar impulsos alcistas o bajistas.
3. **MonitoringRetracement**: realiza un seguimiento de los máximos y mínimos de las velas después del impulso y espera un retroceso de `RetracementPips` (el antiguo parámetro `Otkat`) dentro de la misma hora.
4. **AwaitingExecution**: envía una orden de mercado en la dirección de impulso e inmediatamente aplica niveles protectores de stop-loss y take-profit calculados a partir del instrumento `PriceStep`.

La estrategia solo procesa velas terminadas del período de tiempo configurado e ignora los datos no terminados, reflejando cómo el EA original evaluó las condiciones en las barras horarias cerradas.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `TakeProfitPips` | Distancia entre el precio de entrada y el nivel de toma de ganancias. |
| `RetracementPips` | Se requiere retroceso desde el extremo del impulso antes de entrar. |
| `StopLossPips` | Distancia entre el precio de entrada y el stop de protección. |
| `T1Shift` | Índice del cierre de referencia más antiguo utilizado para la detección de impulsos (predeterminado 3). |
| `T2Shift` | Índice del cierre de referencia más nuevo utilizado para la detección de impulsos (predeterminado 2). |
| `DeltaPips` | Distancia mínima de impulso que debe separar los dos cierres de referencia. |
| `LotSize` | Volumen fijo de cada pedido. |
| `MaxActiveTrades` | Número máximo de operaciones simultáneas; Los valores superiores a uno requieren que la cuenta del corredor admita posiciones netas aditivas. |
| `CandleType` | Marco de tiempo de las velas utilizadas para evaluar las reglas comerciales (el valor predeterminado es 1 hora). |

Todas las distancias basadas en pips se convierten en compensaciones de precios usando `Security.PriceStep`. Cuando el instrumento no tiene información de paso de precio, se utiliza un valor predeterminado de 0,0001, que coincide con la configuración EUR/USD original.

## Notas de implementación
- La estrategia funciona con la suscripción de vela de alto nivel de StockSharp API (`SubscribeCandles().Bind`).
- Los precios cerrados se almacenan en caché en un búfer móvil ligero para emular las búsquedas `Close[i]` de la versión MQL4.
- Después de ejecutar una operación, la estrategia registra la hora de la vela y bloquea nuevas entradas hasta la hora siguiente, reproduciendo la protección `LastTradeTime` original.
- `MaxActiveTrades` se interpreta en función de la posición neta actual. En la compensación de cuentas, esto limita efectivamente el sistema a una única operación abierta, que coincide con el comportamiento predeterminado del experto MQL4.
- Los comentarios dentro del código describen la máquina de estado de C# en inglés para facilitar el mantenimiento.
