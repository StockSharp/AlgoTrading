# Estrategia de promediación de Amstell SL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Conversión del MetaTrader asesor experto `exp_Amstell-SL`. El sistema abre inmediatamente tanto una posición larga como una corta, agrega nuevas órdenes cada vez que el precio se mueve contra la entrada más reciente por un número fijo de puntos, y se basa en niveles virtuales de toma de ganancias y stop-loss (administrados por software) para salir de cada boleto individualmente.

## Lógica de la estrategia

- **Entradas iniciales**: Cuando la estrategia comienza y no hay operaciones abiertas, envía una compra de mercado (a la demanda) y una venta de mercado (a la oferta).
- **Pirámide en reducción**:
  - Lado largo: siempre que la demanda actual esté `ReentryPoints` (por defecto, 10 puntos) por debajo del último precio de entrada larga, se envía una nueva orden de compra del mismo volumen.
  - Lado corto: siempre que la oferta actual esté `ReentryPoints` por encima del último precio de entrada corta, se abre una nueva orden de venta del mismo volumen.
- **Reglas de Salida (gestión virtual)**:
  - Para cada ticket largo, la estrategia monitorea la mejor oferta y la mejor demanda. Si la oferta aumenta un `TakeProfitPoints` con respecto al precio de la orden o la demanda cae un `StopLossPoints`, la posición se cierra en el mercado.
  - Para cada ticket corto, verifica si la demanda es menor en `TakeProfitPoints` o la oferta es mayor en `StopLossPoints`; en cualquier caso, la orden de venta se cubre en el mercado.
- **Orden de procesamiento**: las salidas se evalúan antes de cualquier entrada nueva, replicando el script MetaTrader que detiene acciones adicionales después de cerrar una posición en el tick actual.

## Parámetros

- `TakeProfitPoints` – distancia (en pasos de precio) utilizada para cerrar posiciones rentables. Predeterminado: `30`.
- `StopLossPoints` – distancia (en pasos de precio) para salidas protectoras. Predeterminado: `30`.
- `Volume`: tamaño de lote para cada pedido recién abierto. Predeterminado: `0.01`.
- `ReentryPoints` – movimiento adverso (en pasos de precio) necesario para acumular una orden adicional en el lado correspondiente. Predeterminado: `10`.

## Notas adicionales

- El valor del punto se deriva de `Security.PriceStep`; si el intercambio no lo proporciona, se utiliza un valor de `1`.
- La estrategia puede ser simultáneamente larga y corta porque rastrea la compra y venta de boletos de forma independiente, coincidiendo con el comportamiento de cobertura del asesor experto original.
- Los niveles de toma de ganancias y límite de pérdidas se ejecutan virtualmente mediante órdenes de mercado; no se colocan en el libro de órdenes de cambio.
- El riesgo aumenta rápidamente cuando los precios tienen una fuerte tendencia en una dirección porque se abren órdenes adicionales sin reducir la exposición anterior.
- Funciona mejor con símbolos donde la noción de "punto" equivale a un incremento mínimo de precio, por ejemplo, pares de divisas importantes con precios de estilo MetaTrader.
