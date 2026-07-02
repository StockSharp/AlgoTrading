# Estrategia de revendedor de lotes múltiples
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia Multi Lot Scalper** es un sistema de promedio estilo martingala convertido del clásico asesor experto MetaTrader "Multi Lot Scalper". El algoritmo original fue diseñado para los principales pares de divisas y se basó en la pendiente del histograma MACD para decidir si el mercado está entrando en una fase alcista o bajista. Una vez que se identifica una dirección, la estrategia abre una escalera de órdenes de mercado, aumentando progresivamente el volumen después de cada movimiento adverso. El puerto StockSharp mantiene la lógica de entrada original, las reglas de administración de dinero y los mecanismos de protección mientras aprovecha la suscripción de vela de alto nivel API.

La estrategia funciona mejor en instrumentos líquidos donde los diferenciales son ajustados y la definición de pip es estable. Por defecto se suscribe a velas de 15 minutos, pero cualquier otro plazo compatible con los instrumentos se puede suministrar a través del parámetro `CandleType`.

## Lógica de trading

1. **Detección de señal**: se evalúa un indicador MACD (`MacdFastLength`, `MacdSlowLength`, `MacdSignalLength`) en cada vela terminada. Cuando la línea principal MACD sube en relación con el valor anterior, la estrategia busca oportunidades largas; de lo contrario, se prepara para vender. El parámetro `ReverseSignals` invierte esta interpretación para los usuarios que prefieren entradas contrarias.
2. **Entrada inicial**: la primera posición en una nueva secuencia se abre inmediatamente después de una señal válida, siempre y cuando el filtro de fecha/hora (`StartYear`, `StartMonth`, `EndYear`, `EndMonth`, `EndHour`, `EndMinute`) permita operar. Se utilizan órdenes de mercado, lo que refleja la implementación de MetaTrader.
3. **Pirámide**: las órdenes posteriores se activan solo si el precio se mueve con respecto al último cumplimiento en al menos `EntryDistancePips`. Cada operación adicional multiplica el volumen base por 2 o por 1,5 (cuando `MaxTrades` es superior a 12) para reproducir el tamaño de martingala de EA.
4. **Paradas y objetivos**: `InitialStopPips` y `TakeProfitPips` se convierten en niveles de precios para toda la cesta. Un trailing stop se activa después de que el movimiento a favor supera `EntryDistancePips + TrailingStopPips`, restringiendo la salida a medida que el mercado se acelera.
5. **Protección de cuenta**: cuando la cesta está cerca de su capacidad (`MaxTrades - OrdersToProtect`) y la ganancia flotante alcanza `SecureProfit`, la estrategia cierra la operación más reciente y bloquea temporalmente nuevas entradas si `UseAccountProtection` está habilitado.

## Gestión monetaria

Opcionalmente, el asesor experto original recalculó el tamaño del lote base en función del saldo de la cuenta. El puerto StockSharp mantiene este comportamiento a través de los parámetros `UseMoneyManagement`, `RiskPercent` y `IsStandardAccount`. Cuando la función está activa, el lote base (`LotSize`) se ignora y, en su lugar, se deriva del valor de la cartera, escalado para cuentas mini o estándar como el código MQL.

## Parámetros

