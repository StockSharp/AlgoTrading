# Estrategia rápida de Doji
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia Rapid Doji replica la lógica del asesor experto original "Rapid Doji EA". Escanea velas terminadas de un período de tiempo configurable (diariamente de forma predeterminada) y coloca órdenes de parada de entrada encima y debajo de cada vela doji. Los stop de protección se colocan utilizando un multiplicador de rango verdadero promedio (ATR), mientras que un trailing stop auxiliar mantiene la distancia de riesgo fija en puntos brutos después de que una posición se vuelve rentable.

## Lógica de trading

1. **Suscripción de datos**: la estrategia escucha las velas terminadas del período de tiempo seleccionado y mantiene un indicador ATR con un período configurable.
2. **Detección de Doji**: una vela se trata como un doji cuando el tamaño absoluto del cuerpo es como máximo el 3% del rango total de la vela. Sólo se evalúan velas completas.
3. **Realización de pedidos** – cuando se encuentra un doji válido:
   - Se coloca una orden de compra stop en el máximo del doji.
   - Se coloca una orden de venta stop en el mínimo de doji.
   - Cada entrada recuerda un precio stop de protección igual al extremo opuesto menos/más ATR × multiplicador.
4. **Gestión de riesgos**: una vez que se abre una posición, la orden pendiente restante se cancela, el stop memorizado se registra como un stop de protección y la lógica de seguimiento toma el control.
5. **Trailing stop**: en cada nueva vela, el nivel de stop se mueve para mantener una distancia fija (en puntos convertidos a través del paso del precio del instrumento) desde el último precio de cierre, pero solo cuando la posición ya es rentable.

La estrategia nunca utiliza objetivos de obtención de beneficios; las salidas se realizan mediante el tope de protección o intervención manual.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Tipo de datos de vela utilizado para la detección de patrones (período diario de forma predeterminada). |
| `AtrPeriod` | Longitud retrospectiva del indicador ATR. |
| `AtrMultiplier` | Multiplicador aplicado al valor ATR para el cálculo del stop-loss. |
| `TrailingDistancePoints` | Distancia fija en puntos brutos utilizados al seguir la parada. |

Todos los parámetros admiten la optimización dentro del entorno StockSharp.

## Notas de implementación

- El código se basa en la suscripción de vela de alto nivel API (`SubscribeCandles`) combinada con el enlace de indicador (`Bind`) para evitar el manejo manual del historial.
- Las órdenes se normalizan a través de `Security.ShrinkPrice` para respetar el tamaño del tick de intercambio.
- Las paradas de protección se gestionan explícitamente para imitar el comportamiento del asesor experto MetaTrader original.
- El proyecto omite intencionalmente una implementación de Python según los requisitos de la tarea.
