# Estrategia DLMv FX Fish Grid
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia DLMv FX Fish Grid** replica el comportamiento del asesor experto original de MetaTrader construido alrededor del oscilador "FX Fish 2MA". La estrategia evalúa la Transformada de Fisher del precio, la suaviza con una media móvil y abre posiciones cuando el oscilador cruza su línea de base suavizada en el lado apropiado de cero. La gestión de posiciones imita el comportamiento en cuadrícula del EA fuente: las entradas adicionales están espaciadas por una distancia configurable, las órdenes límite pendientes pueden estratificarse y la automatización protectora maneja los controles de riesgo.

## Lógica de trading

1. **Cálculo del indicador**
   - Los precios más altos y más bajos durante las velas `CalculatePeriod` definen el rango rodante.
   - Una Transformada de Fisher se aplica al precio seleccionado (`AppliedPrice`), usando el mismo factor de suavizado 0.67 que el indicador MT5.
   - Una media móvil simple (`MaPeriod`) del valor de Fisher proporciona la línea de base de la señal.
2. **Generación de señales**
   - **Señal larga**: los valores actuales y anteriores de Fisher están por debajo de cero mientras el oscilador cruza **por encima** de su media móvil (valor anterior por debajo de la media, valor actual por encima).
   - **Señal corta**: los valores actuales y anteriores de Fisher están por encima de cero mientras el oscilador cruza **por debajo** de la media móvil (valor anterior por encima de la media, valor actual por debajo).
   - Las señales pueden invertirse habilitando `ReverseSignals`.
3. **Ejecución de órdenes**
   - Cuando aparece una señal de compra (o venta), la estrategia puede opcionalmente cerrar la exposición opuesta existente (`CloseOpposite`).
   - Se permiten entradas adicionales hasta que el conteo total alcance `MaxTrades`. Cada nueva entrada debe respetar el espaciado mínimo dado por `DistancePips` desde la última operación completada.
   - Las órdenes límite opcionales (`SetLimitOrders`) colocan ofertas/demandas en reposo al espaciado configurado, replicando la cuadrícula escalonada del EA original.
4. **Gestión de riesgos**
   - Los valores fijos de stop-loss, take-profit y trailing stop se aplican mediante `StartProtection`, todos definidos en pips.
   - `TimeLiveSeconds` cierra toda la exposición cuando una operación ha estado abierta más tiempo del permitido.
   - El trading puede deshabilitarse los viernes (`TradeOnFriday = false`). Cuando está deshabilitado, la estrategia cierra posiciones y cancela órdenes pendientes tan pronto como llega una vela de viernes.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `OrderVolume` | Tamaño de orden para cada entrada (lotes). |
| `StopLossPips` | Distancia del stop-loss protector desde la entrada. Establecer en 0 para deshabilitar. |
| `TakeProfitPips` | Distancia del nivel de take-profit. Establecer en 0 para deshabilitar. |
| `TrailingStopPips` | Distancia del trailing stop (0 deshabilita el trailing). |
| `TrailingStepPips` | Paso por el cual se ajusta el trailing stop. |
| `MaxTrades` | Número máximo de operaciones simultáneas por dirección. `0` elimina el límite. |
| `DistancePips` | Distancia mínima entre entradas consecutivas y para las órdenes de cuadrícula opcionales. |
| `TradeOnFriday` | Cuando es `false`, la estrategia deja de operar los viernes y liquida la exposición. |
| `TimeLiveSeconds` | Tiempo máximo (segundos) que las posiciones pueden permanecer abiertas antes de ser cerradas forzosamente. |
| `ReverseSignals` | Invertir condiciones largo/corto. |
| `SetLimitOrders` | Habilitar órdenes límite adicionales en reposo en `DistancePips`. |
| `CloseOpposite` | Cerrar la exposición opuesta antes de entrar en un nuevo trade. |
| `CalculatePeriod` | Lookback para el rango de la Transformada de Fisher. |
| `MaPeriod` | Periodo de la media móvil aplicada al valor de Fisher. |
| `AppliedPrice` | Fuente de precio usada en la Transformada de Fisher (close, open, high, low, median, typical, weighted). |
| `CandleType` | Tipo de datos/marco temporal de las velas procesadas por la estrategia. |

## Notas

- Las distancias de stop-loss, take-profit y trailing stop se convierten de pips a offsets de precio absolutos usando `Security.PriceStep * 10`, coincidiendo con la lógica de pip de cinco dígitos de la versión MQL.
- Las órdenes límite se cancelan automáticamente cuando las señales cambian, el trading se pausa o se activan las protecciones de tiempo/viernes.
- La Transformada de Fisher evita búsquedas repetidas de valores, en cambio almacena las lecturas anteriores del oscilador y de la línea de base para una detección precisa de cruces.
