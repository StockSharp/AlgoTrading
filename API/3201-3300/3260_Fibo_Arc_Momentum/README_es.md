# Estrategia de Fibo Arc Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es un port de StockSharp del asesor experto de MetaTrader "FiboArc" (carpeta `MQL/24924`). El EA original combina múltiples filtros de Momentum con rupturas de arcos de Fibonacci. La implementación de StockSharp mantiene la misma idea adaptándola a la API de velas de alto nivel:

* Dos medias móviles ponderadas linealmente (`FastMaPeriod`, `SlowMaPeriod`) definen la dirección de la tendencia.
* Un oscilador de Momentum medido contra el nivel neutro de 100 filtra configuraciones débiles.
* Un histograma MACD confirma la fuerza de la tendencia y detecta nuevos cruces.
* Un arco de Fibonacci simplificado se reconstruye en cada barra usando los precios de apertura de dos velas ancla seleccionadas por `TrendAnchorLength` y `ArcAnchorLength`. Una ruptura a través de este nivel dinámico reemplaza las verificaciones basadas en objetos de la versión de MetaTrader.

La estrategia funciona con cualquier par símbolo/marco temporal compatible con StockSharp. Todos los cálculos se ejecutan en velas completamente terminadas para reflejar el comportamiento del EA y evitar el sesgo de previsión.

## Indicadores y flujo de datos

La estrategia se suscribe a un único flujo de velas configurado por `CandleType`. Cada nueva vela terminada se alimenta a los siguientes indicadores mediante `SubscribeCandles(...).BindEx(...)`:

| Indicador | Propósito | Configuración predeterminada |
|-----------|---------|------------------|
| LinearWeightedMovingAverage (rápida) | Tendencia a corto plazo y timing de entrada | `FastMaPeriod = 6`, precio típico |
| LinearWeightedMovingAverage (lenta) | Filtro de tendencia de nivel superior | `SlowMaPeriod = 85`, precio típico |
| Momentum | La distancia desde 100 se usa para confirmar movimientos fuertes | `MomentumPeriod = 14` |
| MovingAverageConvergenceDivergenceSignal | Confirma la tendencia y detecta cruces | `MacdFastPeriod = 12`, `MacdSlowPeriod = 26`, `MacdSignalPeriod = 9` |

Las salidas de los indicadores se reciben como instancias `IIndicatorValue`; solo se procesan los valores finales.

## Reconstrucción del arco de Fibonacci

MetaTrader dibuja un objeto de arco real y lee sus valores con `ObjectGetValueByShift`. StockSharp no depende de objetos de gráfico, por lo que el arco se emula numéricamente:

1. La estrategia mantiene una lista continua de velas terminadas (`_history`).
2. `TrendAnchorLength` selecciona el índice del ancla base, y `ArcAnchorLength` selecciona el segundo ancla.
3. El nivel del arco para la vela actual se calcula como una interpolación lineal entre las aperturas de las anclas usando `FibonacciRatio` (predeterminado 0.618).
4. Para la detección de rupturas, se compara la apertura de la vela anterior con el nivel de arco anterior y la apertura de la vela actual con el nivel recién calculado. Un cruce desde abajo (`fibCrossUp`) o desde arriba (`fibCrossDown`) recrea las verificaciones originales del EA.

## Reglas de trading

### Entradas largas

Se abre una posición larga cuando se cumplen todas las condiciones siguientes:

1. La barra anterior abrió por debajo del nivel de arco anterior y la barra actual abre por encima del nuevo nivel (`fibCrossUp`).
2. La LWMA rápida está por encima de la LWMA lenta (`bullishTrend`).
3. La distancia absoluta entre el Momentum y 100 es al menos `MomentumThreshold`.
4. La línea principal del MACD está por encima de su línea de señal, o acaba de cruzar hacia arriba (`macdAboveSignal` o `macdCrossUp`).
5. El tamaño de posición actual es menor o igual a cero (sin exposición larga existente).

La estrategia compra `Volume` más el valor absoluto de cualquier exposición corta abierta para asegurar transiciones plano-a-largo.

### Entradas cortas

Las operaciones cortas reflejan la lógica larga:

1. `fibCrossDown` confirma una ruptura a la baja.
2. La LWMA rápida está por debajo de la LWMA lenta.
3. La distancia del Momentum supera `MomentumThreshold`.
4. El MACD está por debajo de su línea de señal o cruza hacia abajo.
5. No queda exposición larga existente.

### Salidas

Las posiciones se cierran cuando ocurre una de las siguientes:

* Las condiciones de tendencia o MACD giran contra la operación.
* Aparece la señal de ruptura de Fibonacci opuesta.
* Se toca el nivel adaptativo de stop-loss o take-profit.

Todas las salidas se ejecutan con órdenes de mercado para mantener consistencia con la versión de MetaTrader.

## Gestión de riesgo

El EA original ofrecía stops basados en dinero, lógica de trailing y protección de break-even. La estrategia de StockSharp mantiene las mismas características con parámetros transparentes:

* `StopLossDistance` y `TakeProfitDistance` definen distancias fijas en unidades de precio desde el precio ejecutado.
* `EnableBreakEven`, `BreakEvenTrigger` y `BreakEvenOffset` controlan el comportamiento de movimiento al break-even.
* `EnableTrailing`, `TrailingTrigger` y `TrailingDistance` implementan un trailing stop basado en velas.

## Parámetros

| Nombre | Descripción |
|------|-------------|
| `CandleType` | Marco temporal (y tipo de agregación) usado para todos los cálculos. |
| `FastMaPeriod`, `SlowMaPeriod` | Longitudes LWMA que definen la tendencia. |
| `MomentumPeriod`, `MomentumThreshold` | Configuración del filtro de Momentum. |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | Configuración del MACD. |
| `TrendAnchorLength`, `ArcAnchorLength`, `FibonacciRatio` | Controles de reconstrucción del arco de Fibonacci. |
| `StopLossDistance`, `TakeProfitDistance` | Distancias iniciales de stop y objetivo (unidades de precio absolutas). |
| `EnableBreakEven`, `BreakEvenTrigger`, `BreakEvenOffset` | Lógica de break-even. |
| `EnableTrailing`, `TrailingTrigger`, `TrailingDistance` | Configuración del trailing stop. |

## Uso

1. Adjunte la estrategia a un valor y configure `Volume` según el tamaño de posición deseado.
2. Opcionalmente, ajuste el marco temporal, las longitudes de las medias móviles y la configuración de Fibonacci para el mercado objetivo.
3. Lance la estrategia. Todas las decisiones dependen de velas terminadas; no se requiere ejecución intrabarra.
4. Revise los ayudantes de gráficos integrados para los paneles de LWMA rápida/lenta y MACD si el host soporta visualización.
