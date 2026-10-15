# Pendiente RSI Estrategia MTF
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Slope RSI MTF** porta el MetaTrader 4 asesores expertos `SLOPE_RSI_MTF_LBranjord.mq4` junto con su indicador complementario `Slope_Direction_Line_Alert.mq4`. La configuración original apiló múltiples promedios móviles de Hull (llamados "Línea de dirección de pendiente") en varios períodos de tiempo y solo abrió operaciones cuando todas apuntaban en la misma dirección, mientras que un filtro RSI de cuatro niveles confirmaba el impulso. La versión StockSharp reproduce esta lógica de confirmación de múltiples períodos de tiempo con suscripciones de alto nivel, mantiene los objetivos de salida basados ​​en ATR y agrega un amplio soporte de configuración a través de parámetros de estrategia.

## Lógica comercial
1. Suscríbase a cuatro series de velas para el mismo instrumento: el período de negociación (`BaseTimeframe`), una serie de confirmación horaria, una serie de cuatro horas y una serie diaria.
2. Introduzca cada serie en su propia instancia `HullMovingAverage` (el reemplazo StockSharp de la línea de dirección de pendiente) y `RelativeStrengthIndex`. La serie base usa `SlopeTriggerLength` (predeterminado 60) mientras que la serie de confirmación usa `SlopeTrendLength` (predeterminado 200).
3. Realice un seguimiento de los dos últimos valores de Hull por período de tiempo. Un período de tiempo se considera alcista cuando el valor actual de Hull está estrictamente por encima del anterior; es bajista cuando el valor de Hull está estrictamente por debajo del valor anterior.
4. Supervise simultáneamente el RSI en cada período de tiempo:
   - Configuración larga: RSI debe estar por encima de `RsiMiddleLevel` (50 de forma predeterminada) pero por debajo de `RsiUpperBound` (90) en las cuatro series.
   - Configuración breve: RSI debe estar por debajo de `RsiMiddleLevel` pero por encima de `RsiLowerBound` (10) en las cuatro series.
5. Cuando se cierre el período de tiempo base y todas las confirmaciones sean alcistas, active una señal larga. Si todas las confirmaciones son bajistas, se activa una señal corta. Las señales se ignoran hasta que cada indicador haya producido al menos un valor histórico.
6. Antes de agregar una nueva posición, calcule las distancias de protección a partir de los valores ATR:
   - La serie horaria proporciona la distancia de stop-loss.
   - La serie diaria proporciona la distancia de obtención de beneficios.
7. Las entradas al mercado añaden exposición en la dirección de la señal respetando `MaxOrders`. En el entorno de compensación, la exposición opuesta se nivela antes de agregar una nueva operación.
8. Los niveles de protección se recalculan en cada escalamiento y se evalúan en las velas posteriores del período de tiempo base. Si el máximo/mínimo de la vela cruza el nivel de stop-loss o take-profit almacenado, la estrategia sale de la posición completa con una orden de mercado.

## Gestión de riesgos y dimensionamiento de posiciones.
- `UseCompounding` habilita la regla de capitalización del experto MQL: `volume = PortfolioValue / BalanceDivider`. Cuando está deshabilitado, se usa `BaseVolume` en su lugar.
- El asistente `AdjustVolume` redondea el volumen solicitado al `VolumeStep` del valor y aplica `MinVolume`/`MaxVolume`. El valor ajustado también se escribe en `Strategy.Volume` para que las acciones manuales sigan el mismo tamaño.
- El período ATR (`AtrPeriod`, predeterminado 21) refleja la configuración original para los cálculos de stop-loss y take-profit. La parada utiliza el ATR horario mientras que el objetivo de ganancias utiliza el ATR diario.
- Los contadores de posición (`_longEntries`, `_shortEntries`) garantizan que no haya más de `MaxOrders` escalados activos en cualquier dirección a la vez.

## Manejo de datos de múltiples períodos de tiempo
- Todas las suscripciones se crean con `SubscribeCandles(...)` y se procesan a través de `Bind`. La estrategia no almacena en caché las velas históricas manualmente; Los indicadores reaccionan a la transmisión de datos y exponen sus valores finales a través de las devoluciones de llamada `Bind`.
- El ayudante `TimeframeState` almacena los valores de Hull y RSI junto con la lectura de Hull anterior, lo que permite realizar comparaciones de pendientes sin solicitar buffers de indicadores históricos.
- Los valores ATR se toman solo cuando el indicador correspondiente informa `IsFormed`, lo que garantiza que las paradas y los objetivos se calculan a partir de barras completas.

