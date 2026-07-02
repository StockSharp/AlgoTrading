# MACD Ejemplo de estrategia 1010
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Este módulo transfiere el asesor experto MetaTrader **macd_sample_1010.mq4** al API de alto nivel de StockSharp. El script original combinaba Bollinger bandas con reglas simples de administración de dinero: cuando el precio de cierre terminaba por encima de la banda superior más un buffer configurable, abría una orden de venta, mientras que un cierre por debajo de la banda inferior menos el buffer activaba una orden de compra. Las posiciones se cerraban una vez que se alcanzaba una cantidad fija de pérdidas o ganancias (expresada en pips). La versión StockSharp reproduce la misma lógica al suscribirse a la serie de velas solicitada, vincular un indicador `BollingerBands` y emitir órdenes de mercado y llamadas de gestión de posiciones desde la devolución de llamada de la vela.

La conversión mantiene el comportamiento del experto heredado en velas terminadas. Cada evaluación ocurre cuando una vela está completamente formada, lo que garantiza que las decisiones de ruptura y salida coincidan con el procesamiento de cierre de barra de la plataforma MetaTrader. También se implementa el escalado opcional del volumen comercial basado en el saldo para emular el indicador `LotIncrease` del código MQL4.

## Notas de conversión
- Utiliza el flujo de trabajo de alto nivel `SubscribeCandles` + `Bind` para alimentar el indicador `BollingerBands` sin buffers personalizados.
- Emplea la infraestructura StockSharp `StrategyParam<T>` para que todas las entradas sean visibles en la interfaz de usuario y estén listas para la optimización.
- Llama a `BuyMarket` y `SellMarket` con compensaciones calculadas que respetan el `PriceStep` del instrumento, coincidiendo con los cálculos basados en pips en MetaTrader.
- Implementa el escalado de lotes opcional a través de `Portfolio.CurrentValue` (con `BeginValue` como alternativa) y limita el volumen resultante a 500 lotes, al igual que el experto original.
- Funciona estrictamente con velas completas para evitar la agitación paso a paso que el guión original controlaba a través de las barras de la barra.
- Agrega comentarios descriptivos en inglés para aclarar la intención de cada bloque de procesamiento.

## Parámetros
| Parámetro | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `ProfitTargetPips` | `decimal` | `3` | Número de pips de movimiento favorable necesarios para cerrar una posición con beneficios. Establezca en `0` para deshabilitar la regla de obtención de ganancias. |
| `LossLimitPips` | `decimal` | `20` | Número de pips de movimiento adverso tolerados antes de que se liquide la posición. Establezca en `0` para deshabilitar la regla de límite de pérdidas. |
| `BandDistancePips` | `decimal` | `3` | Amortiguador (en pips) agregado por encima de la banda superior y por debajo de la banda inferior antes de que se confirme una ruptura. |
| `BollingerPeriod` | `int` | `4` | Número de velas utilizadas para calcular las Bollinger Bandas. |
| `BollingerDeviation` | `decimal` | `2` | Multiplicador de desviación estándar aplicado por el indicador Bollinger Bandas. |
| `BaseVolume` | `decimal` | `1` | Tamaño inicial de la operación, expresado en lotes. Este valor también se utiliza como base para la lógica de escalado. |
| `LotIncrease` | `bool` | `true` | Cuando está habilitado, vuelve a calcular el volumen comercial en cada vela para que siga la relación entre el saldo actual de la cartera y el saldo inicial. |
| `OneOrderOnly` | `bool` | `true` | Evita que la estrategia abra una nueva posición cuando ya hay una activa. Cuando está deshabilitada, la posición neta aún se administra porque StockSharp usa posiciones agregadas. |
| `CandleType` | `DataType` | `TimeFrame(15m)` | Serie de velas utilizada tanto para cálculos de indicadores como para decisiones comerciales. |

## Lógica comercial
1. `OnStarted` crea el indicador de bandas Bollinger con el período y la desviación configurados, se suscribe al flujo de datos `CandleType` y vincula el método `ProcessCandle`.
2. Cada vela terminada activa `ProcessCandle`, que recalcula el volumen de operaciones actual (si `LotIncrease` está activo) antes de evaluar las señales.
3. Si el precio de cierre es mayor que la banda superior más `BandDistancePips` (convertido a unidades de precio con `PriceStep`), la estrategia envía una orden de venta de mercado. Si el precio de cierre está por debajo de la banda inferior menos el colchón, envía una orden de compra de mercado. Cuando `OneOrderOnly` es `true`, solo se intentan nuevas entradas cuando la posición neta es cero.
4. Después de procesar las entradas potenciales, la estrategia inspecciona la posición actual:
   - Las posiciones largas se cierran una vez que la distancia de beneficio alcanza `ProfitTargetPips` o cuando la pérdida alcanza `LossLimitPips`.
   - Las posiciones cortas se cierran cuando el precio de cierre se mueve a favor de `ProfitTargetPips` o en contra de `LossLimitPips`.
5. Todas las comparaciones de pérdidas y ganancias se realizan en unidades de precio derivadas del símbolo `PriceStep`, replicando fielmente las comprobaciones basadas en pips en el experto MetaTrader.

## Lógica de tamaño de posición
- Cuando `LotIncrease` está deshabilitado, la estrategia intercambia el valor constante `BaseVolume` en cada señal.
- Cuando `LotIncrease` está habilitado, la primera vela almacena el saldo inicial por lote (`initial balance / BaseVolume`). Las velas posteriores calculan la relación entre el saldo actual y esa línea de base, la redondean a un decimal (imitando `NormalizeDouble(..., 1)` de MQL4) y limitan el resultado a un máximo de 500 lotes. Luego, el valor calculado se utiliza como volumen de orden para la siguiente operación.
- Si la información de la cartera no está disponible, la estrategia vuelve elegantemente al estático `BaseVolume`.

## Pautas de uso
1. Adjunte la estrategia al instrumento deseado y confirme que `Security.PriceStep` refleja el tamaño del pip que desea negociar.
2. Seleccione el período de tiempo de la vela en `CandleType`. El script original normalmente se ejecutaba en períodos de tiempo intradiarios (de 5 a 15 minutos), pero se puede utilizar cualquier tamaño de barra.
3. Ajuste la configuración de la banda, las compensaciones de pips y los controles de riesgo para que coincidan con sus preferencias comerciales.
4. Decida si el tamaño de la posición debe escalar con el saldo de la cuenta (`LotIncrease`) o permanecer fijo.
5. Inicia la estrategia. Supervise el registro para verificar que las entradas y salidas se produzcan en velas completadas a los niveles de precios esperados.

## Diferencias con la versión MetaTrader
- StockSharp funciona con posiciones agregadas, por lo que incluso cuando `OneOrderOnly` está deshabilitado, el resultado es una única posición neta en lugar de múltiples tickets independientes.
- Las reglas de take-profit y stop-loss se implementan directamente en la estrategia en lugar de registrar órdenes pendientes con niveles de precios específicos, pero el comportamiento resultante es equivalente porque las comprobaciones se realizan en cada vela terminada.
- Se omiten los indicadores de registro (`logging`, `logerrs`, `logtick`) del experto original; El registro integrado de StockSharp ya registra pedidos y eventos comerciales.
- Los registros y estadísticas basados en archivos de la versión MetaTrader no se vuelven a crear porque StockSharp expone análisis más completos a través de carteras y estrategias.
