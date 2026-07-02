# Estrategia de niveles MartingalaEA-5 (StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de niveles MartingaleEA-5** es una adaptación directa del MetaTrader asesor experto de 5 "Niveles MartinaleEA-5" al API de alto nivel de StockSharp. El sistema supervisa una posición existente y construye una cuadrícula de promedio de cinco pasos cada vez que el mercado se mueve en contra. Toda la lógica se ejecuta en velas terminadas, lo que mantiene el comportamiento reproducible tanto en pruebas históricas como en operaciones reales.

## Lógica de trading

1. **Seguimiento de la exposición existente**: la estrategia espera que esté presente una posición inicial larga o corta. Puede abrir la primera operación manualmente o mediante cualquier otra estrategia.
2. **Detección de movimientos adversos**: en cada vela completada, la estrategia mide qué tan lejos se ha alejado el precio actual de la entrada con el peor precio del grupo activo (la posición larga más alta o la posición corta más baja).
3. **Martingale adiciones**: si la pérdida flotante en el grupo es negativa y el movimiento adverso excede las distancias acumuladas configuradas, la estrategia envía órdenes de mercado adicionales. Cada pedido adicional multiplica el anterior por `VolumeMultiplier`. Se pueden configurar hasta cinco niveles; el parámetro `MaxAdditions` limita cuántos de ellos se utilizan realmente.
4. **Objetivo de pérdidas y ganancias**: mientras un grupo está abierto, la estrategia suma continuamente el PnL no realizado para esa dirección. Una vez que el total alcanza `TakeProfitCurrency` o cae por debajo de `StopLossCurrency`, todas las órdenes de ese lado se cierran con una orden de mercado y los contadores de martingala se reinician.
5. **Normalización de volumen**: cada volumen de pedido pasa por los `VolumeStep`, `MinVolume` y `MaxVolume` del instrumento para evitar el envío de cantidades no ejecutables.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `EnableMartingale` | Activa o desactiva la lógica de promediación y liquidación. | `true` |
| `VolumeMultiplier` | Factor aplicado al volumen del pedido anterior al agregar un nuevo nivel. | `2.0` |
| `MaxAdditions` | Número máximo de pasos de martingala por dirección (hasta cinco). | `4` |
| `Level1DistancePips` | Distancia adversa inicial (en pips) antes de abrir la segunda orden. | `300` |
| `Level2DistancePips` | Distancia adicional requerida para el tercer pedido. | `400` |
| `Level3DistancePips` | Distancia adicional requerida para el cuarto orden. | `500` |
| `Level4DistancePips` | Distancia adicional requerida para el quinto pedido. | `600` |
| `Level5DistancePips` | Distancia adicional requerida para el sexto orden (si está permitido). | `700` |
| `TakeProfitCurrency` | Beneficio no realizado (moneda de la cuenta) que cierra todo el grupo. | `200` |
| `StopLossCurrency` | Pérdida no realizada (moneda de la cuenta) que obliga a una salida de emergencia. | `-500` |
| `CandleType` | Marco de tiempo utilizado para las evaluaciones (velas predeterminadas de 1 minuto). | `TimeFrame(1m)` |

> **Conversión de pips**: cada distancia se multiplica por el paso del precio del instrumento (`PriceStep` o `MinPriceStep`). Para los símbolos cotizados en pips fraccionarios, ajuste los valores en consecuencia.

## Notas y recomendaciones

- La implementación refleja el EA original, incluida la suposición de que solo una cesta direccional está activa a la vez. Abrir posiciones simultáneamente en ambas direcciones hará que cada lado se gestione de forma independiente.
- Debido a que la estrategia reacciona solo al cierre de la vela, elija un período de tiempo que coincida con la capacidad de respuesta deseada. Los plazos más bajos emulan más fielmente el comportamiento a nivel de tick.
- Las técnicas Martingale amplifican el riesgo. Realice siempre una prueba retrospectiva con modelos realistas de deslizamiento y comisión y defina niveles de parada conservadores antes de habilitar la estrategia en los mercados reales.
- La estrategia aún no crea un puerto Python. Solo se incluye la implementación de alto nivel de C# según lo solicitado.
