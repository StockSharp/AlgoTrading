# Estrategia SAR Trading v2.0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia SAR Trading v2.0** recrea el clásico asesor experto Cronex dentro de la API de alto nivel de StockSharp. Combina una media móvil simple (SMA) con el Parabolic SAR para cronometrar las entradas y luego gestiona la posición con órdenes de protección fijas y un trailing stop basado en pips.

- Indicadores: Media Móvil Simple, Parabolic SAR.
- Marco temporal predeterminado: velas de 15 minutos (configurable a través de `CandleType`).
- Mercado: cualquier instrumento que proporcione un valor de `PriceStep` (pip) significativo.

## Lógica de trading
- La estrategia solo evalúa entradas cuando no hay posición abierta.
- **Configuración larga:** o bien el valor del Parabolic SAR cae por debajo de la SMA o el precio de cierre de `MaShift` barras atrás está por debajo de la SMA. Esto refleja la regla MQL `SAR < MA || Close[shift] < MA`.
- **Configuración corta:** o bien el valor del Parabolic SAR sube por encima de la SMA o el cierre de `MaShift` barras atrás está por encima de la SMA.
- Después de enviar una orden de salida, el algoritmo espera hasta que la posición esté plana antes de considerar nuevas señales, coincidiendo con el comportamiento de posición única del EA original.

## Gestión de riesgo
- `StopLossPips` y `TakeProfitPips` convierten pips en distancias de precio absolutas usando `Security.PriceStep`.
- `TrailingStopPips` mantiene el stop de protección a una distancia de pips fija detrás del precio una vez que la operación está en beneficio.
- `TrailingStepPips` exige un buffer adicional de pips antes de mover el trailing stop de nuevo, emulando la lógica de "paso de trailing" del código MQL.
- Si el mercado alcanza los niveles de stop-loss o take-profit, la posición se cierra a mercado.

## Parámetros
- `MaPeriod` (predeterminado **18**): número de barras usadas por la SMA.
- `MaShift` (predeterminado **2**): cuántas barras atrás leer el precio de cierre al comparar con la SMA.
- `SarStep` (predeterminado **0.02**): factor de aceleración del Parabolic SAR.
- `SarMaxStep` (predeterminado **0.2**): factor máximo de aceleración del Parabolic SAR.
- `StopLossPips` (predeterminado **50**): distancia del stop-loss fijo en pips.
- `TakeProfitPips` (predeterminado **50**): distancia del take-profit fijo en pips.
- `TrailingStopPips` (predeterminado **15**): distancia del trailing stop en pips.
- `TrailingStepPips` (predeterminado **5**): ganancia adicional en pips requerida antes de que el trailing stop se mueva de nuevo.
- `CandleType`: suscripción de vela usada para los cálculos.

## Notas adicionales
- La estrategia mantiene un historial interno de cierres para reproducir la llamada `iClose(shift)` usada en la versión MQL.
- Se basa únicamente en velas terminadas para las decisiones, asegurando consistencia con el asesor experto original.
- El volumen se toma de la propiedad `Volume` de la estrategia; por defecto cada señal envía una orden de mercado de un lote.
