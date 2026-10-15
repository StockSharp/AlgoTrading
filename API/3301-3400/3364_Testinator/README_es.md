# Estrategia del testador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia es una adaptación de C# del asesor experto MetaTrader **Testinator v1.30a**. Abre sólo posiciones largas y las gestiona como una canasta. Cada nueva compra se permite sólo cuando un conjunto configurable de filtros técnicos devuelve "verdadero" y el precio ha avanzado un número mínimo de pips. La lógica de salida refleja la lógica de entrada mediante el uso de otra máscara de filtro. El EA original también se basó en mediciones diarias ATR para la gestión de riesgos, por lo que el puerto se suscribe a velas diarias además del período de tiempo principal.

## Lógica comercial

### Máscara de filtro de entrada (parámetro `BuySequence`)

La máscara utiliza los nueve bits inferiores. Un bit que se fija debe superar la prueba correspondiente en la vela terminada anterior.

| poco | Condición |
| --- | --------- |
| 1 | EMA(12) está por encima de SMA(14). |
| 2 | EMA(50) se mantiene por debajo de los mínimos de las últimas tres velas. |
| 4 | El mínimo anterior está por debajo de la banda inferior Bollinger (20, 2). |
| 8 | ADX(14) está por encima de -DI y +DI es más fuerte que -DI. |
| 16 | Stochastic (16, 4, 8) tiene %K por encima de %D y %D por encima de 80. |
| 32 | Williams %R(14) es mayor que -20. |
| 64 | La línea MACD(12, 26, 9) está por encima de la línea de señal. |
| 128 | Ichimoku muestra Senkou Span A por encima del Span B, Tenkan por encima de Kijun y el mínimo anterior por encima del Span A. |
| 256 | RSI (período `RsiEntryPeriod`) está por encima de `RsiEntryLevel` y aumenta en relación con el valor anterior. |

### Máscara de filtro de salida (parámetro `CloseBuySequence`)

| poco | Condición |
| --- | --------- |
| 1 | SMA(14) está por encima de EMA(12). |
| 2 | EMA(50) está por encima de los máximos de las últimas tres velas. |
| 4 | El máximo anterior está por encima de la banda de salida superior Bollinger (`BollingerCloseLength`, `BollingerCloseDeviation`). |
| 8 | -DI está por encima de +DI. |
| 16 | Stochastic %D está por debajo de 80. |
| 32 | Williams %R(14) es inferior a -80. |
| 64 | La línea MACD está debajo de la línea de señal. |
| 128 | Ichimoku Senkou Span B está por encima de Senkou Span A. |
| 256 | RSI (período `RsiClosePeriod`) está por debajo de `RsiCloseLevel`. |

Una cesta se extiende solo si todos los bits de entrada activos son verdaderos, el número de compras es inferior a `MaxBuys` y el último precio de llenado está al menos a `StepPips`. La cesta se aplana cada vez que pasa la máscara de salida o cuando se activan los niveles de protección.

### Control de sesiones y gestión de riesgos.

* Las operaciones se realizan únicamente entre `TradeStartHour` y `TradeStartHour + TradeDurationHours - 1` (hora de Europa del Este). Si la ventana está cerrada y la cesta tiene ganancias, todas las compras se cierran.
* Las distancias de parada protectora y toma de ganancias se expresan en pips. Establecer un valor en `-1` lo desactiva, mientras que `0` activa el multiplicador ATR (`StopRatio`, `TakeRatio`).
* El trailing stop utiliza la misma lógica ATR hasta `StartTrailPips`, `TrailStepPips`, `StartTrailRatio` y `TrailStepRatio`.
* La estrategia calcula valores diarios de ATR(15) en velas D1 para mantener el comportamiento idéntico al EA.

## Parámetros

* `TradeVolume`: tamaño del lote (volumen) para cada compra en el mercado.
* `BuySequence` / `CloseBuySequence`: máscaras de bits que habilitan filtros de indicadores individuales.
* `MaxBuys`: número máximo de compras abiertas manejadas como una cesta.
* `StepPips` – progreso mínimo del precio (pips) antes de agregarlo a la cesta.
* `TradeStartHour`, `TradeDurationHours`: define la ventana de negociación diaria.
* `TakeProfitPips`, `StopLossPips`: niveles de protección fijos (desactivaciones negativas, cambios de cero a relaciones ATR).
* `StartTrailPips`, `TrailStepPips`: distancia inicial y paso de seguimiento (desactivaciones negativas, cero utiliza proporciones ATR).
* `TakeRatio`, `StopRatio`, `StartTrailRatio`, `TrailStepRatio` – multiplicador ATRes utilizados cuando el valor fijo es igual a cero.
* `RsiEntryLevel`, `RsiEntryPeriod` – RSI umbral y período para la máscara de entrada.
* `RsiCloseLevel`, `RsiClosePeriod` – RSI umbral y período para la máscara de salida.
* `BollingerCloseLength`, `BollingerCloseDeviation` – parámetros de las bandas de salida Bollinger.
* `CandleType`: período de tiempo de las velas activas (las velas diarias se suscriben automáticamente durante ATR).

## Notas

* El puerto mantiene el modelo de contabilidad de cesta del EA original: todas las órdenes son compras y solo se utilizan órdenes de mercado.
* La lógica almacena intencionalmente valores de indicadores anteriores para imitar las comprobaciones de "barra[1]" de MetaTrader.
* La estrategia ignora las entradas no utilizadas de EA (`TakeAsBasket`, `StopAsBasket`, etc.) porque no afectaron la lógica de MQL.
