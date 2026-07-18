# Estrategia de cesta troncal (StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Backbone Basket** traslada el asesor experto original MetaTrader 4 "Backbone.mq4" al StockSharp nivel alto API. El sistema recopila los extremos de oferta y demanda para determinar la dirección comercial inicial y luego alterna entre cestas largas y cortas. Cada cesta se construye gradualmente, agregando una orden de mercado por vela completa hasta que se alcanza el recuento `MaxTrades` configurado o las órdenes de protección cierran la posición. El control de riesgos se mantiene a través de un modelo de riesgo fraccionado que escala el volumen comercial según el valor de la cuenta y la distancia del límite de pérdidas.

## Flujo de datos de mercado
- **Velas (`CandleType`)** – velas completadas aceleran la toma de decisiones; Sólo se puede emitir una orden por barra terminada, exactamente como en el script MT4.
- **Instantáneas del libro de pedidos**: se realiza un seguimiento de los mejores valores de oferta y demanda para reproducir los cálculos de trailing-stop y la lógica de descubrimiento "extrema" inicial.
- **Estado de la estrategia**: la base StockSharp `Strategy` mantiene la posición en ejecución, el precio de entrada promedio y el PnL utilizados para administrar las órdenes de protección.

## Lógica de trading
1. **Calibración inicial**: aunque no se define ninguna dirección, la estrategia registra la oferta más alta y la demanda más baja vistas. Cuando el precio retrocede `TrailingStopPoints * PriceStep` desde esos extremos, se elige la primera dirección de la cesta.
2. **Secuenciación de pedidos** –
   - Si la última operación completada fue corta (`_lastPositionDirection == -1`) y no hay operaciones abiertas, se envía una nueva orden de **compra de mercado**.
   - Si la operación anterior fue larga (`_lastPositionDirection == 1`) y la cesta aún tiene capacidad, se envían órdenes de compra adicionales en las velas posteriores.
   - Se aplican reglas simétricas para órdenes de venta cuando la última operación fue larga.
3. **Tamaño del volumen**: cada nuevo pedido llama al análogo `Vol()` inspirado en MT4. El valor disponible de la cuenta (valor actual → saldo → saldo inicial) se multiplica por `MaxRisk` y se divide por la distancia de stop-loss convertida en dinero usando `PriceStepCost`. El resultado se alinea con `VolumeStep`, está limitado por `MinVolume`/`MaxVolume` y se rechaza si cae por debajo del tamaño mínimo de operación.
4. **Órdenes de protección**: una vez que se ejecuta una operación, la estrategia coloca una única orden de limitación de pérdidas y toma de ganancias que cubre toda la cesta. Las distancias se expresan en "puntos" (escalones de precio) al igual que la versión MQL.
5. **Trailing stop**: cuando tanto `StopLossPoints` como `TrailingStopPoints` son positivos, la orden stop se vuelve a emitir para bloquear las ganancias siempre que el precio se mueva más que la distancia de seguimiento más allá del precio de entrada registrado. Las cestas largas toman como referencia la mejor oferta; Las cestas cortas utilizan el mejor pedido.
6. **Completación de la canasta**: si se ejecuta la orden de stop-loss o take-profit, todos los contadores internos se reinician, dejando `LastPosition` sin cambios para que la siguiente vela inicie una canasta en la dirección opuesta, reflejando el comportamiento original de EA.

## Gestión monetaria
- Utiliza la misma fórmula fraccionaria `1 / (MaxTrades / MaxRisk - openTrades)` que el experto MQL.
- El capital de riesgo se estima a partir de `Portfolio.CurrentValue`, retrocediendo a `CurrentBalance` o `BeginBalance`.
- El volumen se descarta si el tamaño calculado está por debajo del `MinVolume` del instrumento después de la alineación con `VolumeStep`.
- Las órdenes de stop-loss y take-profit se recrean cada vez que cambia el volumen, de modo que la protección siempre cubra toda la cesta.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `CandleType` | plazo de 15 minutos | Intervalo de vela utilizado para desencadenar nuevas decisiones. |
| `MaxRisk` | 0,5 | Fracción de la cartera considerada al dimensionar el siguiente pedido. Debe ser positivo. |
| `MaxTrades` | 10 | Número máximo de operaciones que se pueden acumular en la cesta actual. |
| `TakeProfitPoints` | 170 | Distancia de obtención de beneficios medida en incrementos de precios. Establezca en `0` para desactivar. |
| `StopLossPoints` | 40 | Distancia de stop-loss medida en pasos de precio. Requerido para el seguimiento y el tamaño de la posición. |
| `TrailingStopPoints` | 300 | Distancia del trailing-stop en pasos de precio. Establezca en `0` para mantener una parada estática. |

## Notas de conversión
- El EA original modifica cada orden individualmente; la versión StockSharp administra un stop-loss y un take-profit agregados por cesta porque las posiciones StockSharp se compensan de forma predeterminada.
- El tamaño del volumen depende de `Security.PriceStepCost`. Si el conector no proporciona este valor, la estrategia recurre a la propiedad `Volume` configurada.
- Las actualizaciones finales se aplican cuando llega una nueva vela, coincidiendo con el comportamiento "una vez por barra" del script MT4 (que solo actuó cuando `Bars > PrevBars`).
- La lógica alterna mantiene la última dirección ejecutada en `_lastPositionDirection`, por lo que una vez que se cierra una canasta, la siguiente vela abre automáticamente una canasta en la dirección opuesta, al igual que el código fuente.
- Sólo se proporciona la implementación de C#; no hay ningún puerto Python en este directorio.

## Consejos de uso
- Asigne instrumentos con `PriceStep`, `PriceStepCost` y metadatos de volumen precisos para obtener tamaños de posición realistas.
- Al realizar pruebas retrospectivas, asegúrese de que el feed del libro de pedidos esté disponible para que la lógica de trailing-stop pueda acceder a los mejores valores de oferta/demanda.
- Para deshabilitar el escalado agresivo, aumente `MaxTrades` o reduzca `MaxRisk` para que el reemplazo de `Vol()` devuelva volúmenes más pequeños.
