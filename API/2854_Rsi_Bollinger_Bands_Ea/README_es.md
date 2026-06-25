# RSI Bollinger Bands EA (Conversión StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un porto de alto nivel de StockSharp del asesor experto MetaTrader 5 "RSI Bollinger Bands EA". Opera en velas de 15 minutos y combina dos disparadores independientes basados en RSI:

* **Disparador Uno** – umbrales fijos de sobrecompra/sobreventa para RSI en M15, H1 y H4 junto con una confirmación estocástica y un filtro de pendiente.
* **Disparador Dos** – bandas RSI adaptativas calculadas a partir de desviaciones estándar asimétricas (sigma positivo/negativo separado) sobre tamaños de muestra configurables en los tres marcos temporales. El RSI debe perforar las bandas dinámicas mientras el estocástico confirma el momentum.

Ambos disparadores requieren contracción de volatilidad en el marco temporal inferior (spread de Bollinger en M15), expansión de volatilidad en el marco temporal superior (spread de Bollinger en H4) y un entorno tranquilo según el ATR de H4. Solo un disparador puede estar habilitado a la vez, reflejando las restricciones del EA original.

## Requisitos de datos
* Velas de ejecución primaria: `M15CandleType` (predeterminado 15 minutos). Todas las entradas y salidas se evalúan al cierre de estas velas.
* Velas de confirmación: `H1CandleType` (predeterminado 1 hora) para condiciones RSI y estadísticas.
* Velas de marco temporal superior: `H4CandleType` (predeterminado 4 horas) para verificaciones RSI, filtro de spread de Bollinger y filtro de volatilidad ATR.

## Lógica de trading
1. **Filtros de sesión**
   * El trading está limitado a una ventana de tiempo configurable que comienza en `EntryHour` y dura `OpenHours` horas. Cuando `OpenHours` es cero, la ventana dura la hora de apertura única.
   * El trading se detiene los viernes una vez que la hora de la vela alcanza `FridayEndHour` (predeterminado 4, es decir, después de las 04:00 del viernes).
   * Una nueva posición solo puede abrirse cuando la posición neta actual es plana (`Position == 0`).

2. **Filtros de volatilidad y spread (ambos disparadores)**
   * El spread de Bollinger H4 debe ser mayor que `BbSpreadH4MinX` pips (X = 1 o 2) para asegurar que el rango del marco temporal superior sea lo suficientemente amplio.
   * El spread de Bollinger M15 debe permanecer por debajo de `BbSpreadM15MaxX` pips para que el precio esté comprimido en el marco temporal de trading.
   * El ATR H4 convertido a pips debe permanecer por debajo de `AtrLimit`.

3. **Disparador Uno – niveles RSI fijos**
   * Los valores RSI para M15, H1 y H4 deben caer por debajo de sus respectivos umbrales "Low" para desencadenar una configuración larga, mientras permanecen por encima de los fail-safes "Low Limit".
   * El RSI debe subir en relación a la lectura M15 anterior en más de `RDeltaM15Lim1` (predeterminado –3.5 pips) para largos, o caer más del umbral invertido para cortos.
   * La línea principal estocástica debe estar por debajo de `StocLoM15_1` para largos o por encima de `StocHiM15_1` para cortos.
   * Las entradas cortas requieren que los valores RSI estén por encima de sus umbrales "High" pero permanezcan por debajo de los fail-safes "High Limit".

4. **Disparador Dos – bandas sigma RSI adaptativas**
   * Las muestras RSI históricas se mantienen para cada marco temporal (hasta `NumRsi` elementos). Se calculan desviaciones estándar positivas y negativas separadas para construir bandas asimétricas similares a Bollinger.
   * Las bandas inferiores y superiores dinámicas para cada marco temporal se producen aplicando multiplicadores `Rsi*M*Sigma2` (M15/H1/H4). Multiplicadores "límite" adicionales (`Rsi*M*SigmaLim2`) limitan los extremos permitidos.
   * Las entradas largas requieren que los tres valores RSI estén por debajo de sus respectivas bandas inferiores adaptativas pero por encima de los límites inferiores. El estocástico debe estar por debajo de `StocLoM15_2` y la pendiente RSI debe ser mayor que `RDeltaM15Lim2`.
   * Las entradas cortas reflejan la lógica con bandas superiores y umbrales.

5. **Ejecución de órdenes y salidas**
   * Cuando un disparador se activa, se coloca una orden de mercado de tamaño `Volume` (predeterminado 0.1 lotes).
   * Los precios de stop-loss y take-profit se derivan de las distancias en pips configuradas para el disparador activo (`StopLoss*X`, `TakeProfit*X`) usando la heurística de tamaño de pip del instrumento (los símbolos de 5 dígitos y 3 dígitos reciben escalado 10x).
   * Las salidas de protección se simulan en la siguiente vela M15: si el alto/bajo de la vela toca el stop o el nivel de take-profit, la estrategia cierra la posición al mercado y restablece los precios de protección. Esto imita el comportamiento de MT5 que dependía de órdenes stop.

