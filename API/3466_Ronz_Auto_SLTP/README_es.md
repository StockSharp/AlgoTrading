# Estrategia SLTP de Ronz Auto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia Ronz Auto SLTP** es un puerto C# directo de la utilidad MetaTrader 5 *Ronz Auto SLTP*. Actúa como un administrador comercial que asigna automáticamente niveles protectores de stop-loss y take-profit, aplica bloqueo de ganancias y activa reglas de seguimiento para cada posición abierta. La conversión se basa en StockSharp API de alto nivel y admite tanto la supervisión de toda la cuenta como la implementación de un solo símbolo.

Capacidades clave:

- Aplique protección del lado del servidor o virtual (del lado del cliente) según el indicador `UseServerStops`.
- Establezca distancias iniciales de stop-loss y take-profit utilizando mediciones de pips estilo MetaTrader.
- Bloquee una cantidad fija de ganancias después de que la operación alcance un umbral configurable.
- Ejecute tres variaciones de trailing stop (clásico, distancia de paso, paso a paso) reflejando el asesor original.
- Supervise todos los valores de la cartera conectada o restrinja la gestión únicamente al valor de la estrategia.
- Emita notificaciones de registro opcionales cada vez que una parada virtual o una toma de ganancias cierre una posición.

## Parámetros

| Parámetro | Predeterminado | Descripción |
| --- | --- | --- |
| `ManageAllSecurities` | `true` | Supervise cada posición abierta en la cartera. Desactívelo para gestionar solo la seguridad de la estrategia. |
| `TakeProfitPips` | `550` | Distancia en MetaTrader pips agregada al precio de entrada para el objetivo de obtención de ganancias (incluida la distancia mínima de parada del corredor). |
| `StopLossPips` | `350` | Distancia en MetaTrader pips restada del precio de entrada para el nivel de stop-loss (incluida la distancia mínima de stop del corredor). |
| `UseServerStops` | `true` | Cuando esté habilitado, envíe órdenes stop y limit al corredor. Cuando está desactivado, cierre posiciones prácticamente una vez que se alcancen los umbrales. |
| `EnableLockProfit` | `true` | Habilite la lógica de bloqueo de ganancias que mueve el stop por encima o por debajo del precio de entrada después de alcanzar un umbral. |
| `LockProfitAfterPips` | `100` | Beneficio (en pips) que se debe lograr antes de que la lógica de bloqueo se active. Establezca en cero para omitir la etapa de bloqueo y el seguimiento inmediatamente. |
| `ProfitLockPips` | `60` | Beneficio conservado una vez que se activa la cerradura. La parada se mueve al precio de entrada más/menos esta distancia. |
| `TrailingStopMode` | `Classic` | Algoritmo de seguimiento utilizado después del umbral de bloqueo. Opciones: `None`, `Classic`, `StepDistance`, `StepByStep`. |
| `TrailingStopPips` | `50` | Distancia de seguimiento en pips. Actúa como amortiguador principal para los modos de seguimiento clásico y basado en pasos. |
| `TrailingStepPips` | `10` | Incremento utilizado por los modos de seguimiento basados en pasos. Ignorado por la variante trasera clásica. |
| `EnableAlerts` | `false` | Cuando sea verdadero, escriba mensajes de registro cada vez que una parada virtual o una toma de ganancias cierren una orden. |

## Detalles de comportamiento

1. **Protección inicial**
   - Cuando se detecta una nueva posición, la estrategia calcula objetivos de stop-loss y take-profit en relación con el precio de entrada.
   - Las distancias de parada mínimas definidas por el corredor se respetan leyendo los campos de nivel de parada/congelación de las actualizaciones de Nivel 1 y ampliando las distancias solicitadas si es necesario.

2. **Bloqueo de ganancias**
   - Una vez que la ganancia actual supera `LockProfitAfterPips`, el stop se eleva (o se reduce en el caso de posiciones cortas) para bloquear `ProfitLockPips` de ganancias.
   - Si el bloqueo está deshabilitado, la estrategia omite esta etapa y espera las condiciones finales.

3. **Paradas finales**
   - `Classic`: mantiene una distancia fija (`TrailingStopPips`) al precio actual.
   - `StepDistance`: reduce la distancia en `TrailingStepPips` una vez que el precio se ha movido lo suficientemente favorable, coincidiendo estrechamente con la implementación de MetaTrader "paso a mantener distancia".
   - `StepByStep`: empuja el stop hacia adelante en incrementos discretos de `TrailingStepPips` una vez que el precio ha avanzado la distancia de seguimiento configurada.
   - El seguimiento comienza inmediatamente cuando `LockProfitAfterPips` es cero. De lo contrario, se activa una vez que la ganancia excede `LockProfitAfterPips + TrailingStopPips`.

4. **Modo Virtual**
   - Cuando `UseServerStops` es falso, la estrategia no registra ninguna orden stop/limit. En cambio, cierra la posición abierta mediante órdenes de mercado tan pronto como se supera el límite de pérdidas o la toma de ganancias calculados.
   - Se pueden habilitar alertas para documentar estos cierres virtuales en el registro.

5. **Soporte de seguridad múltiple**
   - Con `ManageAllSecurities = true`, la estrategia se suscribe a datos de Nivel 1 para cada valor que tenga una posición abierta en la cartera seleccionada.
   - Cada valor mantiene su propio estado de parada, obtención de ganancias y seguimiento para que las operaciones largas y cortas se supervisen de forma independiente.

## Consejos de uso

- Adjunte la estrategia a una cartera y, opcionalmente, asigne un valor predeterminado cuando solo un instrumento necesite supervisión.
- Asegúrese de que los datos de Nivel 1 (mejor oferta/demanda) estén disponibles para cada símbolo administrado para que los cálculos de pips sigan siendo precisos.
- Revise las restricciones del nivel de parada del corredor: la estrategia ya amplía las distancias solicitadas, pero el centro de negociación aún puede rechazar configuraciones extremadamente estrictas.
- El modo virtual es útil en corredores que no admiten órdenes de protección o durante escenarios de backtesting.

## Diferencias con el experto original

- StockSharp agrega posiciones por valor, mientras que MetaTrader el modo de cobertura rastrea tickets individuales. Por tanto, el puerto gestiona la posición neta por instrumento.
- La funcionalidad de orden de prueba del script MQ5 (abrir operaciones ficticias en el probador) se omitió intencionalmente.
- Las alertas se envían a través del subsistema de registro StockSharp en lugar de ventanas emergentes en pantalla.
