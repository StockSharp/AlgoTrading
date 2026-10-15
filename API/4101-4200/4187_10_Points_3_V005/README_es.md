# Estrategia Diez Puntos 3 v005
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una versión StockSharp del asesor experto MetaTrader 4 "10points 3 v005". Sigue la pendiente MACD para decidir si la cesta promedio actual debe ser larga o corta y sigue abriendo órdenes martingala cada vez que el precio se mueve contra la posición activa en una distancia configurable. La versión mejorada "v005" agrega reglas de protección basadas en acciones, filtros de día y hora y la opción de desactivar el ciclo largo o corto.

## Lógica comercial
- Lea la dirección de la línea principal MACD. Cuando el indicador sube, la siguiente cesta será larga; cuando baje, la cesta será corta. Una opción permite invertir la interpretación.
- Abra la primera posición de mercado inmediatamente una vez que exista una dirección. Se agregan entradas posteriores cada vez que el precio se mueve `EntryDistancePips` contra la posición flotante.
- Los tamaños de los pedidos crecen geométricamente. El multiplicador está controlado por `MartingaleFactor` (o `HighTradeFactor` cuando se permiten más de 12 operaciones). Los volúmenes están alineados con el paso de volumen del instrumento y tienen un límite de 100 lotes.
- Cada entrada actualiza los niveles agregados de stop-loss y take-profit. Los valores iniciales se compensan con `InitialStopPips` y `TakeProfitPips`, mientras que la lógica de seguimiento se activa después de que la posición gana `EntryDistancePips + TrailingStopPips` a favor.
- Si la protección de la cuenta está habilitada, la estrategia puede alinear el objetivo con la mejor entrada (`ReboundLock`) y cerrar la orden más reciente una vez que la ganancia flotante alcance `SecureProfit`.
- Las reglas de protección del capital cierran toda la canasta cuando la pérdida flotante excede `StopLossAmount`, cuando el capital sube por encima de `ProfitTarget + ProfitBuffer` o cuando el capital cae por debajo de `StartProtectionLevel`.
- El comercio está limitado a la ventana `OpenHour`/`CloseHour` y está completamente deshabilitado los viernes de forma predeterminada.

## gestión del dinero
Cuando `UseMoneyManagement` está deshabilitado, el primer pedido utiliza el `LotSize` fijo. Cuando la bandera está habilitada, el volumen base se calcula a partir del valor actual de la cartera y el parámetro `RiskPercent`. El escalado de minicuentas se puede simular a través de `IsStandardAccount`.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `TakeProfitPips` | Distancia (en pips) de la toma de ganancias aplicada a cada entrada. |
| `LotSize` | Tamaño del lote base cuando la administración del dinero está deshabilitada. |
| `InitialStopPips` | Distancia inicial de stop-loss para cada orden. |
| `TrailingStopPips` | Distancia de trailing stop una vez que se alcanza el umbral de activación. |
| `MaxTrades` | Maximum number of simultaneous martingale entries. |
| `EntryDistancePips` | Movimiento adverso mínimo requerido para agregar el siguiente pedido. |
| `SecureProfit` | Beneficio flotante (en unidades monetarias) necesario para activar la salida de protección de cuenta. |
| `UseAccountProtection` | Habilita la lógica de bloqueo de rebote y beneficio seguro. |
| `OrdersToProtect` | Número de pasos finales de martingala protegidos por la regla de beneficio seguro. |
| `ReverseSignals` | Invierte la interpretación de la pendiente MACD. |
| `UseMoneyManagement` | Permite dimensionamiento basado en equilibrio. |
| `RiskPercent` | Porcentaje de riesgo utilizado por la fórmula de gestión del dinero. |
| `IsStandardAccount` | Utiliza escala de lote estándar en lugar de escala mini. |
| `EurUsdPipValue`, `GbpUsdPipValue`, `UsdChfPipValue`, `UsdJpyPipValue`, `DefaultPipValue` | Valores de pip utilizados para convertir las ganancias flotantes en moneda. |
| `CandleType` | Plazo de vela utilizado para la generación de señales. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | Configuración MACD. |
| `EnableLong`, `EnableShort` | Activa o desactiva la cesta larga/corta. |
| `OpenHour`, `CloseHour`, `MinuteToStop` | Configuración de la ventana de negociación. |
| `StopLossProtection`, `StopLossAmount` | Guardia de stop-loss basada en acciones. |
| `ProfitTargetEnabled`, `ProfitTarget`, `ProfitBuffer` | Bloqueo de ganancias basado en acciones. |
| `StartProtectionEnabled`, `StartProtectionLevel` | Guardia de piso de equidad. |
| `ReboundLock` | Alinea las salidas con la mejor entrada cuando la protección está activa. |
| `MartingaleFactor`, `HighTradeFactor` | Martingale multiplicadores. |
| `CloseOnFriday` | Desactiva el comercio durante los viernes. |

## Notas
- La estrategia utiliza el nivel alto StockSharp API (`SubscribeCandles` + `BindEx`) y no expone buffers de indicador sin procesar.
- Cada guardia de acciones cierra la cesta activa utilizando órdenes de mercado para replicar el comportamiento original EA.
- Valide siempre los valores de los parámetros, el tamaño del pip y el valor del pip con las especificaciones de su corredor antes de utilizar la estrategia en producción.
