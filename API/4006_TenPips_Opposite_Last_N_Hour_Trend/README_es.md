# Diez pips frente a la estrategia de tendencia de las últimas N horas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es una fiel adaptación del experto MetaTrader **10pipsOnceADayOppositeLastNHourTrend**. Se negocia exactamente una vez al día a una hora configurable y deliberadamente toma el lado opuesto del cambio de precio observado en las últimas *N* velas horarias completadas. La lógica está diseñada para pares de divisas con precios de cinco dígitos, pero la versión C# adapta automáticamente el tamaño del pip utilizando el `PriceStep` del instrumento y el número de decimales.

En la hora de negociación seleccionada, la estrategia inspecciona el precio de cierre de hace `HoursToCheckTrend` horas y lo compara con el cierre de la vela horaria completada más reciente:

- Si el cierre anterior es **alto**, el mercado ha estado cayendo (bajista), por lo que la estrategia abre una posición **larga**.
- De lo contrario, el mercado ha estado subiendo (alcista), por lo que abre una posición **corta**.

Las posiciones se cierran mediante paradas protectoras, una salida diaria basada en el tiempo o manualmente cuando el mercado está fuera de la ventana de negociación.

## gestión del dinero

El tamaño de la posición refleja la escalera de martingala del experto original:

1. El volumen base proviene de `FixedVolume`. Cuando se establece en cero, la estrategia recurre al dimensionamiento basado en el riesgo utilizando `Portfolio.CurrentValue * MaximumRisk / 1000` redondeado a un decimal.
2. El volumen está limitado por `MinimumVolume`, `MaximumVolume`, los límites de volumen del instrumento y un límite flexible igual a `Portfolio.CurrentValue / 1000` lotes.
3. Después de cada operación cerrada, se almacena el resultado (hasta las últimas cinco operaciones). Al preparar una nueva entrada la estrategia escanea ese historial y multiplica el tamaño del lote según la primera pérdida que encuentre, usando la secuencia `FirstMultiplier`... `FifthMultiplier`. Esto reproduce las comprobaciones anidadas `OrderSelect` de la versión MQL.

## Controles de riesgo

- `StopLossPips`, `TakeProfitPips` y `TrailingStopPips` funcionan en unidades de pips. El puerto recalcula el tamaño del pip con el multiplicador estándar de 3/5 decimal para símbolos Forex.
- Los trailingstops son simétricos para posiciones largas y cortas. En el código original MQL, el rastro del lado corto nunca se activó debido a un error de señal; la versión C# corrige eso para que ambas direcciones se comporten de manera idéntica.
- `OrderMaxAge` cierra cualquier posición que sobreviva más que la duración configurada (21 horas de forma predeterminada).
- Fuera del horario de negociación permitido, la estrategia liquida cualquier exposición abierta para permanecer estable hasta la siguiente sesión.
- `MaxOrders` protege contra reingresos accidentales al exigir que no haya posiciones abiertas ni órdenes activas cuando se evalúa una nueva señal.

## Flujo de trabajo detallado

1. Suscríbase a velas por hora (el período de tiempo se puede cambiar con `CandleType`).
2. Reúna el precio de cierre de cada vela terminada en un pequeño buffer móvil.
3. En la primera vela completa a la hora permitida:
   - Verifique el estado de la cartera/conexión y confirme que no haya ninguna posición abierta.
   - Asegúrese de tener al menos `HoursToCheckTrend` velas históricas para comparar.
   - Determine la dirección comparando el cierre actual con el cierre de hace `HoursToCheckTrend`.
   - Calcule el tamaño del lote utilizando la rutina de administración de dinero anterior y envíe una orden de mercado.
4. Mientras una posición está abierta, la estrategia:
   - Evalúa los niveles de stop-loss, take-profit y trailing utilizando precios máximos/bajos de velas.
   - Actualiza el trailing stop después de nuevos máximos (para largos) o mínimos (para cortos).
   - Realiza un seguimiento de la marca de tiempo de entrada para poder aplicar `OrderMaxAge`.
   - Registra las ganancias/pérdidas realizadas cuando se cierra la operación para alimentar los multiplicadores de martingala.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `FixedVolume` | Tamaño de lote fijo. Establezca en `0` para utilizar el tamaño basado en el riesgo. | `0.1` |
| `MinimumVolume` | Límite inferior estricto para el volumen del pedido. | `0.1` |
| `MaximumVolume` | Límite superior estricto para el volumen del pedido. | `5` |
| `MaximumRisk` | Fracción del capital utilizado cuando `FixedVolume = 0`. | `0.05` |
| `MaxOrders` | Órdenes/posiciones máximas simultáneas. | `1` |
| `TradingHour` | Hora del día (0–23) en la que se permiten nuevas operaciones. | `7` |
| `HoursToCheckTrend` | Ventana retrospectiva en horas para la comparación de tendencias. | `30` |
| `OrderMaxAge` | Vida útil máxima de un puesto. | `21h` |
| `StopLossPips` | Distancia de stop-loss en pips. | `50` |
| `TakeProfitPips` | Distancia de toma de ganancias en pips. | `10` |
| `TrailingStopPips` | Distancia del trailing-stop en pips. | `0` (deshabilitado) |
| `FirstMultiplier` … `FifthMultiplier` | Los multiplicadores de lote se aplican cuando la operación perdedora más reciente se encuentra en la profundidad respectiva. | `4`, `2`, `5`, `5`, `1` |
| `CandleType` | Plazo de suscripción de velas. | `1 hour` |

## Diferencias con el experto MQL original

- Martingale la lógica del tamaño, la antigüedad de las órdenes y la ventana de negociación se reproducen uno a uno. El único cambio deliberado es el trailing stop simétrico del lado corto para corregir el error de señal en el guión original.
- Todos los niveles de protección se ejecutan con órdenes de mercado en la siguiente vela terminada porque las estrategias StockSharp no registran órdenes de parada/límite separadas cuando se utilizan ayudantes de alto nivel. Esto coincide con el comportamiento del experto original cuando se activaron sus órdenes de suspensión.
- El capital de la cuenta se lee desde `Portfolio.CurrentValue`. Si el adaptador no proporciona este campo, la estrategia vuelve a la base `Volume` (predeterminado `1`).
- La lista de horarios de negociación permitidos refleja la matriz original de `0…23`. Para restringir el comercio a días específicos, puede editar `_tradingDayHours` dentro del constructor.

## Notas de uso

- Funciona mejor con datos de Forex por hora donde los cálculos del tamaño de pip utilizando la heurística `PriceStep` ×10 son válidos.
- Verifique siempre que `Security.VolumeStep`, `VolumeMin` y `VolumeMax` estén configurados por el conector para que la estrategia pueda ajustar los tamaños de lote correctamente.
- Debido a que las entradas se evalúan solo una vez por vela terminada, la estrategia debe lanzarse antes de la hora de negociación elegida para no perderse la primera señal del día.
