# Estrategia OzFx Accelerator Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Conversión del asesor experto MetaTrader *OzFx (edición de barabashkakvn)* a la API de estrategia de alto nivel de StockSharp.
- Combina el oscilador Acceleration/Deceleration (AC) con un umbral estocástico para entrar en capas en tendencias.
- Diseñado para trading forex de estilo discrecional donde las órdenes se dimensionan en lotes y la protección se expresa en pips.

## Lógica de trading
1. Calcular el oscilador Acceleration/Deceleration como la diferencia entre el Awesome Oscillator y su SMA de 5 períodos.
2. Suscribirse a un oscilador estocástico con períodos `%K`, `%D` y de suavizado configurables.
3. Cuando se cierra una nueva vela, evaluar los dos valores de AC más recientes junto con el nivel estocástico:
   - **Configuración larga**: `%K` cruza por encima del nivel configurado, el AC actual es positivo y sube mientras el valor anterior era negativo.
   - **Configuración corta**: `%K` cruza por debajo del nivel, el AC actual es negativo y cae mientras el valor anterior era positivo.
4. Con una señal válida se abren hasta cinco órdenes de mercado de igual tamaño. La primera capa refleja el EA original al lanzarse sin stop/objetivo, mientras que las capas restantes heredan el stop loss configurado y take profits escalonados.
5. La gestión de salidas emula el comportamiento original del flag `modok`:
   - Cuando los trailing stops están deshabilitados, la estrategia solo ajusta stops a punto de equilibrio después de una salida rentable, y cerrará todas las capas si la combinación estocástico/AC se voltea contra la posición.
   - Con trailing stops habilitados, el stop sigue al precio una vez que el movimiento supera *TrailingStop + TrailingStep*, y la misma reversión de momentum cierra la pila.

## Escalado de posición y objetivos
- Las posiciones largas colocan cuatro capas adicionales con take profits en `entry + TakeProfit * i` para `i = 1..4`. Los cortos lo reflejan por debajo del precio.
- Los stop losses (cuando están configurados) se adjuntan a cada capa excepto la primera, exactamente como el script MT5.
- Los take profits parciales actualizan el flag interno para que la siguiente campaña comience inmediatamente en estado "modok = true", desbloqueando la protección de punto de equilibrio para la capa inicial.

## Gestión de riesgos
- `StopLossPips` y `TakeProfitPips` se definen en pips. La estrategia los convierte usando el tamaño de tick del instrumento y la precisión de dígitos (`5` o `3` pares decimales cuentan como pips fraccionales).
- `TrailingStopPips = 0` desactiva la lógica de trailing y habilita solo el ajuste de punto de equilibrio después de un take profit. Cualquier valor positivo activa el bloque de trailing descrito anteriormente.
- Todas las salidas se ejecutan con órdenes de mercado cuando el rango de la vela cruza los niveles de stop o objetivo almacenados, coincidiendo con el comportamiento del experto original que dependía de órdenes protectoras del lado del broker.

## Parámetros
| Nombre | Descripción | Valor predeterminado |
| --- | --- | --- |
| `OrderVolume` | Tamaño de lote por capa. | `0.1` |
| `StopLossPips` | Distancia para órdenes stop protectoras (pips). | `100` |
| `TakeProfitPips` | Distancia base entre take profits en capas (pips). | `50` |
| `TrailingStopPips` | Distancia de trailing stop en pips (0 deshabilita trailing). | `50` |
| `TrailingStepPips` | Distancia adicional antes de avanzar el trailing stop. | `5` |
| `KPeriod` | Lookback del `%K` estocástico. | `5` |
| `DPeriod` | Suavizado del `%D` estocástico. | `3` |
| `SmoothingPeriod` | Suavizado final aplicado al `%K`. | `3` |
| `StochasticLevel` | Umbral que separa regímenes alcistas/bajistas. | `50` |
| `CandleType` | Serie de velas fuente para cálculos. | `Marco temporal 4h` |

## Notas de implementación
- Las señales, actualizaciones de trailing y salidas protectoras se procesan en velas completadas para mantenerse consistente con el EA que se activa en nuevas barras.
- El indicador AC se reproduce vinculando el Awesome Oscillator y restando su SMA de 5 períodos; no se accede a buffers de indicadores de bajo nivel.
- La conversión de pips se adapta automáticamente a símbolos forex de 4/5 dígitos y recurre a un valor predeterminado razonable cuando faltan metadatos del tamaño de tick.
- La estrategia mantiene un registro interno de entradas en capas para que los take profits parciales y ajustes de stop coincidan con la lógica por posición de la versión MetaTrader.
- Dado que StockSharp ejecuta salidas mediante órdenes de mercado, los trades se aplanan cuando el máximo/mínimo de la vela perfora los niveles almacenados de stop o objetivo en lugar de esperar activadores del lado del broker.
