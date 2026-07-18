# Martingale Estrategia del simulador comercial
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

`MartingaleTradeSimulatorStrategy` recrea el asesor experto "Martingale Trade Simulator" de MetaTrader dentro del marco StockSharp. La estrategia es un panel de negociación manual que permite al operador enviar órdenes de mercado inmediatas, aplicar promedios estilo martingala y administrar la protección de seguimiento sin secuencias de comandos de automatización adicional. Reacciona a los cambios de parámetros en tiempo real, lo que lo hace adecuado para experimentos de Strategy Tester al igual que el robot MQL original.

## como funciona

### Botones de mercado manuales
- Los parámetros `Buy` y `Sell` actúan como botones virtuales. Cuando cualquiera de los parámetros se establece en `true`, la estrategia envía una orden de mercado con el volumen `Order Volume` y luego restablece automáticamente el parámetro a `false`.
- No se utilizan órdenes pendientes: la estrategia funciona completamente con ejecuciones de mercado, reflejando el comportamiento del simulador dentro del probador visual de MetaTrader.

### Martingale promedio
- Habilitar `Enable Martingale` permite que el panel realice pedidos promedio cuando el parámetro `Martingale` se cambia a `true`.
- La estrategia comprueba la posición activa:
  - **Posición larga:** si el precio de venta actual está al menos `Martingale Step (points)` por debajo del precio de compra más bajo ejecutado, se envía una nueva orden de compra.
  - **Posición corta:** Si el precio de oferta actual está al menos `Martingale Step (points)` por encima del precio de venta más alto ejecutado, se emite una nueva orden de venta.
- Cada volumen de pedido promedio es igual a `Order Volume × Martingale Multiplier^N`, donde `N` es el número de entradas consecutivas en la dirección actual.
- Cuando la martingala está activa, el objetivo de obtención de beneficios se recalcula al precio de entrada medio ponderado más/menos `Martingale TP Offset (points)` para cubrir la reducción acumulada.

### Módulo de parada de seguimiento
- `Enable Trailing` activa un trailing stop protector que sigue el mejor precio más reciente.
- El trailing stop comienza a `Trailing Stop (points)` del precio de mercado y avanza solo después de que el precio mejora al menos `Trailing Step (points)`.
- Si el precio de mercado cruza el nivel final, la estrategia cierra inmediatamente toda la posición con una orden de mercado opuesta.

### Stop-loss y take-profit
- `Stop Loss (points)` y `Take Profit (points)` reproducen los controles de riesgo básicos del asesor experto original.
- Para posiciones largas, el stop se sitúa por debajo del precio medio de entrada, mientras que la toma de beneficios se sitúa por encima. Para posiciones cortas, ambos niveles se reflejan.
- Las salidas protectoras se ejecutan con órdenes de mercado, por lo que la estrategia sigue siendo compatible con cualquier conector compatible con StockSharp.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `Order Volume` | Tamaño base para órdenes de mercado manuales. | `1` |
| `Stop Loss (points)` | Distancia al tope de protección. Zero desactiva el stop-loss. | `500` |
| `Take Profit (points)` | Distancia al objetivo protector. Zero desactiva la toma de ganancias. | `500` |
| `Enable Trailing` | Enciende/apaga el módulo de trailing stop. | `true` |
| `Trailing Stop (points)` | Distancia entre el precio y el trailing stop. | `50` |
| `Trailing Step (points)` | Se requiere un movimiento mínimo favorable para avanzar el trailing stop. | `20` |
| `Enable Martingale` | Permite promediar órdenes controladas por el botón `Martingale`. | `true` |
| `Martingale Multiplier` | Multiplicador de volumen utilizado para cada operación promedio adicional. | `1.2` |
| `Martingale Step (points)` | Se requiere movimiento adverso antes de que se permita una orden de promediación. | `150` |
| `Martingale TP Offset (points)` | Compensación adicional aplicada al nivel promedio de obtención de beneficios. | `50` |
| `Buy` | Establezca en `true` para enviar una orden de compra de mercado (reinicio automático). | `false` |
| `Sell` | Establezca en `true` para enviar una orden de venta de mercado (reinicio automático). | `false` |
| `Martingale` | Establezca en `true` para evaluar y realizar un pedido promedio (reinicio automático). | `false` |

## Consejos de uso

1. Adjunte la estrategia a un instrumento, configure `Order Volume` e iníciela en modo probador o en vivo.
2. Utilice los botones `Buy` / `Sell` para simular clics en los botones del panel MetaTrader.
3. Después de la primera operación, active el interruptor `Martingale` siempre que el precio se mueva en contra de la posición. La estrategia verifica la distancia del precio y aumenta el volumen si se cumplen las condiciones.
4. Ajuste los parámetros de seguimiento y riesgo para replicar el comportamiento del EA original o experimentar con configuraciones alternativas.

## Notas

- La estrategia se basa en datos de Nivel 1 (mejor oferta/demanda y última operación) para evaluar las condiciones del mercado.
- Todos los comentarios dentro del código C# están en inglés, manteniendo la coherencia con las pautas del repositorio.
