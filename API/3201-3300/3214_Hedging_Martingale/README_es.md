# Estrategia de Hedging Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un port StockSharp del asesor experto de MetaTrader "Hedging Martingale" (carpeta `MQL/23693`). Mantiene una cobertura equilibrada abriendo tanto una posición larga como una corta en cada nueva barra y luego aplica un esquema de promediado martingala. Cuando el precio se mueve adversamente por una distancia de pip configurable, la estrategia agrega una nueva posición en el lado perdedor con un volumen aumentado mientras mantiene la cobertura opuesta. La ganancia flotante se gestiona usando objetivos basados en dinero y porcentaje junto con un bloqueo de seguimiento opcional.

## Lógica de trading
- **Cobertura inicial**: siempre que la estrategia esté plana y una nueva vela cierre, simultáneamente compra y vende usando el mismo volumen base.
- **Pasos de martingala**: si el precio se mueve en contra de un lado por `Pip Step` pips, se abre una orden adicional en ese lado. El volumen se multiplica por `Volume Multiplier`, emulando el dimensionamiento de lote progresivo de la versión MQL. El lado opuesto permanece abierto para mantener la cobertura.
- **Take-profit por operación**: cada entrada abierta tiene una distancia de take-profit individual definida por `Take Profit (pips)`. Cuando el mercado se mueve a favor de una pierna por esa distancia, la pierna se reduce emitiendo una orden compensatoria.
- **Salidas de canasta**: el conjunto completo de posiciones puede cerrarse cuando la ganancia flotante alcanza un objetivo monetario, un porcentaje del capital inicial, o después de que un bloqueo de seguimiento da más que el retroceso permitido. Estos comportamientos replican `Take_Profit_In_Money`, `Take_Profit_In_percent`, y `TRAIL_PROFIT_IN_MONEY2` del experto original.
- **Límites de operaciones**: el parámetro `Max Trades` restringe cuántos pasos de martingala pueden estar activos. Si `Close On Max` está habilitado, la canasta se liquida una vez que se supera el límite.

## Parámetros
| Nombre | Descripción |
| ---- | ----------- |
| Candle Type | Marco temporal que impulsa la lógica. Cada vela terminada puede desencadenar nuevas acciones de cobertura. |
| Use Money TP / Money Take Profit | Habilitar y definir la ganancia flotante (en unidades de moneda) que cierra todas las posiciones. |
| Use Percent TP / Percent Take Profit | Cerrar la canasta cuando la ganancia flotante alcanza un porcentaje del valor inicial del portafolio. |
| Enable Trailing / Trailing Start / Trailing Step | Activar el bloqueo de seguimiento basado en dinero para la canasta y configurar el nivel de disparo junto con el retroceso de ganancia permitido. |
| Take Profit (pips) | Distancia en pips para salidas de take-profit por pierna. |
| Pip Step | Movimiento adverso de precio (en pips) requerido antes de agregar otra orden de martingala. |
| Base Volume | Volumen inicial para las piernas de compra y venta. |
| Volume Multiplier | Multiplicador aplicado al volumen de posición más grande al agregar entradas de martingala. |
| Max Trades | Número máximo de entradas abiertas simultáneamente (en ambas direcciones). |
| Close On Max | Si liquidar todas las posiciones una vez que se supera el conteo máximo de operaciones. |

## Notas
- La estrategia usa `BuyMarket` y `SellMarket` para todas las colocaciones de órdenes, reflejando el modelo de ejecución de mercado del experto fuente.
- Los valores de volumen se normalizan al paso de lote del instrumento para evitar órdenes rechazadas.
- Cuando la estrategia queda plana, el bloqueo de seguimiento se reinicia para que las nuevas canastas comiencen con una referencia de ganancia limpia.

## Archivos
- `CS/HedgingMartingaleStrategy.cs` – implementación de la estrategia convertida (C#).
- `README.md` – esta documentación (inglés).
- `README_zh.md` – traducción al chino.
- `README_ru.md` – traducción al ruso.
