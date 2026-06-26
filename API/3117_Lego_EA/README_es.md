# Estrategia Lego EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia Lego EA** es un puerto directo del asesor experto "Lego EA" de MetaTrader. Utiliza una combinación configurable de filtros técnicos—Commodity Channel Index, dobles medias móviles, oscilador estocástico, Accelerator Oscillator, DeMarker y Awesome Oscillator—para validar entradas y salidas. Cada filtro puede activarse o desactivarse independientemente para entradas y salidas, lo que permite reconstruir el "Lego" original bloque por bloque o experimentar con configuraciones personalizadas.

## Parámetros
- `Volume` – volumen de trading base usado cuando la operación anterior fue rentable.
- `LotMultiplier` – multiplicador aplicado al último volumen ejecutado después de una operación perdedora (recuperación tipo martingale).
- `StopLossPips` – stop de protección expresado en pips (convertido internamente usando el tamaño de tick del símbolo).
- `TakeProfitPips` – objetivo de ganancia en pips.
- `UseCciForEntry` / `UseCciForExit` – activar el filtro CCI al abrir o cerrar posiciones.
- `UseMaForEntry` / `UseMaForExit` – usar el cruce de medias móviles rápida/lenta para confirmaciones.
- `UseStochasticForEntry` / `UseStochasticForExit` – requerir alineación del estocástico %K/%D dentro de los umbrales configurados.
- `UseAcceleratorForEntry` / `UseAcceleratorForExit` – requerir patrones de aceleración del Accelerator Oscillator.
- `UseDemarkerForEntry` / `UseDemarkerForExit` – aplicar comprobaciones de nivel DeMarker.
- `UseAwesomeForEntry` / `UseAwesomeForExit` – incluir confirmación de momentum del Awesome Oscillator.
- `CciPeriod` – período del Commodity Channel Index.
- `MaFastPeriod` / `MaSlowPeriod` – longitudes de lookback para las medias móviles rápida y lenta.
- `MaShift` – número de barras completadas para desplazar los valores de media móvil hacia atrás en el tiempo, reproduciendo el parámetro de desplazamiento horizontal de MT5.
- `MaMethod` – método de suavizado (simple, exponencial, suavizado o ponderado).
- `MaPrice` – fuente de precio de la vela suministrada a ambas medias móviles.
- `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlow` – configuración del oscilador estocástico.
- `StochasticLevelUp` / `StochasticLevelDown` – umbrales de sobrecompra/sobreventa usados para señales.
- `DemarkerPeriod`, `DemarkerLevelUp`, `DemarkerLevelDown` – configuración del oscilador DeMarker.
- `CandleType` – marco temporal de la serie de velas usada por todos los indicadores.

## Flujo de trabajo de trading
1. En cada vela completada, la estrategia recopila valores de indicadores de los filtros seleccionados.
2. Cada filtro calcula la disponibilidad de compra/venta basándose en la barra anterior completamente formada (coincidiendo con el desplazamiento `iGetArray(..., 1)` del EA original).
3. Solo se permite una entrada larga cuando **todos los filtros de entrada habilitados** acuerdan una señal alcista. Del mismo modo, una entrada corta requiere confirmación bajista unánime.
4. Si la cuenta está plana y aparece una señal de entrada válida, se envía una orden de mercado usando el `Volume` base o el último volumen de operación perdedora multiplicado por `LotMultiplier`.
5. Cuando ya hay una posición, los filtros de salida habilitados se evalúan de la misma manera. La posición se cierra solo cuando todos los filtros de salida acuerdan una señal opuesta.
6. La protección de stop-loss y take-profit se instala automáticamente usando `StartProtection`, convirtiendo las entradas en pips a distancias de precio absolutas basadas en el tamaño de tick del símbolo.

## Gestión del dinero
- Después de una operación ganadora, la siguiente orden vuelve al `Volume` base.
- Después de una operación perdedora, el volumen se multiplica por `LotMultiplier`, emulando la lógica de escalada de lotes del EA original.
- Los límites de volumen impuestos por el exchange (paso, mín. y máx.) se aplican antes de cada orden.

## Notas y diferencias respecto a la versión de MetaTrader
- Las fuentes de precio del indicador se mapean a equivalentes de StockSharp. CCI usa el precio típico internamente y las medias móviles usan la fuente `MaPrice` seleccionada.
- Todos los cálculos de indicadores dependen de velas completamente cerradas. Esto evita datos parcialmente formados y emula el procesamiento de "nueva barra" del EA.
- Las comprobaciones del nivel de freeze y la colocación manual de precio de SL/TP son manejadas por el servicio `StartProtection` de StockSharp.
- Las salidas parciales de posición actualizan el estado de seguimiento de pérdidas solo cuando toda la posición es flat, coincidiendo con la lógica `DEAL_ENTRY_OUT` del EA.

## Consejos de uso
- Comience con la configuración original (filtro MA habilitado, otros filtros deshabilitados) para reproducir el comportamiento base, luego habilite filtros adicionales para mejorar la calidad de la señal.
- Monitoree la exposición de la cuenta cuando use valores altos de `LotMultiplier`; el riesgo crece rápidamente durante rachas de pérdidas.
- Combine la estrategia con el Backtester para confirmar si su mezcla de filtros elegida se alinea con los instrumentos que planea operar.

Esta estrategia actualmente no tiene versión en Python.
