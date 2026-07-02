# Estrategia Natuseko Protrader 4H
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Natuseko Protrader 4H es una versión StockSharp del asesor experto MetaTrader 4 *NatusekoProtrader4HStrategy*. el original
El robot combina medias móviles exponenciales, un oscilador MACD filtrado por Bollinger bandas, RSI umbrales y el Parabolic SAR para
identifique velas de ruptura fuertes en el marco de tiempo de cuatro horas. Cuando aparece una vela calificada, el sistema se abre inmediatamente o
espera un retroceso hacia el EMA rápida antes de entrar. Una vez posicionada, la estrategia realiza una toma de ganancias parcial y salidas completas.
basado en señales RSI y Parabolic SAR, replicando el bloque de administración de dinero presente en el código MQL.

## Lógica comercial
1. Suscríbase al flujo de velas principal definido por `CandleType` (velas de 4 horas de forma predeterminada) y procese solo velas terminadas.
2. Calcule tres medias móviles exponenciales (rápida, lenta y de tendencia) sobre los precios de cierre. Los tres tienen longitudes configurables.
3. Alimente el indicador MACD (períodos rápido, lento y de señal tomados del EA) y aplique una media móvil simple más Bollinger bandas a
la línea principal MACD. La línea media Bollinger actúa como nivel de referencia utilizado por la versión MQL.
4. Calcule el RSI sobre los precios de cierre y el Parabolic SAR utilizando datos completos de velas. Estos indicadores impulsan tanto las entradas como las salidas.
5. Detecte velas de configuración alcista cuando se cumplan todas las condiciones siguientes:
   - El EMA rápida está por encima tanto del lento como de la tendencia EMA.
   - RSI está por encima de `RsiEntryLevel` pero por debajo de `RsiTakeProfitLong`.
   - La línea principal MACD está por encima de su línea corta SMA y de la línea media Bollinger; el SMA también está por encima de la línea media.
   - El cuerpo de la vela es más grande que ambas sombras, lo que significa que la vela se cierra con fuerza en la dirección del movimiento.
   - Parabolic SAR se encuentra debajo del cierre de la vela.
6. Detecte configuraciones bajistas utilizando los controles simétricos (EMA rápida a continuación, RSI entre `RsiTakeProfitShort` y `RsiEntryLevel`, valores MACD
debajo de la línea media Bollinger, cuerpo de la vela bajista y SAR por encima del cierre).
7. Si la vela calificada está demasiado lejos de la tendencia EMA (distancia por encima de `DistanceThresholdPoints`), establezca una bandera pendiente y espere una
retroceso. Una entrada larga se activa una vez que el precio toca el EMA rápida mientras que RSI y SAR permanecen alineados con el escenario alcista; el
La entrada corta funciona de manera análoga en los retrocesos al EMA rápida desde abajo.
8. Cuando no se requiere un retroceso, la estrategia cierra cualquier exposición opuesta y abre una nueva posición con `TradeVolume` lotes. Stop Loss
La ubicación sigue las reglas de EA: la primera preferencia se da a Parabolic SAR si `UseSarStopLoss` está habilitado; de lo contrario, la tendencia
Se utiliza EMA. `StopOffsetPoints` se convierte en precio distancia con el paso del precio del instrumento y se aplica al nivel de parada.
9. Mientras una posición larga está abierta, la estrategia recalcula continuamente el precio stop y gestiona las salidas:
   - Si el precio cae por debajo del stop, se cierra toda la posición.
   - Después de alcanzar al menos `MinimumProfitPoints` de ganancia (en puntos de instrumento), la estrategia puede cerrar la mitad de la posición cuando el
RSI excede `RsiTakeProfitLong` o cuando el Parabolic SAR supera el precio (controlado por `UseRsiTakeProfit` y
`UseSarTakeProfit`).
   - Una vez que el beneficio es adecuado y RSI vuelve a caer por debajo de `RsiEntryLevel`, se cierra la exposición larga restante.
10. Las posiciones cortas reflejan las mismas reglas con los umbrales RSI invertidos y los cheques SAR invertidos en relación con el precio.

