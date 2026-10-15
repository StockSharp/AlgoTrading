# Estrategia MACD Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
Esta estrategia es un port a StockSharp del sistema MetaTrader 5 "MACD Stochastic". Combina un cruce clásico de MACD con un filtro de confirmación estocástico opcional y opera solo durante tres sesiones intradía configurables. Cada posición usa controles de riesgo basados en pips con lógica de trailing stop opcional que puede mover el stop hacia el punto de equilibrio una vez que la operación ha alcanzado una ganancia especificada.

## Indicadores
- **MACD (Convergencia/Divergencia de Medias Móviles)** – genera las señales primarias de reversión de tendencia siguiendo el cruce entre las medias móviles exponenciales rápida y lenta y su línea de señal.
- **Oscilador Stochastic** – filtro opcional que confirma las señales del MACD verificando que las líneas %K y %D hayan cruzado recientemente en la misma dirección que la operación.

## Lógica de Trading
### Entradas Largas
1. La línea principal del MACD cruza por encima de la línea de señal y ambas líneas están por debajo de cero, indicando una posible reversión alcista.
2. La posición más reciente se abrió en una barra anterior (solo se permite una entrada por barra).
3. La hora actual (hora local del instrumento) cae dentro de una de las sesiones de trading configuradas.
4. Si el filtro estocástico está habilitado, el valor actual de %K debe estar por encima de %D y el valor de *StochasticBarsToCheck* barras atrás debe mostrar la relación opuesta (%K por debajo de %D), confirmando un cruce alcista reciente.

### Entradas Cortas
1. La línea principal del MACD cruza por debajo de la línea de señal y ambas líneas están por encima de cero, señalando una reversión bajista.
2. La estrategia no tiene posición abierta y no abrió ya una operación en la barra actual.
3. La hora actual está dentro de al menos una ventana de sesión activa.
4. Cuando el filtro estocástico está activo, el %K actual debe estar por debajo de %D y el valor de *StochasticBarsToCheck* barras atrás debe estar por encima de %D, confirmando un cruce bajista.

### Gestión de Posición
- **Stop-Loss / Take-Profit** – los niveles iniciales se calculan en pips usando el paso de precio del instrumento. La implementación ajusta automáticamente las cotizaciones de 3 y 5 dígitos multiplicando el paso de precio por 10 para aproximar un pip estándar.
- **Trailing Stop** – una vez que la posición ha ganado al menos *WhenSetNoLossStopPips* de ganancia, el stop puede seguir al mercado:
  - Las posiciones largas requieren un stop inicial. El stop se incrementa por *TrailingStopPips* cuando permanece al menos *TrailingStepPips + TrailingStopPips* alejado del cierre actual y permanece por encima del buffer de punto de equilibrio definido por *NoLossStopPips*.
  - Las posiciones cortas mueven el stop hacia abajo bajo restricciones similares. Si no existe stop inicial, el algoritmo puede colocar un stop de punto de equilibrio en *NoLossStopPips* una vez que el precio ha avanzado lo suficiente.
- **Activación de Take-Profit / Stop** – si el máximo o mínimo de una vela toca los niveles de salida almacenados, la posición se cierra a mercado y el estado interno se reinicia.

## Parámetros
- **MacdFastPeriod, MacdSlowPeriod, MacdSignalPeriod** – configuración del MACD.
- **UseStochastic** – habilita el filtro de confirmación estocástico.
- **StochasticBarsToCheck, StochasticLength, StochasticKPeriod, StochasticDPeriod** – configuración del oscilador estocástico.
- **Volume** – tamaño del trade en lotes.
- **StopLossPips, TakeProfitPips** – distancias en pips para salidas iniciales.
- **TrailingStopPips, TrailingStepPips** – configuración del trailing stop.
- **NoLossStopPips, WhenSetNoLossStopPips** – umbrales de punto de equilibrio y activación para la lógica de trailing.
- **MaxPositions** – retenido por compatibilidad; StockSharp trabaja con posiciones netas, por lo que la estrategia mantiene solo una posición abierta a la vez.
- **Session1/2/3 Start-End** – ventanas intradía cuando el trading está permitido. Establezca inicio y fin en `00:00` para deshabilitar una ventana.
- **CandleType** – serie de velas utilizada para la generación de señales.

## Notas Adicionales
- Las entradas se procesan solo en velas completadas. La estrategia no abrirá más de una posición por vela, reflejando el comportamiento original del EA.
- Las distancias basadas en pips dependen del paso de precio del instrumento. Asegúrese de que los metadatos del símbolo proporcionen un `PriceStep` válido.
- El filtro estocástico almacena un pequeño historial rotativo para evaluar valores pasados sin usar acceso de indicador de bajo nivel, cumpliendo con las mejores prácticas de la API de alto nivel.
