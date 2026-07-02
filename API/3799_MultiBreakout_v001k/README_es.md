# Estrategia MultiBreakout V001k
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia MultiBreakout V001k reproduce el clásico asesor experto MT4 "Multibreakout_v001k". Opera con rupturas de la sesión horaria anterior acumulando órdenes de compra y venta una vez finalizada la hora de referencia. La gestión de posiciones sigue la lógica original de obtención de beneficios y equilibrio por etapas, incluido el equilibrio móvil opcional que rastrea las paradas utilizando los últimos máximos y mínimos horarios.

## Reglas de trading
1. **Hora de referencia** – Se pueden definir hasta cuatro sesiones de negociación. Después de que se cierra cada hora de sesión habilitada, la estrategia mide la vela horaria terminada y prepara órdenes para la siguiente hora.
2. **Ubicación de entrada** –
   - Las órdenes de stop de compra se colocan en el máximo de la hora anterior más el diferencial actual y un colchón de entrada adicional (`PipsForEntry`).
   - Las órdenes Sell-Stop se colocan en el mínimo de la hora anterior menos el colchón de entrada.
   - Cada lado coloca `NumberOfOrdersPerSide` órdenes pendientes con volumen idéntico.
3. **Escalera de obtención de beneficios**: cada entrada recibe un objetivo de beneficio individual espaciado por `TakeProfitIncrement` puntos. Cuando el mercado toca cada nivel, la estrategia cierra un tramo en el mercado para imitar la cola de obtención de beneficios original de MT4.
4. **Gestión de stop-loss**: se establece un stop inicial a `StopLoss` puntos del precio de entrada. Una vez que el precio se mueve `BreakEven` puntos a favor, el stop salta al punto de equilibrio. Si `MovingBreakEven` está habilitado y pasa el retraso configurado, la parada sigue utilizando los mínimos horarios más recientes (para largos) o máximos (para cortos) cuando esos niveles continúan apretándose.
5. **Salida de la sesión**: a las `ExitMinute` dentro de la hora de la sesión configurada, la estrategia cierra por completo todas las posiciones y elimina todas las órdenes pendientes.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `TradeVolume` | Volumen para cada orden de ruptura. |
| `NumberOfOrdersPerSide` | Cantidad de órdenes pendientes apiladas para ambas direcciones. |
| `TakeProfitIncrement` | Distancia (en puntos) entre objetivos de obtención de beneficios consecutivos. |
| `PipsForEntry` | Se agregaron puntos adicionales al disparador de ruptura por encima/por debajo del rango de la sesión. |
| `StopLoss` | Distancia de parada inicial desde el precio de entrada. |
| `BreakEven` | Beneficio (en puntos) requerido antes de que el stop llegue al punto de equilibrio. |
| `MovingBreakEven` | Habilita la lógica móvil de seguimiento del punto de equilibrio. |
| `MovingBreakEvenHoursToStart` | Retraso (en horas) después de la sesión de referencia antes de que el punto de equilibrio móvil pueda retroceder. |
| `BrokerOffsetToGmt` | Desfase horario entre la hora del corredor y el GMT utilizado por el programador de equilibrio móvil. |
| `TradeSession1..4` | Alterna para las cuatro sesiones de negociación independientes. |
| `SessionHour1..4` | Hora (0-23) que define cada sesión de referencia. |
| `ExitMinute` | Minuto dentro de la hora de sesión para liquidar posiciones y cancelar órdenes. |
| `CandleType` | Tipo de vela utilizado para medir la hora de referencia (el valor predeterminado es velas de 1 hora). |

## Notas de uso
- Asegúrese de que el instrumento tenga un `PriceStep` válido para que los cálculos del valor en puntos coincidan con la versión MT4.
- La estrategia supone que los tiempos de los corredores están alineados con las marcas de tiempo de las velas. Ajuste `BrokerOffsetToGmt` cuando históricamente se haya utilizado un desplazamiento de servidor MT4 diferente.
- El punto de equilibrio móvil evalúa las dos últimas velas horarias terminadas antes de apretar el tope, igualando el comportamiento del asesor experto original.
