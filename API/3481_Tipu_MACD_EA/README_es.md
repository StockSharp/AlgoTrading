# Tipu MACD EA Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia es un puerto StockSharp de alto nivel del **Tipu MACD EA** de MQL4. Opera con un solo símbolo utilizando señales basadas en MACD y refleja las características originales del asesor experto:

* Filtro de horario de negociación opcional con dos ventanas horarias configurables.
* MACD entradas de cruce de línea cero y línea de señal con longitudes y desplazamiento EMA ajustables.
* Gestión automática de posiciones que incluye toma de ganancias, stop-loss, trailing stop y punto de equilibrio.
* Limitación de volumen que emula la configuración de "lotes máximos" del código fuente.

Todas las operaciones utilizan órdenes de mercado. Los niveles de protección se rastrean internamente y las órdenes se cierran una vez que una vela atraviesa los niveles de stop-loss o take-profit.

## Lógica comercial
1. Suscríbase al tipo de vela configurado y calcule un indicador `MovingAverageConvergenceDivergenceSignal` (MACD línea + línea de señal).
2. Evalúe los valores de MACD usando el cambio seleccionado (`MacdShift` 0 = vela actual, 1 = vela anterior) y cree señales cruzadas:
   * **Cruce de línea cero** (opcional): compre cuando MACD cruce por encima de cero, venda cuando cruce por debajo.
   * **Cruce de línea de señal** (opcional): compre cuando MACD cruce por encima de la línea de señal, venda cuando cruce por debajo.
3. Antes de abrir una posición, asegúrese de que la hora actual pertenezca al menos a una de las dos ventanas horarias cuando el filtro está habilitado.
4. Cuando aparece una señal larga:
   * Si la cobertura está deshabilitada y hay una posición corta abierta, ciérrela opcionalmente (`CloseOnReverseSignal`) u omita la nueva operación.
   * Realice una orden de compra de mercado por el menor de `TradeVolume` y el volumen restante hasta alcanzar `MaxPositionVolume`.
   * Actualice la instantánea de entrada larga y calcule los niveles de parada/toma de protección si está habilitado.
5. Cuando aparezca una señal corta, siga la lógica simétrica para las órdenes de venta.
6. Mientras una posición está activa:
   * Supervise las paradas y los objetivos en cada vela terminada y cierre la operación si se supera cualquiera de los niveles.
   * Cuando el seguimiento está habilitado y el precio avanza `TrailingPips + TrailingCushionPips`, mueva el stop para mantener una distancia de `TrailingPips` del precio.
   * Cuando el módulo de equilibrio esté activo y la ganancia supere `RiskFreePips`, mueva el stop al precio de entrada.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `CandleType` | Serie de velas utilizada para los cálculos de MACD. |
| `TradeVolume` | Volumen de cada entrada al mercado (lotes). |
| `MaxPositionVolume` | Máxima exposición acumulada larga o corta permitida. |
| `UseTimeFilter` | Habilita el filtro de horas de negociación de ventana dual. |
| `Zone1StartHour`, `Zone1EndHour` | Horas de inicio/finalización de la primera ventana de negociación (inclusive, hora de intercambio). |
| `Zone2StartHour`, `Zone2EndHour` | Horas de inicio/finalización de la segunda ventana de negociación. |
| `FastPeriod`, `SlowPeriod`, `SignalPeriod` | MACD EMA rápida, EMA lenta y señal de SMA longitudes. |
| `MacdShift` | 0 = evaluar la barra actual, 1 = evaluar la barra anterior (que coincide con MQL `iShift`). |
| `UseZeroCross` | Habilita MACD entradas cruzadas de línea cero. |
| `UseSignalCross` | Habilita MACD frente a entradas cruzadas de líneas de señal. |
| `AllowHedging` | Permite construir exposiciones tanto largas como cortas sin cerrar primero el lado opuesto. |
| `CloseOnReverseSignal` | Cierra la posición opuesta cuando aparece una nueva señal (se utiliza cuando la cobertura está desactivada). |
| `UseTakeProfit`, `TakeProfitPips` | Habilita y configura la distancia de toma de ganancias (pips). |
| `UseStopLoss`, `StopLossPips` | Habilita y configura la distancia de stop-loss (pips). |
| `UseTrailingStop`, `TrailingPips`, `TrailingCushionPips` | Permite la gestión de seguimiento, establece la distancia de seguimiento y la amortiguación (pips). |
| `UseRiskFree`, `RiskFreePips` | Mueve el stop al punto de equilibrio una vez que la ganancia excede los pips especificados. |

## Notas de uso
* Configure el tipo de vela para que coincida con el período de tiempo utilizado en MetaTrader (barras predeterminadas de 15 minutos).
* El tamaño del pip se deriva de `Security.PriceStep`. Si el instrumento carece de estos metadatos, se utiliza un valor predeterminado de 0,0001.
* La estrategia supone la ejecución inmediata de las órdenes de mercado. Cuando funcione en vivo, asegúrese de manejar adecuadamente el deslizamiento si es necesario.
* Cuando se desactivan las entradas de línea cero y de línea de señal, la estrategia permanece inactiva.
