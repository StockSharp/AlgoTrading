# Estrategia de Emboscada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Emboscada rodea continuamente el mercado con un par de órdenes buy-stop y sell-stop. Las órdenes pendientes se colocan
a una indentación configurable por encima del mejor ask y por debajo del mejor bid, con una anulación dinámica que impone una distancia
mínima basada en el spread actual. Cada vez que un lado se activa, la estrategia reconstruye inmediatamente ambas órdenes para que el
mercado permanezca "emboscado" desde ambas direcciones. Un disyuntor simple basado en el patrimonio puede aplanar las posiciones una vez
que se alcanza un objetivo de ganancia diaria o un límite de pérdida.

Esta implementación en C# replica el comportamiento del experto original de MetaTrader 5 de Zuzabush. Opera puramente con cotizaciones
de Nivel 1 y no requiere velas ni indicadores. Cada decisión es impulsada por cambios en tiempo real del bid/ask, por lo que la
estrategia es más adecuada para instrumentos líquidos con spreads ajustados.

## Lógica de trading

1. **Recepción de datos de mercado**
   - La estrategia se suscribe a actualizaciones de Nivel 1 y rastrea el último mejor bid y best ask.
   - Los cálculos se detienen hasta que ambos lados del libro de órdenes estén disponibles y la estrategia tenga permiso de operar.
2. **Salvaguardas de patrimonio**
   - El PnL realizado (`PnL`) y el componente no realizado derivado del bid/ask actual y `PositionPrice` se suman.
   - Si el patrimonio combinado supera `EquityTakeProfit`, o cae por debajo de `-EquityStopLoss`, la posición neta actual se aplana
     con una orden de mercado. Las órdenes pendientes se dejan intactas, coincidiendo con el comportamiento original del experto.
3. **Colocación de órdenes pendientes**
   - El spread en unidades de precio se compara con `MaxSpreadPoints`. Si el spread es demasiado amplio, no se colocan nuevas órdenes.
   - De lo contrario, una distancia se calcula como `max(IndentationPoints * step, spread * 3)`. Ese valor replica la lógica MT5 de
     respetar la indentación del usuario o imponer tres spreads cuando el `StopsLevel` del bróker es cero.
   - Una orden buy-stop se coloca en `ask + distancia` y una sell-stop en `bid - distancia`. Los precios se normalizan al tick más
     cercano. Solo se permite una orden activa por lado; las órdenes obsoletas se limpian cuando su estado pasa a `Done`, `Failed` o `Canceled`.
4. **Trailing de órdenes pendientes**
   - Cuando `TrailingStopPoints` es mayor que cero, la estrategia recalcula periódicamente (no más frecuentemente que `Pause`) la
     distancia de stop usando `max((TrailingStopPoints + TrailingStepPoints) * step, spread * 3)` y vuelve a registrar las órdenes si
     el cambio supera medio tick.
   - El trailing mantiene las órdenes cerca del mercado mientras respeta la distancia mínima que evita el disparo prematuro.

El resultado final es un motor de ruptura tipo grid que espera constantemente que el precio se mueva decisivamente en cualquier dirección.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `IndentationPoints` | Distancia base en puntos entre el mercado y cada orden stop pendiente. |
| `MaxSpreadPoints` | Spread máximo permitido (en puntos). Las órdenes se suspenden mientras el spread es más amplio. |
| `TrailingStopPoints` | Distancia base de trailing en puntos aplicada a órdenes pendientes existentes. Establecer en cero para deshabilitar el trailing. |
| `TrailingStepPoints` | Buffer adicional añadido encima de la distancia base de trailing. |
| `Pause` | Tiempo mínimo entre dos recálculos de trailing. El valor predeterminado refleja la pausa de un segundo del experto MT5. |
| `EquityTakeProfit` | Ganancia de patrimonio en moneda de cuenta que desencadena un aplanamiento inmediato de posición. |
| `EquityStopLoss` | Pérdida de patrimonio permitida antes de cerrar la posición abierta. |
| `Volume` | Tamaño de orden heredado de la clase base `Strategy`. Usar el mínimo del bróker para imitar el predeterminado MT5. |

Todos los offsets de precio se convierten de puntos a unidades de precio reales usando `Security.PriceStep`. Si el instrumento no expone
un paso de precio, se usa un valor de respaldo de 1.

## Notas prácticas

- Debido a que la estrategia trabaja solo con órdenes stop, no se requieren velas ni indicadores. Puede ejecutarse durante backtests
  que no proporcionan velas históricas siempre que los datos de Nivel 1 estén disponibles.
- Los brókers que imponen un `StopsLevel` no nulo deben configurar `IndentationPoints` para que la diferencia de precio resultante
  satisfaga la regla del mercado. La red de seguridad de triple spread actúa como guarda secundaria.
- El filtro de patrimonio es intencionalmente ligero y no cancela órdenes pendientes. Esto refleja el comportamiento original de
  Ambush, permitiendo nuevas operaciones después del evento de aplanamiento sin intervención manual.
- El deslizamiento y la tolerancia de llenado de órdenes son controlados por el bróker o simulador conectado. Ajustar `Volume` y los
  valores de parámetros para que coincidan con la volatilidad del instrumento.

Esta documentación proporciona intencionalmente el máximo nivel de detalle para que tanto traders discrecionales como algorítmicos puedan
entender la conversión y personalizar la estrategia para su lugar de ejecución.
