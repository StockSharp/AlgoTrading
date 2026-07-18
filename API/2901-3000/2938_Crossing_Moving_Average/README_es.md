# Estrategia de Cruce de Medias Móviles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
- Conversión del asesor experto de MetaTrader 5 **"Crossing Moving Average (barabashkakvn's edition)"** del fuente `MQL/21515`.
- Implementa la lógica sobre la API de alto nivel de StockSharp con suscripciones de velas y vinculación de indicadores.
- Diseñado para instrumentos donde el momentum y los cruces de medias móviles capturan reversiones de tendencia.
- Este paquete contiene solo la versión en C#. Una traducción a Python se omite intencionalmente según lo solicitado.

## Idea Central
La estrategia monitorea dos medias móviles configurables (rápida y lenta) con desplazamientos opcionales hacia adelante y combina su cruce con un filtro de confirmación de momentum. Un trade se abre solo cuando:
1. La media rápida cruza la media lenta por al menos la distancia mínima configurada (en pips) durante las dos barras completadas más recientes.
2. El indicador de momentum sube por encima (para largo) o cae por debajo (para corto) del umbral definido por el usuario y mejora en la dirección del trade.
3. La fuente de precio de la señal puede elegirse entre precios de vela de apertura, máximo, mínimo, cierre, mediana, típico o ponderado para imitar los modos de precio aplicado de MetaTrader.

## Gestión de Riesgo y Trade
- El **volumen de orden** es fijo por trade y se aplica tanto cuando se entra en una posición nueva como cuando se revierte una posición existente.
- Las distancias de **stop-loss / take-profit** se configuran en pips y se traducen automáticamente en offsets de precio usando `Security.PriceStep`. Para instrumentos cotizados con 3 o 5 dígitos decimales, la estrategia multiplica el paso por 10 para reproducir el tamaño de pip de MetaTrader.
- El **trailing stop** se activa después de que el precio se mueve por `TrailingStop + TrailingStep` (en pips) desde la entrada. Una vez activado, el stop se mueve a `precio actual - TrailingStop` para posiciones largas (o `precio actual + TrailingStop` para cortas) siempre que pueda avanzar al menos `TrailingStep` pips.
- Los niveles protectores se evalúan en cada vela terminada: si el rango de la vela toca el stop-loss o take-profit, la posición se cierra al mercado para imitar la ejecución de órdenes en MetaTrader.

## Indicadores
- **Media Móvil Rápida** – período configurable, desplazamiento y método de suavizado (SMA, EMA, SMMA, WMA).
- **Media Móvil Lenta** – mismas opciones que la MA rápida.
- **Momentum** – período y fuente de precio idénticos a las medias móviles. La estrategia detecta automáticamente si el indicador emite valores alrededor de 0 o 100 y aplica el filtro en consecuencia.

## Lógica de Señales
1. Esperar hasta que todos los indicadores estén completamente formados. El algoritmo mantiene un historial interno de los valores más recientes para evaluar los cruces desplazados exactamente como en el asesor experto original.
2. Calcular la distancia de precio entre las medias rápida y lenta en las dos barras anteriores (con desplazamientos aplicados). La línea rápida debe cruzar la línea lenta y superar el filtro de distancia mínima.
3. Recuperar los valores de momentum en las mismas barras. Para entradas largas, el momentum actual debe ser mayor que tanto el umbral configurado como el valor de momentum anterior; para entradas cortas, se requiere lo opuesto.
4. Si aparece una nueva señal mientras la posición es opuesta, la estrategia cierra la posición existente e inmediatamente abre una en la nueva dirección con el tamaño de lote configurado.

## Referencia de Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `OrderVolume` | Volumen base usado para cada orden de mercado. | `1` |
| `StopLossPips` | Distancia de stop-loss en pips (0 deshabilita el stop). | `50` |
| `TakeProfitPips` | Distancia de take-profit en pips (0 deshabilita el objetivo). | `50` |
| `TrailingStopPips` | Distancia del trailing stop en pips (0 deshabilita el trailing). | `5` |
| `TrailingStepPips` | Mejora mínima en pips requerida para mover el trailing stop. | `5` |
| `MinDistancePips` | Separación mínima entre MAs para validar el cruce. | `0` |
| `MomentumFilter` | Diferencia de momentum mínima requerida para permitir entradas. | `0.1` |
| `FastPeriod` / `FastShift` | Longitud de MA rápida y desplazamiento horizontal (barras). | `13` / `1` |
| `SlowPeriod` / `SlowShift` | Longitud de MA lenta y desplazamiento horizontal (barras). | `34` / `3` |
| `MaMethod` | Tipo de suavizado de media móvil (Simple, Exponential, Smoothed, Weighted). | `Exponential` |
| `AppliedPrice` | Precio de la vela usado para cálculos del indicador. | `Close` |
| `MomentumPeriod` | Longitud de retrospección del momentum en barras. | `14` |
| `CandleType` | Tipo de datos de velas suministradas a la estrategia. | `TimeFrame(1m)` |

## Notas Prácticas
- Siempre asegurarse de que `Security.PriceStep` esté configurado para el instrumento; de lo contrario, la gestión de riesgo basada en pips recurrirá a unidades de precio brutas.
- La lógica de trailing requiere un `TrailingStepPips` positivo cuando `TrailingStopPips` está habilitado—reflejando la validación original de MetaTrader.
- Dado que los niveles de stop y take se evalúan en los rangos de velas, el uso de velas de mayor resolución proporciona una aproximación más cercana a la ejecución basada en ticks.
- Se incluyen mensajes de registro en entradas y ajustes de trailing para facilitar la depuración y la optimización de parámetros.
