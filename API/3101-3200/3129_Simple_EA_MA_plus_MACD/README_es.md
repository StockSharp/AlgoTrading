# Estrategia Simple EA MA plus MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general
Esta estrategia porta el asesor experto MetaTrader 5 **Simple EA MA plus MACD** a la API de alto nivel de StockSharp. Busca un rompimiento desde una "barra de señal" que satisface dos condiciones: una media móvil desplazada está por debajo/encima de los máximos de la barra, y el histograma MACD acaba de cruzar la línea cero. Cuando la siguiente vela cierra más allá del extremo de la barra de señal, la estrategia entra en la dirección del rompimiento.

La implementación mantiene el comportamiento original del EA:

1. **Detección de señal** – en cada vela terminada la estrategia inspecciona la barra anterior. Una media móvil configurable (LWMA por defecto) calculada en el precio aplicado elegido debe ser menor que los máximos de la vela anterior y actual para largos (mayor para cortos). Simultáneamente la línea principal MACD debe haber cruzado cero entre las dos barras precedentes.
2. **Confirmación de señal** – una vez almacenada una barra de señal, la estrategia espera la siguiente vela completada. Un cierre por encima del máximo almacenado activa un rompimiento largo; un cierre por debajo del mínimo almacenado activa un rompimiento corto. Si el precio invalida la señal cerrando de vuelta dentro de la barra de señal, la configuración se cancela.
3. **Gestión de posición** – los nuevos trades heredan distancias de stop-loss, take-profit y trailing-stop expresadas en pips. Los niveles de protección se convierten a precios absolutos usando el `PriceStep` del instrumento. Los instrumentos con tres o cinco decimales reciben el ajuste clásico de forex (step × 10) para imitar las definiciones de pip de MetaTrader.

## Gestión de riesgo
- **Stop-loss / take-profit** – las distancias opcionales definidas en pips se evalúan en cada cierre de vela. Cuando el mercado imprime más allá del nivel correspondiente, la estrategia sale con una orden de mercado.
- **Trailing stop** – cuando el beneficio supera `TrailingStopPips + TrailingStepPips`, una referencia de trailing se mueve detrás del mejor precio alcanzado. Si el precio retrocede al nivel de trailing, la posición se cierra. Un paso de trailing de cero reactiva el stop en cada nuevo extremo.
- **Aplanar en reversión** – si aparece un rompimiento opuesto mientras hay una posición opuesta abierta, la estrategia envía una única orden de mercado suficientemente grande para cerrar la exposición existente y abrir el nuevo trade en un solo movimiento.

## Notas de implementación
- La media móvil soporta los mismos métodos de suavizado y opciones de precio aplicado que MetaTrader (Simple, Exponential, Smoothed, LinearWeighted y precios Close/Open/High/Low/Median/Typical/Weighted).
- `MaShift` reproduce el desplazamiento horizontal del indicador de MetaTrader leyendo valores de barras anteriores antes de evaluar las reglas de rompimiento.
- MACD usa el indicador integrado `MovingAverageConvergenceDivergence`. Solo se requiere el histograma (diferencia entre EMAs rápida y lenta); el período de la línea de señal se retiene para mantenerse fiel a la configuración del EA.
- Las suscripciones de velas y el procesamiento de indicadores dependen exclusivamente de la API de alto nivel de StockSharp. No se usa manejo manual de ticks ni buffers de indicadores.

## Parámetros
| Parámetro | Por defecto | Descripción |
|-----------|-------------|-------------|
| `Volume` | `1` | Tamaño de la orden para cada entrada de rompimiento. |
| `TakeProfitPips` | `50` | Distancia del objetivo de beneficio expresada en pips (convertida a precio absoluto usando el paso de precio del instrumento). Establecer en 0 para deshabilitar. |
| `StopLossPips` | `50` | Distancia del stop de protección en pips. Establecer en 0 para deshabilitar. |
| `TrailingStopPips` | `5` | Distancia del trailing stop en pips que se fija una vez que el precio avanza suficientemente. |
| `TrailingStepPips` | `5` | Progreso adicional mínimo (en pips) antes de que el trailing stop avance de nuevo. |
| `MaPeriod` | `100` | Longitud de la media móvil usada para validar la barra de señal. |
| `MaShift` | `0` | Desplazamiento horizontal aplicado a la media móvil, emulando el parámetro `ma_shift` de MetaTrader. |
| `MaMethod` | `LinearWeighted` | Método de suavizado de la media móvil (Simple, Exponential, Smoothed, LinearWeighted). |
| `MaAppliedPrice` | `Weighted` | Fuente de precio alimentada a la media móvil (Close, Open, High, Low, Median, Typical, Weighted). |
| `MacdFastPeriod` | `12` | Período EMA rápida usado en el cálculo MACD. |
| `MacdSlowPeriod` | `26` | Período EMA lenta usado en el cálculo MACD. |
| `MacdSignalPeriod` | `9` | Período de suavizado de la línea de señal retenido para paridad con el EA original. |
| `MacdAppliedPrice` | `Weighted` | Precio aplicado usado al alimentar valores al MACD. |
| `CandleType` | `1 hour` time frame | Serie de velas principal analizada para señales y gestión de trades. |

## Consejos de uso
- Ajustar las protecciones basadas en pips para coincidir con el tamaño de tick del instrumento seleccionado; los valores `PriceStep` incorrectos en el lado del conector distorsionarán las conversiones de pip.
- Para mercados altamente volátiles, considerar aumentar `TrailingStepPips` para reducir salidas prematuras, o disminuirlo para ajustar el comportamiento del trailing.
- Dado que los trades se ejecutan en velas cerradas, el rompimiento debe persistir hasta que la barra se complete; habilitar marcos temporales más pequeños aumenta la frecuencia de trading pero puede introducir más ruido.
