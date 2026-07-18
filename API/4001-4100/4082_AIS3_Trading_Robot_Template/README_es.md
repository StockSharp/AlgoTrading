# Plantilla de robot comercial AIS3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La plantilla del robot comercial AIS3 es un sistema de ruptura MetaTrader que se basa en dos marcos de tiempo coordinados. El plazo principal
captura la estructura de la vela anterior, mientras que un período de tiempo secundario mide la volatilidad reciente para controlar las actualizaciones finales.
Este puerto StockSharp reproduce fielmente el tamaño del pedido original, las comprobaciones de entrada y la lógica de seguimiento, pero se implementa en
parte superior de la estrategia de alto nivel API para que pueda ejecutarse dentro de Designer, Shell o cualquier host StockSharp personalizado.

## Flujo de trabajo comercial
- **Suscripciones a datos de mercado**: la estrategia se suscribe a dos series de velas. La serie principal (predeterminada de 15 minutos) proporciona
el máximo, mínimo, cierre, punto medio y rango de la vela anterior. La serie secundaria (por defecto 1 minuto) mide el rango rápido utilizado
para paradas finales. Un feed del libro de pedidos en vivo mantiene los mejores precios de oferta y demanda actuales sincronizados con el MQL `MarketInfo` original
solicitudes.
- **Validación de rupturas**:
  - Una configuración larga se activa cuando el cierre anterior está por encima del punto medio y el precio de venta actual supera el anterior.
alto más el diferencial medido. El precio de entrada es el pedido actual.
  - Una configuración corta requiere que el cierre anterior se mantenga por debajo del punto medio y que la oferta supere el mínimo anterior. el precio de entrada
es la oferta actual.
  - Ambas direcciones heredan las comprobaciones de seguridad del corredor de la plantilla: la distancia entre la entrada y la parada/objetivo proyectado.
debe exceder el buffer de parada configurado, y la parada debe permanecer en el lado correcto del precio de entrada incluso después de sumar el
difundir.
- **Órdenes de protección**:
  - La distancia de stop-loss es igual a `primaryRange × StopMultiplier` y está anclada por encima (para largos) o por debajo (para cortos) del
vela de ruptura como se describe en el manual de integración.
  - La distancia de obtención de beneficios es igual a `primaryRange × TakeMultiplier` y se coloca desde el precio de entrada en la dirección comercial.
- **Gestión comercial**:
  - Cuando una posición está abierta, el rango del período de tiempo secundario multiplicado por `TrailMultiplier` define la distancia de seguimiento.
  - El trailing stop solo se actualiza si la operación genera ganancias y el nuevo nivel está más lejos que la congelación y parada configuradas.
zonas de amortiguamiento y la distancia entre la parada actual y la propuesta supera `TrailStepMultiplier × spread`. Esto refleja el
Requisito de plantilla de que el precio debe avanzar al menos un paso del recorrido antes de modificar la parada.
  - Las posiciones se cierran con órdenes de mercado siempre que la oferta/demanda toque los niveles almacenados de stop-loss o take-profit.

## Gestión del riesgo
- **Reserva de cuenta**: `AccountReserve` mantiene bloqueada una fracción del capital de la cartera. La estrategia se niega a abrir nuevas posiciones
si el capital reservado fuera inferior al presupuesto del pedido solicitado. Esto coincide con el comportamiento de la plantilla donde el riesgo
La reserva protege la cuenta de pérdidas en cascada.
- **Reserva de orden**: `OrderReserve` controla la parte del capital restante que se puede arriesgar por operación. El tamaño de la posición
se calcula como `riskBudget / |entry - stop|` y luego se alinea con el paso de volumen de seguridad. Si no hay métricas de cartera
disponible, en su lugar se utiliza el parámetro alternativo `BaseVolume`.
- **Detener y congelar buffers**: `StopBufferTicks` y `FreezeBufferTicks` traducen las limitaciones de detención del corredor (por ejemplo, `MODE_STOPLEVEL`
y `MODE_FREEZELEVEL` de MetaTrader) en unidades de precio utilizando el paso del precio del valor. Impiden que la estrategia se emita
órdenes que violen las restricciones cambiarias o que muevan el trailing stop de manera demasiado agresiva.
- **Multiplicador de pasos finales**: `TrailStepMultiplier` refleja la constante `acd.TrailStepping` de la plantilla MQ4. Asegura
que las actualizaciones finales solo ocurren cuando la nueva parada está al menos a un múltiplo del valor anterior.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `AccountReserve` | Fracción del capital mantenido como reserva de seguridad (0–0,95).
| `OrderReserve` | Fracción de capital negociable asignada al presupuesto de riesgo por operación (0–0,5 por defecto).
| `PrimaryCandleType` | Plazo de trabajo para la detección de rupturas (velas predeterminadas de 15 minutos).
| `SecondaryCandleType` | Marco de tiempo más rápido que controla la distancia de seguimiento (velas predeterminadas de 1 minuto).
| `TakeMultiplier` | Multiplicador del rango principal utilizado para realizar la orden de toma de ganancias.
| `StopMultiplier` | Multiplicador del rango primario utilizado para calcular la parada de protección.
| `TrailMultiplier` | Multiplicador del rango secundario que define la distancia de seguimiento.
| `BaseVolume` | Tamaño de la posición alternativa cuando las métricas de la cartera no están disponibles.
| `StopBufferTicks` | Distancia adicional, en ticks de precio, que debe permanecer entre los niveles de entrada y stop/objetivo.
| `FreezeBufferTicks` | Búfer adicional que evita detener las actualizaciones demasiado cerca del nivel de congelación del broker.
| `TrailStepMultiplier` | Multiplicador de diferencial que define el incremento mínimo entre los ajustes finales.

## Notas de uso
- Alimente la estrategia con series de velas y un flujo de libro de órdenes o de nivel 1 para que los mejores precios de oferta y demanda estén disponibles. corriendo
basarse únicamente en los datos de la última operación alterará los controles de ruptura porque dependen del diferencial.
- Los valores de parámetros predeterminados replican el ejemplo de plantilla MQ4 (`TakeMultiplier = 1`, `StopMultiplier = 2`,
`TrailMultiplier = 3`). Ajústelos para que coincidan con los activos con los que opera o para experimentar con la intensidad de la ruptura.
- El trailing stop es virtual: las órdenes no se modifican en la bolsa. Cuando se cumple la condición de seguimiento, la estrategia simplemente
emite una salida de mercado, reflejando cómo el asesor experto original gestionó las paradas internamente.
- Combine la estrategia con el módulo de protección integrado de StockSharp (ya habilitado en el constructor) para mantener la emergencia.
manejo de stop-loss incluso si la estrategia se desconecta temporalmente.