## Parámetros
| Nombre | Tipo | Predeterminado | MetaTrader contraparte | Descripción |
| --- | --- | --- | --- | --- |
| `SlopeTriggerLength` | `int` | `60` | `SDL1_trigger` | Eslora del casco en el marco temporal de negociación. |
| `SlopeTrendLength` | `int` | `200` | `SDL1_period` | Eslora del casco en confirmaciones horarias, de cuatro horas y diarias. |
| `RsiPeriod` | `int` | `14` | RSI período | RSI retrospectiva aplicada a cada período de tiempo. |
| `RsiLowerBound` | `decimal` | `10` | RSI límite inferior | Filtro inferior RSI para señales cortas. |
| `RsiMiddleLevel` | `decimal` | `50` | RSI nivel medio (implícito) | Nivel neutro RSI que separa regímenes largos y cortos. |
| `RsiUpperBound` | `decimal` | `90` | RSI límite superior | Filtro superior RSI para señales largas. |
| `AtrPeriod` | `int` | `21` | `ATR_Period` | ATR longitud para cálculos de stop y take-profit. |
| `MaxOrders` | `int` | `5` | `MaxOrders` | Número máximo de entradas escaladas por dirección. |
| `UseCompounding` | `bool` | `true` | `compounding` | Permite dimensionar las posiciones basadas en la cartera. |
| `BaseVolume` | `decimal` | `0.1` | `Lots` | Lote fijo cuando la capitalización está deshabilitada. |
| `BalanceDivider` | `decimal` | `100000` | implícito (`AccountBalance()/100000`) | Divisor para la fórmula compuesta. |
| `BaseTimeframe` | `DataType` | `5m` | cronograma del gráfico | Serie de velas que impulsa la ejecución comercial. |
| `HourTimeframe` | `DataType` | `1h` | `PERIOD_H1` | Primera serie de confirmación. |
| `FourHourTimeframe` | `DataType` | `4h` | `PERIOD_H4` | Segunda serie de confirmación. |
| `DayTimeframe` | `DataType` | `1d` | `PERIOD_D1` | Serie de confirmación más alta. |

## Diferencias con el asesor experto original
- StockSharp opera en modo de compensación, por lo que las posiciones opuestas se cierran antes de abrir una nueva operación. MetaTrader 4 permitía cubrir múltiples tickets en ambas direcciones.
- Los objetivos y paradas de protección se ejecutan mediante monitoreo basado en velas en lugar de modificaciones de órdenes por parte del corredor. Esto mantiene la lógica dentro de la estrategia mientras reproduce las distancias ATR del EA original.
- Los valores del indicador los proporcionan los `HullMovingAverage`, `RelativeStrengthIndex` y `AverageTrueRange` integrados de StockSharp. No se accede directamente a búferes de indicadores personalizados, lo que cumple con las mejores prácticas de alto nivel API.
- Los metadatos de parámetros, los nombres fáciles de localizar y las sugerencias de rango se exponen a través de `Param(...).SetDisplay(...)`, lo que hace que la estrategia sea más fácil de configurar y optimizar.

## Notas de uso
- Mantenga los plazos de confirmación estrictamente mayores o iguales al plazo de negociación. Mezclar períodos más cortos puede producir señales contradictorias y anula el propósito de la confirmación de pendiente de múltiples marcos de tiempo.
- Asegúrese de que los metadatos de seguridad (`PriceStep`, `VolumeStep`, `MinVolume`, `MaxVolume`) estén completos para que el redondeo de parada/objetivo y los ajustes de volumen se comporten correctamente.
- Debido a que el monitoreo de stop-loss y take-profit ocurre una vez por vela base completada, las salidas intrabar se producirán en el siguiente cierre de la barra. Si se requiere una gestión intrabar más estricta, reduzca el plazo de negociación o amplíe la estrategia con un seguimiento a nivel de tick.
- La prueba de pendiente de Hull requiere que los valores consecutivos difieran. Las secuencias Flat Hull (valores iguales) bloquean nuevas operaciones incluso si pasan los filtros RSI, reflejando la condición "SDL > SDL[1]" del script MetaTrader.
