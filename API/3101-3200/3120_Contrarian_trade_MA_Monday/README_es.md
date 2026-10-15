# Estrategia de Operación Contraria MA del Lunes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia recrea el asesor experto de MetaTrader **"Contrarian trade MA"** usando la API de alto nivel de StockSharp. Combina el contexto semanal con un filtro de entrada solo los lunes para operar contra los extremos. El sistema espera una nueva semana de trading, mide qué tan lejos cerró la semana anterior en relación con el máximo más alto y el mínimo más bajo durante la ventana de lookback, y verifica si el precio abrió la nueva semana en el lado opuesto de una media móvil desplazada. Si el mercado termina la primera vela diaria de la semana fuera de esos umbrales, se abre una posición contraria.

La lógica depende únicamente de velas completadas. Una serie diaria (predeterminada) impulsa las entradas y salidas, mientras que una serie semanal proporciona los niveles extremos y la señal de media móvil. Cada vez que se completa una vela del lunes, la estrategia evalúa si la semana anterior terminó por encima de la banda de máximos recientes o por debajo de la banda de mínimos recientes, o si el valor anterior de la media móvil está en el otro lado del precio de apertura semanal actual. La suposición es que tales movimientos sobreextendidos tienden a revertirse a la media durante la semana.

## Cómo funciona

1. Las velas semanales alimentan dos indicadores:
   - `Highest`/`Lowest` encuentran el máximo y mínimo extremos durante `CalcPeriod` semanas.
   - Una media móvil configurable (`MaPeriod`, `MaMethod`, `MaShift`, `AppliedPrice`) procesa las mismas velas semanales.
2. Las velas diarias (o cualquier `TradeCandleType` seleccionado) desencadenan decisiones de trading una vez que se completan.
3. En la primera vela completada cuyo `OpenTime.DayOfWeek == Monday`, la estrategia evalúa las condiciones de entrada:
   - **Largo** si el cierre semanal anterior está por encima del máximo más alto del lookback o si el valor anterior de la MA es mayor que el precio de apertura semanal actual (lo que significa que el precio abrió por debajo de la MA).
   - **Corto** si el cierre semanal anterior está por debajo del mínimo más bajo del lookback o si el valor anterior de la MA es menor que el precio de apertura semanal actual (precio abrió por encima de la MA).
4. Las órdenes se envían con `BuyMarket` o `SellMarket` usando el volumen de la estrategia sin promediación. Solo puede haber una posición abierta a la vez.

## Gestión de salidas

- Una distancia de stop-loss fija se calcula como `StopLossPips * Security.PriceStep`. Cuando está habilitado (> 0), la estrategia monitorea los máximos y mínimos de las velas diarias; si el precio toca el nivel de stop dentro del día, la posición se cierra a mercado.
- Una salida basada en tiempo cierra cualquier posición abierta una vez que han pasado siete días desde la entrada (`604800` segundos en el EA original). La comprobación se realiza en cada vela diaria completada.
- La estrategia nunca abre una nueva operación hasta que la anterior esté completamente cerrada.

## Indicadores y datos

- **Extremos semanales:** indicadores `Highest` y `Lowest` adjuntos a la serie `MaCandleType` (velas de 1 semana por defecto).
- **Media móvil semanal:** se dispone de los métodos `Simple`, `Exponential`, `Smoothed` o `LinearWeighted`. La media móvil puede desplazarse hacia adelante por `MaShift` barras para imitar la configuración de MetaTrader y puede consumir diferentes fuentes de precio (`AppliedPrice`).
- **Marco temporal primario:** `TradeCandleType` define qué velas impulsan el timing de las operaciones; el valor predeterminado son las velas diarias para que las entradas se evalúen después del cierre del primer día de la semana de trading.

## Parámetros

| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `CalcPeriod` | `int` | `4` | Número de velas de marco temporal superior usadas para calcular el máximo y el mínimo. |
| `StopLossPips` | `int` | `300` | Distancia de stop-loss en pasos de precio. Establezca en `0` para deshabilitar el stop de protección. |
| `MaPeriod` | `int` | `7` | Longitud de la media móvil semanal. |
| `MaShift` | `int` | `0` | Desplazamiento hacia adelante de la media móvil en barras. Refleja el parámetro de desplazamiento de MA de MetaTrader. |
| `MaMethod` | `MovingAverageMethod` | `LinearWeighted` | Método de cálculo de la media móvil (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). |
| `AppliedPrice` | `AppliedPriceType` | `Weighted` | Fuente de precio alimentada a la media móvil (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). |
| `TradeCandleType` | `DataType` | `TimeSpan.FromMinutes(5).TimeFrame()` | Marco temporal primario que desencadena las entradas y gestiona los stops/salidas. |
| `MaCandleType` | `DataType` | `TimeSpan.FromDays(7).TimeFrame()` | Marco temporal superior usado para la media móvil y para calcular los extremos. |

## Notas

- La distancia de stop-loss se adapta al instrumento multiplicando el recuento de pips por `Security.PriceStep`. Los instrumentos sin un paso definido deshabilitarán efectivamente el stop.
- Dado que la estrategia evalúa velas completadas, las entradas ocurren al cierre de la barra del lunes en lugar del primer tick de la semana. Esto mantiene el comportamiento determinista entre los backtests.
- La lógica asume solo una posición abierta; cualquier operación abierta se cierra por el stop-loss o por el timeout de siete días antes de que se considere una nueva señal.
