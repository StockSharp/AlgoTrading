# Estrategia Williams AO + AC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **estrategia Williams AO + AC** convierte el experto de MetaTrader 4 "Williams_AOAC" a la API de estrategias de alto nivel de StockSharp. El enfoque combina varias herramientas de Bill Williams para encontrar ráfagas de momentum en el gráfico horario (marco temporal predeterminado):

1. **Filtro Bollinger Band** - la estrategia opera solo cuando la anchura de la banda está dentro de un rango configurable de puntos, lo que ayuda a evitar mercados planos y volatilidad excesiva.
2. **Confirmación Relative Strength Index** - el RSI debe estar por encima de un umbral alcista para largos o por debajo de un umbral bajista para cortos.
3. **Cruce de línea cero de Awesome Oscillator** - el oscilador debe cruzar el eje cero en la dirección de la operación, señalando un cambio de momentum.
4. **Aceleración de Accelerator Oscillator** - los tres últimos valores de Accelerator deben estar en el mismo lado de cero y la barra más reciente debe extender ese movimiento, confirmando aceleración.
5. **Filtro de sesión de trading** - las entradas solo se permiten dentro de una ventana temporal configurable expresada en horas del día.

En cada vela completada, la estrategia procesa los valores de indicadores entregados por la canalización `Bind`. Cuando todos los filtros se alinean, cierra una posición opuesta si es necesario y abre una nueva orden de mercado con el tamaño de lote solicitado. Stop-loss y take-profit se aplican usando distancia en puntos de precio, y un trailing stop opcional puede ajustar el stop de protección después de que la operación se vuelve rentable.

## Reglas de entrada
### Condiciones largas
1. El spread de Bollinger (banda superior menos banda inferior convertido a puntos) está entre **BollingerSpreadLower** y **BollingerSpreadUpper**.
2. La lectura RSI es estrictamente mayor que **RsiBuyThreshold**.
3. Awesome Oscillator cruza de negativo a positivo en la barra actual.
4. Los valores de Accelerator Oscillator de las tres últimas velas son todos positivos y el valor más reciente es mayor que el anterior, señalando momentum alcista creciente.
5. La hora de apertura de la barra actual cae dentro de la ventana de trading que comienza en **EntryHour** y se extiende durante **TradingWindowHours** horas (envolviendo la medianoche si es necesario).
6. La estrategia aún no mantiene una posición larga (puede estar plana o corta).

Cuando la lógica se satisface, la estrategia cierra cualquier exposición corta, abre una orden larga de mercado con **TradeVolume** y aplica las distancias configuradas de stop-loss / take-profit. El seguimiento de trailing stop comienza después de que la operación se mueve a favor al menos **TrailingStopPoints**.

### Condiciones cortas
1. El spread de Bollinger está dentro del rango permitido.
2. La lectura RSI es estrictamente menor que **RsiSellThreshold**.
3. Awesome Oscillator cruza de positivo a negativo en la barra actual.
4. Los valores de Accelerator Oscillator de las tres últimas velas son todos negativos y el valor más reciente es menor que el anterior, indicando presión bajista creciente.
5. La hora de apertura de la vela está dentro de la ventana de sesión de trading.
6. La estrategia aún no mantiene una posición corta (puede estar plana o larga).

Cuando se activa, el módulo cierra la exposición larga, entra en una orden corta de mercado con **TradeVolume** y reasigna las órdenes de protección.

## Gestión de salida
* **Take-profit** - si **TakeProfitPoints** es mayor que cero, se adjunta a cada nueva posición un objetivo de ganancia igual a esa cantidad de puntos de precio desde el precio de entrada.
* **Stop-loss** - si **StopLossPoints** es mayor que cero, se aplica un stop fijo relativo al precio de entrada.
* **Trailing stop** - si **TrailingStopPoints** es mayor que cero, el stop-loss se mueve más cerca del mercado cuando la ganancia supera la distancia trailing. Para operaciones largas, el stop se eleva a `Close - TrailingStopPoints * pip`; para cortos, se baja a `Close + TrailingStopPoints * pip`. El trailing es unidireccional: el stop nunca retrocede.
* Los cambios manuales de posición por parte del usuario se respetan; la lógica trailing reacciona a la posición agregada actual reportada por el motor.

## Parámetros
| Nombre | Descripción | Predeterminado |
|------|-------------|----------------|
| `CandleType` | Serie de velas principal usada para cálculos. | Velas de 1 hora |
| `BollingerPeriod` | Período retrospectivo para Bollinger Bands. | 20 |
| `BollingerDeviation` | Multiplicador de desviación estándar. | 2.0 |
| `BollingerSpreadLower` | Anchura mínima de banda en puntos requerida para habilitar el trading. | 40 |
| `BollingerSpreadUpper` | Anchura máxima de banda en puntos permitida para trading. | 210 |
| `AoFastPeriod` | Período corto de Awesome Oscillator. | 11 |
| `AoSlowPeriod` | Período largo de Awesome Oscillator. | 40 |
| `RsiPeriod` | Longitud de cálculo RSI. | 20 |
| `RsiBuyThreshold` | Valor RSI mínimo para operaciones largas. | 46 |
| `RsiSellThreshold` | Valor RSI máximo para operaciones cortas. | 40 |
| `EntryHour` | Hora (0-23) en que comienza la ventana de trading. | 0 |
| `TradingWindowHours` | Duración de la ventana de trading permitida en horas (`0` mantiene solo la hora inicial). | 20 |
| `TradeVolume` | Tamaño de lote para cada nueva posición. | 0.01 |
| `StopLossPoints` | Distancia de stop-loss en puntos de precio. | 60 |
| `TakeProfitPoints` | Distancia de take-profit en puntos de precio. | 90 |
| `TrailingStopPoints` | Distancia de trailing stop en puntos de precio. | 30 |

## Notas adicionales
* El valor de Accelerator Oscillator se deriva internamente restando una media móvil simple de 5 períodos de Awesome Oscillator de la lectura AO actual, lo que coincide con la implementación de MetaTrader usada por el experto original.
* Los cálculos de spread de banda dependen del `PriceStep` del instrumento. Cuando no está disponible, la estrategia recurre a diferencias de precio sin procesar.
* La ventana de sesión de trading envuelve la medianoche cuando `EntryHour + TradingWindowHours` supera 23, reproduciendo el filtro horario de MetaTrader.
* La estrategia cierra automáticamente la exposición opuesta antes de abrir una nueva posición, replicando el límite de una sola orden del código MQL4 original.
