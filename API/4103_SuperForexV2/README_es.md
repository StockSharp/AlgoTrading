# Estrategia SuperForexV2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
SuperForexV2 es una StockSharp versión del MetaTrader 4 asesores expertos `SuperForexV2.mq4`. El guión original combina una corta duración
Oscilador de índice de fuerza relativa (RSI) con distancias fijas de toma de ganancias, stop-loss y trailing stop. La implementación de C#
reconstruye el mismo proceso de decisión con el StockSharp API de alto nivel: la estrategia observa velas terminadas, reacciona a RSI
cruces de umbrales y gestiona una única posición neta utilizando límites de riesgo basados en pips.

## Lógica de trading
1. **Canalización de indicadores**
   - Se suscribe a la serie de velas configurables (barras de 15 minutos de forma predeterminada) e introduce cada barra terminada en un indicador RSI.
   - La longitud RSI es configurable y por defecto es el valor MT4 original de 4.
2. **Tamaño de posición dinámico**
   - Antes de cada entrada, la estrategia deriva un tamaño de lote de trabajo del valor actual de la cartera dividido por `BalanceToVolumeDivider`.
   - El volumen resultante se fija mediante `InitialVolume` (retroceso cuando se desconoce el saldo) y `MaxVolume`, luego se redondea al
paso de volumen del instrumento.
3. **Reglas de entrada**
   - Cuando no hay ninguna posición abierta y RSI cae por debajo de `RsiLowerLevel`, se coloca una orden de compra de mercado.
   - Cuando RSI supera `RsiUpperLevel`, se envía una orden de venta de mercado.
4. **Salida y gestión de riesgos**
   - Cada posición almacena niveles absolutos de stop-loss y take-profit calculados a partir de distancias basadas en pips.
   - En cada vela terminada, la estrategia comprueba si la barra ha tocado esos niveles; si es así, cierra la posición en el mercado.
   - Un trailing stop imita la lógica de MT4: una vez que el precio ha avanzado al menos `TrailingStopPips`, el stop se acerca para que el
el beneficio actual está bloqueado.
   - Las posiciones también se cierran cada vez que RSI cruza al extremo opuesto (por ejemplo, los largos salen cuando RSI excede el nivel superior).
5. **Alcance del puesto**
   - El bot refleja el comportamiento de "una operación por símbolo" de EA al aplicar un libro plano antes de evaluar nuevas entradas.

## Parámetros
| Nombre | Descripción | Predeterminado | Notas |
| --- | --- | --- | --- |
| `CandleType` | Serie de velas que impulsa los cálculos del indicador. | `15m` período de tiempo | Acepta cualquier `DataType` admitido por el conector. |
| `RsiPeriod` | RSI longitud retrospectiva. | `4` | Debe ser mayor que cero. |
| `RsiUpperLevel` | Umbral de sobrecompra utilizado para salidas cortas y largas. | `62` | Coincide con la entrada MT4 `Pos`. |
| `RsiLowerLevel` | Umbral de sobreventa utilizado para salidas largas y cortas. | `42` | Coincide con la entrada MT4 `Neg`. |
| `TakeProfitPips` | Distancia de toma de ganancias expresada en pips. | `109` | Establezca en `0` para deshabilitar la obtención de ganancias. |
| `StopLossPips` | Distancia de stop-loss expresada en pips. | `9` | Establezca en `0` para desactivar el stop-loss. |
| `TrailingStopPips` | Distancia del trailing stop expresada en pips. | `6` | Establezca en `0` para desactivar el comportamiento de seguimiento. |
| `InitialVolume` | Tamaño de lote alternativo cuando el saldo de la cartera no está disponible. | `0.1` | También se utiliza si el tamaño dinámico produce un valor no positivo. |
| `MaxVolume` | Volumen máximo permitido por entrada. | `100` | Evita que el tamaño basado en el equilibrio se sobreescale. |
| `BalanceToVolumeDivider` | Divisor aplicado al saldo de la cuenta para calcular el volumen. | `10000` | Replica la fórmula MT4 `Lots = AccountBalance()/10000`. |

## Notas de implementación
- El procesamiento de velas ocurre solo después de `CandleStates.Finished` para reflejar el comportamiento de fin de ciclo de MT4 `start()` y al mismo tiempo evitar
datos incompletos.
- Las distancias de pips se convierten en precios absolutos utilizando el `PriceStep` del instrumento. Para símbolos Forex de 3 y 5 dígitos, el código
multiplica el paso por diez para que el "pip" StockSharp coincida con la definición del punto MetaTrader.
- Los niveles de stop-loss, take-profit y trailing se almacenan internamente y se comparan con los máximos y mínimos de las velas, porque StockSharp
no gestiona automáticamente las paradas a nivel de órdenes estilo MT4.
- La estrategia redondea el volumen calculado al lote válido más cercano respetando `MinVolume`, `MaxVolume` y `VolumeStep`
límites expuestos por la seguridad.
- Sólo se permite una posición neta a la vez; la lógica de entrada sale temprano si la estrategia ya es larga o corta.

## Diferencias en comparación con la versión MT4
- El puerto StockSharp funciona con velas terminadas en lugar de ticks individuales, por lo que se detectan paradas intrabar o aciertos de objetivos en el
cierre del siguiente bar.
- La protección `AccountFreeMargin()` de MetaTrader se reemplaza por un volumen derivado del equilibrio más seguro; si el conector no puede proporcionar la
valor de cartera se utiliza el respaldo `InitialVolume` en lugar de cancelar.
- Los valores de stop-loss y take-profit de la orden no se envían al corredor. En cambio, la estrategia cierra posiciones en el mercado una vez que un nivel
se viola, porque las órdenes StockSharp de alto nivel dependen de salidas administradas por estrategia.
- La entrada `NumeroMagico` utilizada para filtrar pedidos MT4 no es necesaria en StockSharp y se ha omitido.
- Los mensajes de registro del EA original no se reproducen; Se deben utilizar las propias instalaciones de registro de StockSharp si se realizan más
Se necesita instrumentación.
