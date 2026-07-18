# LCS MACD Estrategia comercial
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un puerto StockSharp del asesor experto "LCS-MACD-Trader" MetaTrader 4. Intercambia MACD cruces que ocurren por debajo o por encima de la línea cero y, opcionalmente, requiere una confirmación del oscilador Stochastic. La lógica también refleja los filtros de hora del día originales y la gestión de punto de equilibrio/punto de equilibrio dinámico estilo MetaTrader.

## como funciona

- Las entradas largas se activan cuando la línea MACD cruza por encima de su línea de señal mientras ambas permanecen por debajo de cero. Si el filtro estocástico está habilitado, la línea %D debe haber estado por encima de %K dentro del período retrospectivo especificado y la vela actual debe mostrar que %D vuelve a caer por debajo de %K.
- Las entradas cortas se activan cuando la línea MACD cruza por debajo de su línea de señal mientras ambas permanecen por encima de cero. Con el filtro estocástico habilitado, la línea %D debe haber estado recientemente por debajo de %K y ahora vuelve a subir por encima de ella.
- Solo se permite operar dentro de tres ventanas intradiarias configurables que replican la configuración de EA.
- Las distancias de toma de ganancias, stop-loss, punto de equilibrio y trailing-stop se expresan en pips y se convierten utilizando el tamaño de puntos del instrumento.
- Sólo se mantiene una posición neta por dirección (StockSharp netting). Se permite el apilamiento de posiciones hasta `MaxOrders` lotes; las señales opuestas esperan hasta que la gestión de riesgos cierre la posición neta actual.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Serie de velas utilizadas para los cálculos de indicadores. | plazo de 15 minutos |
| `FastEmaPeriod` | Período EMA rápida en el MACD. | 12 |
| `SlowEmaPeriod` | Período EMA lenta en el MACD. | 26 |
| `SignalPeriod` | Período de línea de señal en el MACD. | 9 |
| `UseStochasticFilter` | Requerir confirmación estocástica antes de las entradas. | cierto |
| `BarsToCheckStochastic` | Barras máximas cerradas desde la relación estocástica opuesta. | 5 |
| `StochasticKPeriod` | Longitud retrospectiva de %K. | 5 |
| `StochasticDPeriod` | Longitud de suavizado de %D. | 3 |
| `StochasticSlowing` | Suavizado adicional aplicado a %K. | 3 |
| `TradeVolume` | Tamaño de lote utilizado por entrada. | 0.1 |
| `TakeProfitPips` | Distancia de toma de ganancias en pips. | 100 |
| `StopLossPips` | Distancia de stop-loss en pips. | 100 |
| `MaxOrders` | Entradas apiladas máximas por dirección. | 5 |
| `EnableTrailing` | Habilite la lógica de parada dinámica de estilo MetaTrader. | falso |
| `TrailingActivationPips` | Se requiere beneficio antes de que comience el seguimiento. | 50 |
| `TrailingDistancePips` | Distancia mantenida por el trailing stop. | 25 |
| `BreakEvenActivationPips` | Beneficio necesario para mover el stop al punto de equilibrio. | 25 |
| `BreakEvenOffsetPips` | Se agregaron pips adicionales al colocar el tope de equilibrio. | 1 |
| `Session1Start/End`, `Session2Start/End`, `Session3Start/End` | Ventanas de negociación intradía. | 08:15-08:35, 13:45-14:42, 22:15-22:45 |

## Notas

- La estrategia supone una cuenta de compensación. Cierra posiciones existentes a través de las reglas de riesgo configuradas en lugar de cubrir órdenes opuestas como la versión MT4 original.
- La conversión de pips utiliza el tamaño de puntos del instrumento. Para símbolos FX de 5 dígitos, la lógica escala automáticamente los valores de pip en 10 para que coincidan con la configuración del multiplicador EA.
- La lógica de trailing stop y punto de equilibrio se evalúa en velas terminadas y utiliza el máximo/mínimo de cada barra para emular el comportamiento MetaTrader basado en ticks.
