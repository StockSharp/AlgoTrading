# Estrategia de Candle Trailing Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Candle Trailing Stop** es un puerto de StockSharp del experto asesor MetaTrader con el mismo nombre. El robot original combinaba filtros de tendencia multitemporal, confirmación de momentum y un motor de trailing stop agresivo que seguía los mínimos y máximos de las velas recientes. La versión C# mantiene el mismo flujo de trabajo pero se apoya en componentes de alto nivel de StockSharp y expone todas las configuraciones críticas como parámetros de estrategia.

## Lógica principal

1. **Suscripciones de datos**
   - El marco temporal de trading impulsa las entradas y las actualizaciones del trailing stop.
   - Un marco temporal superior proporciona confirmación usando medias móviles linealmente ponderadas (LWMA) y un indicador de momentum.
   - Una tercera suscripción calcula una línea MACD en un marco temporal lento (mensual por defecto) para filtrar las operaciones.
2. **Alineación de tendencia**
   - Las operaciones solo se permiten cuando las secuencias de LWMA rápida, media y lenta están alineadas en ambos marcos temporales (secuencia alcista para largos, bajista para cortos).
3. **Puerta de momentum**
   - El indicador de momentum debe estar cerca del valor neutral de 100 en al menos una de las últimas tres barras del marco temporal superior.
4. **Confirmación de MACD**
   - Los largos requieren que la línea MACD esté por encima de la línea de señal; los cortos requieren la relación inversa.
5. **Disparador de entrada**
   - Una ruptura a través de la LWMA rápida en el marco temporal actual (vela cerrando por encima/debajo de la media tras tocarla en la barra anterior) inicia nuevas operaciones respetando un límite de posición configurable.
6. **Gestión de riesgo y salida**
   - Las distancias iniciales de stop-loss y take-profit se definen en pips y se convierten automáticamente a pasos de precio.
   - Los stops pueden migrar al punto de equilibrio, seguir el extremo de las velas recientes, o retroceder a un trailing clásico de distancia fija.
   - Las funciones opcionales basadas en capital replican el EA original: take profit monetario, take profit porcentual, trailing de capital y protección contra drawdown.

## Parámetros

| Grupo | Nombre | Descripción | Por defecto |
|--------------|-------------------------|---------------------------------------------------------------------------------------------|---------|
| Negociación | `Volume` | Tamaño de la orden en lotes/contratos. | `1` |
| | `MaxTrades` | Exposición máxima agregada expresada como `Volume * MaxTrades`. | `10` |
| Indicadores | `FastCurrentLength` | LWMA rápida en el marco temporal de trading. | `9` |
| | `MiddleCurrentLength` | LWMA media en el marco temporal de trading. | `20` |
| | `SlowCurrentLength` | LWMA lenta en el marco temporal de trading. | `52` |
| | `FastHigherLength` | LWMA rápida en el marco temporal superior. | `9` |
| | `MiddleHigherLength` | LWMA media en el marco temporal superior. | `20` |
| | `SlowHigherLength` | LWMA lenta en el marco temporal superior. | `52` |
| | `MomentumPeriod` | Período de momentum en el marco temporal superior. | `14` |
| | `MomentumBuyThreshold` | Desviación máxima desde 100 permitida para operaciones largas. | `0.3` |
| | `MomentumSellThreshold` | Desviación máxima desde 100 permitida para operaciones cortas. | `0.3` |
| | `MacdFastLength` | Longitud de la EMA rápida para confirmación MACD. | `12` |
| | `MacdSlowLength` | Longitud de la EMA lenta para confirmación MACD. | `26` |
| | `MacdSignalLength` | Longitud de la EMA de señal para confirmación MACD. | `9` |
| Riesgo | `StopLossPips` | Distancia del stop-loss en pips. | `20` |
| | `TakeProfitPips` | Distancia del take-profit en pips. | `50` |
| | `UseMoveToBreakEven` | Activa la lógica de punto de equilibrio. | `true` |
| | `BreakEvenTriggerPips` | Beneficio en pips requerido antes de mover el stop. | `30` |
| | `BreakEvenOffsetPips` | Offset añadido al desplazar el stop al punto de equilibrio. | `30` |
| | `UseCandleTrail` | Elegir entre trailing basado en velas (`true`) o trailing clásico (`false`). | `true` |
| | `CandleTrailLength` | Número de velas cerradas usadas para calcular los extremos de trailing. | `3` |
| | `PadAmountPips` | Buffer extra añadido debajo/encima del extremo de trailing. | `10` |
| | `TrailTriggerPips` | Beneficio requerido antes de que el trailing clásico se active. | `40` |
| | `TrailAmountPips` | Distancia mantenida por el trailing clásico. | `40` |
| Reglas de capital | `UseMoneyTakeProfit` | Cerrar todas las posiciones cuando el beneficio flotante supere el objetivo monetario. | `false` |
| | `MoneyTakeProfit` | Objetivo de beneficio monetario. | `40` |
| | `UsePercentTakeProfit` | Cerrar todas las posiciones cuando el beneficio flotante supere el objetivo porcentual. | `false` |
| | `PercentTakeProfit` | Porcentaje del capital inicial usado como objetivo de beneficio. | `10` |
| | `EnableMoneyTrailing` | Activa el trailing del beneficio flotante tras un umbral. | `true` |
| | `MoneyTrailTarget` | Nivel de beneficio que activa la lógica de trailing monetario. | `40` |
| | `MoneyTrailStop` | Retroceso máximo permitido una vez alcanzado el objetivo. | `10` |
| | `UseEquityStop` | Activa la protección contra drawdown de capital. | `true` |
| | `EquityRiskPercent` | Drawdown máximo desde el pico de capital antes de forzar posición plana. | `1` |
| Datos | `CurrentCandleType` | Marco temporal de trading. | `5m` |
| | `HigherCandleType` | Marco temporal superior usado para filtros. | `30m` |
| | `MacdCandleType` | Marco temporal para confirmación MACD (mensual por defecto). | `30d` |

## Notas y suposiciones

- Los pips se convierten a pasos de precio usando el tamaño de tick del instrumento. En símbolos donde un pip difiere de un tick puede ser necesario ajustar las distancias de pip predeterminadas.
- Las funciones monetarias se basan en el beneficio no realizado aproximado como `(cierre - precioPromedio) * posición`. Los ajustes de swap y comisión no están simulados.
- La estrategia usa órdenes de mercado para entradas y salidas. Las órdenes iniciales de take-profit se registran una vez que se abre una operación, mientras que la gestión del stop-loss se maneja internamente y se cierra mediante órdenes de mercado cuando se cruza el nivel calculado.
- Todos los comentarios en el código están escritos en inglés según las directrices del proyecto.
