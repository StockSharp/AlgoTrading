# Estrategia Surefirething
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Surefirething recrea el clásico asesor experto de MetaTrader 5 que coloca órdenes límite simétricas de compra y venta alrededor del cierre de la vela más reciente. El sistema reconstruye constantemente la cuadrícula después de cada vela completada, gestiona los stops de protección en unidades de pip, y fuerza una posición completamente plana diez minutos antes de la medianoche, hora del servidor.

## Procesamiento de velas
- Funciona con un tipo de vela configurable (predeterminado: marco temporal de 1 hora).
- Después de cada vela terminada, la estrategia calcula un rango amplificado: `range = (high - low) * 1.1`.
- Deriva dos niveles de ruptura de ese rango:
  - `L4 = close - range / 2` para la orden límite de compra.
  - `H4 = close + range / 2` para la orden límite de venta.
- Las órdenes pendientes existentes se cancelan antes de publicar la nueva cuadrícula, por lo que solo una orden límite de compra y una de venta permanecen activas.

## Gestión de órdenes
- La orden límite de compra en `L4` y la orden límite de venta en `H4` se registran con el volumen de orden configurado.
- Una vez que se abre una posición, la orden pendiente opuesta se cancela inmediatamente.
- Cada día a las **23:50** (hora del servidor), la estrategia:
  - Cancela cualquier orden pendiente restante.
  - Cierra la posición abierta al mercado, si la hay.
  - Restablece todos los rastreadores de stop/take-profit para comenzar la próxima sesión limpiamente.

## Gestión de riesgos
- Las distancias de stop-loss y take-profit se definen en pips y se traducen en precios usando el paso de precio del instrumento (los símbolos de 5 dígitos y 3 dígitos se ajustan a las unidades pip clásicas automáticamente).
- Se puede habilitar un trailing stop (también en pips). Cada vez que el precio se mueve más allá de `TrailingStopPips + TrailingStepPips`, el stop avanza a `precio actual - TrailingStopPips` para posiciones largas o `precio actual + TrailingStopPips` para posiciones cortas.
- Ambos niveles de protección se monitorean en cada vela. Si la vela opera a través del stop o el objetivo, la estrategia sale de la posición usando órdenes de mercado.

## Parámetros
- `OrderVolume` – volumen base para ambas órdenes límite (predeterminado: `0.1`).
- `StopLossPips` – distancia del stop-loss en pips (predeterminado: `50`).
- `TakeProfitPips` – distancia del take-profit en pips (predeterminado: `50`).
- `TrailingStopPips` – distancia del trailing stop en pips (predeterminado: `25`).
- `TrailingStepPips` – movimiento adicional en pips requerido antes de que se mueva el trailing stop (predeterminado: `1`). Debe ser mayor que cero cuando el trailing stop está habilitado.
- `CandleType` – tipo de datos de vela usado para los cálculos (predeterminado: marco temporal de 1 hora).

## Notas
- La implementación coincide con la lógica MQL original al garantizar que el paso de trailing sea diferente de cero siempre que el trailing esté activo.
- No se proporciona implementación Python para esta estrategia.
