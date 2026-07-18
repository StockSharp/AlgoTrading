# Estrategia MACD Sample con Filtro de Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port directo del clásico asesor experto **MACD Sample** de MetaTrader 5. Utiliza cruces de MACD filtrados por un detector de tendencia EMA. Las órdenes se dimensionan con la propiedad estándar `Volume`, mientras que la gestión de riesgos se basa en umbrales de pips configurables para el histograma MACD, take profit y trailing stop.

## Lógica principal

- **Indicadores**
  - `MovingAverageConvergenceDivergenceSignal` con períodos *(12, 26, 9)* proporciona líneas MACD y de señal.
  - `ExponentialMovingAverage` con período *26* actúa como filtro de tendencia.
- **Criterios de entrada**
  - **Largo**: MACD está por debajo de cero, cruza por encima de la línea de señal, tiene magnitud por encima del *Nivel de Apertura MACD*, y la EMA está subiendo.
  - **Corto**: MACD está por encima de cero, cruza por debajo de la línea de señal, tiene magnitud por encima del *Nivel de Apertura MACD*, y la EMA está bajando.
- **Criterios de salida**
  - MACD cruza en contra de la posición con magnitud por encima del *Nivel de Cierre MACD*.
  - El take profit alcanza la distancia en pips configurada desde el precio de entrada.
  - Se activa el trailing stop (si fue activado por beneficio > distancia de trailing).
- **Mecánica del trailing stop**
  - Las posiciones largas activan el trailing stop una vez que el precio alto supera el precio de entrada por la distancia de trailing. El stop se mantiene en *máximo − distancia de trailing*.
  - Las posiciones cortas activan el trailing stop una vez que el precio bajo se mueve por debajo del precio de entrada por la distancia de trailing. El stop se mantiene en *mínimo + distancia de trailing*.

## Parámetros

| Parámetro | Valor predeterminado | Descripción |
|-----------|----------------------|-------------|
| `FastPeriod` | 12 | Período EMA rápida dentro del MACD. |
| `SlowPeriod` | 26 | Período EMA lenta dentro del MACD. |
| `SignalPeriod` | 9 | Período EMA de señal dentro del MACD. |
| `TrendPeriod` | 26 | Longitud del filtro de tendencia EMA. |
| `MacdOpenLevelPips` | 3 | Magnitud mínima del MACD (en pips) requerida para abrir una operación. |
| `MacdCloseLevelPips` | 2 | Magnitud mínima del MACD (en pips) requerida para cerrar una operación en cruce. |
| `TakeProfitPips` | 50 | Distancia de take profit expresada en pips. |
| `TrailingStopPips` | 30 | Distancia de trailing stop expresada en pips. Establecer en 0 para deshabilitar el trailing. |
| `CandleType` | Marco temporal de 15 minutos | Tipo de vela utilizado para los cálculos. |

### Conversión de pips

El asesor experto original usaba la normalización de pips de MetaTrader (multiplicando por 10 para símbolos de 3/5 dígitos). La conversión sigue la misma idea inspeccionando `Security.PriceStep`:

- Si el paso de precio tiene 3 o 5 decimales, el tamaño del pip es `PriceStep * 10`.
- De lo contrario, el tamaño del pip es igual a `PriceStep`.
- Cuando el paso de precio no está disponible, los umbrales basados en pips caen de vuelta a valores brutos.

## Notas de comportamiento

- Las posiciones se cierran antes de que se evalúen las nuevas señales, reflejando la implementación MT5.
- Las instrucciones `LogInfo` reportan entradas, salidas y actualizaciones de trailing stop para facilitar la depuración.
- Las órdenes de protección no se colocan automáticamente; las salidas se gestionan dentro de `ProcessCandle` para imitar la lógica del EA.
- Use `Volume` para definir el tamaño base de la operación. Las reversiones compensan automáticamente la exposición actual añadiendo `Math.Abs(Position)` al volumen de la orden.

## Diferencias con la versión MQL5

- El procesamiento ocurre en velas finalizadas en lugar de en cada tick. Esto evita señales repetidas mientras mantiene un comportamiento determinista.
- Las comprobaciones de trailing stop y take profit usan máximos y mínimos de las velas para aproximar los hits de bid/ask del EA original.
- Cuando falta `Security.PriceStep`, los parámetros de pips actúan como distancias absolutas de precio y deben ajustarse manualmente.

Ajuste los umbrales de pips y el tipo de vela para adaptarse al instrumento negociado, especialmente cuando se porta a mercados con diferentes tamaños de tick.
