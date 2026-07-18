# Estrategia de FIBO1 (Conversión MQL 24845)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia FIBO1** reproduce las reglas de trading del asesor experto original `FIBO1.mq4` de Aharon Tzadik (script MQL 24845) usando la API de alto nivel de StockSharp. La estrategia opera un único símbolo en una temporalidad seleccionada y combina tres grupos de filtros:

1. **Filtro de tendencia** – una LWMA rápida y una lenta (Media Móvil Ponderada Linealmente) del precio típico. Las señales largas requieren que la LWMA rápida permanezca por encima de la LWMA lenta, mientras que los cortos requieren la relación inversa.
2. **Confirmación de Momentum** – tres lecturas de Momentum consecutivas se comparan contra umbrales de compra/venta definidos por el usuario. El algoritmo imita la desviación absoluta de 100 que el código MQL usaba en temporalidades superiores.
3. **Filtro MACD** – un MACD de temporalidad superior debe confirmar la dirección del trade. El puerto de StockSharp mantiene los valores predeterminados 12/26/9 y verifica la relación entre las líneas principal y de señal del MACD exactamente como en el asesor experto.

Una vez que una posición está activa, la estrategia recrea la sofisticada lógica de salida de `FIBO1.mq4`:

- Distancias de stop-loss y take-profit basadas en pips tradicionales.
- Objetivos de take-profit opcionales basados en dinero/porcentaje y trailing.
- Stops de seguimiento basados en velas que siguen los máximos/mínimos recientes, incluido un buffer de precio adicional idéntico a la configuración "PAD AMOUNT".
- Distancias de trailing clásicas que se activan después de un umbral mínimo de beneficio.
- Protección automática de punto de equilibrio con un offset expresado en pips.
- Un stop de capital que monitorea el drawdown flotante contra el pico histórico de capital.

> **Nota:** El experto MQL original dependía de una línea "FIBO" dibujada manualmente en el gráfico para el trading en vivo. Las estrategias de StockSharp no pueden acceder a los objetos de dibujo del terminal, por lo tanto el puerto siempre se comporta como la rama de prueba del código MQL (la parte que ignora el filtro de retroceso Fib). Todas las demás características se conservan y la documentación a continuación explica cada parámetro disponible.

## Lógica de trading

1. **Detección de señales**
   - Esperar a una vela terminada en la temporalidad principal.
   - Asegurarse de que la LWMA rápida esté por encima (largo) o por debajo (corto) de la LWMA lenta.
   - Verificar el patrón de precio que compara el par máximo/mínimo de la vela anterior, reflejando `Low[2] < High[1]` para largos y `Low[1] < High[2]` para cortos.
   - Evaluar la desviación absoluta máxima de los últimos tres valores de Momentum del nivel neutro 100. Si supera el umbral configurado, el filtro de Momentum pasa.
   - Confirmar que la línea principal MACD de temporalidad superior permanezca por encima (largo) o por debajo (corto) de su línea de señal.
   - Cuando todos los filtros se alinean, revertir cualquier exposición opuesta y abrir una orden de mercado usando el volumen de trade configurado.

2. **Gestión de riesgos**
   - Cada nueva posición recibe inmediatamente órdenes de stop-loss y take-profit basadas en pips a través de la API protectora de StockSharp.
   - La lógica de punto de equilibrio ajusta el stop una vez que el beneficio flotante iguala el umbral de activación.
   - El trailing basado en precio puede operar en dos modos: (a) seguir los extremos de las velas con un offset de margen, o (b) mantener una distancia fija en pips después de que el trade entre en beneficio.
   - Un módulo de gestión monetaria maneja objetivos basados en efectivo, objetivos de porcentaje de capital y un stop de seguimiento de beneficio flotante idéntico al EA original.
   - El stop de capital global rastrea continuamente el nivel de capital más alto observado desde el inicio y cierra todas las posiciones cuando se supera el drawdown máximo permitido.

## Parámetros

