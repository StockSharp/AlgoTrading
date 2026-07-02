# Estrategia de seguimiento de rango
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Range Follower reproduce el MetaTrader 5 asesor experto "Range Follower" utilizando la API de alto nivel de StockSharp. Supervisa el rango de precios del día actual en relación con un punto de referencia diario de rango verdadero promedio (ATR) y abre una única operación de ruptura cuando el precio se aleja lo suficiente del máximo o mínimo de la sesión. La conversión mantiene el enfoque original de dividir el ATR en una porción de activación y una porción residual que se convierte en la distancia de obtención de ganancias.

## Lógica de trading
1. **Línea base de volatilidad diaria**
   - Un ATR de 20 períodos calculado sobre velas diarias proporciona el rango de referencia para el día de negociación actual.
   - El valor ATR se divide por `TriggerPercent` en dos segmentos: la distancia de activación que se debe exceder antes de ingresar y la distancia restante que se utiliza como objetivo de ganancias.
2. **Seguimiento de alcance**
   - La estrategia registra continuamente los máximos y mínimos de la sesión actual de la vela diaria activa.
   - Las actualizaciones de Nivel 1 proporcionan los últimos mejores precios de oferta y mejor demanda que se utilizan para medir la distancia desde las cotizaciones actuales hasta los extremos de la sesión.
3. **Entrada única por día**
   - Cuando la mejor oferta supera la distancia de activación por encima del mínimo de la sesión y aún no se ha abierto ninguna operación, la estrategia compra en el mercado.
   - Cuando la mejor demanda es mayor que la distancia de activación por debajo del máximo de la sesión y aún no se ha abierto ninguna operación, la estrategia vende al mercado.
   - Sólo se permite una operación por día; la bandera se restablece cuando comienza una nueva sesión.
4. **Detener pérdidas y tomar ganancias**
   - Para posiciones largas, el stop-loss se coloca una distancia de activación por debajo del precio de entrada y el take-profit una distancia residual por encima de este.
   - Para posiciones cortas, el stop-loss está una distancia de activación por encima del precio de entrada y el take-profit una distancia residual por debajo de él.
   - El seguimiento de precios se realiza tanto en los ticks de Nivel 1 como en las actualizaciones de velas para cerrar posiciones tan pronto como se supera un nivel.
5. **Restablecimiento de sesión diaria**
   - En la primera vela de un nuevo día de negociación, la estrategia cierra cualquier posición abierta, borra el estado interno y recarga la línea de base ATR.
   - Si el rango diario actual ya excede la distancia de activación cuando se inicializa la sesión, se omite la negociación durante el resto del día para imitar la verificación de seguridad del EA original.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `CandleType` | velas de 15 minutos | Horario de trabajo utilizado para detectar límites de sesión. |
| `TriggerPercent` | 60 | Porcentaje del ATR diario utilizado como distancia de activación de ruptura. Debe permanecer entre 10 y 90. |
| `Volume` | 0.1 | Volumen de órdenes de mercado para entradas largas y cortas. |

## Gestión del riesgo
- Las paradas y los objetivos se derivan de la misma línea base ATR de modo que la relación recompensa-riesgo siempre sea igual a `(100 - TriggerPercent) : TriggerPercent`.
- La estrategia registra una única posición a la vez y la liquida inmediatamente cuando se toca el stop o el objetivo, evitando múltiples operaciones superpuestas.
- `StartProtection()` habilita la infraestructura de protección de StockSharp, lo que permite que los componentes externos conecten topes dinámicos o protectores de cartera si es necesario.

## Notas de implementación
- Los valores diarios ATR se producen mediante una suscripción de vela diaria dedicada y el indicador `AverageTrueRange` vinculado a través del nivel alto API.
- Los datos de nivel 1 son necesarios para reflejar las decisiones basadas en ticks de EA; Los mejores precios de oferta y demanda impulsan tanto las comprobaciones de entrada como de salida.
- Los límites de las sesiones diarias se derivan de las velas del período de trabajo, lo que garantiza que cualquier calendario comercial utilizado en StockSharp restablecerá la estrategia de manera consistente.
- La conversión evita búferes de indicadores manuales o bucles históricos y, en cambio, se basa en campos con estado actualizados por las devoluciones de llamada `Bind`.
