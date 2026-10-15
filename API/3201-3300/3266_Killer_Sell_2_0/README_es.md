# Estrategia Killer Sell 2.0 (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Killer Sell 2.0 es un asesor experto de MetaTrader 4 solo-corto que cronometra entradas después de lecturas extendidas de sobrecompra y asegura beneficios cuando el impulso oscila hacia territorio de sobreventa. Este port reescribe la lógica original sobre la API de estrategia de alto nivel de StockSharp. Todo el procesamiento de indicadores es controlado por eventos a través de `SubscribeCandles().BindEx(...)`, y las reglas de gestión de dinero están encapsuladas dentro de la clase de estrategia.

## Lógica de trading
La lógica convertida sigue la cadena de señales original mientras usa el modelo de posición neta de StockSharp. Cada vela completada del marco temporal configurado ejecuta los siguientes pasos:

1. **Preparación de datos.** La estrategia actualiza un MACD (12/120/9), Williams %R (período 350 para ambos filtros) y dos osciladores Estocásticos (10/1/3 para entrada, 90/7/1 para salidas). Los valores de los indicadores se consumen solo cuando la nueva barra está terminada y las entradas están completamente formadas.
2. **Filtro de entrada.** Un setup corto es válido cuando se cumplen todas las condiciones siguientes:
   - Williams %R sube por encima de −10, señalando un mercado sobrecomprado.
   - La línea principal del MACD es mayor que `0.0014`.
   - El %K del Estocástico de entrada cruza **por debajo** del nivel de entrada configurable (predeterminado 90). La detección de cruce se realiza en lecturas consecutivas de %K.
3. **Colocación de orden.** Una vez que los filtros se alinean, la estrategia envía una venta de mercado usando el tamaño de lote martingala actual. Las órdenes heredan un take-profit configurado `N` pips más allá (predeterminado 100 pips) a través de `StartProtection`.
4. **Gestión de salida.** Mientras existe exposición corta, la estrategia calcula la media aritmética del beneficio en pips de las tickets abiertas. Dependiendo del impulso:
   - Si el beneficio promedio está **por debajo** de 10 pips y Williams %R cae por debajo de −80, todos los cortos se cierran inmediatamente.
   - Si el beneficio promedio está **por encima** de 15 pips y el %K del Estocástico de salida cae por debajo de 12, la posición se cierra para asegurar la ganancia.

## Gestión de dinero
Killer Sell 2.0 usa una escalera martingala similar al EA original. La implementación de StockSharp mantiene una lista interna de lotes cortos abiertos para imitar los cálculos por ticket de MetaTrader:

- La primera operación usa `InitialVolume` (predeterminado 0.05 lotes).
- Después de un ciclo rentable o en break-even, el volumen se restablece al tamaño de lote inicial.
- Después de un ciclo perdedor, la siguiente orden se multiplica por `MartingaleMultiplier` (predeterminado ×1.2). Un límite de seguridad `MaxVolume` previene el crecimiento descontrolado.

El asistente también rastrea el PnL realizado en los fills para decidir si el ciclo anterior fue rentable.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Marco temporal principal que alimenta cada indicador. |
| `EntryWprPeriod` / `ExitWprPeriod` | Longitudes de Williams %R para confirmaciones de entrada y salida. |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | Configuración del MACD. |
| `MacdThreshold` | Valor mínimo de la línea principal del MACD requerido para una venta. |
| `StochasticEntryKPeriod`, `StochasticEntryDPeriod`, `StochasticEntrySlow` | Parámetros del Estocástico de entrada. |
| `EntryStochasticLevel` | Nivel que %K debe cruzar desde arriba para validar una señal. |
| `StochasticExitKPeriod`, `StochasticExitDPeriod`, `StochasticExitSlow` | Parámetros del Estocástico de salida. |
| `ExitStochasticLevel` | Límite de sobreventa verificado antes de asegurar beneficios. |
| `EntryWprThreshold` / `ExitWprThreshold` | Umbrales de Williams %R para entradas/salidas. |
| `LossExitPips` / `ProfitExitPips` | Límites de beneficio promedio (en pips) que controlan las salidas defensivas y objetivo. |
| `TakeProfitPips` | Take-profit protector asignado a cada orden de venta. |
| `InitialVolume` | Volumen del primer paso martingala. |
| `MartingaleMultiplier` | Factor aplicado después de pérdidas. |
| `MaxVolume` | Límite absoluto aplicado al próximo tamaño de lote. |

## Notas de conversión
- MetaTrader mantiene tickets individuales; StockSharp trabaja con una posición neta. La estrategia por lo tanto almacena cada corto llenado (volumen + precio) para reproducir los cálculos de beneficio promedio y para evaluar los resets martingala.
- El bloque "martingala" de MT4 exponía muchos modos adicionales (fijo, riesgo porcentual, 1326, Fibonacci, etc.). La configuración original usaba la rama martingala simple; solo ese comportamiento se replica aquí.
- El stop-loss de emergencia estaba deshabilitado en el proyecto fuente. El port refleja esa configuración adjuntando solo un take-profit y gestionando otras salidas internamente.

## Consejos de uso
1. Adjunte la estrategia a un portfolio y valor, luego configure el mismo marco temporal usado en los backtests de MT4 (los valores predeterminados asumen H1).
2. Asegúrese de que los datos de mercado entreguen velas completadas; los indicadores dependen de eventos `CandleStates.Finished`.
3. Revise el apalancamiento de la cuenta y los tamaños de lote permisibles. El límite martingala predeterminado (5 lotes) debe ajustarse a los requisitos de su bróker.
4. Realice backtests exhaustivos — las estrategias martingala amplían el riesgo cuando los mercados tienen tendencia fuerte contra el sesgo corto.