| Parámetro | Descripción | Predeterminado |
| --- | --- | --- |
| `TakeProfitPips` | Distancia de toma de ganancias aplicada a cada entrada, expresada en pips. | `40` |
| `LotSize` | Tamaño de lote base utilizado cuando la administración del dinero está deshabilitada. | `0.1` |
| `InitialStopPips` | Distancia inicial de stop-loss en pips. | `0` |
| `TrailingStopPips` | Distancia de trailing stop que se activa después del umbral. | `20` |
| `MaxTrades` | Número máximo de entradas de martingala permitidas simultáneamente. | `10` |
| `EntryDistancePips` | Movimiento adverso mínimo antes de agregar una nueva orden. | `15` |
| `SecureProfit` | Se requiere ganancia flotante (en moneda) para activar la protección de la cuenta. | `10` |
| `UseAccountProtection` | Permite cerrar la última operación cuando se alcanza el umbral de beneficio seguro. | `true` |
| `OrdersToProtect` | Número de operaciones finales afectadas por la regla de beneficio seguro. | `3` |
| `ReverseSignals` | Invierte la interpretación MACD (alcista se vuelve corto, bajista se vuelve largo). | `false` |
| `UseMoneyManagement` | Permite el cálculo de lotes basado en el saldo de la cuenta. | `false` |
| `RiskPercent` | Porcentaje de riesgo utilizado cuando la gestión del dinero está activa. | `12` |
| `IsStandardAccount` | Utiliza escala de lote estándar en lugar de escala de mini lote. | `false` |
| `EurUsdPipValue` | Anulación del valor del pip para EURUSD. | `10` |
| `GbpUsdPipValue` | Anulación del valor del pip para GBPUSD. | `10` |
| `UsdChfPipValue` | Anulación del valor del pip para USDCHF. | `10` |
| `UsdJpyPipValue` | Anulación del valor del pip para USDJPY. | `9.715` |
| `DefaultPipValue` | Valor de pip alternativo utilizado para otros instrumentos. | `5` |
| `StartYear` | Primer año natural en el que se podrán abrir nuevas plazas. | `2005` |
| `StartMonth` | Primer mes permitido para nuevas entradas. | `1` |
| `EndYear` | Último año calendario para iniciar operaciones. | `2006` |
| `EndMonth` | Último mes calendario para iniciar operaciones. | `12` |
| `EndHour` | Hora (24h) después de la cual se bloquean las nuevas entradas. | `22` |
| `EndMinute` | Componente de minutos de la hora límite diaria. | `30` |
| `CandleType` | Tipo de vela utilizado para la generación de señales (el valor predeterminado es 15 minutos). | `15-minute time frame` |
| `MacdFastLength` | Longitud rápida EMA del indicador MACD. | `14` |
| `MacdSlowLength` | Longitud lenta de EMA del indicador MACD. | `26` |
| `MacdSignalLength` | Longitud de la señal EMA del indicador MACD. | `9` |

## Pautas de uso

- Asegúrese de que el paso de pip del instrumento coincida con la configuración del valor de pip. Actualice los parámetros del valor del pip al aplicar la estrategia a CFD, metales o criptoactivos.
- La escala de martingala puede aumentar la exposición rápidamente. Comience con valores conservadores de `MaxTrades`, `EntryDistancePips` y `TrailingStopPips` antes de experimentar con cestas más grandes.
- Optimice la configuración de MACD y el intervalo de velas para el instrumento que se comercializa. Los gráficos más lentos generalmente reducen la cantidad de pasos promedio, mientras que los gráficos más rápidos aumentan la actividad.
- La regla de protección de cuentas es particularmente importante en mercados propensos a cambios repentinos. Si el beneficio garantizado se ve afectado con frecuencia, considere reducir `SecureProfit` o ajustar `TrailingStopPips`.
- El filtro de la ventana de negociación permite desactivar la estrategia después de un horario intradiario elegido. Esto es útil para evitar comunicados de prensa o volatilidad al final de la sesión.

## Notas de conversión

- La versión StockSharp utiliza la suscripción de vela de alto nivel API (`SubscribeCandles().BindEx(...)`) en lugar del procesamiento manual de ticks, lo que mantiene la gestión de indicadores transparente.
- Los trailingstops se manejan internamente mediante la gestión del nivel de stop agregado de la cesta en lugar de modificar cada orden secundaria individualmente, lo que refleja el comportamiento previsto en un entorno consciente de la cartera.
- El uso de EA de `AccountBalance` para el tamaño de la posición se asigna a la propiedad `Portfolio.CurrentValue`, manteniendo la paridad entre las implementaciones MetaTrader y StockSharp.
