# Estrategia de cierre de ganancias virtuales
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Virtual Profit Close replica el comportamiento del MetaTrader 4 asesor experto *Virtual_Profit_Close.mq4*. La estrategia vigila el
posición actual del valor configurado y sale tan pronto como se alcanza un objetivo de beneficio virtual. A diferencia de una orden regular de toma de ganancias,
el nivel de salida se evalúa internamente, por lo que no quedan órdenes de ganancias en el libro de órdenes. Un trailing stop configurable puede mover la salida
precio más cercano al mercado a medida que el comercio avanza hacia la obtención de ganancias. Cuando se ejecuta en modo de prueba, la estrategia puede abrir automáticamente posiciones de muestra.
para demostrar su lógica.

## Notas de conversión

- Los eventos de tick se consumen a través de `SubscribeTrades().Bind(ProcessTrade).Start()` para imitar la rutina original `OnTick`.
- Los MetaTrader "puntos" se convierten en pips inspeccionando `Security.PriceStep` y ajustando los símbolos de 3/5 dígitos.
- Los cálculos de ganancias virtuales y seguimiento utilizan la oferta actual para posiciones largas y la demanda para posiciones cortas, coincidiendo con el MQL
implementación que se basó en los precios `Bid` y `Ask`.
- La lógica del trailing stop se activa después del umbral de beneficio configurado y mantiene el stop a una distancia fija del mercado.
precio, similar a llamar repetidamente a `OrderModify` en MQL.
- Un modo de demostración reemplaza el asistente de prueba de estrategia original (`SendTest`) al abrir órdenes de mercado de acuerdo con el método seleccionado.
dirección y volumen. Las paradas protectoras opcionales se colocan usando `SetStopLoss`.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `ProfitPips` | Nivel de toma de ganancias virtual expresado en MetaTrader pips. La estrategia cierra la posición una vez que el beneficio supera esta distancia. |
| `UseTrailingStop` | Habilita el comportamiento de seguimiento cuando se establece en `true`. |
| `TrailingOffsetPips` | Distancia mantenida entre el precio actual y el trailing stop una vez activo. |
| `TrailingActivationPips` | Beneficio mínimo en pips requerido antes de que se active el trailing stop. |
| `EnableDemoMode` | Abre automáticamente órdenes de demostración cada vez que la posición se vuelve plana. Útil para pruebas retrospectivas. |
| `DemoOrderDirection` | Dirección de pedidos de demostración (`Buy` o `Sell`). |
| `DemoOrderVolume` | Volumen enviado para pedidos de demostración. |
| `DemoStopPips` | Stop de protección opcional para órdenes de demostración, expresado en pips. |

## Comportamiento

1. Cuando comienza la estrategia, calcula el tamaño del pip y las distancias para obtener ganancias, paradas finales y de demostración.
2. Cada tick recibido a través de `ProcessTrade` evalúa la posición actual:
   - Las posiciones largas se cierran cuando el precio de oferta genera el beneficio virtual configurado.
   - Las posiciones cortas se cierran cuando el precio de venta cubre la misma distancia en la dirección opuesta.
3. Si el seguimiento está habilitado y se alcanza el umbral de activación, el trailing stop se mueve junto con el movimiento favorable del precio. una vez
el mercado cruza el nivel final, la estrategia envía una orden de mercado para salir.
4. El modo de demostración puede abrir automáticamente una nueva posición cada vez que la estrategia se vuelve plana, recreando la característica exclusiva para probadores del
experto original.

## Requisitos

- La estrategia necesita datos de mercado a nivel de tick para responder con precisión a los cambios de precios.
- Sólo se debe asignar un símbolo a la instancia de estrategia. No se admiten varios símbolos simultáneos que coincidan con el original
Implementación MQL que monitoreó el símbolo del gráfico actual.
