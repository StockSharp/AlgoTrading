# Estrategia de Trading Criteria
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La Estrategia de Trading Criteria es un enfoque de seguimiento de tendencia multi-marco temporal convertido del asesor experto original MQL4 "Trading Criteria". El port se basa en medias móviles lineales ponderadas, filtros de desviación de momentum y confirmaciones MACD extraídas de marcos temporales de tendencia y mensual. Las características de gestión de riesgo incluyen trailing stops, protección de break-even y objetivos de stop-loss/take-profit configurables.

## Lógica de entrada

1. **Marco temporal primario**: Usa una media móvil lineal ponderada (LWMA) rápida y lenta. Las señales largas requieren que la MA rápida se mantenga por encima de la lenta; las cortas requieren lo contrario.
2. **Filtro de momentum**: Calcula la desviación del momentum (|Momentum-100|) en el marco temporal de tendencia y verifica los tres valores más recientes contra umbrales alcistas o bajistas.
3. **Filtro MACD de tendencia**: Evalúa la línea principal del MACD relativa a su línea de señal en el mismo marco temporal de tendencia. Las señales solo se disparan cuando la relación actual se alinea con la barra anterior para evitar cambios rápidos.
4. **Filtro MACD mensual**: Confirma el sesgo direccional mayor usando MACD en un marco temporal mensual (o de usuario especificado lento).
5. **Exposición de posición**: Limita el tamaño máximo de posición neta a `MaxPositions * Volume`. Si aparece una nueva señal mientras se mantiene una posición opuesta, la estrategia primero neutralizará la exposición comprando o vendiendo suficiente volumen.

## Salida y gestión de riesgo

- **Stop Loss / Take Profit**: Definido via `StopLossPoints` y `TakeProfitPoints`, convertido en offsets de precio real usando el tamaño de pip normalizado del instrumento.
- **Trailing stop**: Habilitado con `EnableTrailing` y `TrailingStopPoints`. Para largos, el stop rastrea el precio más alto menos la distancia de trailing una vez que el movimiento supera el umbral; los cortos hacen lo contrario usando el precio más bajo.
- **Movimiento de break-even**: Cuando está habilitado (`EnableBreakEven`), el stop migra al precio de entrada más un offset opcional una vez que el precio de cierre alcanza la distancia `BreakEvenTriggerPoints` a favor de la posición abierta.
- **Salidas protectoras manuales**: Si la vela toca los niveles calculados de stop o objetivo, la estrategia cierra toda la posición neta en esa barra.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Marco temporal base para la generación de señales y medias móviles. |
| `TrendCandleType` | Marco temporal usado para los filtros de momentum y MACD. |
| `MonthlyCandleType` | Marco temporal lento que proporciona confirmación MACD a largo plazo. |
| `FastMaPeriod` / `SlowMaPeriod` | Longitudes de las LWMA rápida y lenta en el marco temporal de entrada. |
| `MomentumPeriod` | Período de lookback de momentum en el marco temporal de tendencia. |
| `MomentumBuyThreshold` / `MomentumSellThreshold` | Desviación mínima de 100 requerida para entradas largas o cortas. |
| `MaxPositions` | Número máximo de lotes base que pueden permanecer abiertos simultáneamente. |
| `StopLossPoints` / `TakeProfitPoints` | Distancias, en puntos, para stops protectores y objetivos de beneficio. |
| `EnableTrailing` / `TrailingStopPoints` | Activa los trailing stops y define su distancia. |
| `EnableBreakEven` | Activa el comportamiento de break-even. |
| `BreakEvenTriggerPoints` / `BreakEvenOffsetPoints` | Controla cuánto debe moverse el precio antes de que el stop se mueva a break-even y qué offset aplicar. |

## Notas de uso

- Adjuntar la estrategia a un instrumento con soporte adecuado de series de velas para los marcos temporales seleccionados.
- Asegurarse de que el instrumento proporcione un `PriceStep` preciso; la implementación ajusta los instrumentos de pips fraccionarios (3 o 5 decimales) para coincidir con las convenciones MQL.
- Las protecciones de trailing y break-even operan en velas completadas. En mercados rápidos, los niveles protectores pueden ejecutarse en la siguiente barra cuando ocurre un gap.
- El conjunto de parámetros predeterminado refleja los inputs MQL publicados, pero pueden optimizarse via los metadatos de parámetros integrados.