## Parámetros
| Parámetro | Descripción | Valor predeterminado |
|-----------|-------------|----------------------|
| `Volume` | Volumen de trade en lotes. | `0.1` |
| `TriggerOne` | Habilitar el disparador RSI fijo. | `true` |
| `TriggerTwo` | Habilitar el disparador RSI adaptativo (mutuamente excluyente con el disparador uno). | `false` |
| `BbSpreadH4Min1` | Spread mínimo de Bollinger H4 (pips) para el disparador uno. | `84` |
| `BbSpreadM15Max1` | Spread máximo de Bollinger M15 (pips) para el disparador uno. | `64` |
| `RsiPeriod1` | Longitud RSI usada por el disparador uno en todos los marcos temporales. | `10` |
| `RsiLoM15_1`, `RsiHiM15_1` | Umbrales RSI para M15. | `24`, `66` |
| `RsiLoH1_1`, `RsiHiH1_1` | Umbrales RSI para H1. | `34`, `54` |
| `RsiLoH4_1`, `RsiHiH4_1` | Umbrales RSI para H4. | `48`, `56` |
| `RsiLoLim*`, `RsiHiLim*` | Límites de seguridad para bloquear lecturas RSI extremas. | `20–92` |
| `RDeltaM15Lim1` | Pendiente mínima RSI en M15 para el disparador uno. | `-3.5` |
| `StocLoM15_1`, `StocHiM15_1` | Límites estocásticos para el disparador uno. | `26`, `64` |
| `BbSpreadH4Min2` | Spread mínimo de Bollinger H4 (pips) para el disparador dos. | `65` |
| `BbSpreadM15Max2` | Spread máximo de Bollinger M15 (pips) para el disparador dos. | `75` |
| `RsiPeriod2` | Longitud RSI usada por el disparador dos. | `20` |
| `NumRsi` | Tamaño de muestra para estadísticas RSI. | `60` |
| `Rsi*M*Sigma2` | Multiplicadores para bandas adaptativas principales (M15/H1/H4). | `1.20 / 0.95 / 0.9` |
| `Rsi*M*SigmaLim2` | Multiplicadores para límites externos (M15/H1/H4). | `1.85 / 2.55 / 2.7` |
| `RDeltaM15Lim2` | Pendiente mínima RSI en M15 para el disparador dos. | `-5.5` |
| `StocLoM15_2`, `StocHiM15_2` | Límites estocásticos para el disparador dos. | `24`, `68` |
| `TakeProfitBuy1`, `StopLossBuy1` | Distancias de protección en pips para largos del disparador uno. | `150`, `70` |
| `TakeProfitSell1`, `StopLossSell1` | Distancias de protección en pips para cortos del disparador uno. | `70`, `35` |
| `TakeProfitBuy2`, `StopLossBuy2` | Distancias de protección en pips para largos del disparador dos. | `140`, `35` |
| `TakeProfitSell2`, `StopLossSell2` | Distancias de protección en pips para cortos del disparador dos. | `60`, `30` |
| `AtrPeriod` | Período de cálculo ATR H4. | `60` |
| `BollingerPeriod` | Longitud de Bollinger Bands en M15 y H4. | `20` |
| `AtrLimit` | ATR máximo en pips para permitir entradas. | `90` |
| `EntryHour` | Hora de inicio de sesión (0–23). | `0` |
| `OpenHours` | Duración de la sesión en horas (0 = una hora). | `14` |
| `NumPositions` | Máximo de posiciones netas simultáneas (la estrategia abre solo cuando está plana). | `1` |
| `FridayEndHour` | Hora del viernes después de la cual el trading se detiene. | `4` |
| `StochasticK`, `StochasticD`, `StochasticSlowing` | Parámetros para el oscilador estocástico. | `12 / 5 / 5` |
| `M15CandleType`, `H1CandleType`, `H4CandleType` | Tipos de datos de velas para cada marco temporal. | `15m / 1h / 4h` |

## Notas
* Las órdenes stop-loss y take-profit de protección del EA original se emulan monitoreando los altos/bajos de las velas M15. Si se requiere precisión de tick intrabarra, considere agregar órdenes stop a través de la API de bajo nivel.
* Asegúrese de que el portafolio proporcione acceso a todos los marcos temporales solicitados; de lo contrario, las colas de disparadores no se formarán y la estrategia permanecerá inactiva.
* La heurística de tamaño de pip sigue la convención común de MetaTrader: los símbolos de 5 dígitos (o 3 dígitos para cruces de JPY) multiplican el `PriceStep` del intercambio por 10.