## Gestión de posiciones
- Las salidas parciales ocurren como máximo una vez por lado comercial. Después de cerrar la mitad de la posición, la estrategia espera la condición de salida total.
(RSI cruzando nuevamente el nivel neutral o un hit de stop-loss).
- Los precios de stop-loss se recalculan en cada vela utilizando el último valor Parabolic SAR o tendencia EMA para mantenerse alineados con la lógica MQL.
- Cuando el tamaño de la posición vuelve a cero, el estado interno (indicadores de entrada pendiente, referencias de parada y marcadores de salida parcial) se restablece para que el tamaño de la posición vuelva a cero.
La próxima operación comienza limpiamente.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | plazo de 4 horas | Plazo primario procesado por la estrategia. |
| `TradeVolume` | `decimal` | `0.1` | Volumen de pedidos utilizado para las entradas. |
| `FastEmaPeriod` | `int` | `13` | Longitud del filtro EMA rápida. |
| `SlowEmaPeriod` | `int` | `21` | Longitud del filtro EMA más lento. |
| `TrendEmaPeriod` | `int` | `55` | EMA se utiliza para comprobaciones a distancia y colocación de stop-loss. |
| `MacdFastPeriod` | `int` | `5` | Longitud rápida EMA dentro del indicador MACD. |
| `MacdSlowPeriod` | `int` | `200` | Longitud lenta de EMA dentro del indicador MACD. |
| `MacdSignalPeriod` | `int` | `1` | Longitud de la media móvil de la señal dentro del indicador MACD. |
| `BollingerPeriod` | `int` | `20` | Número de MACD muestras utilizadas para calcular Bollinger bandas. |
| `BollingerWidth` | `decimal` | `1` | Multiplicador de desviación estándar para las Bandas MACD Bollinger. |
| `MacdSmaPeriod` | `int` | `3` | Longitud del suavizado MACD SMA. |
| `RsiPeriod` | `int` | `21` | Longitud del indicador RSI. |
| `RsiEntryLevel` | `decimal` | `50` | Umbral neutral RSI compartido por las reglas de entrada y salida. |
| `RsiTakeProfitLong` | `decimal` | `65` | RSI nivel que permite la toma de ganancias parcial para posiciones largas. |
| `RsiTakeProfitShort` | `decimal` | `35` | RSI nivel que permite la toma de ganancias parcial para posiciones cortas. |
| `DistanceThresholdPoints` | `decimal` | `100` | Distancia máxima en puntos del instrumento entre el precio y la tendencia EMA antes de que se retrase la entrada. |
| `SarStep` | `decimal` | `0.02` | Paso de aceleración para el Parabolic SAR. |
| `SarMaximum` | `decimal` | `0.2` | Aceleración máxima para el Parabolic SAR. |
| `UseSarStopLoss` | `bool` | `false` | Utilice el Parabolic SAR para derivar la parada de protección. |
| `UseTrendStopLoss` | `bool` | `true` | Utilice la tendencia EMA para derivar la parada de protección. |
| `StopOffsetPoints` | `int` | `0` | Compensación adicional (en puntos) agregada al precio de parada de protección. |
| `UseSarTakeProfit` | `bool` | `true` | Habilite salidas parciales cuando el precio cruce el Parabolic SAR. |
| `UseRsiTakeProfit` | `bool` | `true` | Habilite las salidas parciales cuando RSI alcance el umbral de obtención de beneficios. |
| `MinimumProfitPoints` | `decimal` | `5` | Beneficio mínimo (en puntos) antes de que se activen las reglas de toma de ganancias parcial o total. |

## Diferencias con el EA original
- StockSharp negocia posiciones netas. Para emular el comportamiento de un solo ticket de MetaTrader, la estrategia cierra automáticamente lo contrario.
exposición antes de abrir una nueva operación en la otra dirección.
- Los asistentes de administración de dinero se implementan con órdenes de mercado en lugar de modificar órdenes individuales porque StockSharp no administra
paradas por boleto. El efecto coincide con el EA: una salida parcial seguida de una salida final cuando el impulso de RSI se desvanece.
- Los cálculos de distancia de precio se basan en el instrumento `PriceStep`. Si el valor no define un paso de precio, la estrategia asume un
paso 1. Ajuste `DistanceThresholdPoints` y `MinimumProfitPoints` en consecuencia para instrumentos que utilizan diferentes tamaños de puntos.

## Consejos de uso
- Configure `TradeVolume` según el paso del lote del instrumento; el constructor también asigna el mismo valor a `Strategy.Volume` entonces
Los métodos auxiliares utilizan el tamaño esperado.
- Si las operaciones se retrasan con demasiada frecuencia porque las velas cierran lejos de la tendencia EMA, baje `DistanceThresholdPoints` o desactive el filtro
poniéndolo a cero.
- Se recomienda trazar la estrategia: el código dibuja velas, las tres EMA, RSI, Parabolic SAR y MACD Bollinger bandas para que puedas
Confirme visualmente la lógica convertida.
- Los parámetros MACD reflejan la combinación inusual de EA (rápido=5, lento=200, señal=1). Considere optimizarlos antes de publicarlos
porque un período lento tan amplio produce valores muy suaves pero rezagados.
