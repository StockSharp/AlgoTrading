# 4218 RSI Estrategia MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un puerto C# del asesor experto original MetaTrader ubicado en `MQL/9925`. Recrea el oscilador de impulso RSI_MA combinando un RSI clásico con la pendiente de una media móvil exponencial construida sobre el precio ponderado `(High + Low + 2 * Close) / 4`. Las señales se generan únicamente en velas completadas, manteniendo el comportamiento idéntico a la implementación fuente.

El script está diseñado para velas EURUSD diarias (período de tiempo D1) y abre una única posición a la vez. Sin embargo, se puede utilizar cualquier instrumento con un incremento de precio significativo siempre que el tipo de vela esté configurado en consecuencia.

## Lógica estratégica
1. **Cálculo del indicador**
   - Se calcula un índice de fuerza relativa con longitud configurable sobre los precios de cierre.
   - Se calcula una media móvil exponencial con la misma longitud sobre el precio ponderado.
   - El valor del indicador es igual a `RSI * (EMA(current) - EMA(previous)) / pipSize` y se recorta al rango `[1, 99]`.
2. **Entrada larga**
   - Valor del indicador anterior por debajo del extremo de sobreventa (predeterminado 5).
   - Último valor del indicador por encima del umbral de activación de sobreventa (predeterminado 20).
   - Ninguna posición abierta o una posición corta existente (la corta se cierra antes de abrir una nueva larga).
3. **Entrada corta**
   - Valor del indicador anterior por encima del extremo de sobrecompra (predeterminado 95).
   - Último valor del indicador por debajo del umbral de activación de sobrecompra (predeterminado 80).
   - Ninguna posición abierta o una posición larga existente (la posición larga se cierra antes de abrir una nueva corta).
4. **Salida basada en indicadores**
   - Las posiciones largas se cierran cuando el indicador cae desde arriba del extremo de sobrecompra hasta debajo del nivel de activación (95 → 80 por defecto).
   - Las posiciones cortas se cierran cuando el indicador sube desde debajo del extremo de sobreventa hasta por encima del nivel de activación (5 → 20 por defecto).
5. **Salidas de protección**
   - Las distancias opcionales de stop-loss, take-profit y trailing stop se expresan en pips. Las distancias se convierten automáticamente en precio utilizando la seguridad `PriceStep` (respaldo 0,0001).
   - El ajuste del trailing stop sigue el comportamiento del EA original: se activa solo después de que el precio se mueve más que la distancia configurada en la dirección favorable.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `RsiPeriod` | RSI y EMA longitud.|
| `OversoldActivationLevel` | Umbral que confirma una configuración larga después de un extremo de sobreventa. |
| `OversoldExtremeLevel` | Extremo al que hay que llegar antes de que se permitan largos. |
| `OverboughtActivationLevel` | Umbral que confirma una configuración corta después de un extremo de sobrecompra. |
| `OverboughtExtremeLevel` | Extremo que se debe alcanzar antes de que se permitan los pantalones cortos. |
| `StopLossPips` | Distancia para el stop-loss de protección. Activar/desactivar mediante `UseStopLoss`. |
| `TakeProfitPips` | Distancia para el objetivo de ganancias. Activar/desactivar mediante `UseTakeProfit`. |
| `TrailingStopPips` | Distancia para el trailing stop. Activar/desactivar mediante `UseTrailingStop`. |
| `UseStopLoss` | Activa la gestión de stop-loss. |
| `UseTakeProfit` | Activa la gestión del take-profit. |
| `UseTrailingStop` | Activa las actualizaciones de trailing stop. |
| `UseMoneyManagement` | Habilita el tamaño de posición basado en `RiskPercent`. |
| `RiskPercent` | Porcentaje de cartera arriesgado por operación cuando la gestión del dinero está activa. |
| `TradeVolume` | Volumen fijo utilizado cuando la administración del dinero está deshabilitada. |
| `CandleType` | Tipo de datos de velas procesadas por la estrategia (por defecto Diaria). |

## Notas de uso
- Adjunte la estrategia a las velas diarias EURUSD para reproducir el comportamiento del EA original. Se admiten otros instrumentos/plazos de tiempo después de ajustar `CandleType` y umbrales.
- Sólo se mantiene abierta una posición en cualquier momento. Al ingresar una nueva operación, se cierra automáticamente primero la dirección opuesta.
- La administración del dinero recurre al `TradeVolume` fijo cuando la información de la cartera no está disponible o el volumen calculado deja de ser positivo.
- Asegúrese de que el valor `PriceStep` refleje un pip (0,0001 para la mayoría de los pares de divisas). De lo contrario, ajuste los parámetros en consecuencia.

## Gestión de riesgos
- Los niveles de stop-loss y take-profit se evalúan en cada vela completa utilizando rangos máximos/bajos de velas.
- El trailing stop se actualiza solo cuando la operación genera ganancias por más de la distancia configurada y nunca se mueve en una dirección desfavorable.
- Las salidas basadas en indicadores siguen funcionando incluso cuando los controles de riesgo están deshabilitados, lo que garantiza una degradación elegante similar a la versión MQL.
