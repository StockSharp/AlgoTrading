# Estrategia Proper Bot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Proper Bot** es un sistema de trading en rejilla convertido del asesor experto original de MetaTrader 4 "Proper Bot". La estrategia abre una cesta de órdenes con sesgo direccional, expande esa cesta usando un mapa configurable de distancia/volumen y gestiona todo el ciclo con una combinación de filtros basados en tiempo, volumen y precio. El puerto en C# se apoya en las suscripciones de velas e indicadores de alto nivel de StockSharp para mantener la implementación cercana al flujo de trabajo de trading gestionado.

## Principios de operación
1. **Detección de señales**
   - Cuando el filtro EMA está habilitado, la estrategia rastrea medias móviles exponenciales rápida, media y lenta en la serie de velas seleccionada. Los cruces entre las medias rápida y lenta generan la dirección, mientras que la media intermedia bloquea las operaciones que aún no han confirmado la tendencia.
   - Cuando el filtro está deshabilitado, el algoritmo simplemente reutiliza la dirección del cuerpo de la vela terminada anterior.
2. **Filtros previos a la operación**
   - Una media móvil simple del volumen de velas impone un requisito de volumen promedio mínimo.
   - Las operaciones solo se permiten entre la hora de inicio y fin de sesión configurables.
   - Los niveles de precio superior e inferior impiden comprar demasiado alto o vender demasiado bajo. Los movimientos extremos más allá de esas bandas también pueden forzar una entrada en la dirección correspondiente.
3. **Expansión de la rejilla**
   - La orden de mercado inicial utiliza el parámetro `FirstVolume`. Las adiciones posteriores siguen el parámetro `GridMap` que contiene una lista de pares `distancia/volumen`. Cuando el precio se mueve en contra de la posición actual por la distancia configurada, se añade una nueva orden del volumen mapeado.
   - Las distancias se interpretan en pasos de precio usando el `PriceStep` del instrumento. Si el valor no está disponible, la estrategia recurre a `0.0001`.
4. **Gestión de riesgos**
   - Toda la cesta comparte una toma de ganancias agregada y una distancia de stop-loss medida desde el precio de entrada promedio ponderado.
   - Una salida trailing monitorea la suma del beneficio flotante de todas las órdenes abiertas. Una vez que el beneficio supera el umbral de activación, cualquier retroceso mayor que `TrailStepPoints` cierra todo el ciclo.
   - Si cualquier condición de salida se activa, la estrategia cierra la posición completa con una orden de mercado y resetea el estado de la rejilla.

## Parámetros
| Parámetro | Descripción | Valor predeterminado |
|-----------|-------------|----------------------|
| `FastMaPeriod` | Longitud de la EMA rápida usada en el filtro de entrada. | 10 |
| `MidMaPeriod` | Longitud opcional de la EMA media que debe quedar entre las líneas rápida y lenta para confirmar la señal. Establecer en 0 para deshabilitar. | 25 |
| `SlowMaPeriod` | Longitud de la EMA lenta usada en el filtro de entrada. | 50 |
| `DisableMaFilter` | Cuando está habilitado, la estrategia ignora las reglas EMA y sigue la dirección de la vela anterior. | true |
| `VolumePeriod` | Número de velas usadas para promediar el volumen. Un valor de 0 deshabilita el filtro. | 1 |
| `VolumeMinimum` | Volumen promedio mínimo requerido para permitir nuevas entradas. | 69 |
| `HighLevel` | Umbral de precio que bloquea entradas largas por encima de él y puede forzar cortos. | 1.50001 |
| `LowLevel` | Umbral de precio que bloquea entradas cortas por debajo de él y puede forzar largos. | 1.40001 |
| `FirstVolume` | Volumen usado para la primera orden de cada ciclo de rejilla. | 0.08 |
| `GridMap` | Lista de pares `distancia/volumen` (puntos separados por espacios) que definen cuánto debe moverse el precio antes de añadir la siguiente orden y qué volumen usar. | `120/0.1 ... 120/0.19` |
| `TakeProfitPoints` | Distancia de ganancia (en pasos de precio) aplicada al precio de entrada promedio ponderado para toda la cesta. | 10000 |
| `StopLossPoints` | Distancia de pérdida (en pasos de precio) aplicada al precio de entrada promedio ponderado para toda la cesta. | 30000 |
| `TrailStartPoints` | Beneficio flotante mínimo requerido antes de que la lógica trailing pueda armarse. | 52 |
| `TrailDistancePoints` | Distancia de beneficio que debe alcanzarse (menos el paso de trailing) antes de que la lógica trailing se active. | 52 |
| `TrailStepPoints` | Máximo retroceso de beneficio tolerado una vez que la lógica trailing está activa. | 2 |
| `StartHour` / `StartMinute` | Inicio de la sesión de trading (inclusive). | 06:00 |
| `FinishHour` / `FinishMinute` | Fin de la sesión de trading (inclusive, soporta ventanas nocturnas). | 21:00 |
| `CandleType` | Tipo de datos de velas procesado por la estrategia. | Marco temporal de 1 minuto |

## Notas de uso
- Los valores de `GridMap` se analizan usando decimales de cultura invariante. Asegúrese de que las distancias estén expresadas en puntos del instrumento antes de la barra y los volúmenes después.
- Todas las distancias de riesgo se convierten usando el `PriceStep` del instrumento. Si el instrumento expone un tamaño de tick diferente, configure `PriceStep` en consecuencia antes de iniciar la estrategia.
- La implementación trailing agrega el beneficio flotante de todas las órdenes abiertas (coincidiendo con el EA original) pero realiza la verificación en velas completadas. Las salidas rápidas intrabarra se pueden habilitar ejecutando la estrategia en marcos temporales más pequeños.
- Las entradas forzadas producidas al sobrepasar `HighLevel` o `LowLevel` usan el precio de cierre de la vela como aproximación de los valores bid/ask.
- El puerto de StockSharp cierra toda la cesta cuando se cumple una condición de toma de ganancias, stop-loss o trailing. Esto difiere de la implementación MT4 donde cada ticket lleva su propio stop/objetivo, pero simplifica la gestión de órdenes de alto nivel.

## Diferencias respecto a la versión MT4
- El EA de MT4 enviaba niveles de protección individuales con cada orden. La implementación de StockSharp calcula las salidas contra la posición combinada para mantenerse dentro del API de alto nivel.
- Los precios bid/ask se aproximan con el precio de cierre de la vela porque las suscripciones de velas de StockSharp no entregan spreads por tick de forma predeterminada.
- El bloque trailing usa el mayor entre `TrailDistancePoints - TrailStepPoints` y `TrailStartPoints` como umbral de activación para que el comportamiento permanezca estable incluso cuando los parámetros se superponen.
- Los horarios de trading dependen del `DateTimeOffset` de la vela entrante. Asegúrese de que el feed de datos suministre marcas de tiempo en la zona horaria deseada.

## Archivos
- `CS/ProperBotStrategy.cs` – implementación de la estrategia.
- `README.md` – descripción en inglés.
- `README_zh.md` – traducción al chino.
- `README_ru.md` – traducción al ruso.
