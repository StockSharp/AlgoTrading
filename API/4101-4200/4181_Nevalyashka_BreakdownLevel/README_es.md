# Estrategia de nivel de avería de Nevalyashka
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Nevalyashka Breakdown Level es una conversión directa del asesor experto MT4 *Nevalyashka_BreakdownLevel*. El sistema construye un rango de apertura entre dos momentos configurables y comercializa rupturas de ese rango. Cuando falla una ruptura y se detiene la operación, la estrategia inmediatamente invierte la dirección utilizando un multiplicador martingala para recuperar la pérdida. Las operaciones rentables bloquean cualquier entrada adicional durante el resto del día de negociación, coincidiendo con el comportamiento original de EA.

## Conceptos clave
- **Rango de apertura:** El máximo más alto y el mínimo más bajo impresos entre `RangeStart` y `RangeEnd` definen el canal de ruptura para el día actual.
- **Entradas de ruptura:** Se abre una posición larga cuando el precio de cierre excede el máximo del rango; Se abre una posición corta cuando cae por debajo del mínimo del rango.
- **Órdenes de protección:** El stop-loss siempre se coloca en el lado opuesto del rango. La toma de ganancias se coloca a una distancia igual al ancho del rango.
- **Movimiento de equilibrio:** Cuando está habilitado, el stop se mueve al precio de entrada una vez que la operación avanza hasta la mitad del camino hacia el objetivo.
- **Martingale recuperación:** Después de un stop-loss, la estrategia invierte la dirección, multiplica el volumen de la orden por `MartingaleMultiplier` y utiliza un tamaño objetivo/stop simétrico para recuperar la pérdida anterior.
- **Bloqueo diario:** Cualquier cierre rentable (toma de ganancias o salida manual por encima de cero) evita nuevas operaciones hasta que cambie el día de negociación.
- **Plano forzado:** Cuando `OrdersCloseTime` es posterior a `RangeEnd`, todas las posiciones abiertas se cierran en ese momento y las nuevas entradas se bloquean durante el resto del día.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `RangeStart` | Hora de inicio (inclusive) del rango de referencia. | `04:00` |
| `RangeEnd` | Hora de finalización (inclusive) del rango de referencia. | `09:00` |
| `OrdersCloseTime` | Hora del día para cerrar posiciones con fuerza. Cuando esta hora es posterior a `RangeEnd`, también bloquea nuevas operaciones posteriores. | `23:30` |
| `OrderVolume` | Volumen utilizado para cada operación de ruptura. | `0.1` |
| `MartingaleMultiplier` | Multiplicador aplicado a la siguiente orden después de un stop-loss para recuperar la pérdida anterior. | `2` |
| `UseBreakeven` | Permite mover el stop al punto de equilibrio una vez que la operación ha recorrido la mitad de la distancia objetivo. | `true` |
| `CandleType` | Tipo de vela utilizado para construir el rango y generar señales. | `1 hour` velas |

## Reglas de trading
1. **Cálculo de rango**: Para cada nuevo día de negociación, la estrategia registra los máximos y mínimos de las velas terminadas entre `RangeStart` y `RangeEnd` (inclusive).
2. **Condiciones de entrada**:
   - Vaya en largo cuando el precio de cierre de la vela actual esté por encima del máximo del rango registrado.
   - Vaya en corto cuando el precio de cierre de la vela actual esté por debajo del mínimo del rango registrado.
   - Las entradas se omiten si hay pendiente una reversión de martingala, si ya se produjo una operación rentable el mismo día o si la hora actual es pasada `OrdersCloseTime` (cuando `OrdersCloseTime > RangeEnd`).
3. **Gestión de riesgos**:
   - El stop-loss está anclado en el lado opuesto del rango de apertura.
   - La toma de ganancias se establece en el precio de entrada más/menos el ancho del rango de apertura.
   - Cuando `UseBreakeven` está habilitado, el stop se mueve al precio de entrada después de que se haya cubierto la mitad de la distancia objetivo.
4. **Martingale reversión**:
   - Si se alcanza el stop-loss, la posición se cierra, el volumen se multiplica por `MartingaleMultiplier` y se envía una orden de mercado inmediata en la dirección opuesta.
   - La nueva parada y el objetivo se colocan a una distancia igual a la pérdida por lote dividida por el multiplicador, coincidiendo con la lógica de recuperación del EA original.
5. **Bloqueo comercial diario**:
   - Si una operación se cierra con una ganancia no negativa o se alcanza el objetivo, no se permiten nuevas operaciones hasta que cambie la fecha de negociación.
6. **Salida forzada**:
   - Cuando `OrdersCloseTime` está después de la ventana de rango y la hora actual alcanza este valor, todas las posiciones abiertas se aplanan y el día se bloquea.

## Notas
- La estrategia utiliza el StockSharp API (`Strategy.SubscribeCandles().Bind(...)`) de alto nivel para mantenerse cerca de las convenciones del marco.
- Todos los cálculos con estado (límites de rango, órdenes de martingala pendientes, estado de equilibrio) se almacenan dentro de la clase de estrategia para evitar búsquedas históricas.
- La conversión conserva el comportamiento original del EA de contar los días de negociación por fecha del calendario y gestionar los pasos de martingala inmediatamente después de una parada.
