# Estrategia Cycle Lines
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia Cycle Lines es la adaptación a StockSharp del asesor experto de MetaTrader "Cycle Lines". El script original combinaba dibujo en el gráfico con botones manuales de trading. Esta versión se centra en la lógica de negociación automatizada que acompañaba a esos controles. La estrategia opera cruces de la línea MACD, mantiene el riesgo bajo control estricto mediante límites absolutos de stop-loss y take-profit, y admite gestión de break-even y trailing stop.

Cuando la línea MACD cruza por encima de su línea de señal, la estrategia abre una posición larga. Cuando la línea cruza por debajo de la línea de señal, abre una posición corta. Las operaciones abiertas se cierran si el indicador cambia a la dirección opuesta o si se activa cualquiera de las reglas de protección (stop-loss, take-profit, break-even o trailing stop).

## Reglas de trading

1. **Condiciones de entrada**
   - **Largo:** MACD cruza por encima de la línea de señal en la serie de velas seleccionada.
   - **Corto:** MACD cruza por debajo de la línea de señal en la serie de velas seleccionada.
   - Las entradas solo se evalúan después de que el indicador esté completamente formado y la estrategia esté conectada y autorizada para operar.
2. **Condiciones de salida**
   - Cruce MACD opuesto.
   - Stop-loss alcanzado.
   - Take-profit alcanzado.
   - Nivel de protección break-even tocado.
   - Nivel de trailing stop tocado.

## Parámetros

| Nombre | Descripción | Predeterminado | Notas |
| ---- | ----------- | --------------- | ----- |
| `Volume` | Volumen de orden por operación. | `1` | Debe ser positivo. |
| `MacdFastPeriod` | Período de la EMA rápida dentro del cálculo MACD. | `12` | Optimizable. |
| `MacdSlowPeriod` | Período de la EMA lenta dentro de MACD. | `26` | Optimizable. |
| `MacdSignalPeriod` | Período de la línea de señal MACD. | `9` | Optimizable. |
| `StopLoss` | Distancia absoluta de precio para el stop de protección. | `0` | Se desactiva cuando se establece en `0`. |
| `TakeProfit` | Distancia absoluta de precio para el objetivo de take-profit. | `0` | Se desactiva cuando se establece en `0`. |
| `TrailingOffset` | Distancia mantenida entre el mejor precio favorable y el trailing stop. | `0` | Se desactiva cuando se establece en `0`. |
| `BreakEvenTrigger` | Distancia de ganancia necesaria antes de mover el stop a break-even. | `0` | Se desactiva cuando se establece en `0`. |
| `BreakEvenOffset` | Desplazamiento adicional aplicado al nivel de break-even. | `0` | Permite asegurar algo de ganancia extra por encima/debajo de la entrada. |
| `CandleType` | Serie de velas usada para los cálculos de indicadores. | Velas de marco temporal de `15` minutos | Acepta cualquier `DataType` compatible con StockSharp. |

## Gestión de posiciones

- **Stop-loss / take-profit:** Ambas comprobaciones evalúan los extremos intrabar (máximo/mínimo) de velas cerradas, asegurando que la salida respete la distancia absoluta configurada desde el precio de entrada.
- **Break-even:** Cuando el precio avanza a favor al menos `BreakEvenTrigger`, la estrategia arma un stop en `entry ± BreakEvenOffset`. Cualquier retroceso que toque ese nivel cierra la posición.
- **Trailing stop:** En operaciones largas se vigila el precio máximo alcanzado. El nivel de stop sigue al máximo menos `TrailingOffset`. En operaciones cortas, la lógica refleja el comportamiento alrededor del precio mínimo.

## Notas de uso

- La estrategia mantiene una sola posición a la vez. Las señales consecutivas no piramidarán una posición existente.
- Los parámetros se exponen como objetos `StrategyParam<T>` y, por lo tanto, admiten optimización dentro de StockSharp.
- Para reproducir el comportamiento predeterminado del EA original, configure los períodos MACD en `12 / 26 / 9` y ajuste los parámetros de riesgo según los valores en pips deseados.

## Diferencias con la versión MQL

- Las funciones de dibujo en el gráfico y los botones manuales BUY/SELL se omitieron porque StockSharp gestiona la visualización de otra forma.
- Todas las reglas de protección se reescribieron para operar sobre datos de velas en lugar de eventos tick de MetaTrader, lo que las mantiene compatibles con la API de alto nivel de StockSharp.
- La lógica de trailing y break-even es simétrica para operaciones largas y cortas, por claridad y robustez.
