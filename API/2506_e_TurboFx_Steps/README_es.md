# Estrategia e-TurboFx Steps
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
La estrategia **e-TurboFx** es un sistema de reversión por agotamiento del impulso originalmente escrito para MetaTrader 5. Monitorea las velas terminadas más recientes y busca secuencias donde los cuerpos de las velas siguen expandiéndose en la misma dirección. Una serie creciente de velas bajistas indica capitulación y por tanto un posible setup largo, mientras que una serie creciente de velas alcistas anuncia una posible oportunidad corta. El puerto de StockSharp usa la API de alto nivel con suscripciones a velas y protección automatizada de posición.

## Lógica de Trading
- Inspeccionar las últimas `DepthAnalysis` velas terminadas del `CandleType` seleccionado.
- Contar cuántas velas consecutivas cerraron por debajo de su apertura (bajistas) y cuántas cerraron por encima de su apertura (alcistas).
- Seguir la progresión del tamaño del cuerpo: cada nueva vela en la secuencia debe tener un cuerpo absoluto más grande que la anterior. Cuando esta condición falla, la secuencia se reinicia.
- **Entrada larga:** `DepthAnalysis` velas bajistas consecutivas con cuerpos estrictamente en expansión desencadenan una compra a mercado, siempre que no haya posición abierta actualmente.
- **Entrada corta:** `DepthAnalysis` velas alcistas consecutivas con cuerpos estrictamente en expansión desencadenan una venta a mercado, igualmente solo cuando la posición está plana.
- Mientras una posición está activa, la estrategia pausa la detección de señales para evitar apilar operaciones. La gestión del riesgo se delega al bloque de protección integrado configurado al inicio.

## Gestión de Posición
- `StartProtection` registra automáticamente órdenes de stop-loss y take-profit usando distancias medidas en pasos de precio (ticks del exchange). Establecer una distancia en cero deshabilita la orden de protección correspondiente.
- La estrategia mantiene solo una posición abierta. Cuando aparece una nueva señal después de que la operación anterior se cierra, las secuencias de velas se reconstruyen desde cero basándose en datos de mercado frescos.
- Las entradas a mercado usan el parámetro `TradeVolume`. Cambiar el parámetro en la UI actualiza inmediatamente el volumen de la estrategia.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `DepthAnalysis` | Número de velas terminadas recientes usadas para validar el patrón de expansión. Valores más altos exigen rachas más largas antes de operar. | `3` |
| `TakeProfitSteps` | Distancia del take-profit en pasos de precio del exchange (ticks). `0` deshabilita el take-profit. | `120` |
| `StopLossSteps` | Distancia del stop-loss en pasos de precio del exchange (ticks). `0` deshabilita el stop-loss. | `70` |
| `TradeVolume` | Volumen de orden enviado con cada entrada a mercado. | `0.1` |
| `CandleType` | Tipo de datos de vela (marco temporal) suscrito para el análisis. | Marco temporal de `1 hora` |

Todos los parámetros numéricos tienen metadatos de optimización para que puedan incluirse en las optimizaciones de StockSharp si se desea.

## Notas y Recomendaciones
- El asesor experto MQL5 original recalculaba los datos de velas en cada tick; la implementación de StockSharp logra el mismo comportamiento con eventos de velas terminadas y contadores internos.
- Debido a que la estrategia se basa en comparaciones del cuerpo de velas, es sensible al marco temporal seleccionado. Los marcos temporales más cortos producirán más señales pero pueden requerir stops más ajustados.
- Asegúrese de que el instrumento conectado exponga un `PriceStep` válido para que las distancias de stop-loss y take-profit definidas en pasos se traduzcan correctamente a precios.
- Antes de operar en vivo, valide el comportamiento en el Designer/Backtester para confirmar que las distancias de stop y objetivo se alineen con el instrumento elegido.
