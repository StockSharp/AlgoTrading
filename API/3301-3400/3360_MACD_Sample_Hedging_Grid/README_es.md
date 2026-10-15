# MACD Ejemplo de estrategia de cuadrícula de cobertura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una versión StockSharp del asesor experto MetaTrader "MACD Sample Hedging Grid". Combina un cruce MACD a corto plazo, un filtro de pendiente local EMA y confirmaciones de períodos de tiempo más altos. Cuando las condiciones se alinean, la estrategia construye una cuadrícula de posiciones en la dirección detectada, escalando el tamaño de la operación en un exponente configurable.

## Lógica del mercado
- **Período de tiempo base:** configurable (velas de 5 minutos por defecto).
- **Filtro de tendencias:** un EMA (26 períodos predeterminados) debe inclinarse hacia arriba para operaciones largas o hacia abajo para operaciones cortas.
- **MACD activador:** la línea rápida MACD debe cruzar la línea de señal en el período de tiempo base mientras excede un valor absoluto mínimo (expresado en pasos de precio).
- **Confirmación de impulso:** la distancia absoluta entre el impulso y el nivel neutral 100 en un período de tiempo más alto debe exceder umbrales separados para posiciones largas y cortas. Se inspeccionan las últimas tres velas de período de tiempo superior, replicando el comportamiento original EA.
- **Confirmación a largo plazo:** un MACD calculado en un período de tiempo largo (mensual de forma predeterminada) debe coincidir con la dirección comercial (MACD arriba de la señal para entornos alcistas, abajo para entornos bajistas).

Una vez que se activa una señal, la estrategia inicia una nueva cuadrícula en esa dirección o se suma a la cuadrícula existente siempre que no se haya alcanzado el número máximo de entradas.

## Gestión de Puestos
- **Tamaño de la cuadrícula:** cada entrada adicional multiplica el volumen inicial por `LotExponent` (predeterminado 1,44). El tamaño de la posición se restablece cuando cambia la dirección o se cierra la posición.
- **Controles de riesgo:** las distancias opcionales de toma de ganancias y límite de pérdidas se traducen en StockSharp órdenes de protección en incrementos de precios.
- **Cambio de dirección:** cada vez que llega una señal opuesta, la exposición actual se aplana antes de abrir la cuadrícula en la nueva dirección.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `CandleType` | Periodo principal utilizado para los cálculos MACD y EMA. | marco de tiempo de 5 minutos |
| `MomentumCandleType` | Un plazo más alto alimenta la confirmación del impulso. | plazo de 30 minutos |
| `TrendCandleType` | Período de tiempo largo utilizado para el filtro de tendencia MACD. | plazo de 30 días |
| `FastMaPeriod` | Longitud rápida de EMA dentro de MACD. | 12 |
| `SlowMaPeriod` | Longitud lenta de EMA dentro de MACD. | 26 |
| `SignalPeriod` | Longitud de la señal SMA para MACD. | 9 |
| `TrendMaPeriod` | EMA longitud para el filtro de tendencia local. | 26 |
| `MomentumPeriod` | Longitud del indicador de impulso (período de tiempo más alto). | 14 |
| `MacdOpenLevel` | Nivel mínimo absoluto de MACD (en pasos de precio) requerido para una operación. | 3 |
| `MomentumBuyThreshold` | Distancia mínima de impulso absoluto desde 100 para posiciones largas. | 0.3 |
| `MomentumSellThreshold` | Distancia mínima de impulso absoluto desde 100 para cortos. | 0.3 |
| `MaxTrades` | Número máximo de entradas de cuadrícula por dirección. | 10 |
| `LotExponent` | Multiplicador utilizado para cada entrada adicional de la cuadrícula. | 1.44 |
| `StopLossSteps` | Distancia de stop-loss medida en pasos de precio. | 20 |
| `TakeProfitSteps` | Distancia de obtención de beneficios medida en incrementos de precios. | 50 |

## Notas
- El EA original también contenía seguimiento basado en dinero, movimientos de equilibrio y paradas de capital de la cuenta. Estas características requieren datos de cartera específicos del corredor y gestión manual de órdenes; no se implementan en esta conversión StockSharp de alto nivel.
- Las suscripciones de velas, las vinculaciones de indicadores y la ejecución comercial siguen el uso de alto nivel recomendado por API.
- Asegúrese de que los instrumentos seleccionados admitan los tipos de velas configurados y que los datos históricos estén disponibles para todos los períodos de tiempo referenciados.
