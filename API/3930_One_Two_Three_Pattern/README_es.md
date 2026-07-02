# Estrategia de patrón uno-dos-tres
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia reproduce el asesor experto MetaTrader 4 “1-2-3_forCodeBase_v01.mq4” de Martes. Escanea velas terminadas en busca del patrón de reversión clásico 1-2-3: dos tramos de tendencia consecutivos completados por un tercer tramo de retroceso. El puerto mantiene todas las reglas del sistema original, incluidos los indicadores de longitud de tendencia personalizados (`RelDownTrLen_forCodeBase_v01` y `RelUpTrLen_forCodeBase_v01`) y la lógica de confirmación MACD.

Una configuración larga requiere un valle nuevo (punto 3) cerca del precio actual, un pico anterior (punto 2) y un valle más antiguo (punto 1). La tendencia bajista anterior debe ser al menos `TrendRatio` veces más larga que el retroceso alcista actual, y MACD tiene que cruzar por encima de la línea de señal (o cero) mientras permanece positiva en el punto 3. El lado corto refleja esos controles con picos y valles invertidos. Los stop se colocan un punto más allá del punto 3, la toma de ganancias es igual a la altura de la oscilación anterior y un trailing stop opcional basado en pips ajusta la salida una vez que la operación genera ganancias.

## Reglas comerciales

1. Suscríbase a la serie de velas configuradas (`CandleType`) y calcule MACD (períodos rápido/lento/de señal) sobre los precios de cierre.
2. Mantenga un historial continuo de los cuerpos de las velas para detectar la estructura 1-2-3. Los valles son mínimos locales de los cuerpos de las velas, los picos son máximos locales.
3. Evalúe las métricas de longitud de tendencia personalizadas utilizando el método de casco convexo de los indicadores MQL. La última duración de la tendencia bajista (escalada a `[0,1]`) debe dominar la tendencia alcista anterior (y viceversa para los cortos) según `TrendRatio`.
4. Confirme la configuración con MACD:
   - Largo: `MACD` cruza por encima de la señal (o por encima de cero) y el valor de MACD en el punto 3 es positivo.
   - Corto: `MACD` cruza por debajo de la señal (o por debajo de cero) y el valor de MACD en el punto 3 es negativo.
5. Filtros de entrada adicionales:
   - La distancia desde el precio actual hasta el punto 2 debe ser de cinco puntos.
   - La distancia de parada proyectada (`|point2 - point3|`) debe ser de al menos 13 puntos.
   - `TakeProfitPips` debe permanecer ≥ 10; de lo contrario, el comercio se desactiva (refleja la verificación de seguridad original).
6. Manejo de pedidos:
   - Ingrese usando `BuyMarket`/`SellMarket` con `TradeVolume` lotes (agregados con el volumen de la posición actual para reversiones).
   - Stop loss inicial = punto 3 ± un paso de precio.
   - Tomar ganancias = entrada ± `|point2 - point3|`.
   - Si `TrailingStopPips` > 0, siga la parada esa misma cantidad de puntos una vez que la ganancia no realizada exceda la distancia de seguimiento.
7. Salga con stop, toma de ganancias o trailing stop. Sólo se puede abrir una posición a la vez.

## Parámetros

| Parámetro | Tipo | Predeterminado | Descripción |
|-----------|------|---------|-------------|
| `TakeProfitPips` | `decimal` | `60` | Indicador de compatibilidad del EA. El comercio se detiene si el valor se establece por debajo de 10. |
| `TradeVolume` | `decimal` | `0.5` | Volumen en MetaTrader lotes utilizados para cada orden de mercado. |
| `TrailingStopPips` | `decimal` | `30` | Distancia del trailing stop en MetaTrader puntos. Establezca en `0` para deshabilitar el seguimiento. |
| `TrendRatio` | `decimal` | `4` | Relación mínima entre la duración de la tendencia principal anterior y el retroceso reciente. |
| `CandleType` | `DataType` | `H1` | Serie de velas utilizada para cálculos de patrones y MACD. |
| `MacdFast` | `int` | `12` | Período EMA rápida del oscilador MACD. |
| `MacdSlow` | `int` | `26` | Periodo EMA lento del oscilador MACD. |
| `MacdSignal` | `int` | `9` | Línea de señal EMA período. |
| `PatternLookback` | `int` | `100` | Número máximo de velas históricas escaneadas al localizar los puntos 1-2-3. |

## Notas de implementación

- Los indicadores personalizados originales se trasladan palabra por palabra: las búsquedas de casco convexo calculan los segmentos monótonos más largos de los cuerpos de las velas y devuelven sus longitudes relativas en `[0,1]`. Estos valores impulsan el filtro de relación de tendencia.
- Las velas históricas y los valores MACD se almacenan en búferes delimitados (600 elementos) para evitar el uso excesivo de memoria y al mismo tiempo mantener suficiente profundidad para la mirada retrospectiva.
- Las paradas y los objetivos se gestionan manualmente para coincidir con el comportamiento MetaTrader: los precios se comparan con los máximos y mínimos de las velas, y el trailing stop solo se ajusta cuando el precio avanza al menos la distancia configurada.
- `Volume` se sincroniza con `TradeVolume` al reiniciar y al iniciar, por lo que la optimización puede depender de la propiedad de estrategia estándar.

## Referencias

- Asesor experto original MQL4: `MQL/8131/1-2-3_forCodeBase_v01.mq4`.
- Indicadores personalizados: `RelDownTrLen_forCodeBase_v01.mq4`, `RelUpTrLen_forCodeBase_v01.mq4`.
