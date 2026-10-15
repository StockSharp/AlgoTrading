# Estrategia de Open Close
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Open Close es un port del asesor experto MetaTrader 5 `Open Close.mq5` (ticket 23090). La estrategia observa la relación entre las aperturas y cierres de las dos velas finalizadas más recientes. Opera una sola posición a la vez: cuando la vela más nueva se revierte en relación con la anterior, la estrategia entra; y cuando ambas velas apuntan en la misma dirección, sale. La versión en C# reproduce el dimensionamiento adaptativo de lotes original que reduce la exposición tras una racha de operaciones perdedoras.

## Lógica de la estrategia
### Filtro de patrón de velas
* La estrategia trabaja exclusivamente con velas completadas suministradas por el parámetro configurable `CandleType`.
* Mantiene una ventana deslizante de las dos últimas velas finalizadas (denominadas `previous` y `older`).
* El patrón compara tanto las aperturas como los cierres de estas velas:
  * **Reversión alcista** – `previous.Open > older.Open` **y** `previous.Close < older.Close`.
  * **Reversión bajista** – `previous.Open < older.Open` **y** `previous.Close > older.Close`.

### Reglas de entrada
* Si no hay posición abierta y aparece el patrón de reversión alcista, la estrategia envía una orden de compra de mercado.
* Si no hay posición abierta y aparece el patrón de reversión bajista, envía una orden de venta de mercado.
* Solo se permite una posición. Las señales opuestas se ignoran hasta que se cierra la operación activa.

### Reglas de salida
* Cuando se mantiene una posición larga, la estrategia sale si las dos velas rastreadas se mueven más bajas (`previous.Open < older.Open` y `previous.Close < older.Close`).
* Cuando se mantiene una posición corta, el desencadenante de salida es simétrico (`previous.Open > older.Open` y `previous.Close > older.Close`).
* No hay órdenes de stop-loss o take-profit en el asesor original, por lo que el port depende únicamente de la relación de velas para cerrar operaciones.

### Dimensionamiento de posición y manejo de racha de pérdidas
* El volumen de la orden se determina principalmente por `MaximumRiskPercent` – la fracción deseada del valor de la cartera invertida por operación. El tamaño bruto es `Portfolio.CurrentValue × MaximumRiskPercent ÷ referencePrice` usando el último cierre como proxy de precio.
* Si la valoración de la cartera o el precio no están disponibles, el parámetro `FallbackVolume` actúa como valor predeterminado seguro.
* Tras cada operación completamente cerrada, el PnL realizado se almacena. La racha de pérdidas consecutivas se cuenta durante los últimos `HistoryDays` días.
  * Cuando la racha es mayor a una operación, el tamaño de la siguiente orden se reduce en `volume × losses ÷ DecreaseFactor`, imitando la lógica de MT5.
* El volumen final respeta el paso de volumen del instrumento así como los límites mínimos y máximos de volumen.

### Notas adicionales de implementación
* La estrategia reacciona solo a `CandleStates.Finished`, asegurando que el patrón use datos de mercado completos.
* Las comprobaciones de entrada y salida ocurren al cierre de la vela más nueva. En MetaTrader, la orden se envía a la apertura de la siguiente barra; la diferencia es insignificante para marcos temporales más altos pero debe considerarse para intervalos muy cortos.
* Las métricas de cartera en StockSharp aproximan la información de cuenta de MetaTrader. Ajuste `MaximumRiskPercent` o `FallbackVolume` si el broker usa multiplicadores de contrato diferentes.

## Parámetros
| Parámetro | Tipo | Predeterminado | Descripción |
|-----------|------|----------------|-------------|
| `MaximumRiskPercent` | `decimal` | `0.02` | Fracción del valor de cartera usada para dimensionar una nueva posición (0.02 = 2%). |
| `DecreaseFactor` | `decimal` | `3` | Divisor aplicado al tamaño del lote tras operaciones perdedoras consecutivas. Valores mayores suavizan la reducción. |
| `HistoryDays` | `int` | `60` | Número de días de calendario escaneados al contar la última racha de pérdidas. |
| `FallbackVolume` | `decimal` | `0.1` | Volumen de orden usado cuando no se puede realizar el cálculo basado en riesgo. |
| `CandleType` | `DataType` | `TimeFrame(15m)` | Serie de velas que proporciona los valores de apertura/cierre para la generación de señales. |

## Diferencias con la versión MetaTrader
* Las comprobaciones de margen de cuenta dependen del `Portfolio.CurrentValue` de StockSharp; MetaTrader usaba `AccountFreeMargin`. El comportamiento coincide con la regla de riesgo original solo cuando ambas plataformas informan valoraciones similares.
* El historial de operaciones se recopila de las propias ejecuciones de la estrategia en lugar del historial de toda la terminal. Asegúrese de que la estrategia funcione suficiente tiempo para acumular estadísticas de racha.
* El port mantiene el modelo de posición única (sin pirámide) y refleja la falta original de órdenes protectoras. Agregue stops externamente si es necesario para el control de riesgo.
