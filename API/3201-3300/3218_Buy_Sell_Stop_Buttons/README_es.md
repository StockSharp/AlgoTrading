# Estrategia de Buy Sell Stop Buttons
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Recrea el asesor experto "Buy Sell Stop Buttons" de MetaTrader 4 dentro de StockSharp.
- Proporciona tres parámetros manuales (`BuyRequest`, `SellRequest`, `CloseRequest`) que emulan los botones del gráfico.
- Implementa los mismos asistentes de gestión monetaria: take-profit de dinero fijo, take-profit porcentual, bloqueo de trailing de capital, break-even y trailing stops en pips.
- Usa una suscripción de vela de un minuto puramente como latido para evaluar las reglas de gestión en barras terminadas.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `OrderLots` | Tamaño de lote base utilizado cuando se solicita una entrada manual. Refleja la entrada extern `Lots` (`0.01` por defecto). |
| `NumberOfTrades` | Número de tickets despachados por solicitud. El puerto C# consolida el volumen en una sola orden a mercado. |
| `UseTakeProfitInMoney` / `TakeProfitInMoney` | Habilitar y configurar el objetivo monetario directo que cierra todas las operaciones al alcanzarse. |
| `UseTakeProfitPercent` / `TakeProfitPercent` | Habilitar y configurar el objetivo de porcentaje de capital. La estrategia usa `Portfolio.CurrentValue` para aproximar el saldo de la cuenta. |
| `EnableTrailing`, `TrailingProfitMoney`, `TrailingLossMoney` | Configurar el bloque de trailing de capital: una vez que el beneficio supera `TrailingProfitMoney`, se rastrea el máximo y todas las operaciones se cierran si el beneficio retrocede `TrailingLossMoney`. |
| `UseBreakEven`, `BreakEvenTriggerPips`, `BreakEvenOffsetPips` | Mover el stop a break-even más offset después de que la posición gane la distancia de pips configurada. |
| `StopLossPips`, `TakeProfitPips`, `TrailingStopPips` | Configuración de gestión de tickets convertida a distancias en pips en StockSharp. |
| `CandleType` | Serie de velas que impulsa el latido (por defecto velas de un minuto). |
| `BuyRequest`, `SellRequest`, `CloseRequest` | Comandos manuales que reemplazan los botones originales del gráfico. Las banderas se restablecen automáticamente después de que la acción se realiza correctamente. |

## Lógica de trading
1. `OnStarted` se suscribe a la serie de velas configurada, establece el `Volume` base y habilita la protección de posición integrada.
2. Cada vela terminada activa el siguiente flujo de trabajo:
   - Los comandos manuales se evalúan: compra y venta envían una orden a mercado con volumen `OrderLots * NumberOfTrades`, opcionalmente compensando una posición opuesta; las solicitudes de cierre aplanan la estrategia.
   - Los objetivos monetarios se verifican en orden: importe fijo, porcentaje de capital, luego el bloqueo de trailing de capital.
   - Los stops de break-even y pip trailing ajustan los niveles de stop internos basándose en el precio de entrada promedio.
   - Se aplican las distancias estáticas de stop-loss/take-profit.
   - La salida opcional de Bandas de Bollinger cierra posiciones largas que tocan la banda superior o posiciones cortas que tocan la banda inferior (20 períodos, ancho 2).
3. El beneficio abierto se calcula con `Security.PriceStep`/`Security.StepPrice` cuando está disponible; de lo contrario, se usa una diferencia de precio de respaldo.

## Diferencias de la versión MQL
- MetaTrader permitía posiciones con cobertura; StockSharp consolida la exposición, por lo que las solicitudes de compra/venta primero neutralizan las posiciones opuestas.
- Las salidas basadas en MACD mensual (`Close_BUY`/`Close_SELL`) no están presentes porque nunca fueron llamadas en el script original.
- El escalado automático de volumen mediante `MaximumRisk`/`DecreaseFactor` se reemplaza por el parámetro explícito `OrderLots`. El asistente MQL dependía del historial de cuenta que no está disponible en este puerto.
- Los ajustes de stop se conducen por velas terminadas en lugar de ticks sin procesar, siguiendo las directrices del repositorio.
- Los valores de los indicadores se procesan a través de `Bind`, evitando colecciones directas o buffers de historial manuales.

## Notas de uso
- Mantener `BuyRequest`, `SellRequest` y `CloseRequest` bajo el grupo "Controles manuales" deshabilitados al ejecutar optimizaciones.
- La lógica de trailing de capital y take-profit monetario requieren `Security.StepPrice` para traducir el beneficio en moneda. Cuando no está disponible, el respaldo usa diferencias de precio puras.
- Break-even y trailing stops usan el tamaño de pip del instrumento inferido de `MinPriceStep`/`PriceStep` y dígitos decimales.
- No hay traducción a Python, como se solicitó.

## Pruebas
- No se modificaron pruebas automatizadas; la estrategia se integra con la estructura de solución existente y se basa en alternancia manual de parámetros para verificación.
