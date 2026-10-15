# Estrategia de límite de cambio de suerte
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Lucky Shift Limit** es una conversión directa del MetaTrader 4 asesores expertos `Lucky_acnl6p6j89zn91fa.mq4`. Observa las mejores cotizaciones de oferta y demanda en tiempo real y reacciona ante saltos repentinos medidos en MetaTrader "puntos" (pips). Cuando el precio de venta se acelera hacia arriba en la distancia de desplazamiento configurada, la estrategia desvanece el movimiento vendiendo, mientras que una fuerte caída en la oferta provoca una compra contraria. Todas las operaciones abiertas se monitorean y cierran constantemente una vez que se vuelven rentables o cuando la pérdida flotante excede un umbral de seguridad idéntico a la lógica original de MQ4.

## Requisitos de datos y ejecución

- **Datos de mercado**: se suscribe únicamente a cotizaciones de Nivel 1; no se requieren velas ni profundidad de mercado.
- **Estilo de ejecución**: las entradas y salidas se basan en órdenes de mercado para imitar las llamadas `OrderSend` inmediatas de MetaTrader.
- **Modo de cuenta**: funciona tanto con cuentas de cobertura como de compensación. En las cuentas de compensación, la estrategia acumula exposición en una sola posición y el módulo de salida la aplana.
- **Tamaño del volumen**: el tamaño del pedido predeterminado proviene de `Strategy.Volume`, pero el asistente emula `AccountFreeMargin/10000` de MetaTrader cuando el valor de la cartera está disponible.

## Parámetros

| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `Shift points` | 3 | Número mínimo de MetaTrader puntos entre solicitudes/ofertas consecutivas que activan un nuevo pedido. Los valores más grandes filtran el ruido, los valores más pequeños reaccionan más rápido. |
| `Limit points` | 18 | Excursión adversa máxima permitida para una operación abierta. Si el precio se mueve contra la posición en tantos puntos, la operación se cierra a la fuerza. |

Ambos parámetros se expresan en MetaTrader puntos y se convierten internamente en compensaciones de precios absolutas utilizando el tamaño del tick del instrumento. Los límites de optimización en la interfaz de usuario coinciden con los rangos prácticos de la versión MQ4.

## Lógica comercial

1. **Inicialización**
   - Convierte la configuración basada en puntos en distancias de precios reales usando `Security.PriceStep`.
   - Restablece las cotizaciones de oferta/demanda almacenadas en caché e inicia una suscripción de nivel 1 con procesamiento `Bind` de alto nivel.
2. **Condiciones de entrada**
   - Si la demanda aumenta al menos `Shift points` en comparación con la demanda anterior, la estrategia envía una orden de venta de mercado (atenuando el pico) con una nota de registro que explica el desencadenante.
   - Si la oferta cae al menos en la misma distancia que la oferta anterior, se abre una compra de mercado.
   - Las señales pueden dispararse varias veces en secuencia, exactamente como el experto original que no restringía el número de posiciones simultáneas.
3. **Gestión de salida**
   - Cada tick de cotización invoca `TryClosePosition()`. Las posiciones largas se cierran inmediatamente cuando la oferta está por encima de la entrada promedio (beneficio realizado) o cuando la demanda es inferior a la entrada en `Limit points` (límite de pérdidas).
   - Las posiciones cortas reflejan esta lógica, cerrándose con cotizaciones de venta rentables o cuando la oferta excede la entrada por el límite configurado.
   - Todas las salidas utilizan órdenes de mercado para replicar `OrderClose` y garantizar que la posición se aplana en el mismo tick.
4. **Tamaño de posición**
   - Calcula el volumen predeterminado del capital de la cartera (`equity / 10,000`, lote redondeado a un decimal) cuando esté disponible, coincidiendo con el asistente MQ4 `GetLots()`.
   - Vuelve a la propiedad de la estrategia `Volume` cuando faltan datos de capital.

## Notas de implementación

- Utiliza solo API StockSharp de alto nivel: `SubscribeLevel1().Bind(ProcessLevel1)` elimina la necesidad de escuchas de cotizaciones manuales.
- No se almacenan colecciones personalizadas; Los valores de oferta/demanda anteriores se mantienen en variables simples que aceptan valores NULL según lo permitido por las pautas.
- El límite de pérdidas funciona con el tamaño del tick del instrumento, por lo que los símbolos exóticos con pasos de pip fraccionarios se asignan automáticamente al delta de precio correcto.
- Se respetan los cambios de parámetros durante el tiempo de ejecución: la estrategia vuelve a calcular los umbrales cuando llegan los datos del Nivel 1.
- Las declaraciones de registro documentan cada motivo de entrada y salida, lo que simplifica las pruebas retrospectivas y los diagnósticos en vivo.

## Consejos de uso

- Ideal para pares de divisas o índices de alta liquidez donde se producen con frecuencia shocks de oferta y demanda.
- Considere combinar la estrategia con protecciones a nivel de cartera (`StartProtection`) si se requieren límites de reducción o stop loss adicionales.
- Aumente `Shift points` en feeds ruidosos para reducir el exceso de operaciones o disminúyalo para capturar movimientos a ultracorto plazo.
- La lógica es inherentemente contraria; si desea un comportamiento de ruptura, simplemente establezca `Shift points` lo suficientemente alto o combínelo con otro indicador de filtro.
