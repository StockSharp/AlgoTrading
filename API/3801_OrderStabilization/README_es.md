# Estrategia de estabilización de pedidos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Estabilización de Órdenes** es una conversión del MetaTrader asesor experto `hjueiisyx8lp2o379e_www_forex-instruments_info.mq4`. El robot original coloca un par de órdenes stop alrededor del precio actual y espera una ruptura. Una vez que se abre una posición, el sistema monitorea los cuerpos de velas recientes para determinar si la acción del precio se ha estancado ("estabilizado") y sale de la operación cuando el mercado pierde impulso o cuando se alcanza un umbral de ganancias predefinido.

Este puerto de C# mantiene la misma lógica utilizando el nivel alto StockSharp API. Se basa en velas completas en lugar de ticks sin procesar, lo que hace que el comportamiento sea determinista durante las pruebas retrospectivas y las operaciones en vivo.

## Reglas de trading
1. Cuando no hay posiciones abiertas ni órdenes activas, la estrategia envía un **stop de compra** por encima del mercado y un **stop de venta** por debajo del mercado. La distancia se mide en MetaTrader puntos (generalmente igual a un pip).
2. Si se ejecuta una orden de parada:
   - La orden ejecutada abre una posición de `OrderVolume` lotes.
   - La orden de stop opuesta sigue pendiente para detectar una ruptura en la otra dirección.
3. Mientras una posición está abierta, la estrategia verifica el tamaño del cuerpo de las dos velas terminadas más recientes:
   - Si el último cuerpo de la vela es menor que `StabilizationPoints` y el beneficio flotante es mayor que `ProfitThreshold`, la posición se cierra y la orden pendiente opuesta se cancela.
   - Si dos velas consecutivas son más pequeñas que `StabilizationPoints`, la operación se cierra independientemente de las ganancias actuales.
   - Si la ganancia alcanza `AbsoluteFixation`, la operación se cierra inmediatamente.
4. Las órdenes pendientes se cancelan y se vuelven a crear después de `ExpirationMinutes` a menos que el valor se establezca en cero (vida útil infinita).

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `OrderVolume` | Volumen comercial en lotes utilizados para ambas entradas de parada. | `0.1` |
| `OrderDistancePoints` | Distancia entre el precio de cierre actual y cada orden stop, expresada en MetaTrader puntos. | `20` |
| `ProfitThreshold` | Beneficio flotante mínimo (moneda de la cuenta) requerido antes de que se permita una salida provocada por la estabilización. | `-2` |
| `AbsoluteFixation` | Nivel de beneficio (moneda de la cuenta) que obliga a una salida inmediata. | `30` |
| `StabilizationPoints` | Tamaño máximo del cuerpo de la vela (puntos) que indica un mercado plano. | `25` |
| `ExpirationMinutes` | Vida útil de las órdenes stop pendientes en minutos. `0` desactiva la caducidad. | `20` |
| `CandleType` | Tipo de vela utilizada para evaluar la estabilización (el valor predeterminado es un período de tiempo de 5 minutos). | `TimeFrame(5m)` |

## Notas de conversión
- El asesor experto original operaba según los ticks del gráfico. Este puerto evalúa solo velas terminadas, preservando la lógica y garantizando pruebas retrospectivas reproducibles.
- MetaTrader "puntos" se asignan a StockSharp `PriceStep`. Si el instrumento carece de un paso de precio, se supone un paso de `1`.
- La ganancia se aproxima usando `PriceStep` y `StepPrice` para traducir el movimiento de precios a la moneda de la cuenta.
- Todos los comentarios del código se reescribieron en inglés y los metadatos de los parámetros incluyen descripciones fáciles de usar con agrupación.

## Uso
1. Agregue la estrategia a su solución StockSharp y asigne la seguridad y el portafolio deseados.
2. Configure los parámetros, especialmente el período de tiempo de la vela y la distancia en puntos para que coincidan con las características del instrumento.
3. Inicia la estrategia. Enviará órdenes stop emparejadas y gestionará posiciones de acuerdo con la lógica de estabilización descrita anteriormente.

## Más ideas
- Experimente con diferentes intervalos de velas para equilibrar la capacidad de respuesta y el filtrado de ruido.
- Combine la estrategia con filtros de volatilidad (ATR, Bollinger Bandas) para evitar operar durante sesiones extremadamente tranquilas.
- Amplíe la lógica con trailingstops o salidas parciales de posiciones una vez que se acerque al objetivo de beneficio absoluto.
