# Estrategia Demarker Martingale (StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Demarker Martingale** recrea el asesor experto de MetaTrader "Demarker Martingale" usando la API de alto nivel de StockSharp. El sistema combina una señal del oscilador DeMarker de medio plazo con un filtro de tendencia MACD de marco temporal superior. Las entradas son seguidas por un dimensionamiento de posición al estilo martingala, niveles fijos de stop-loss y take-profit, protección de break-even y un trailing stop que imita el conjunto de herramientas de gestión monetaria del experto original.

## Lógica de operación principal
1. **Feeds de datos** – la estrategia se suscribe a un marco temporal de trading definido por el usuario (velas de 15 minutos por defecto) para la generación de señales y una serie de marco temporal superior (velas mensuales por defecto) para calcular el filtro MACD.
2. **Disparador DeMarker** – cuando el valor de DeMarker supera el límite neutral `DemarkerThreshold` (por defecto 0.5) y la acción de precio reciente forma una superposición alcista (`Low[2] < High[1]`), se considera una configuración larga. A la inversa, una superposición bajista con DeMarker por debajo del umbral prepara un corto.
3. **Confirmación MACD** – el MACD de marco temporal superior debe coincidir con la dirección. Una señal alcista requiere que la línea principal de MACD esté por encima de su línea de señal, mientras que una señal bajista espera la relación opuesta. Esto reproduce el filtro MACD mensual del experto MQL.
4. **Ejecución de órdenes** – las señales válidas colocan órdenes de mercado con el volumen ajustado por martingala. Solo se mantiene una posición direccional a la vez.
5. **Monitoreo de posición** – mientras hay una posición abierta, la estrategia evalúa cada vela finalizada para detectar disparadores de stop-loss, take-profit, break-even o trailing stop. Los eventos de brecha cierran la posición completa mediante órdenes de mercado.

## Gestión monetaria
- **Dimensionamiento inicial** – las órdenes comienzan con `InitialVolume` alineado al `VolumeStep` del instrumento y acotado por `VolumeMin`/`VolumeMax`.
- **Escalada martingala** – después de un trade perdedor, el siguiente volumen se multiplica por `MartingaleMultiplier` (`DoubleLotSize = true`) o se incrementa por `LotIncrement`. Los trades rentables restablecen la escalera al volumen base. La profundidad de escalada está limitada por `MaxMartingaleSteps` para evitar una exposición descontrolada.
- **Stop-loss y take-profit** – las distancias se expresan en pips al estilo MetaTrader. El tamaño del pip se adapta automáticamente a las cotizaciones Forex de 3/5 dígitos, coincidiendo con la lógica `ticksize` original.
- **Break-even** – una vez que la ganancia no realizada alcanza `BreakEvenTriggerPips`, el stop-loss se desplaza a la entrada más `BreakEvenOffsetPips` (largo) o menos el offset (corto).
- **Trailing stop** – las ganancias más allá de `TrailingStopPips` mueven un umbral de trailing interno que se ajusta con cada vela, replicando el comportamiento `TrailingStop` del EA.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `CandleType` | Marco temporal de trading usado para señales DeMarker. |
| `MacdCandleType` | Marco temporal superior usado para calcular el filtro de tendencia MACD. |
| `DemarkerPeriod` | Período de lookback de DeMarker. |
| `DemarkerThreshold` | Límite neutral entre configuraciones alcistas y bajistas. |
| `MacdFast` / `MacdSlow` / `MacdSignal` | Longitudes EMA del MACD. |
| `InitialVolume` | Tamaño de orden base antes de los ajustes martingala. |
| `MartingaleMultiplier` | Factor de multiplicación cuando `DoubleLotSize` está habilitado. |
| `LotIncrement` | Incremento aditivo cuando el doblado está deshabilitado. |
| `DoubleLotSize` | Alternar entre martingala multiplicativa y aditiva. |
| `MaxMartingaleSteps` | Número máximo de escalaciones consecutivas. |
| `StopLossPips` | Distancia de stop-loss en pips. |
| `TakeProfitPips` | Distancia de take-profit en pips. |
| `TrailingStopPips` | Distancia de trailing stop en pips. |
| `UseBreakEven` | Habilitar o deshabilitar la lógica de break-even. |
| `BreakEvenTriggerPips` | Umbral de ganancia (en pips) antes de cambiar a break-even. |
| `BreakEvenOffsetPips` | Buffer aplicado al stop de break-even. |

## Notas de conversión
- La conversión de pip refleja el EA MQL (`ticksize == 0.00001` o `0.001` implica una escala de pip 10x). Esto preserva distancias de riesgo consistentes en cotizaciones de 3/5 dígitos.
- El filtro de tendencia MACD usa `MovingAverageConvergenceDivergenceSignal` con las longitudes EMA originales y procesa una serie de velas separada para emular la lógica del gráfico mensual.
- El seguimiento de la contabilidad martingala rastrea los precios de entrada ponderados promedio y el PnL realizado para decidir si el siguiente trade debe escalar o reiniciar.
- Todas las acciones protectoras (stop-loss, take-profit, break-even, trailing) se ejecutan mediante salidas a mercado porque la API de alto nivel desaconseja las modificaciones directas de órdenes bajo la guardia `StartProtection`.

## Consejos de uso
- Asegúrese de que el instrumento asignado exponga `PriceStep`, `VolumeStep`, `VolumeMin` y `VolumeMax` para alinear los cálculos de pip y el redondeo de volumen con las restricciones del exchange.
- Experimente con `MacdCandleType` (p. ej., velas semanales) para ajustar el filtro de tendencia para mercados más rápidos.
- Al optimizar, ajuste conjuntamente `DemarkerThreshold`, `TrailingStopPips` y los parámetros martingala para mantener los drawdowns bajo control.
- Combine la estrategia con controles de riesgo a nivel de cartera o filtros de sesión de trading al desplegar en vivo, ya que las secuencias martingala inherentemente aumentan la exposición después de las pérdidas.
