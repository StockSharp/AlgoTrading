# Estrategia MTF suavizada de Heiken Ashi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Heiken Ashi Smoothed MTF es una adaptación del asesor experto "HASNEWJ" MetaTrader. Reconstruye el indicador Heiken Ashi suavizado personalizado en seis marcos de tiempo (M1, M5, M15, M30, H1, H4) y espera la alineación de la tendencia en los marcos superiores. Se abre una operación cuando la corriente inferior de M5 muestra un nuevo retroceso mientras que las velas suavizadas a largo plazo siguen siendo fuertemente alcistas o bajistas. La lógica manual de stop-loss y take-profit replica el comportamiento del EA original, incluida la capacidad de ampliar ligeramente el stop después de una operación perdedora.

## Indicadores y datos
- **Velas Heiken Ashi suavizadas** en M1, M5, M15, M30, H1 y H4.
  - La primera pasada de suavizado aplica un método/longitud de promedio móvil configurable a los valores OHLC sin procesar.
  - La segunda pasada suaviza la apertura/cierre provisional de Heiken Ashi con otra media móvil configurable.
- **Contadores direccionales** que rastrean cuántas actualizaciones de un minuto cada período de tiempo se ha mantenido alcista o bajista.
- **Precio de cierre bruto** de la serie M1 para comprobaciones de gestión de riesgos.

## Lógica de entrada
1. Actualice la dirección Heiken Ashi suavizada para cada período de tiempo cada vez que finalice una vela.
2. En cada vela M1 terminada, incremente o reinicie los contadores alcistas/bajistas dependiendo de la última dirección de cada período de tiempo.
3. **Condiciones de compra:**
   - M5 suavizado Heiken Ashi es alcista y el contador alcista está por debajo de `MaxM5TrendLength` (10 actualizaciones predeterminadas).
   - El Heiken Ashi suavizado M15 es alcista y su contador alcista está por encima de `MinM15TrendLength` (200 actualizaciones predeterminadas).
   - Las velas Heiken Ashi suavizadas M30, H1 y H4 también son alcistas.
   - Actualmente no hay ninguna posición larga abierta (se permite la exposición corta y se invertirá).
4. **Condiciones de venta:**
   - M5 suavizado Heiken Ashi es bajista y el contador bajista está por debajo de `MaxM5TrendLength`.
   - M15 suavizado Heiken Ashi es bajista y su contador bajista está por encima de `MinM15TrendLength`.
   - Las velas suavizadas M30, H1 y H4 son bajistas.
   - Actualmente no hay ninguna posición corta abierta (la exposición larga está cerrada o revertida).
5. El volumen de la orden de mercado es igual a `TradeVolume` más el valor absoluto de la exposición opuesta para garantizar que los cambios cierren la operación anterior.

## Gestión del riesgo
- Se evalúan manualmente un stop-loss y una toma de ganancias en cada vela M1 terminada usando `Security.PriceStep`.
- La toma de ganancias cierra la posición una vez que el precio se mueve `TakeProfitPoints` pasos a favor de la operación.
- El stop-loss cierra la posición una vez que el precio se mueve `StopLossPoints` pasos en contra de la operación.
- Después de una operación perdedora, la siguiente entrada amplía el límite de pérdidas en `ExtraStopLossPoints` pasos, imitando la bandera de "fallo" de EA.
- El volumen comercial está fijado por `TradeVolume`; no se aplica ninguna lógica piramidal o de escalamiento más allá de revertir la exposición existente.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `TradeVolume` | Volumen de pedido base utilizado para las entradas | `0.1` |
| `TakeProfitPoints` | Distancia de toma de ganancias en pasos de precios | `20` |
| `StopLossPoints` | Distancia de stop-loss en pasos de precio | `500` |
| `ExtraStopLossPoints` | Se aplican pasos de parada adicionales después de una operación perdedora | `5` |
| `FirstMaPeriod` | Longitud de la primera media móvil de suavizado | `6` |
| `FirstMaMethod` | Método del primer MA de suavizado (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`) | `Smoothed` |
| `SecondMaPeriod` | Longitud de la segunda media móvil de suavizado | `2` |
| `SecondMaMethod` | Método del segundo suavizado MA. | `LinearWeighted` |
| `MaxM5TrendLength` | Número máximo de actualizaciones de M5 permitidas antes de cancelar una entrada de retroceso | `10` |
| `MinM15TrendLength` | Número mínimo de actualizaciones de M15 necesarias para confirmar la tendencia más alta | `200` |
| `M1CandleType` | Tipo de datos para el flujo de velas base de un minuto | `TimeFrame(00:01:00)` |
| `M5CandleType` | Tipo de datos para el flujo de confirmación de cinco minutos | `TimeFrame(00:05:00)` |
| `M15CandleType` | Tipo de datos para el flujo de confirmación de quince minutos | `TimeFrame(00:15:00)` |
| `M30CandleType` | Tipo de datos para el flujo de confirmación de treinta minutos | `TimeFrame(00:30:00)` |
| `H1CandleType` | Tipo de datos para el flujo de confirmación por hora | `TimeFrame(01:00:00)` |
| `H4CandleType` | Tipo de datos para el flujo de confirmación de cuatro horas | `TimeFrame(04:00:00)` |

## Notas de uso
- Los contadores direccionales se actualizan una vez por vela M1 terminada, lo que se aproxima a los contadores basados en ticks de MetaTrader mientras se mantiene la implementación impulsada por velas.
- Asegúrese de que `Security.PriceStep` esté configurado; de lo contrario, la estrategia vuelve a caer a un paso de 0,0001 al calcular los niveles objetivo y de parada.
- Ambos pases de suavizado se basan en promedios móviles; experimentar con diferentes combinaciones de métodos y períodos puede adaptar el sistema a instrumentos con diferentes perfiles de volatilidad.
