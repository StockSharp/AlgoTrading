# Estrategia de cruce de niveles de precios de aplicaciones
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Conversión del MetaTrader 4 asesor experto **BT_v4** (`MQL/8543/BT_v4.mq4`).
- Reimplementado con la estrategia API de alto nivel de StockSharp (suscripciones de velas, procesamiento sin indicadores, protecciones integradas).
- Enfocado en reaccionar ante el precio de cierre cruzando un nivel horizontal definido por el usuario (`AppPrice`).

## Lógica de trading
1. Cada vela terminada actualiza un búfer interno con el último precio de cierre.
2. Cuando el cierre se mueve **por encima** de `AppPrice` mientras que el cierre anterior estaba **en el nivel o por debajo** del mismo, la estrategia
   - Se comercializa solo si `BuyOnly = true` (refleja el valor predeterminado original EA).
   - Cancela cualquier orden pendiente, compensa una venta corta existente mediante el mismo volumen de orden de mercado y establece una posición larga del tamaño de lote calculado.
3. Cuando el cierre se mueve **por debajo** de `AppPrice` mientras que el cierre anterior estaba **en o por encima** del nivel, la estrategia
   - Se negocia solo si `BuyOnly = false` (modo de solo venta del EA).
   - Cancela órdenes pendientes, compensa cualquier posición larga existente y establece una posición corta del tamaño de lote calculado.
4. Las señales se evalúan estrictamente en velas completas; las velas parcialmente formadas se ignoran al igual que en el script MQL.

## Dimensionamiento de posiciones
- `EnableMoneyManagement = false` → use `FixedVolume` (equivalente a la entrada MQL `Lots`.
- `EnableMoneyManagement = true` → calcula el lote usando la fórmula original:

\[
\text{lote} = \text{round}_{\text{LotPrecision}} \left( \frac{\text{LotBalancePercent}}{100} \times \frac{\text{Saldo}}{\text{divisor}} \right)
\]

  - `divisor = 1000` para lotes de un decimal y `100` para lotes de dos decimales (misma regla que `LotPrec` en MQL).
  - El resultado se fija en [`MinLot`, `MaxLot`] y luego se alinea con las restricciones de seguridad `VolumeStep`, `VolumeMin` y `VolumeMax`.
  - Si los datos del saldo de la cartera no están disponibles, la estrategia vuelve a `FixedVolume`.

## Gestión del riesgo
- `StopLossPoints` y `TakeProfitPoints` se miden en puntos de precio de instrumento (ticks).
- Si cualquiera de los valores es positivo, `StartProtection` se activa con las compensaciones convertidas a través de `Security.PriceStep`.
- Establecer una distancia en `0` desactiva esa pata protectora en particular, de acuerdo con el comportamiento original de EA.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `AppPrice` | Nivel que activa las operaciones cuando el cierre lo cruza. | `0` |
| `BuyOnly` | `true` = modo solo largo (valor predeterminado original), `false` = solo corto. | `true` |
| `FixedVolume` | Tamaño del lote cuando MM está deshabilitado. | `0.1` |
| `EnableMoneyManagement` | Permite dimensionar el porcentaje de equilibrio. | `false` |
| `LotBalancePercent` | Porcentaje del saldo utilizado cuando MM está activado. | `10` |
| `MinLot` / `MaxLot` | Límites para el tamaño de lote calculado. | `0.1` / `5` |
| `LotPrecision` | Número de decimales para redondear el lote calculado. | `1` |
| `StopLossPoints` | Distancia de stop-loss en puntos de precio (0 = deshabilitado). | `140` |
| `TakeProfitPoints` | Distancia de obtención de beneficios en puntos de precio (0 = deshabilitado). | `180` |
| `CandleType` | Plazo de vela utilizado para la detección cruzada. | `1 Minute` |

## Notas de implementación
- Utiliza `SubscribeCandles(...).Bind(...)` por lo que los indicadores son innecesarios; Los precios de cierre llegan directamente en la devolución de llamada.
- Las órdenes de mercado (`BuyMarket`/`SellMarket`) se dimensionan para aplanar la posición opuesta antes de abrir una nueva, reflejando la lógica EA de cerrar órdenes opuestas antes de ingresar.
- `CancelActiveOrders()` se invoca antes de cada orden de mercado para evitar órdenes pendientes no deseadas.
- Parámetros como `Magic`, `Slippage` y configuraciones de color del archivo MQL se omiten porque no tienen un equivalente directo en StockSharp.
- Asegúrese de que los metadatos `Security` (`PriceStep`, `VolumeStep`, `VolumeMin`, `VolumeMax`) estén completos para que los ajustes de precio/volumen coincidan con las reglas del corredor.

## Consejos de uso
- Establezca `AppPrice` en el nivel horizontal que desea monitorear (por ejemplo, precio psicológico, pivote diario, etc.).
- Apague `BuyOnly` para replicar el modo original de "solo venta"; déjelo activado para ejecutar el comportamiento de solo largo predeterminado proporcionado.
- Al habilitar la administración del dinero, verifique que la conexión de la cartera proporcione actualizaciones del saldo; de lo contrario, la estrategia vuelve al volumen fijo.
- No se proporciona ningún puerto Python por solicitud; solo se genera la estrategia C#.
