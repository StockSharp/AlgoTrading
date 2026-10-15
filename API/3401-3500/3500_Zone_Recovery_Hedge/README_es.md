# Estrategia de cobertura de recuperación de zona
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de cobertura de recuperación de zona** es una versión StockSharp del asesor experto MetaTrader *Zone Recovery Hedge V1*. El algoritmo alterna posiciones de compra y venta alrededor de un precio ancla para que se realice una nueva orden cada vez que el precio cruce la zona de recuperación configurada. La secuencia expande el volumen de la posición siguiendo un programa de martingala hasta que se alcanza el objetivo de ganancias flotantes o la protección de pérdidas opcional.

## Lógica estratégica

1. **Filtro de entrada**: cuando se selecciona el modo *RSI Multi-Timeframe*, la estrategia inspecciona una lista configurable de RSI lecturas (desde M1 hasta MN1) y requiere que cada período de tiempo habilitado abandone un área de sobrecompra/sobreventa simultáneamente. El cruce desde la sobreventa genera un ciclo de compra, mientras que el cruce desde la sobrecompra inicia un ciclo de venta. En el modo *Manual* se pueden llamar los métodos auxiliares `StartManualMarketCycle` y `StartManualPendingCycle` para comenzar una nueva secuencia sin señales automáticas.
2. **Operación inicial**: la primera operación utiliza el tamaño de lote fijo o un tamaño basado en el riesgo derivado del capital de la cartera y la distancia de parada planificada. Cuando el tamaño ATR está activo, la distancia de parada y el ancho de la zona se derivan del ATR diario; de lo contrario, se utilizan puntos de corredor.
3. **Cuadrícula de recuperación**: si el precio viaja en contra de la dirección activa a lo largo de la distancia de la zona de recuperación, la estrategia abre el lado opuesto con un mayor volumen (escalera de lote personalizada, multiplicador o paso aditivo). El ciclo sigue alternando direcciones alrededor del precio ancla original, aumentando el volumen hasta que se alcanza el objetivo de ganancias o se alcanza el número máximo de operaciones.
4. **Control de ganancias**: el objetivo se evalúa en la moneda de la cuenta, utilizando la distancia de obtención de ganancias base o la toma de ganancias de recuperación dedicada (con fracciones ATR opcionales). Las comisiones se pueden simular a través del parámetro *Comisión de Prueba*. Cuando el beneficio flotante supera el objetivo más los costes, se cierra todo el ciclo.
5. **Protección de riesgos**: si `MaxTrades` es distinto de cero y `SetMaxLoss` está habilitado, alcanzar el recuento máximo de operaciones mientras el PnL flotante supera el límite de `MaxLoss` cerrará todas las posiciones y restablecerá el ciclo.

> **Nota:** Las estrategias StockSharp se compensan de forma predeterminada. El puerto reproduce la lógica de recuperación invirtiendo la posición neta en lugar de mantener posiciones cubiertas simultáneamente. Esto mantiene las matemáticas de ganancias compatibles con StockSharp y al mismo tiempo preserva los pasos de recuperación alternativos del asesor original.

## Parámetros

| grupo | Parámetro | Descripción |
| --- | --- | --- |
| generales | `CandleType` | Periodo de tiempo principal que impulsa la lógica de entrada. |
| generales | `Mode` | `Manual` desactiva las señales, `RsiMultiTimeframe` activa el filtro RSI. |
| Señales | `RsiPeriod`, `OverboughtLevel`, `OversoldLevel` | RSI período de cálculo y umbrales. |
| Señales | `UseM1Timeframe` … `UseMonthlyTimeframe` | Activa/desactiva las confirmaciones RSI para el período de tiempo correspondiente. |
| Señales | `TradeOnBarOpen` | Utilice la barra anterior como barra de confirmación (comportamiento original EA). |
| Recuperación | `RecoveryZoneSize`, `TakeProfitPoints` | Ancho de zona y toma de ganancias base cuando ATR está deshabilitado. |
| Recuperación | `UseAtr`, `AtrPeriod`, `AtrZoneFraction`, `AtrTakeProfitFraction`, `AtrRecoveryFraction`, `AtrCandleType` | ATR configuración de tamaño basada. |
| Recuperación | `UseRecoveryTakeProfit`, `RecoveryTakeProfitPoints` | Distancia de toma de ganancias dedicada cuando el ciclo ya está en recuperación. |
| Riesgo | `MaxTrades`, `SetMaxLoss`, `MaxLoss` | Limite el número de operaciones y defina una protección contra pérdidas basada en dinero. |
| Riesgo | `TestCommission` | Comisión estimada (en dinero) aplicada por volumen comercial al evaluar el objetivo de ganancias. |
| Gestión del dinero | `RiskPercent`, `InitialLotSize`, `LotMultiplier`, `LotAddition`, `CustomLotSize1` … `CustomLotSize10` | Controla cómo se generan los volúmenes para cada paso del ciclo. |
| Temporizador | `UseTimer`, `StartHour`, `StartMinute`, `EndHour`, `EndMinute`, `UseLocalTime` | Restrinja el comercio a una ventana de tiempo diaria. |
| manuales | `PendingPrice` | Precio de referencia utilizado por `StartManualPendingCycle`. |

## Consejos de uso

- Adjunte la estrategia a una fuente de datos que proporcione el período de tiempo más alto que desee utilizar para las confirmaciones de RSI. El agregador interno puede crear plazos más altos a partir del plazo base.
- Cuando el modo *Manual* esté activo, llame a `StartManualMarketCycle(true)` o `StartManualMarketCycle(false)` para abrir un ciclo de compra o venta al precio actual, o a `StartManualPendingCycle` para anclar el ciclo a un nivel de precio personalizado.
- El tamaño de la posición basado en el saldo limita el porcentaje de riesgo al 10 % al igual que el EA original.
- La lógica de recuperación supone que `Security.PriceStep` y `Security.StepPrice` están llenos del conector. Sin ellos no se puede calcular el objetivo de beneficios.

## Diferencias con la versión MetaTrader

- El puerto StockSharp funciona con posiciones netas en lugar de cestas largas/cortas cubiertas. La secuencia de recuperación aún alterna direcciones comerciales, pero las posiciones se invierten al cambiar de dirección.
- Los elementos gráficos (botones, líneas, comentarios) del panel MT4 no se reproducen. Los comandos manuales y del temporizador se exponen a través de parámetros de estrategia y métodos auxiliares.
- Se omite el modelado de costos basado en diferenciales; solo el valor `TestCommission` se resta del objetivo de ganancias.