| Nombre | Predeterminado | Descripción |
|------|---------|-------------|
| `UseMoneyTakeProfit` | `false` | Cerrar todas las posiciones cuando el beneficio no realizado alcance `MoneyTakeProfit` (moneda de cuenta). |
| `MoneyTakeProfit` | `10` | Objetivo de beneficio en moneda de cuenta. Efectivo solo si `UseMoneyTakeProfit = true`. |
| `UsePercentTakeProfit` | `false` | Habilitar un objetivo de beneficio expresado como porcentaje del snapshot inicial de capital. |
| `PercentTakeProfit` | `10` | Porcentaje utilizado por el objetivo de beneficio basado en capital. |
| `EnableMoneyTrailing` | `true` | Activa el trailing basado en dinero una vez que el beneficio no realizado alcanza `MoneyTrailTarget`. |
| `MoneyTrailTarget` | `40` | Beneficio flotante mínimo que habilita la lógica de trailing monetario. |
| `MoneyTrailStop` | `10` | Drawdown máximo permisible (en unidades de moneda) después de que se activa el trailing monetario. |
| `UseEquityStop` | `true` | Habilitar la protección global contra drawdown de capital. |
| `EquityRiskPercent` | `1` | Drawdown máximo (porcentaje del capital pico) antes de cerrar todas las posiciones. |
| `TradeVolume` | `1` | Volumen base (lotes/contratos) para entradas de mercado. |
| `FastMaPeriod` | `20` | Período de la LWMA rápida calculada sobre el precio típico. |
| `SlowMaPeriod` | `100` | Período de la LWMA lenta calculada sobre el precio típico. |
| `MomentumPeriod` | `14` | Longitud del indicador Momentum utilizado por el filtro de confirmación. |
| `MomentumBuyThreshold` | `0.3` | Desviación absoluta mínima de 100 requerida para trades largos. |
| `MomentumSellThreshold` | `0.3` | Desviación absoluta mínima de 100 requerida para trades cortos. |
| `MacdFastPeriod` | `12` | Longitud EMA rápida dentro del MACD de temporalidad superior. |
| `MacdSlowPeriod` | `26` | Longitud EMA lenta dentro del MACD de temporalidad superior. |
| `MacdSignalPeriod` | `9` | Longitud EMA de señal dentro del MACD de temporalidad superior. |
| `TakeProfitPips` | `50` | Distancia de take-profit de protección en pips. |
| `StopLossPips` | `20` | Distancia de stop-loss de protección en pips. |
| `TrailingActivationPips` | `40` | Beneficio mínimo (pips) requerido antes de que se active el trailing basado en pips. |
| `TrailingDistancePips` | `40` | Distancia mantenida por el stop de seguimiento basado en precio. |
| `UseCandleTrailing` | `true` | Cuando está habilitado, el stop de seguimiento sigue los extremos de las velas recientes en lugar de usar una distancia fija. |
| `CandleTrailingLength` | `3` | Número de velas terminadas utilizadas para calcular el extremo del trailing. |
| `CandleTrailingOffsetPips` | `3` | Buffer de pips adicional añadido al precio del trailing de velas. |
| `MoveToBreakEven` | `true` | Habilitar la protección de punto de equilibrio. |
| `BreakEvenActivationPips` | `30` | Beneficio (pips) requerido antes de que el stop se mueva al punto de equilibrio. |
| `BreakEvenOffsetPips` | `30` | Offset (pips) añadido más allá del precio de entrada cuando el stop se mueve al punto de equilibrio. |
| `CandleType` | `15m` | Serie de velas principal utilizada para las señales de trading. |
| `MomentumCandleType` | `15m` | Serie de velas que alimenta el indicador Momentum. |
| `MacdCandleType` | `1d` | Serie de temporalidad superior utilizada por el filtro MACD. |

## Notas de uso

- Los tipos de vela predeterminados reflejan la lógica multi-temporalidad del asesor experto: las series principal y de Momentum usan la temporalidad del gráfico, mientras que el MACD trabaja en una temporalidad superior (diaria por defecto). Las tres series pueden reconfigurarse.
- La rutina de conversión de pips automáticamente tiene en cuenta los símbolos forex de 3/5 decimales multiplicando el paso de precio por 10. Los instrumentos con otros tamaños de tick mantienen el multiplicador de `PriceStep` sin procesar.
- La estrategia se basa exclusivamente en velas terminadas. Asegúrese de que el proveedor de datos conectado publique estados de vela, de lo contrario las condiciones de entrada nunca se activarán.
- Cuando el símbolo opera en un entorno de netting, las reversiones de posición se ejecutan cerrando la exposición opuesta antes de abrir un nuevo trade, exactamente como lo hizo el EA original con órdenes de mercado.

## Diferencias respecto al EA original

- Las verificaciones de objetos de retroceso de Fibonacci no están presentes porque StockSharp no puede acceder a los dibujos del gráfico MT4. La estrategia siempre se comporta como la rama de prueba del código MQL.
- Los parámetros de gestión monetaria (`Lots`, `LotExponent` y `Max_Trades`) fueron reemplazados por una única propiedad `TradeVolume` porque las estrategias de StockSharp operan en posiciones netas. El escalado de volumen puede programarse externamente a través de optimizadores si es necesario.
- Todas las rutinas de registro y alerta (`Alert`, `SendMail`, `SendNotification`) se eliminaron intencionalmente para mantener la versión de StockSharp autónoma.

Con estos ajustes, el puerto de StockSharp permanece fiel a la lógica de trading de `FIBO1.mq4` mientras proporciona una implementación limpia y parametrizada que se integra con otras muestras de AlgoTrading.
