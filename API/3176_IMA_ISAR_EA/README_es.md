# Estrategia de iMA iSAR EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia replica el Asesor Experto "iMA iSAR EA" de MetaTrader 5 usando la API de alto nivel de StockSharp. Combina un filtro de triple media móvil ponderada con dos trayectorias de SAR Parabólico para identificar rupturas de momentum. Se abre una posición larga cuando la media móvil ponderada más rápida permanece por encima de las otras dos medias y ambas trayectorias SAR están por debajo del cierre de la vela. Una condición espejo genera entradas cortas. Los stops protectores, los objetivos de beneficio y un trailing stop opcional se gestionan en puntos de precio (pips).

La implementación funciona en una única serie de velas configurable a través del parámetro `CandleType`. Todos los indicadores se evalúan en este marco temporal. El experto de MetaTrader original usaba múltiples marcos temporales para sus indicadores; en StockSharp este comportamiento se aproxima permitiendo desplazamientos individuales de las medias móviles que pueden retrasar cada señal un número de barras completadas.

## Reglas de trading
- **Indicadores**
  - Tres medias móviles ponderadas (`Fast`, `Normal`, `Slow`) calculadas en el flujo de velas configurado. Los desplazamientos de barras opcionales emulan los buffers retrasados del código MQ5 original.
  - Dos indicadores SAR Parabólico (`FastSAR`, `NormalSAR`) comparten el mismo flujo de velas pero tienen parámetros de aceleración y máximo independientes.
- **Condiciones de entrada**
  - **Largo**: la MA `Fast` está por encima de `Normal` y `Slow`, mientras que ambos valores SAR están por debajo del cierre de la vela.
  - **Corto**: la MA `Fast` está por debajo de `Normal` y `Slow`, mientras que ambos valores SAR están por encima del cierre de la vela.
  - Cuando aparece una señal de reversión, la estrategia cierra cualquier exposición opuesta y cambia de dirección en una única orden de mercado.
- **Controles de riesgo**
  - Los niveles fijos de stop-loss y take-profit se expresan en pips (múltiplos del paso de precio del instrumento). Se evalúan en velas completadas.
  - Trailing stop opcional: una vez habilitado, el stop sigue el precio de cierre a una distancia configurable y solo avanza después de moverse la cantidad especificada por el paso de trailing.
  - Los volúmenes se ajustan a la configuración `VolumeStep`, `MinVolume` y `MaxVolume` del instrumento antes de enviar las órdenes.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
|--------|------|----------------|-------------|
| `Volume` | `decimal` | `0.1` | Tamaño base de la orden. Se incrementa automáticamente para cubrir una posición opuesta al cambiar de dirección. |
| `StopLossPips` | `decimal` | `50` | Distancia de stop protector en pips. Establecer en `0` para deshabilitar. |
| `TakeProfitPips` | `decimal` | `50` | Distancia del objetivo de beneficio en pips. Establecer en `0` para deshabilitar. |
| `UseTrailing` | `bool` | `true` | Habilita la gestión dinámica del trailing stop. |
| `TrailingStopPips` | `decimal` | `25` | Distancia entre el precio y el trailing stop, en pips. |
| `TrailingStepPips` | `decimal` | `5` | Movimiento favorable mínimo (pips) antes de que el trailing stop avance. |
| `CandleType` | `DataType` | `TimeFrameCandle 15m` | Serie de velas usada para todos los cálculos. |
| `FastMaPeriod` | `int` | `10` | Período de la media móvil ponderada rápida. |
| `FastMaShift` | `int` | `0` | Número de barras completadas para desplazar la MA rápida hacia atrás. |
| `NormalMaPeriod` | `int` | `30` | Período de la media móvil ponderada normal. |
| `NormalMaShift` | `int` | `3` | Número de barras completadas para desplazar la MA normal hacia atrás. |
| `SlowMaPeriod` | `int` | `60` | Período de la media móvil ponderada lenta. |
| `SlowMaShift` | `int` | `6` | Número de barras completadas para desplazar la MA lenta hacia atrás. |
| `FastSarStep` | `decimal` | `0.02` | Factor de aceleración para el SAR Parabólico rápido. |
| `FastSarMax` | `decimal` | `0.2` | Aceleración máxima para el SAR Parabólico rápido. |
| `NormalSarStep` | `decimal` | `0.02` | Factor de aceleración para el SAR Parabólico normal. |
| `NormalSarMax` | `decimal` | `0.2` | Aceleración máxima para el SAR Parabólico normal. |

## Notas
- Las verificaciones del trailing stop se realizan al cierre de la vela. Si se requiere precisión intrabarra, combine la estrategia con un componente protector a nivel de tick.
- El tamaño del pip equivale al paso de precio del instrumento cuando está disponible. De lo contrario, se asume un tick estándar de `0.0001` para pares FX.
- Para mayor consistencia con la versión de MetaTrader, todas las señales de indicadores operan en velas cerradas. Las transacciones pendientes no se preparan; cada señal envía una orden de mercado inmediata.
