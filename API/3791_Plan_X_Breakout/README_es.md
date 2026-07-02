# Estrategia de ruptura del Plan X
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de ruptura del Plan X replica el MetaTrader asesor experto "plan x" de Peter Ingram. Se centra en la sesión de última hora de la mañana de Londres y espera a que el precio se separe de una vela de referencia antes de entrar. Sólo se puede abrir una posición neta a la vez, y el riesgo se controla mediante paradas basadas en pips que siguen a la operación a medida que se mueve a favor.

## Lógica de trading

1. **Ancla de sesión**
   - La estrategia observa velas de 15 minutos.
   - A la hora de inicio de sesión configurada (por defecto 11:00), registra el cierre de esa vela. Este cierre actúa como precio ancla para el resto de la sesión.
   - La negociación solo se considera después de que se haya cerrado al menos una vela adicional y antes de la hora de finalización de la sesión (por defecto, 15:00).

2. **Condiciones de entrada**
   - **Largo**: cuando la última vela terminada cierra más de `LongTargetPips` (25 pips predeterminado) por encima del cierre del ancla y no hay ninguna posición abierta.
   - **Corto**: Cuando la última vela terminada cierra más de `ShortTargetPips` (20 pips predeterminado) por debajo del cierre del ancla y no hay ninguna posición abierta.
   - Todas las comparaciones se realizan en unidades de pips derivadas del tamaño del tick del instrumento.

3. **Gestión de posiciones**
   - Se establece un stop-loss inicial fijo igual a `InitialStopPips` (25 pips por defecto) en relación con el precio de entrada.
   - El stop se convierte en un trailing stop una vez que la operación gana al menos `TrailTriggerPips` (10 pips por defecto).
   - Cada vez que el precio avanza otro `TrailTriggerPips`, el stop se mueve `TrailStepPips` (5 pips predeterminado) más en la dirección rentable.
   - Si el precio llega al tope, la posición se cierra en el mercado.

4. **Volumen**
   - Los pedidos utilizan el parámetro `TradeVolume` (por defecto 0,1 lotes). Ajústelo para que coincida con el tamaño del contrato de seguridad.

## Parámetros

| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `TradeVolume` | Volumen de órdenes de mercado utilizado para entradas y salidas. | 0.1 |
| `LongTargetPips` | Distancia de ruptura por encima del ancla requerida para entradas largas. | 25 |
| `ShortTargetPips` | Distancia de ruptura debajo del ancla requerida para entradas cortas. | 20 |
| `InitialStopPips` | Distancia desde el precio de entrada hasta el stop-loss protector. | 25 |
| `TrailTriggerPips` | Ganancia en pips necesarios antes de que se active o avance el trailing stop. | 10 |
| `TrailStepPips` | Incremento de pip aplicado al trailing stop cada vez que se mueve. | 5 |
| `SessionStartHour` | Hora decimal que indica cuándo comienza la vela ancla (por ejemplo, `11.0`, `11.5`). | 11.0 |
| `SessionEndHour` | Hora decimal a partir de la cual no se realizan nuevas entradas. Debe ser posterior a `SessionStartHour`. | 15.0 |
| `CandleType` | Serie de velas utilizadas para evaluaciones. El valor predeterminado es velas de 15 minutos. | 15 minutos |

## Notas

- El tamaño del pip se adapta automáticamente según el `PriceStep` y la precisión decimal del instrumento (3 o 5 decimales reciben un multiplicador de 10x).
- La estrategia espera un mercado intradiario continuo; en instrumentos con brechas diarias, el comportamiento de reancla ocurre cada día de negociación.
- Debido a que las estrategias StockSharp utilizan posiciones netas, la conversión asume solo una dirección abierta a la vez. Esto refleja el comportamiento predeterminado del experto original cuando no hay cobertura activa.

## Archivos

- `CS/PlanXBreakoutStrategy.cs`: implementación en C# de la lógica de ruptura del Plan X para StockSharp.
