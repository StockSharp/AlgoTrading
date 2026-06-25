# Estrategia Dealers Trade MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Dealers Trade MACD es un sistema de piramidación que fue portado del asesor experto MQL5 original "Dealers Trade v7.74". Sigue la pendiente de la línea principal MACD para decidir cuándo acumular posiciones en la dirección de la tendencia. La lógica está diseñada para swing trading en gráficos H4 y D1 donde los cambios de momentum son menos ruidosos.

## Cómo funciona la estrategia

- **Generación de señales** – la estrategia se suscribe a velas del marco temporal seleccionado y evalúa el valor de la línea principal MACD en cada barra cerrada. Un MACD ascendente implica sesgo largo y un MACD descendente implica sesgo corto. La señal puede invertirse con el parámetro `ReverseCondition` para coincidir con cuentas que históricamente operaron entradas contrarias.
- **Dimensionamiento de posición** – la primera orden usa el tamaño fijo `FixedVolume` o, si está en `0`, el sistema asigna riesgo dinámicamente desde el capital del portafolio usando el parámetro `RiskPercent` y la distancia de stop loss configurada. Las entradas adicionales se multiplican por `VolumeMultiplier` elevado al recuento de posiciones actuales (p.ej. 1.6, 1.6², 1.6³, …) y solo se envían cuando el precio se ha movido al menos `IntervalPoints * PriceStep` desde el último relleno. Las órdenes se omiten una vez que la exposición neta excedería `MaxVolume` o el número de entradas alcanza `MaxPositions`.
- **Gestión de órdenes** – cada posición mantiene sus propios objetivos de stop loss y take profit calculados desde el precio de entrada y los offsets basados en puntos (`StopLossPoints`, `TakeProfitPoints`). Si `TrailingStopPoints` es mayor que cero, el stop se arrastra hacia arriba (o hacia abajo para cortos) una vez que el beneficio supera `TrailingStopPoints + TrailingStepPoints`, emulando el comportamiento de trailing original.
- **Protección de cuenta** – cuando el número de operaciones abiertas es mayor que `PositionsForProtection` y el beneficio no realizado agregado cruza `SecureProfit`, la estrategia cierra la posición más rentable para fijar ganancias antes de añadir nueva exposición. Esto refleja el bloque de "Protección de cuenta" de la versión MQL.

## Parámetros

| Nombre | Por defecto | Descripción |
| --- | --- | --- |
| `CandleType` | H4 | Marco temporal usado para cálculos MACD y decisiones de operación. |
| `FixedVolume` | 0.1 | Tamaño de lote para la primera entrada. Establecer en 0 para habilitar el dimensionamiento basado en riesgo. |
| `RiskPercent` | 5 | Porcentaje del capital actual arriesgado cuando `FixedVolume` es cero. |
| `StopLossPoints` | 90 | Distancia del stop loss expresada en pasos de precio. Usar 0 para deshabilitar stops duros. |
| `TakeProfitPoints` | 30 | Distancia del take profit en pasos de precio. Usar 0 para deshabilitar. |
| `TrailingStopPoints` | 15 | Distancia del trailing stop en pasos de precio. Establecer en 0 para desactivar el trailing. |
| `TrailingStepPoints` | 5 | Distancia adicional que debe ganarse antes de que el trailing stop se mueva de nuevo. |
| `MaxPositions` | 5 | Número máximo de entradas abiertas simultáneamente. |
| `IntervalPoints` | 15 | Distancia mínima en pasos de precio requerida entre entradas consecutivas. |
| `SecureProfit` | 50 | Umbral de beneficio (en moneda de cotización) que activa la protección de cuenta. |
| `AccountProtection` | true | Habilita cerrar la operación con mejor rendimiento cuando se alcanza el objetivo de beneficio seguro. |
| `PositionsForProtection` | 3 | Número mínimo de operaciones que deben estar abiertas antes de que la protección pueda activarse. |
| `ReverseCondition` | false | Invierte la interpretación de la pendiente MACD. |
| `MacdFastPeriod` | 14 | Longitud EMA rápida para el indicador MACD. |
| `MacdSlowPeriod` | 26 | Longitud EMA lenta para el indicador MACD. |
| `MacdSignalPeriod` | 1 | Longitud EMA de señal para el indicador MACD (establecida en 1 en el asesor experto original). |
| `MaxVolume` | 5 | Límite superior para el tamaño de posición acumulado. |
| `VolumeMultiplier` | 1.6 | Multiplicador aplicado al tamaño base para cada nueva entrada. |

## Notas y limitaciones

- El experto MQL original podía mantener posiciones largas y cortas cubiertas simultáneamente. StockSharp usa posiciones neteadas por defecto, por lo que este port cierra la exposición opuesta antes de añadir nuevas operaciones en la otra dirección.
- Los valores MACD se evalúan solo en velas cerradas. Las señales intrabarra pueden aparecer más tarde que en la implementación MQL basada en ticks, pero el comportamiento es mucho más estable para pruebas históricas.
- Todas las distancias basadas en puntos se multiplican por el `PriceStep` del instrumento. Si el instrumento no proporciona esos metadatos, la estrategia recurre a un paso de 0.0001, así que ajuste los parámetros al operar instrumentos con diferentes tamaños de tick.
- Cuando `FixedVolume` es cero, la estrategia requiere una distancia de stop loss no cero para calcular el dimensionamiento basado en riesgo. Si el stop está deshabilitado, el volumen por defecto es cero y no se envía ninguna operación.
