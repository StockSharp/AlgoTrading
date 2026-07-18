# Estrategia Cryptocurrency Divergence
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general
La **estrategia Cryptocurrency Divergence** busca divergencias clásicas de momentum entre la acción del precio y el Relative Strength Index (RSI), confirmando la dirección de tendencia con medias móviles y MACD. El asesor experto original de MetaTrader dependía de comprobaciones de momentum multimarco, gestión monetaria y una lógica amplia de trailing. Este port StockSharp conserva el espíritu del sistema al:

- Detectar divergencias alcistas cuando el precio marca un mínimo más bajo pero el RSI forma un mínimo más alto.
- Detectar divergencias bajistas cuando el precio crea un máximo más alto pero el RSI imprime un máximo más bajo.
- Validar configuraciones con medias móviles rápida/lenta y la línea MACD frente a la señal.
- Gestionar posiciones mediante stop loss, take profit, break-even y trailing stop configurables expresados en pasos de precio.

La estrategia está diseñada para criptomonedas al contado, pero puede aplicarse a cualquier instrumento que entregue suficiente volatilidad y puntos de giro claros.

## Indicadores
- **Media móvil simple (SMA)**: una SMA rápida y una lenta proporcionan el filtro principal de tendencia.
- **Relative Strength Index (RSI)**: suministra los valores de pivote de momentum usados para medir la fuerza de divergencia.
- **Moving Average Convergence Divergence (MACD)**: confirma que el momentum coincide con la dirección de divergencia detectada.

Todos los indicadores se enlazan mediante la API de alto nivel, por lo que no se requiere búfer manual.

## Lógica de negociación
1. Suscribirse al tipo de vela configurado y calcular valores SMA, RSI y MACD en cada barra terminada.
2. Seguir los swing highs y lows más recientes junto con sus valores RSI. Solo las extensiones monotónicas (nuevos máximos más altos o mínimos más bajos) actualizan los datos de swing.
3. Una **divergencia alcista** aparece cuando un nuevo mínimo más bajo del precio se combina con un mínimo RSI más alto. La operación también requiere que la SMA rápida esté por encima de la lenta, que la línea MACD supere su señal y que el RSI permanezca por debajo del nivel neutral (45 por defecto) para asegurar condiciones de sobreventa.
4. Una **divergencia bajista** requiere un nuevo máximo más alto en precio con un máximo RSI más bajo, SMA rápida por debajo de la lenta, línea MACD bajo su señal y RSI por encima del nivel bajista neutral (55 por defecto).
5. La estrategia abre solo una posición neta a la vez. Las reversiones cierran la posición existente y entran inmediatamente en la dirección opuesta cuando las señales se alinean.

## Gestión de riesgo
- **Volumen**: tamaño de operación definido por el usuario y aplicado a todas las órdenes de mercado.
- **Stop Loss / Take Profit**: expresados en pasos de precio y adjuntados después de cada ejecución usando el precio real de ejecución.
- **Movimiento a break-even**: opcionalmente reemplaza el stop loss con un desplazamiento por encima/debajo de la entrada una vez que el precio recorre una distancia configurable.
- **Trailing Stop**: opcionalmente se ajusta detrás del cierre a una distancia fija medida en pasos. El trailing stop tiene prioridad sobre el stop loss original después de activarse.

Los stops y objetivos se evalúan en cada vela terminada, garantizando un comportamiento determinista que coincide entre backtests y ejecución en tiempo real.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `CandleType` | Serie de velas usada para el análisis (marco de 15 minutos por defecto). |
| `TradeVolume` | Volumen de orden aplicado a todas las entradas. |
| `FastMaLength` / `SlowMaLength` | Periodos de las SMA rápida y lenta. |
| `RsiLength` | Longitud de cálculo del RSI. |
| `RsiBullishLevel` / `RsiBearishLevel` | Umbrales RSI que definen zonas de sobreventa y sobrecompra para confirmar divergencias. |
| `MacdShortLength` / `MacdLongLength` / `MacdSignalLength` | Configuración MACD. |
| `StopLossPoints` / `TakeProfitPoints` | Distancias en pasos de precio para riesgo y objetivos de recompensa. |
| `EnableBreakEven`, `BreakEvenTrigger`, `BreakEvenOffset` | Controles para el movimiento a break-even. |
| `EnableTrailing`, `TrailDistance` | Activación y separación del trailing stop. |

Cada parámetro se expone mediante `StrategyParam<T>` para poder optimizarlo dentro del diseñador de StockSharp.

## Notas de uso
1. Adjunte la estrategia a un símbolo de criptomoneda y asegúrese de que el instrumento tenga `PriceStep` y `Board` definidos. Sin paso de precio la estrategia no puede calcular stops.
2. Alinee el tipo de vela con el mercado que opera (por ejemplo, 15m, 1h). La detección de divergencias es sensible al marco temporal.
3. Ajuste las distancias de stop y objetivo a la volatilidad del instrumento. Los pares cripto con cinco decimales suelen requerir recuentos de pasos más grandes.
4. Active break-even o trailing solo después de observar suficiente colchón de beneficio en pruebas históricas; un trailing agresivo puede sacar operaciones demasiado pronto.
5. Supervise la estrategia en el diseñador de StockSharp o el panel de datos de mercado para visualizar la alineación de indicadores y operaciones ejecutadas.

## Diferencias frente a la versión MQL
- El trailing basado en dinero y las protecciones de stop de patrimonio se simplifican en una gestión de stops basada en pasos de precio.
- Las comprobaciones de momentum multimarco se reemplazan por confirmación MACD de marco único para mayor claridad.
- Se omiten efectos secundarios de email/notificación porque se gestionan externamente en los ecosistemas StockSharp.

A pesar de estos ajustes, la detección central de divergencias y la lógica protectora permanecen fieles a la intención del asesor experto original.
