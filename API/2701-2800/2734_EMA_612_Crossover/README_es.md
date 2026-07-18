# Estrategia Ema612CrossoverStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
- Port del asesor experto de MetaTrader 5 **"EMA 6.12 (edición de barabashkakvn)"** a la API de alto nivel de StockSharp.
- Negocia el cruce entre una media móvil simple rápida y una lenta (el script original también usaba MODE_SMA a pesar de su nombre EMA).
- Añade gestión opcional de take profit y trailing stop expresados en unidades de precio absolutas para que el comportamiento pueda ajustarse por instrumento.

## Lógica de trading
### Preparación de datos
- La estrategia se suscribe a velas del tipo definido por `CandleType` (marco temporal de 15 minutos por defecto).
- Se calculan dos medias móviles simples: longitud `FastPeriod` para la curva rápida y longitud `SlowPeriod` para la curva lenta. El período lento debe ser mayor que el período rápido.

### Reglas de entrada
- Las señales se evalúan al cierre de cada vela terminada.
- Un **cruce alcista** ocurre cuando la SMA lenta estaba por encima de la SMA rápida en la vela anterior y cae por debajo de ella en la vela actual. Cualquier posición corta abierta se cierra y se abre una posición larga con el `Volume` configurado.
- Un **cruce bajista** ocurre cuando la SMA lenta estaba por debajo de la SMA rápida en la vela anterior y sube por encima de ella en la vela actual. Cualquier posición larga abierta se cierra y se abre una posición corta con el `Volume` configurado.

### Reglas de salida
- Las posiciones abiertas se cierran en el cruce opuesto como se describe arriba.
- Take profit opcional: si `TakeProfitOffset` es mayor que cero, la estrategia calcula un objetivo de precio fijo desde el precio de entrada. Las operaciones largas salen cuando el precio alcanza `entrada + TakeProfitOffset`; las operaciones cortas salen cuando el precio alcanza `entrada - TakeProfitOffset`.
- Trailing stop opcional: cuando `TrailingStopOffset` es mayor que cero, la estrategia espera hasta que el beneficio no realizado supere `TrailingStopOffset + TrailingStepOffset`. Una vez que ese umbral es cruzado, el precio de stop se ajusta para mantenerse `TrailingStopOffset` alejado del último cierre, pero solo si el nuevo nivel está al menos `TrailingStepOffset` más cerca del precio que el stop anterior. Las operaciones largas usan los mínimos para activar el stop, los cortos usan los máximos.

## Parámetros
| Parámetro | Por defecto | Descripción |
|-----------|-------------|-------------|
| `CandleType` | Marco temporal de 15 minutos | Resolución de vela usada para cálculos SMA y evaluación de señales. |
| `FastPeriod` | 6 | Período para la media móvil simple rápida. Debe ser > 0 y menor que `SlowPeriod`. |
| `SlowPeriod` | 54 | Período para la media móvil simple lenta. Debe ser > 0 y mayor que `FastPeriod`. |
| `Volume` | 1 | Volumen de orden usado para nuevas entradas. |
| `TakeProfitOffset` | 0.001 | Distancia de precio absoluta opcional para el objetivo de take profit. Establecer en 0 para deshabilitar. |
| `TrailingStopOffset` | 0.005 | Distancia absoluta entre el precio y el trailing stop. Establecer en 0 para deshabilitar el trailing. |
| `TrailingStepOffset` | 0.0005 | Movimiento favorable adicional requerido antes de que el trailing stop se mueva. |

> **Importante:** los offsets se especifican en unidades de precio absolutas. Ajústelos para que coincidan con el tamaño del tick del instrumento (por ejemplo, en EURUSD con un paso de 0.0001, los valores por defecto corresponden a 10, 50 y 5 pips respectivamente).

## Notas de implementación
- Usa el flujo de trabajo de alto nivel `SubscribeCandles().Bind()` según lo requieren las pautas del proyecto.
- La salida del gráfico traza ambas SMAs y marcadores de operaciones cuando el gráfico está disponible en el entorno.
- Las variables de estado rastrean el precio de entrada, el nivel de trailing stop y el objetivo de take profit exactamente como la versión MQL.
- La implementación en C# aplica `SlowPeriod > FastPeriod` al inicio para evitar una configuración de indicador no válida.

## Consejos de uso
- Optimice el marco temporal de las velas y los períodos SMA para que coincidan con el mercado que se negocia (p.ej., períodos más cortos para futuros intradía, más largos para swing trading).
- Convierta los offsets de pips o ticks a unidades de precio absolutas antes de ejecutar la estrategia.
- El trailing puede desactivarse estableciendo `TrailingStopOffset` en cero; la estrategia entonces dependerá únicamente del cruce opuesto o del take profit opcional para las salidas.
