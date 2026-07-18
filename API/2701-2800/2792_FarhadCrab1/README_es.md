# Estrategia FarhadCrab1 (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia FarhadCrab1 es un sistema de seguimiento de tendencia que entra en operaciones en retrocesos hacia una media móvil exponencial (EMA) y gestiona las salidas usando stops fijos, take-profits, un trailing stop inspirado en el Parabolic SAR y un filtro de marco temporal superior. El asesor experto original de MetaTrader 5 se basa en velas horarias para la ejecución mientras hace referencia a datos diarios para decidir cuándo cerrar posiciones abiertas. Este port en C# mantiene la misma lógica central combinando un filtro EMA de marco temporal de trabajo con una regla de salida por cruce de EMA diaria.

## Conceptos principales
- **Filtro de tendencia:** Una EMA calculada en el marco temporal de trabajo (por defecto EMA de 15 períodos en velas de 1 hora). Solo se permiten señales largas cuando el mínimo de la vela anterior permanece por encima de la EMA, y solo se permiten señales cortas cuando el máximo de la vela anterior se mantiene por debajo de la EMA.
- **Filtro diario:** Una EMA separada calculada en velas diarias. Cuando la EMA diaria cruza por encima del cierre diario, se cierran todas las posiciones largas. Cuando cruza por debajo, se cierran todas las posiciones cortas. Esto imita la lógica original `ClosePositions` del código MQL5.
- **Controles de riesgo:** Los niveles de stop-loss y take-profit fijos se derivan de distancias en pips. Un trailing stop mueve el stop de protección una vez que la posición gana suficiente beneficio, emulando la función de trailing de MT5 que combina los ajustes `TrailingStop` y `TrailingStep`.
- **Gestión de posición única:** La estrategia opera con una única posición neta. Entrar en una posición larga mientras se mantiene una corta (o viceversa) primero cierra la exposición opuesta antes de abrir la nueva operación.

## Reglas de trading
1. **Detección de señal (marco temporal de trabajo):**
   - Entrada larga cuando el mínimo de la vela anterior es mayor que el valor de la EMA (después de aplicar el desplazamiento configurado).
   - Entrada corta cuando el máximo de la vela anterior es menor que el valor de la EMA.
2. **Dimensionamiento de posición:** El parámetro `Volume` establece el tamaño base de la orden. Al revertir de corto a largo (o viceversa), el motor envía automáticamente la cantidad adicional requerida para voltear la posición neta.
3. **Stop-loss y take-profit:**
   - Las distancias se definen en pips. El tamaño del pip se adapta automáticamente al tamaño del tick del instrumento, con símbolos FX de cinco y tres dígitos usando un multiplicador de 10x para coincidir con el comportamiento de MT5.
   - El stop-loss o take-profit se puede deshabilitar estableciendo la distancia en pips respectiva en cero.
4. **Trailing stop:**
   - Se activa solo cuando `TrailingStopPips` es mayor que cero.
   - El stop se mueve a `precio_actual - TrailingStopPips` (para largos) o `precio_actual + TrailingStopPips` (para cortos) una vez que el beneficio de la posición supera `TrailingStopPips + TrailingStepPips`.
   - El paso adicional de trailing previene modificaciones frecuentes.
5. **Filtro de salida diario:**
   - Usa las últimas dos velas diarias completadas.
   - Las posiciones largas se cierran si la EMA diaria estaba por debajo del cierre diario hace dos días y está por encima del cierre diario en el día más reciente (cruce bajista).
   - Las posiciones cortas se cierran si ocurre el cruce opuesto.

## Parámetros
| Nombre | Tipo | Valor predeterminado | Descripción |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Marco temporal de 1 hora | Marco temporal de trabajo usado para la EMA de ejecución y la lógica de entrada. |
| `MaLength` | `int` | 15 | Período de la EMA en el marco temporal de trabajo. |
| `MaShift` | `int` | 0 | Número de velas completadas usadas para desplazar la EMA hacia atrás. |
| `DailyMaLength` | `int` | 15 | Período de la EMA diaria que proporciona el filtro de salida por cruce. |
| `StopLossPips` | `decimal` | 50 | Distancia del stop-loss en pips. Establezca en `0` para deshabilitar. |
| `TakeProfitPips` | `decimal` | 50 | Distancia del take-profit en pips. Establezca en `0` para deshabilitar. |
| `TrailingStopPips` | `decimal` | 10 | Distancia del trailing stop en pips. Establezca en `0` para deshabilitar el trailing. |
| `TrailingStepPips` | `decimal` | 5 | Ganancia adicional mínima en pips antes de que el trailing stop se actualice nuevamente. |
| `Volume` | `decimal` | 0.1 | Tamaño base de la operación en lotes/contratos. |

## Notas y diferencias con la versión MQL
- Este port siempre usa medias móviles exponenciales, reflejando el valor predeterminado original (`MODE_EMA`). Otros modos de suavizado de MT5 no están soportados.
- El asesor experto de MT5 trabaja con cotizaciones de oferta/demanda en cada tick. Esta traducción opera en velas terminadas, por lo que las verificaciones de stop-loss y take-profit se evalúan en los máximos/mínimos de las velas.
- El indicador Parabolic SAR presente en el archivo original no influyó en las decisiones de trading y por tanto se omite de la implementación en C#.
- La lógica de trailing ajusta el nivel de stop almacenado pero no envía órdenes de stop al broker. La salida ocurre cuando el rango de la vela toca el nivel de stop o take-profit calculado.

## Consejos de uso
- Elegir un tipo de vela que coincida con el horizonte de trading deseado. Las velas de una hora por defecto replican el comportamiento del script fuente.
- Ajustar `MaLength` y `DailyMaLength` juntos para sintonizar la capacidad de respuesta entre las entradas intradía y los filtros de tendencia de mayor marco temporal.
- Para símbolos FX cotizados con cinco dígitos (p.ej., EURUSD), las distancias en pips se escalarán automáticamente para que 1 pip equivalga a 0.0001.
- Cuando se ejecute en backtests, asegurarse de que el flujo de datos diarios esté disponible para que el filtro de salida pueda funcionar correctamente.
