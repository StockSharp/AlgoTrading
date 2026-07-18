# Estrategia Two MA RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Two MA RSI es una conversión del asesor experto original de MetaTrader "2MA_RSI". Utiliza un cruce de media móvil exponencial (EMA) rápida y lenta confirmado por un filtro de Índice de Fuerza Relativa (RSI). Las órdenes se dimensionan con un bloque de gestión de dinero estilo martingala que aumenta el volumen de la siguiente orden después de una pérdida. La versión StockSharp funciona completamente en velas terminadas y reproduce el comportamiento original de take-profit y stop-loss en puntos de precio.

## Datos e indicadores
- La estrategia se suscribe a una única serie de velas definida por `CandleType` (velas de 5 minutos por defecto).
- Se calculan tres indicadores en cada barra completada:
  - EMA de `FastLength` (aplicada al cierre de la vela).
  - EMA de `SlowLength`.
  - RSI con longitud `RsiLength`.
- Los valores históricos de los indicadores se almacenan internamente para detectar cruces de EMA sin extraer datos de los buffers de indicadores.

## Lógica de entrada
1. La vela anterior debe estar terminada para evitar la re-evaluación intrabarra.
2. No se permite ninguna posición activa (`Position == 0`).
3. **Entrada larga:**
   - La EMA rápida cruza por encima de la EMA lenta (la EMA rápida en la barra actual es mayor que la EMA lenta, mientras que en la barra anterior EMA rápida < EMA lenta).
   - El valor del RSI está por debajo de `RsiOversold`, confirmando un mercado sobrevendido.
4. **Entrada corta:**
   - La EMA rápida cruza por debajo de la EMA lenta con la condición análoga (EMA rápida ahora por debajo de EMA lenta, anteriormente por encima).
   - El RSI está por encima de `RsiOverbought`, indicando un mercado sobrecomprado.
5. Cuando se satisfacen todas las condiciones, la estrategia envía una orden a mercado dimensionada según el módulo de martingala.

## Lógica de salida
- Un stop loss de protección y un take profit se calculan inmediatamente después de cada entrada. Las distancias se definen en "puntos" y se convierten a través del `PriceStep` del instrumento:
  - **Largo:**
    - Stop loss = `precio de entrada - StopLossPoints * PriceStep`.
    - Take profit = `precio de entrada + TakeProfitPoints * PriceStep`.
  - **Corto:**
    - Stop loss = `precio de entrada + StopLossPoints * PriceStep`.
    - Take profit = `precio de entrada - TakeProfitPoints * PriceStep`.
- Solo estos niveles de protección cierran una operación. La estrategia espera la siguiente vela para confirmar si el mínimo/máximo tocó el objetivo o el stop y envía una orden `ClosePosition()` a mercado en consecuencia.
- La prioridad de salida coincide con el comportamiento conservador del robot original: un stop loss se evalúa antes que un take profit si ambos niveles caen dentro del mismo rango de vela.

## Dimensionamiento de posición y martingala
1. El volumen base se calcula en cada entrada como `floor(balance / BalanceDivider) * VolumeStep`. El valor siempre se mantiene en o por encima de un paso de volumen y usa `CurrentValue` del portafolio (recurriendo a `BeginValue` cuando sea necesario).
2. Después de cada salida perdedora, la etapa de martingala aumenta en uno hasta `MaxDoublings`. El siguiente volumen de orden se multiplica por `2^stage`.
3. Cualquier operación ganadora o alcanzar el número máximo de duplicaciones restablece la etapa a cero, volviendo al volumen base.
4. Si `MaxDoublings` es cero o negativo, el tamaño nunca aumenta e iguala el volumen base.

## Comportamiento adicional
- La estrategia lleva un registro de los valores previos de EMA internamente y no solicita valores históricos de indicadores.
- Las órdenes se ejecutan solo cuando la estrategia está en línea, los indicadores están formados y el trading está permitido.
- La salida de gráfico dibuja velas de precios, operaciones propias y los tres indicadores para análisis visual.

## Parámetros
| Parámetro | Descripción | Valor predeterminado |
|-----------|-------------|---------|
| `FastLength` | Longitud de la EMA rápida. | 5 |
| `SlowLength` | Longitud de la EMA lenta. | 20 |
| `RsiLength` | Número de barras usadas en el cálculo del RSI. | 14 |
| `RsiOverbought` | Nivel RSI que bloquea nuevos largos y permite cortos. | 70 |
| `RsiOversold` | Nivel RSI que permite largos. | 30 |
| `StopLossPoints` | Distancia del stop loss expresada en pasos de precio. | 500 |
| `TakeProfitPoints` | Distancia del take profit en pasos de precio. | 1500 |
| `BalanceDivider` | Divide el valor del portafolio para obtener el tamaño base de la orden. | 1000 |
| `MaxDoublings` | Número máximo de duplicaciones de martingala después de pérdidas consecutivas. | 1 |
| `CandleType` | Serie de velas utilizada por la estrategia. | Marco temporal de 5 minutos |

## Notas de uso
- Proporcionar un portafolio e instrumento con metadatos válidos de `PriceStep` y `VolumeStep` para que la gestión de riesgo basada en puntos y el dimensionamiento de posición permanezcan consistentes.
- Dado que se usan órdenes a mercado para las salidas, el deslizamiento y los spreads son posibles en comparación con las órdenes límite de la versión MetaTrader, pero la lógica de evaluación de stop/take se preserva.
- La estrategia no crea una versión Python; solo se suministra la implementación C# según lo solicitado.
