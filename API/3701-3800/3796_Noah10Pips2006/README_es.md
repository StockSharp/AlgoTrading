# Estrategia Noah 10 Pips 2006
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
- Recrea la lógica de ruptura y reversión del asesor experto original Noah10pips2006 MetaTrader 4.
- Crea canales de precios de la sesión anterior y coloca órdenes stop alrededor del punto medio.
- Aplica un seguimiento de ganancias seguro, un tamaño de posición dinámico opcional y una operación de reversión opcional después del cierre de la primera posición.

## Lógica de trading
1. **Cálculo del rango de sesiones**
Al comienzo de cada nuevo día de negociación (después de aplicar el desplazamiento de zona horaria configurado), la estrategia registra los máximos y mínimos de la sesión anterior. Estos niveles se utilizan para calcular:
   - El punto medio entre lo alto y lo bajo.
   - Un buffer de "pase" colocado 20 pips por encima/por debajo del rango.
   - Un canal de entrada obtenido restando/sumando 40 pips (o el 25% del rango si el rango es mayor a 160 pips).
2. **Pedido inicial pendiente**
Cuando el mercado entra en la ventana de negociación, la estrategia comprueba el último cierre:
   - Si el cierre está entre el punto medio y el buffer superior, se coloca un **stop de venta** en el punto medio.
   - Si el cierre se produce entre el buffer inferior y el punto medio, se coloca un **stop de compra** en el punto medio.
El ancho del rango debe exceder el mínimo configurado antes de realizar cualquier pedido.
3. **Segundo pedido pendiente**
Si solo queda activa una orden de stop, el sistema añade la orden de dirección opuesta en el buffer correspondiente (buffer superior para un stop de compra, buffer inferior para un stop de venta). Esto refleja el comportamiento original de EA y prepara la estrategia para rupturas en ambos lados del rango.
4. **Gestión de posiciones**
   - Las órdenes protectoras de stop-loss y take-profit se crean después de que se completa una entrada.
   - Una vez que la ganancia flotante alcanza el umbral de activación seguro, el límite de pérdidas se mueve para bloquear la ganancia segura configurada.
   - Cuando el bloqueo de seguridad está activo, un trailing stop opcional sigue el precio con la distancia especificada.
5. **Apagado diario**
Todas las órdenes pendientes y posiciones abiertas se cierran cuando finaliza la ventana de negociación o cuando se alcanza el límite del viernes.
6. **Operación de reversión**
La primera posición completada puede desencadenar una orden de mercado en dirección opuesta, reproduciendo el comportamiento "inverso después de la parada" del código original. La reversión se omite si el ajuste de ganancias seguras ya aseguró ganancias.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Serie de velas utilizada para impulsar cálculos y tiempos. Predeterminado: velas de 1 hora. |
| `TimeZoneOffset` | Turno (en horas) aplicado a las marcas de tiempo del intercambio antes de los cálculos diarios. |
| `StartHour`, `StartMinute` | Hora de apertura de la ventana de negociación en la zona horaria desplazada. |
| `EndHour`, `EndMinute` | Hora de cierre de la ventana de negociación. Las nuevas entradas no se colocan posteriormente. |
| `FridayEndHour` | Hora del viernes en la que se fuerza el cierre de posiciones. |
| `TradeFriday` | Habilita o deshabilita la apertura de nuevas posiciones el viernes. |
| `StopLossPips`, `TakeProfitPips` | Distancia (en pips) de las órdenes de protección creadas después de la entrada. |
| `TrailingStopPips` | Distancia del trailing-stop utilizada después del paso de obtención de beneficios seguros. Establezca en 0 para desactivar el seguimiento. |
| `SecureProfitPips` | Beneficio bloqueado cuando se activa el disparador seguro. |
| `TrailSecureProfitPips` | Umbral de beneficio requerido antes de mover el stop al nivel seguro. |
| `MinimumRangePips` | Ancho mínimo del canal de entrada requerido para realizar pedidos. |
| `StartYear`, `StartMonth` | Ignore los datos de mercado anteriores a esta fecha. |
| `MinVolume`, `MaxVolume` | Límites aplicados al volumen de orden calculado. |
| `MaximumRiskPercent` | Porcentaje del valor de la cartera arriesgado por operación cuando el dimensionamiento dinámico está habilitado. |
| `FixedVolume` | Cuando `true`, la estrategia utiliza la propiedad `Volume` en lugar del modelo de riesgo. |

## Notas prácticas
- El instrumento debe proporcionar valores `PriceStep` y `StepPrice` válidos cuando se utiliza el modo de dimensionamiento de posiciones basado en el riesgo.
- Los ajustes de seguimiento y de ganancias seguras dependen de las velas completadas, por lo que los rellenos intrabar se procesan en la siguiente vela terminada.
- La estrategia cancela y reemplaza las órdenes de protección cada vez que la lógica de seguimiento actualiza el precio de parada.
- Asegúrese de que el desplazamiento de la zona horaria coincida con la fuente de datos históricos; de lo contrario, el rango del día anterior puede diferir del experto MT4 original.

## Advertencias de conversión
- Se omitieron los objetos de dibujo visuales de la versión MT4; utilice los niveles suministrados o agregue anotaciones de gráficos personalizadas si es necesario.
- El algoritmo asume cotizaciones Forex de cuatro dígitos al convertir los buffers fijos de 20/40 pips; ajustar los parámetros para diferentes clases de activos.
- Las operaciones inversas se ejecutan en el mercado con el modelo de volumen actual, igualando el comportamiento del EA original después de eliminar las órdenes pendientes opuestas.
