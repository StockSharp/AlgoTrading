# Estrategia de Ruptura de la Vela Anterior
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de Ruptura de la Vela Anterior** observa el máximo y el mínimo de la vela más recientemente cerrada de un marco temporal definido por el usuario (predeterminado: 4 horas). Siempre que la vela en vivo perfora más allá de esos niveles de referencia por un margen de entrada configurable, la estrategia abre operaciones de ruptura. Un filtro de tendencia de media móvil opcional mantiene las operaciones alineadas con la dirección prevaleciente, mientras que la lógica de salida en capas (stop loss fijo, take profit y stop de seguimiento basado en pips) gestiona el riesgo después de la entrada.

## Características principales

- Usa una vela de marco temporal superior como ancla de ruptura. Todas las señales se originan del máximo o mínimo de la última vela de referencia completada.
- Admite cuatro tipos de media móvil (SMA, EMA, Smoothed, WMA) con desplazamientos independientes para las líneas rápida y lenta. Cuando ambos períodos son distintos de cero, el filtro requiere que la MA rápida esté por encima/debajo de la MA lenta antes de aceptar operaciones largas/cortas.
- Convierte distancias basadas en pips (margen, stop loss, take profit, stop de seguimiento y paso) en unidades de precio usando la configuración del instrumento. Para instrumentos de 3 o 5 decimales, el pip equivale a 10 pasos de precio, reflejando la lógica MQL original.
- Permite el dimensionamiento de posición ya sea a través de volumen fijo o arriesgando un porcentaje del patrimonio de la cuenta relativo a la distancia del stop loss.
- Limita el número máximo de entradas por dirección y opcionalmente cierra todas las posiciones abiertas cuando el beneficio flotante alcanza una cantidad en efectivo especificada.
- La lógica del stop de seguimiento emula el asesor experto MQL5: después de que el precio avance más allá del margen de seguimiento más el paso, el nivel de stop avanza en pasos discretos.

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| `CandleType` | Marco temporal utilizado para construir la referencia de la vela anterior (predeterminado: 4 horas). |
| `IndentPips` | Distancia en pips añadida sobre el máximo o restada al mínimo antes de activar entradas. |
| `FastPeriod` / `SlowPeriod` | Longitudes de las medias móviles. Establezca cualquiera en 0 para deshabilitar el filtro de tendencia. |
| `FastShift` / `SlowShift` | Desplazamiento horizontal (en barras) aplicado a cada media móvil antes de la comparación. |
| `MaType` | Método de cálculo de la media móvil (Simple, Exponential, Smoothed, Weighted). |
| `StopLossPips` | Distancia en pips para el stop de protección inicial. Establecer en 0 para deshabilitar. |
| `TakeProfitPips` | Distancia en pips para órdenes de take profit. Establecer en 0 para deshabilitar. |
| `TrailingStopPips` | Distancia del stop de seguimiento. Requiere `TrailingStepPips` > 0. |
| `TrailingStepPips` | Mejora mínima de pips antes de que el stop de seguimiento se actualice. |
| `OrderVolume` | Volumen de operación fijo. Dejar en 0 para dimensionar posiciones por porcentaje de riesgo. |
| `RiskPercent` | Porcentaje del patrimonio del portafolio a arriesgar por operación cuando `OrderVolume` es 0. Requiere un stop loss distinto de cero. |
| `MaxPositions` | Número máximo de entradas permitidas por dirección. |
| `ProfitClose` | Cierra todas las posiciones abiertas cuando el beneficio flotante alcanza este valor (moneda base). |

## Lógica de operación

1. Rastrear la vela completada más reciente del `CandleType` y almacenar su máximo/mínimo.
2. En cada actualización de la vela actual:
   - Aplicar el filtro de media móvil si está habilitado. Sin historial de MA suficiente, la estrategia espera.
   - Calcular los niveles de ruptura: máximo anterior + margen y mínimo anterior − margen.
   - Cuando el máximo de la vela actual cruza el nivel superior, abrir una posición larga (sujeto a filtros, conteo máximo de posiciones y bloqueo de entrada por vela).
   - Cuando el mínimo de la vela actual cruza el nivel inferior, abrir una posición corta usando las mismas verificaciones.
3. Después de la entrada, la estrategia adjunta niveles de stop loss y take profit (si están configurados) y los mantiene en memoria. Cuando el precio toca cualquier límite, la posición se cierra mediante orden de mercado.
4. La activación del stop de seguimiento replica el asesor experto MQL: el precio debe superar el margen de seguimiento más el paso de seguimiento antes de mover el stop. Las actualizaciones posteriores requieren otra mejora completa de `TrailingStepPips`.
5. El beneficio flotante se recalcula en cada tick desde el precio de entrada promedio. Si llega a `ProfitClose`, toda la exposición abierta se liquida inmediatamente.
6. Para el dimensionamiento basado en riesgo, la estrategia convierte la distancia del stop en pips a moneda usando el `PriceStep` y `StepPrice` del instrumento. El volumen resultante respeta `MaxPositions` para escalar.

## Notas

- Establezca `TrailingStopPips` en 0 para deshabilitar el seguimiento. Si habilita el seguimiento, asegúrese de que `TrailingStepPips` también sea positivo; de lo contrario, no se producirán actualizaciones de seguimiento.
- La estrategia almacena marcas de tiempo de entrada por vela para evitar múltiples entradas en la misma barra de referencia, coincidiendo con el comportamiento original del EA.
- Para instrumentos sin metadatos `PriceStep`/`StepPrice`, el dimensionamiento basado en riesgo no puede calcularse y las operaciones se omitirán a menos que se especifique `OrderVolume`.
- Todos los comentarios en el código están escritos en inglés para alinearse con las directrices del proyecto.

## Archivos

- `CS/PreviousCandleBreakdownStrategy.cs` – Implementación en C# de la estrategia.

La traducción a Python no está disponible para esta estrategia.
