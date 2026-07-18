# Estrategia de Elli
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia de Elli transfiere el MetaTrader 4 asesor experto "Elli" al StockSharp nivel alto API. El robot original combinó la estructura Ichimoku Kinko Hyo en el período de tiempo H1 con un filtro de período de tiempo más bajo ADX y parámetros de riesgo estrictos. La conversión mantiene la misma lógica direccional, reemplaza la gestión manual de pedidos con `StartProtection` y expone cada perilla de ajuste como un `StrategyParam<T>` optimizable para que el comportamiento se pueda adaptar a diferentes mercados.

## Lógica de trading
1. **Ichimoku estructura de tendencia**
   - La estrategia se suscribe al marco de tiempo definido por `CandleType` (H1 por defecto) y calcula los tramos Tenkan-sen, Kijun-sen y Senkou utilizando los períodos originales (19, 60, 120).
   - Una configuración alcista requiere Tenkan > Kijun > Senkou Span A > Senkou Span B con la vela cerca de Kijun. Las configuraciones bajistas reflejan esta condición.
   - La distancia absoluta entre Tenkan y Kijun debe exceder los `TenkanKijunGapPips` pips para evitar nubes planas o de gran alcance.
2. **Confirmación de movimiento direccional**
   - Una segunda suscripción de vela ejecuta el índice direccional promedio en el período de tiempo especificado por `AdxCandleType` (M1 de forma predeterminada).
   - Las señales largas solo se permiten cuando el valor +DI anterior está por debajo de `ConvertLow` y el +DI actual supera `ConvertHigh`. Los cortos requieren la misma relación para el componente −DI, replicando el filtro de aceleración presente en el código MT4.
3. **Ejecución de entrada**
   - Cuando todos los filtros se alinean, la estrategia emite una orden de mercado con volumen `OrderVolume + |Position|`. Esto cierra automáticamente cualquier exposición opuesta antes de unirse a la tendencia.
   - Sólo se mantiene una exposición direccional a la vez, siguiendo la guardia original `OrdersTotal() < 1`.
4. **Gestión de riesgos**
   - `StartProtection` adjunta órdenes de stop loss simétricas y toma de ganancias convertidas a partir de distancias de pips utilizando el tamaño de pip del instrumento.
   - Por lo demás, la posición se gestiona de forma pasiva, permitiendo que las órdenes de protección manejen las salidas como el asesor experto MT4.

## Indicadores y Suscripciones de Datos
- Velas primarias: `CandleType` (velas predeterminadas de 1 hora) para el procesamiento de Ichimoku.
- ADX velas: `AdxCandleType` (velas predeterminadas de 1 minuto) para comprobaciones de aceleración DI.
- Indicadores: `Ichimoku` (Tenkan, Kijun, Senkou Span B) y `AverageDirectionalIndex` (que proporcionan +DI/−DI).
- Ambas suscripciones admiten la representación de gráficos a través de `DrawCandles`, `DrawIndicator` y `DrawOwnTrades` si hay un área de gráfico disponible.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `OrderVolume` | `1` | Volumen de orden de mercado base. |
| `TakeProfitPips` | `60` | Distancia de toma de ganancias expresada en pips. |
| `StopLossPips` | `30` | Distancia de stop-loss expresada en pips. |
| `TenkanPeriod` | `19` | Período Tenkan-sen para el indicador Ichimoku. |
| `KijunPeriod` | `60` | Período Kijun-sen para el indicador Ichimoku. |
| `SenkouSpanBPeriod` | `120` | Período Senkou Span B para la nube Ichimoku. |
| `TenkanKijunGapPips` | `20` | Distancia mínima Tenkan/Kijun (en pips) requerida antes de operar. |
| `ConvertHigh` | `13` | Umbral DI que el valor actual debe superar para confirmar el impulso. |
| `ConvertLow` | `6` | Umbral DI por debajo del cual el valor anterior debe permanecer antes de realizar una nueva operación. |
| `AdxPeriod` | `10` | Período utilizado para el cálculo ADX. |
| `CandleType` | `H1` | Periodo de tiempo que impulsa el cálculo de Ichimoku. |
| `AdxCandleType` | `M1` | Plazo utilizado para ADX y seguimiento de DI. |

Todos los parámetros se implementan con `StrategyParam<T>` ayudantes, lo que permite la optimización y ajustes de tiempo de ejecución dentro de StockSharp Designer.

## Notas de implementación
- La conversión de pips sigue la convención estándar de Forex (0,0001 para cotizaciones de 5 dígitos y 0,01 para instrumentos de 3 dígitos) para preservar los umbrales originales basados en pips.
- Los valores ADX se almacenan en caché en `_latestPlusDi`, `_previousPlusDi`, `_latestMinusDi` y `_previousMinusDi`, lo que garantiza que la verificación de aceleración DI coincida con las llamadas MQL `iADX` con los turnos 0 y 1.
- `IsFormedAndOnlineAndAllowTrading()` bloquea las señales hasta que la estrategia, los indicadores y las fuentes de datos estén listos, evitando operaciones prematuras durante el calentamiento.
- Las entradas al mercado dependen de `Volume + Math.Abs(Position)`, de modo que los cambios de dirección aplanan instantáneamente las operaciones existentes, emulando el comportamiento de posición única del script MT4.
