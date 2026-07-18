# Estrategia de Retorno
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el clásico asesor experto "Return Strategy". Prepara una cuadrícula de órdenes de compra y venta limitadas en el inicio de una ventana de trading configurada. La cuadrícula es simétrica alrededor del precio de mercado, usa espaciado fijo en pips, y puede dimensionarse por un volumen fijo o un modelo de riesgo porcentual. Una vez que se ejecutan las órdenes, la estrategia supervisa la posición con lógica estática y de trailing stop-loss, monitorea el beneficio abierto acumulado y fuerza un cierre completo en la hora de corte diaria o cada viernes.

El sistema original fue diseñado para cuentas de netting y se enfocó en capturar movimientos de reversión a la media después de horarios programados. La conversión mantiene esa estructura mientras adapta la gestión de órdenes, trailing y controles de capital a la API de alto nivel de StockSharp.

## Reglas de Trading

- **Preparación diaria** – En el `StartHour` la estrategia verifica que no haya órdenes de cuadrícula activas y coloca `PendingOrderCount` límites de compra por debajo y límites de venta por encima del precio actual. El primer nivel se desplaza por `DistancePips` y cada nivel subsiguiente agrega `StepPips` de espaciado.
- **Control de riesgo** – Cada orden pendiente puede usar un `OrderVolume` fijo o un tamaño basado en riesgo derivado de `RiskPercent`. Cuando se usa el dimensionamiento por riesgo, el capital disponible y la distancia del stop-loss determinan el volumen por orden para que el riesgo total de la cuadrícula iguale el porcentaje configurado.
- **Gestión de stops** – Cada posición ejecutada recibe un stop-loss inicial basado en `StopLossPips`. Si `TrailingStopPips` es mayor que cero, una vez que el precio avanza más allá del umbral de trailing, el stop se ajusta en pasos de `TrailingStepPips`.
- **Objetivo de beneficio y salida de sesión** – El beneficio abierto neto se rastrea en pips. Cuando alcanza `TotalProfitPips` la estrategia marca todas las posiciones y órdenes para cierre. También realiza el mismo vaciado en el `EndHour` configurado y cada viernes independientemente del beneficio.
- **Expiración de órdenes** – Las órdenes pendientes pueden expirar automáticamente después de `ExpirationHours`. Las órdenes expiradas o canceladas manualmente se eliminan de la lista de seguimiento para permitir que se coloque una nueva cuadrícula al día siguiente.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `StopLossPips` | Distancia del stop inicial para cualquier posición ejecutada (en pips ajustados). |
| `StartHour` | Hora (0–23) cuando se crea la cuadrícula de órdenes pendientes. |
| `EndHour` | Hora (0–23) que desencadena una salida completa de posiciones y órdenes. |
| `TotalProfitPips` | Objetivo de beneficio abierto neto (en pips) que fuerza el cierre de todos los trades. |
| `TrailingStopPips` | Distancia del trailing stop desde el precio una vez activado. Establecer en cero para deshabilitar el trailing. |
| `TrailingStepPips` | Avance adicional requerido antes de mover el trailing stop. Debe ser positivo cuando el trailing está habilitado. |
| `DistancePips` | Desplazamiento inicial para la primera orden pendiente en cada lado del mercado. |
| `StepPips` | Espaciado incremental entre órdenes pendientes consecutivas. |
| `PendingOrderCount` | Número de límites de compra y límites de venta a registrar en `StartHour`. |
| `ExpirationHours` | Vida útil de las órdenes pendientes en horas. Cero deshabilita la expiración. |
| `OrderVolume` | Volumen fijo por orden pendiente. Dejar en cero para habilitar el dimensionamiento basado en riesgo. |
| `RiskPercent` | Porcentaje del portafolio asignado a toda la cuadrícula. El tamaño por orden se deriva de este valor cuando `OrderVolume` es cero. |
| `CandleType` | Serie de velas usada para controlar el timing y la lógica de gestión de stops. |

## Notas Adicionales

- La conversión de pips refleja la lógica original de MetaTrader ajustando el tamaño del paso para instrumentos de tres y cinco decimales.
- Cuando se usa `RiskPercent`, el porcentaje se aplica a la cuadrícula combinada y se divide equitativamente entre todas las órdenes pendientes.
- La estrategia aplica reglas de validación idénticas al EA fuente: las horas deben estar dentro del rango diario, el trailing requiere un paso distinto de cero, y solo uno de `OrderVolume`/`RiskPercent` puede estar activo a la vez.
- Todos los comentarios públicos en el código se proporcionan en inglés por coherencia con las pautas del repositorio.
