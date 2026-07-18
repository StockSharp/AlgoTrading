# Estrategia SVOS EURJPY D1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una conversión de C# del MetaTrader 4 asesor experto **SVOS_EURJPY_D1**. Opera con velas diarias para EURJPY y
Combina un clasificador de régimen con reconocimiento de patrones y filtros indicadores. El filtro vertical horizontal (VHF) distingue
entre estados de mercado de tendencia y de rango. Cuando el mercado tiene tendencia, la estrategia se basa en la pendiente del histograma MACD (OSMA),
mientras que en condiciones de rango vuelve al oscilador Stochastic. Patrones de velas japonesas como barras envolventes y
Las estrellas de la mañana y de la tarde se utilizan para cerrar posiciones agresivamente contra la acción desfavorable del precio.

## Lógica comercial
- **Detección de régimen**: el valor VHF del día anterior se compara con `VhfThreshold`. Los valores por encima del umbral activan el
bloque de seguimiento de tendencia; de lo contrario, se utiliza el bloque de rango.
- **Confirmación de tendencia**: dos EMA (5 y 20 períodos) se comparan con un EMA lenta (130 períodos, que coincide con el filtro de seis meses de
el EA original) para escalar los tamaños de posición. En tendencias alcistas, el volumen de compra se multiplica por `RiskBoost`; en tendencias bajistas el volumen de ventas es
multiplicado.
- **Filtros de indicador**:
  - Régimen de tendencia: ir en largo cuando OSMA sea positivo y alcista (`OSMA[1] > 0` y `OSMA[1] > OSMA[2]`). Vaya en corto cuando OSMA sea negativo
y cayendo.
  - Régimen de rango: vaya en largo cuando la línea principal Stochastic cruce por encima de su señal, vaya en corto cuando cruce por debajo.
  - Protección de volatilidad: la desviación estándar anterior debe exceder `StdDevMinimum` antes de que se acepte cualquier señal.
- **Filtros de acción del precio**: la vela completada más reciente no debe formar un doji (relación `DojiDivisor`) y debe confirmar la
dirección (alcista para largos, bajista para cortos). Los patrones envolventes o estelares opuestos desencadenan la liquidación inmediata del
lado respectivo.
- **Límites de posición**: el número total de órdenes abiertas está limitado por `MaxTrendOrders` en los mercados de tendencia y por `MaxRangeOrders`
en distintos mercados.
- **Gestión de riesgos**: cada orden tiene niveles fijos de límite de pérdidas y obtención de ganancias (`StopLossPips`, `TakeProfitPips`). un rastro
el stop se activa cuando el beneficio flotante supera `TrailingStopPips`; se recalcula utilizando los extremos de las velas para imitar el
MetaTrader comportamiento.

## Uso del indicador
- **Promedio móvil exponencial (5, 20, 130)**: se utiliza para confirmar la dirección y escalar el volumen.
- **Filtro horizontal vertical**: indicador personalizado que mide la relación entre el movimiento neto y el cierre acumulado
cambios para detectar tendencias versus rangos.
- **MACD (OSMA)**: la diferencia entre MACD y su línea de señal impulsa las entradas y salidas de tendencia.
- **Stochastic Oscilador**: los valores %K y %D proporcionan señales de reversión a la media para mercados de rango.
- **Desviación estándar**: garantiza que la volatilidad sea lo suficientemente alta antes de permitir nuevas operaciones.

## Gestión de pedidos
- Las órdenes se ejecutan con `BuyMarket`/`SellMarket` y se almacenan internamente para que se puedan simular paradas y objetivos individuales en
Entorno de compensación de StockSharp.
- Cuando se tocan los niveles de stop-loss o take-profit dentro del rango de la vela, se cierra la parte correspondiente de la posición.
- El trailing stop sigue el máximo de la vela (para largos) o el mínimo (para cortos) manteniendo la distancia configurada.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `LotSize` | Tamaño base del pedido expresado en lotes. | `0.1` |
| `RiskBoost` | Multiplicador aplicado al tamaño del lote cuando el filtro de tendencia EMA está alineado. | `3` |
| `TakeProfitPips` | Distancia de toma de ganancias en pips. | `350` |
| `StopLossPips` | Distancia de stop-loss en pips. | `90` |
| `TrailingStopPips` | Distancia del trailing-stop en pips (siempre activo). | `150` |
| `StochKPeriod` | %K longitud del oscilador Stochastic. | `8` |
| `StochDPeriod` | %D longitud del oscilador Stochastic. | `3` |
| `StochSlowing` | Factor de suavizado aplicado a %K. | `3` |
| `StdDevPeriod` | Ventana retrospectiva para el filtro de desviación estándar. | `20` |
| `StdDevMinimum` | Se requiere una desviación estándar mínima antes de que se puedan abrir nuevas operaciones. | `0.3` |
| `VhfPeriod` | Longitud del filtro horizontal vertical. | `20` |
| `VhfThreshold` | Umbral del régimen; los valores más altos denotan mercados en tendencia. | `0.4` |
| `MaxTrendOrders` | Número máximo de órdenes abiertas simultáneamente durante las tendencias. | `4` |
| `MaxRangeOrders` | Número máximo de órdenes abiertas simultáneamente durante rangos. | `2` |
| `MacdFastLength` | Longitud rápida de EMA dentro de MACD. | `10` |
| `MacdSlowLength` | Longitud lenta de EMA dentro de MACD. | `25` |
| `MacdSignalLength` | Longitud de la señal EMA para MACD. | `5` |
| `DojiDivisor` | Proporción utilizada para marcar velas doji (cuerpo más pequeño que el rango/divisor). | `8.5` |
| `CandleType` | Tipo de vela utilizada para el análisis (diaria por defecto). | `1 day` |
| `PipSizeOverride` | Anulación opcional del tamaño de pipa; `0` habilita la detección automática desde `Security.PriceStep`. | `0` |

## Notas de implementación
- El EA original hacía referencia a un EMA de seis meses de un período de tiempo mensual. El puerto calcula un EMA de 130 períodos en los cierres diarios a
reproducir el mismo suavizado manteniendo una única suscripción de datos.
- Las paradas, los objetivos y la lógica de seguimiento se reproducen dentro de la estrategia porque StockSharp neta posiciones de forma predeterminada. Cada entrada es
rastreado individualmente para respetar el comportamiento MetaTrader.
- Las actualizaciones de trailing stop utilizan máximos y mínimos de velas para aproximar los movimientos de precios intradía. Los resultados pueden diferir ligeramente de los basados en garrapatas.
detrás en MetaTrader cuando se producen grandes reversiones intradía.
- El tamaño del pip se calcula a partir de `Security.PriceStep`; utilice `PipSizeOverride` si el corredor utiliza un paso no estándar para pares JPY.

## Uso
1. Adjunte la estrategia a los datos diarios del EURJPY o actualice `CandleType` si desea otro período de tiempo.
2. Verifique que el tamaño del pip se detecte correctamente; ajuste `PipSizeOverride` si es necesario.
3. Configure los parámetros de administración de dinero (`LotSize`, `RiskBoost`) para que coincidan con las restricciones de la cuenta.
4. Ejecute la estrategia en StockSharp Designer o API Runner para validar el comportamiento antes de operar en vivo.
