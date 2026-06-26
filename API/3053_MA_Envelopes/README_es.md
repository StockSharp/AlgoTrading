# Estrategia de MA con Envelopes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Convertida del experto de MetaTrader 5 "MA Envelopes". La estrategia busca retrocesos de precio hacia una media móvil envuelta por un canal de envelopes. Cuando una vela completada cierra entre la media móvil y una de las bandas del envelope durante la ventana de trading configurada, la estrategia coloca entradas limitadas en la media móvil con órdenes de salida protectoras derivadas del envelope.

## Lógica de trading

1. Se calcula una media móvil con el método, fuente de precio y período seleccionados. El mismo valor se usa para construir bandas simétricas de envelope usando el parámetro de desviación.
2. Cuando una vela terminada cierra por encima de la media móvil pero por debajo de la banda superior del envelope y el precio ask actual permanece por encima de la media móvil, se prepara una secuencia escalonada de órdenes de compra limitadas en el precio de la media móvil.
   * Cada compra limitada usa el envelope inferior como nivel de stop-loss y el envelope superior más un offset adicional en pips como take-profit.
   * Se gestionan hasta tres órdenes independientes, cada una con su propio offset de take-profit (parámetros SL/TP de `First`, `Second`, `Third`).
3. Cuando una vela terminada cierra por debajo de la media móvil pero por encima de la banda inferior del envelope y el precio bid actual permanece por debajo de la media móvil, la lógica se refleja para órdenes de venta limitadas.
4. La ventana de trading está controlada por `StartHour` y `EndHour` (hora del terminal). Después de la hora de fin, todas las órdenes de entrada aún activas se cancelan.
5. El riesgo por operación se estima a través de `MaximumRisk` y se reduce después de pérdidas consecutivas usando `DecreaseFactor`. El volumen de la orden se alinea con el paso de volumen y los límites del instrumento.
6. Una vez que una orden de entrada está completamente ejecutada, las órdenes de stop-loss y take-profit protectoras se registran inmediatamente. Si se activa una orden de salida, la orden contrapartida se cancela y, si hay volumen de posición restante, se emiten nuevas órdenes protectoras para el resto.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `MaximumRisk` | Fracción del capital disponible arriesgada por posición. |
| `DecreaseFactor` | Reduce el tamaño de la posición después de operaciones perdedoras consecutivas. |
| `First/Second/ThirdStopTakeProfitPips` | Distancias en pips añadidas a las bandas del envelope para las tres órdenes escalonadas. |
| `StartHour`, `EndHour` | Límites de la sesión de trading en hora del terminal (0–23). |
| `MaPeriod`, `MaShift`, `MaMethodType`, `AppliedPrice` | Configuración de la media móvil. |
| `EnvelopeDeviation` | Ancho del canal de envelope en porcentaje. |
| `CandleType` | Marco temporal de las velas usadas para los cálculos. |

## Notas

* Las órdenes protectoras se recrean siempre que solo parte de una posición está cerrada, manteniendo cubierto el tamaño restante.
* Las órdenes de entrada pendientes se cancelan al final de la sesión; las posiciones abiertas siguen siendo gestionadas por sus órdenes protectoras.
* La estrategia depende de las actualizaciones del libro de órdenes para capturar los últimos precios bid/ask; los valores de cierre de las velas se usan como alternativa cuando los datos del libro de órdenes no están disponibles.
