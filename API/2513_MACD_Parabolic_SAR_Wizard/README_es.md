# Estrategia Asistente MACD Parabolic SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
Esta estrategia es una conversión de StockSharp del Asesor Experto de MetaTrader generado por el Asistente MQL5 que combina el impulso MACD con la dirección de tendencia del Parabolic SAR. La lógica reproduce el mecanismo de puntuación del asistente asignando una puntuación normalizada (0..100) a cada indicador y luego ponderando las contribuciones antes de tomar decisiones de trading.

## Lógica de Trading
- **Indicadores**
  - *MACD (12, 24, 9)*: el signo del histograma define si el impulso alcista (histograma > 0) o el impulso bajista (histograma < 0) está activo.
  - *Parabolic SAR (0.02, 0.2)*: el precio de cierre por encima del punto SAR se interpreta como una tendencia alcista, y por debajo del punto SAR como una tendencia bajista.
- **Construcción de puntuación**
  - MACD produce 100 (alcista) o 0 (bajista) puntos para el lado largo. Los valores inversos se usan para el lado corto.
  - El Parabolic SAR se comporta igual, proporcionando 100 puntos cuando la tendencia concuerda con la dirección respectiva.
  - Ambas puntuaciones se combinan mediante los pesos definidos por el usuario (`MacdWeight` y `SarWeight`). Con los pesos predeterminados (0.9 y 0.1), el MACD domina la decisión final igual que en la plantilla del asistente.
- **Reglas de entrada**
  - Calcular la puntuación alcista: `bullScore = macdBull * MacdWeight + sarBull * SarWeight`.
  - Calcular la puntuación bajista: `bearScore = macdBear * MacdWeight + sarBear * SarWeight`.
  - Abrir una posición larga (o invertir desde corto) cuando `bullScore >= OpenThreshold` (predeterminado `20`).
  - Abrir una posición corta (o invertir desde largo) cuando `bearScore >= OpenThreshold`.
- **Reglas de salida**
  - Las posiciones largas se cierran cuando la puntuación bajista alcanza el nivel de confirmación fuerte `CloseThreshold` (predeterminado `100`).
  - Las posiciones cortas se cierran cuando la puntuación alcista alcanza `CloseThreshold`.
  - Las señales de salida se evalúan antes que las señales de entrada para imitar el comportamiento del asesor experto original que prioriza el cierre de operaciones conflictivas.

## Gestión de Riesgos
- `StopLossPoints` y `TakeProfitPoints` replican la gestión del dinero basada en puntos del asistente. Ambos valores se convierten a unidades de precio usando el `PriceStep` del instrumento y luego se pasan a `StartProtection`.
- Establezca cualquier parámetro en `0` para deshabilitar la orden de protección correspondiente.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `MacdFastPeriod` | Período de EMA rápida para MACD. | 12 |
| `MacdSlowPeriod` | Período de EMA lenta para MACD. | 24 |
| `MacdSignalPeriod` | Período de SMA de señal para MACD. | 9 |
| `MacdWeight` | Peso de la puntuación MACD (0..1). | 0.9 |
| `SarWeight` | Peso de la puntuación del Parabolic SAR (0..1). | 0.1 |
| `OpenThreshold` | Puntuación mínima para abrir/invertir posiciones. | 20 |
| `CloseThreshold` | Puntuación opuesta mínima para salir de posiciones. | 100 |
| `SarStep` | Paso de aceleración del Parabolic SAR. | 0.02 |
| `SarMax` | Aceleración máxima del Parabolic SAR. | 0.2 |
| `StopLossPoints` | Distancia del stop-loss en puntos de precio. | 50 |
| `TakeProfitPoints` | Distancia del take-profit en puntos de precio. | 115 |
| `CandleType` | Fuente de datos de velas para cálculos de indicadores. | Marco temporal de 15 minutos |

## Notas de Uso
- Los parámetros predeterminados reflejan la plantilla `.mq5`, por lo que la estrategia se comporta de manera consistente con el asesor experto original generado por el asistente.
- Ajuste `MacdWeight`, `SarWeight` y los umbrales para cambiar la sensibilidad de las entradas y salidas. Por ejemplo, aumentar `OpenThreshold` requerirá una confirmación más fuerte antes de abrir nuevas operaciones.
- Los campos internos `_lastBullScore` y `_lastBearScore` se actualizan en cada barra y pueden registrarse o exponerse si necesita monitorear cómo evoluciona la puntuación combinada a lo largo del tiempo.
- Debido a que la estrategia depende de velas terminadas, asegúrese de que su feed de datos proporcione actualizaciones completas de velas para el `CandleType` seleccionado.
- La gestión del dinero se expresa en puntos; asegúrese de que el instrumento elegido use el paso de precio esperado para que las órdenes de protección se alineen con las distancias previstas.
