# Estrategia de cuadrícula TenPointThree MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es una adaptación de C# del asesor experto MetaTrader **10p3v003 (10point3.mq4)**. Combina un disparador de cruce MACD con un motor de cuadrícula martingala. La lógica original se replicó utilizando el nivel alto de StockSharp API con los siguientes comportamientos clave:

- **MACD lógica de señal**: una dirección comercial se determina cuando la línea principal MACD cruza la línea de señal en la barra desplazada (`SignalShift`). Las entradas largas requieren que el valor de la señal anterior esté por debajo de `-TradingRangePips`, que el valor actual MACD permanezca por debajo de cero y viceversa para las entradas cortas. Opcionalmente, las señales se pueden invertir a través de `ReverseSignal`.
- **Capas de cuadrícula**: después de abrir la primera posición, las entradas adicionales en la misma dirección solo se permiten una vez que el precio se mueve con respecto al último llenado en al menos `GridStepPips`. Cada nueva pierna multiplica el volumen por `LotMultiplier` (o por `1.5` si `MaxTrades > 12`), imitando la escala de martingala de MQL4.
- **Protección contra riesgos**: el tramo más reciente se cierra y no se agregan más entradas cuando `OrdersToProtect` o más operaciones están activas y la ganancia flotante excede el umbral monetario. El umbral se basa en el porcentaje de riesgo configurado (administración del dinero habilitada) o en la heurística del tamaño del contrato (administración del dinero deshabilitada).
- **Salidas por tramo**: cada tramo rastrea su propia toma de ganancias, stop-loss virtual y stop dinámico. La distancia de parada coincide con la fórmula original: `InitialStopPips + (MaxTrades - existingOrders) * GridStepPips`. El seguimiento se activa solo después de que el precio se mueve `TrailingStopPips + GridStepPips` a favor de la posición y cierra el tramo cuando el precio retrocede `TrailingStopPips`.
- **Filtro de sesión**: cuando `UseTimeFilter` está habilitado, no se inician nuevas cuadrículas mientras el tiempo de la vela esté estrictamente entre `StopHour` y `StartHour`, lo que reproduce la guardia de "zona horaria de peligro" del script.

Todas las conversiones de dinero utilizan los metadatos `PriceStep`/`StepPrice` de la seguridad. Si el intercambio no expone un tamaño de contrato, se aplica un valor alternativo de `100000`, que coincide con el supuesto original de Forex.

## Parámetros

| Nombre | Descripción |
| ---- | ----------- |
| `CandleType` | Suscripción de vela utilizada para el procesamiento de MACD (predeterminado: período de tiempo de 30 minutos). |
| `Volume` | Tamaño de lote base para el primer pedido de cuadrícula. |
| `TakeProfitPips` | Distance in pips for each leg's take-profit (0 disables). |
| `InitialStopPips` | Distancia de parada base en pips. La parada real crece con el número de espacios libres en la parrilla. |
| `TrailingStopPips` | Distancia del trailing stop en pips aplicada después de que el tramo sea suficientemente rentable (0 inhabilitaciones). |
| `MaxTrades` | Maximum number of simultaneous martingale entries. |
| `LotMultiplier` | Multiplier applied to the volume of each additional grid leg (overridden to `1.5` when `MaxTrades > 12`). |
| `GridStepPips` | Minimum adverse price move (in pips) required before opening the next grid entry. |
| `OrdersToProtect` | Minimum number of active legs before the floating-profit protection can close the latest trade. |
| `UseMoneyManagement` | Enables dynamic lot calculation based on account equity. |
| `AccountType` | Selecciona la fórmula de riesgo: `0` – Estándar (capital / 10.000); `1` – Normal (capital / 100.000); `2` – Nano (capital / 1000). |
| `RiskPercent` | Percentage of equity used when money management is enabled. |
| `ReverseSignal` | Invierte señales MACD largas/cortas. |
| `FastEmaLength`, `SlowEmaLength`, `SignalLength` | MACD períodos (26/12/9 de forma predeterminada). |
| `SignalShift` | Number of closed bars back used for the crossover check (default: 1). |
| `TradingRangePips` | MACD signal band (in pips) that must be breached before a crossover is accepted. |
| `UseTimeFilter` | Enables the session guard based on `StopHour`/`StartHour`. |
| `StopHour`, `StartHour` | Exclusive range that blocks the creation of a new grid when `UseTimeFilter` is true. |

## notas de gestión del dinero

Cuando `UseMoneyManagement` está deshabilitado, el lote base (`Volume`) se utiliza directamente. De lo contrario, el EA calcula el tamaño del lote a partir del valor actual utilizando las mismas fórmulas que el EA original:

- Tipo de cuenta **0**: `Ceil(risk% * equity / 10,000) / 10`
- Tipo de cuenta **1**: `risk% * equity / 100,000`
- Tipo de cuenta **2**: `risk% * equity / 1,000`

Volumes are normalised with `Security.VolumeStep`, then capped by `Security.MinVolume`/`MaxVolume`.

## Flujo de trabajo de ejecución

1. Subscribe to the configured candle stream and feed the MACD indicator through `BindEx`.
2. On each finished candle, update trailing/stop logic for active legs.
3. Cuando se activan las reglas de cruce MACD, asegúrese de que el filtro de sesión permita el comercio, que la dirección de la cuadrícula coincida con la posición existente y que el precio se haya movido `GridStepPips` con respecto al último llenado.
4. Calculate the next leg volume using the martingale multiplier and send a market order.
5. Monitorear las ganancias flotantes; una vez que se alcanza el umbral de protección, cierre el tramo más nuevo y haga una pausa hasta la siguiente vela.

## Notas de conversión

- Todos los comentarios se han reescrito en inglés según sea necesario.
- Se utiliza StockSharp API de alto nivel (velas + `BindEx`). Se evita el acceso directo al valor del indicador.
- Los cálculos de beneficios flotantes se basan en `PriceStep`/`StepPrice`. For exotic instruments make sure these fields are filled.
- La estrategia mantiene el estado por tramo internamente para emular la gestión de pedidos de MQL4, porque StockSharp agrega posiciones de forma predeterminada.
