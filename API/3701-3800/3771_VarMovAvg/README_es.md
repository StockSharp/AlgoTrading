# Estrategia VarMovAvg
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia VarMovAvg es un sistema de parada y marcha atrás convertido del MetaTrader 4 asesor experto `VarMovAvg_v0011`. Utiliza una media móvil variable (VMA) adaptativa para medir la dirección de la tendencia y espera un patrón de retroceso de dos pasos (llamado Barra A y Barra B en el EA original) antes de revertir la posición. Mientras una posición está activa, un trailing stop basado en la media móvil protege las ganancias e invierte la operación cuando se completa la secuencia opuesta de Barra A/Barra B.

## Lógica de trading
1. **VMA adaptable**: el indicador personalizado `VariableMovingAverage` replica la fórmula MT4:
   - El índice de eficiencia compara el cierre actual con el cierre de hace `AmaPeriod` barras y lo divide por el movimiento absoluto acumulado del precio.
   - El coeficiente de suavizado interpola entre los períodos rápido y lento y se eleva al parámetro `SmoothingPower` al igual que el valor original `G`.
2. **Detección de señal (barra A/barra B)**: dos máquinas de estado independientes rastrean configuraciones largas y cortas:
   - *Barra A*: El precio se mueve `SignalPipsBarA` (en pips) más allá del VMA en la dirección comercial potencial.
   - *Barra B*: El precio se extiende otros `SignalPipsBarB` pips en la misma dirección, bloqueando el precio extremo.
   - *Entrada*: Cuando el cierre regresa a la banda de entrada definida por `SignalPipsTrade ± EntryPipsDiff`, la estrategia entra (o revierte) usando órdenes de mercado.
3. **Trailing Stop y reversión**: mientras una posición está abierta, una media móvil calculada en máximos (para cortos) o mínimos (para largos) se desplaza `StopMaShift` barras y se rellena con `StopPipsDiff`.
   - Si la vela atraviesa el nivel de stop, la posición se cierra.
   - Si la secuencia opuesta de Barra A/Barra B se activa mientras existe una posición, la estrategia emite una orden de mercado única con un tamaño de `|Position| + Volume` para cambiar de dirección inmediatamente, coincidiendo con el comportamiento de EA.

## Parámetros
| Parámetro | Descripción | Fuente MT4 |
|-----------|-------------|------------|
| `AmaPeriod` | Ventana retrospectiva utilizada por el VMA. | `prm.vma.periodAMA` |
| `FastPeriod` | Factor de suavizado rápido dentro del VMA. | `prm.vma.nfast` |
| `SlowPeriod` | Factor de suavizado lento dentro del VMA. | `prm.vma.nslow` |
| `SmoothingPower` | Exponente `G` aplicado al coeficiente adaptativo. | `prm.vma.G` |
| `SignalPipsBarA` | Distancia desde el VMA requerida para aceptar la Barra A. | `prm.sig.pipsBarA` |
| `SignalPipsBarB` | Se requiere distancia adicional para aceptar la barra B. | `prm.sig.pipsBarB` |
| `SignalPipsTrade` | Desplazamiento desde el extremo de la barra B hasta la línea de entrada. | `prm.sig.pipsTrade` |
| `EntryPipsDiff` | Tolerancia aceptada alrededor de la línea de entrada. | `prm.entry.diff` |
| `StopPipsDiff` | Compensación aplicada a la media móvil del trailing stop. | `prm.stop.diff` |
| `StopMaPeriod` | Período del stop de media móvil. | `prm.mastop.period` |
| `StopMaShift` | Desplazamiento (barras) de la media móvil stop. | `prm.mastop.shift` |
| `StopMaMethod` | Método de media móvil (`MODE_SMA`, `EMA`, `SMMA`, `LWMA`). | `prm.mastop.method` |
| `CandleType` | Plazo de trabajo. | Periodo de tiempo del gráfico |

> **Conversión de pips**: todas las distancias de pips se multiplican por `Security.PriceStep` cuando esté disponible. Si el instrumento no tiene un paso configurado, los valores brutos se interpretan en unidades de precio, replicando el respaldo EA.

## Notas de uso
- La estrategia se basa en `SubscribeCandles` y se ejecuta completamente con velas terminadas; la lógica de la banda de entrada refleja las comprobaciones tick a tick del EA utilizando precios de cierre.
- Las órdenes de protección se modelan a través de salidas del mercado cuando la vela cruza el nivel de parada, lo que coincide con el comportamiento de EA porque las órdenes de parada se recalculan en cada tick.
- El cambio de media móvil se implementa a través de un búfer FIFO, lo que garantiza que `StopMaShift = 0` utilice el último valor y que los cambios positivos miren hacia atrás el número de barras solicitado.
- Después de cada operación (entrada, reversión o parada), ambos rastreadores de señales se reinician al estado neutral para evitar órdenes duplicadas, emulando la lógica de reinicio `STATUS_TRADE` en MetaTrader.

## Inicio rápido
1. Agregue la estrategia a un entorno StockSharp y asigne un instrumento con un `PriceStep` y un tamaño de tick válidos.
2. Configure el período de tiempo hasta `CandleType` (el experto original fue probado en gráficos intradiarios como M5).
3. Ajuste las distancias de los pips y los parámetros finales para que coincidan con la precisión de la cotización del corredor.
4. Iniciar la estrategia; alternará entre posiciones largas y cortas siempre que se cumplan las condiciones de la Barra A/Barra B.

## Diferencias con el original EA
- La versión StockSharp funciona con velas cerradas en lugar de con una ejecución tick a tick. La banda de tolerancia de entrada mantiene el tiempo de activación cerca del comportamiento de MT4.
- El manejo de stop-loss se implementa verificando los extremos de las velas en lugar de colocar/modificar órdenes MT4, porque las estrategias StockSharp generalmente administran las salidas mediante programación.
- El indicador `VariableMovingAverage` se implementa directamente en C# y expone el poder de suavizado, eliminando el parámetro `dK` no utilizado que existía en la fuente MQL.
