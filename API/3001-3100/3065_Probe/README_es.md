# Estrategia Probe
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Probe reproduce el asesor experto de MetaTrader 5 "Probe" dentro del marco de alto nivel de StockSharp. Monitorea el Commodity Channel Index (CCI) en un marco temporal configurable y reacciona cuando el oscilador rompe fuera de un canal simétrico. Cuando ocurre un breakout, la estrategia coloca una orden stop con un desplazamiento del precio de mercado actual basado en pips. El enfoque busca capturar la continuación del momentum tras el breakout mientras mantiene el riesgo limitado mediante niveles protectores basados en pips y un trailing stop adaptativo.

## Lógica de trading
1. Calcular el CCI en el tipo de vela configurado.
2. Rastrear los valores anteriores y actuales del CCI para detectar cuándo el indicador sale del límite inferior o superior del canal.
3. Cuando el CCI cruza hacia arriba a través de `-CCI Channel`, enviar una orden stop de compra por encima del último cierre usando la distancia `Indent (pips)`.
4. Cuando el CCI cruza hacia abajo a través de `+CCI Channel`, enviar una orden stop de venta por debajo del último cierre usando el mismo indent en pips.
5. Solo puede permanecer activa una orden stop pendiente a la vez. Las órdenes opuestas se cancelan y las nuevas señales se ignoran mientras una orden está activa.

## Gestión de órdenes
- Las órdenes stop pendientes se retiran si el mercado se aleja del precio de entrada más de `1.5 * Indent (pips)`. Esto refleja la lógica de MetaTrader que evita que órdenes obsoletas permanezcan en el libro cuando el momentum se desvanece.
- Una vez que se ejecuta una orden stop, la estrategia almacena el precio ejecutado como la referencia de entrada. Cualquier orden pendiente opuesta se cancela inmediatamente.

## Gestión de riesgo
- Un stop-loss inicial se deriva de `Stop Loss (pips)` y se adjunta a la posición activa mediante monitoreo interno. Cuando el precio toca el stop, la posición se sale con una orden de mercado.
- El comportamiento de trailing comienza después de que el beneficio flotante supera `Trailing Stop (pips) + Trailing Step (pips)`. El stop se mueve entonces para asegurar ganancias respetando la distancia mínima de trailing.
- Todas las distancias basadas en pips se ajustan automáticamente para cotizaciones de 3 y 5 dígitos escalando el tamaño del tick de la bolsa.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Marco temporal principal usado para construir velas y calcular el CCI. |
| `CciLength` | Período de promediado del oscilador CCI. |
| `CciChannelLevel` | Umbral absoluto del CCI que forma el canal de breakout simétrico. |
| `IndentPips` | Distancia en pips agregada al último cierre al colocar la orden stop pendiente. |
| `StopLossPips` | Distancia del stop-loss protector medida en pips. |
| `TrailingStopPips` | Umbral de beneficio en pips requerido antes de que se active el trailing stop. |
| `TrailingStepPips` | Distancia adicional de beneficio necesaria antes de que el trailing stop se mueva de nuevo. |

## Notas
- Use la propiedad `Volume` de la estrategia para controlar el tamaño negociado.
- La estrategia está diseñada para netting de posición única, coincidiendo con el comportamiento del Asesor Experto original.
- La renderización del gráfico dibuja velas, el indicador CCI y operaciones ejecutadas cuando hay un área de gráfico disponible.
