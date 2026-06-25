# Estrategia de Flechas Doji
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Concepto
La estrategia de Flechas Doji convierte el asesor experto original "Doji Arrows" de MetaTrader a la API de alto nivel de StockSharp. La idea es esperar a una vela doji genuina y luego operar un rompimiento de su rango. Una vela doji representa indecisión, por lo tanto, un cierre por encima del máximo del doji sugiere fortaleza alcista mientras que un cierre por debajo del mínimo del doji indica control bajista.

1. La estrategia procesa solo velas completadas de la suscripción `CandleType` configurada.
2. La vela anterior se analiza para determinar si es un doji. La vela se clasifica como doji cuando la diferencia absoluta entre la apertura y el cierre es menor o igual que `DojiBodyPoints` multiplicado por el paso de precio del instrumento. Si el parámetro se establece en `0`, se usa un único paso de precio como tolerancia, lo que coincide con la verificación de igualdad estricta en la versión MQL5.
3. Cuando la siguiente vela cierra por encima del máximo del doji, la estrategia envía una orden de compra a mercado. Cuando la siguiente vela cierra por debajo del mínimo del doji, se emite una orden de venta a mercado. Las posiciones opuestas existentes se aplanan automáticamente por el volumen de la orden de mercado.

Esta secuencia refleja el asesor experto original que reaccionaba una vez en la apertura de cada nueva barra.

## Gestión de Riesgo
La implementación convertida mantiene el comportamiento protector del script MQL:

- **Stop loss**: `StopLossPoints` controla cuán lejos, en pasos de precio, se coloca el stop loss inicial desde el precio de entrada. Establezca en cero para deshabilitar el stop fijo.
- **Take profit**: `TakeProfitPoints` define la distancia al objetivo de beneficio en pasos de precio. Establezca en cero para omitir el objetivo.
- **Trailing stop**: `TrailingStopPoints` y `TrailingStepPoints` reproducen el mecanismo de trailing. Una vez que la operación gana más de `TrailingStopPoints + TrailingStepPoints`, el nivel de stop se mueve a `TrailingStopPoints` desde el último cierre (cierre más alto para largos, cierre más bajo para cortos). El trailing es opcional y se activa solo cuando `TrailingStopPoints` es mayor que cero.

Los stops y objetivos se evalúan en cada vela terminada. Cuando se viola cualquier nivel (usando el máximo/mínimo de la vela), la estrategia sale de la posición con una orden de mercado y reinicia el estado de protección.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `StopLossPoints` | `30` | Distancia del stop loss inicial en pasos de precio. |
| `TakeProfitPoints` | `90` | Distancia del take profit en pasos de precio. |
| `TrailingStopPoints` | `15` | Distancia usada por el trailing stop en pasos de precio. |
| `TrailingStepPoints` | `5` | Beneficio adicional requerido antes de ajustar el trailing stop, en pasos de precio. |
| `DojiBodyPoints` | `1` | Tamaño máximo permitido del cuerpo de la vela anterior en pasos de precio para tratarla como un doji. `0` usa un paso de precio como tolerancia. |
| `CandleType` | `1 hora` | Tipo de vela suscrita para la generación de señales. |

## Notas de Implementación
- La estrategia se suscribe a velas mediante `SubscribeCandles(CandleType).Bind(ProcessCandle)` y mantiene solo la última vela completada en memoria.
- El paso de precio del instrumento se obtiene mediante `Security?.PriceStep`. Cuando no está disponible, se usa un valor de respaldo de `1` para que la estrategia pueda operar igualmente con datos sintéticos o históricos.
- Los niveles protectores se recalculan después de cada entrada, y la lógica de trailing puede crear un stop incluso cuando el stop loss fijo está deshabilitado (coincidiendo con el comportamiento MQL donde el trailing stop podía comenzar desde cero).
- Todas las acciones se ejecutan con órdenes de mercado para mantenerse alineadas con el asesor original que dependía de ejecución inmediata al mercado.

## Consejos de Uso
1. Configure las propiedades `Security`, `Portfolio` y `Volume` antes de iniciar la estrategia.
2. Ajuste los parámetros basados en puntos de acuerdo con el instrumento operado. Para instrumentos cotizados con pips fraccionarios, aumente los valores para que coincidan con el tamaño del tick del broker.
3. Combine la estrategia con controles de riesgo o módulos de análisis de StockSharp si se requiere dimensionamiento de posición más avanzado, porque la conversión mantiene la lógica de volumen fijo del código original.
