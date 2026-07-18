# Estrategia de Ruptura de Canal con una MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
La **Estrategia de Ruptura de Canal con una MA** replica el asesor experto MetaTrader 5 *One MA EA* usando la API de estrategia de alto nivel de StockSharp. El sistema dibuja una media móvil desplazada y la rodea con un canal configurable basado en pips. Cuando el precio abre fuera del canal después de haberlo probado en la misma barra, la estrategia abre una posición en la dirección de la ruptura mientras las protecciones opcionales de stop-loss y take-profit gestionan el riesgo automáticamente.

Características clave:
- Admite múltiples métodos de cálculo de media móvil (SMA, EMA, SMMA, LWMA).
- Permite elegir el precio de la vela (cierre, apertura, máximo, mínimo, mediana, típico, ponderado) que alimenta la media móvil.
- Aplica desplazamientos independientes al valor de la media móvil y a la vela usada para la evaluación de señales, coincidiendo con los controles `Current Bar` del EA original.
- Convierte distancias en pips a incrementos de precio absolutos usando el `PriceStep` del instrumento y la precisión decimal (los instrumentos de 3/5 decimales se asignan automáticamente a pips FX clásicos).

## Lógica de Trading
1. **Preparación del indicador**
   - Se calcula una media móvil con período `MaPeriod`, método `MaMethodParam`, desplazamiento `MaShift` y precio aplicado `AppliedPriceType` a partir de la serie de velas suscrita (`CandleType`).
   - Los offsets del canal se convierten de pips a incrementos de precio: `ChannelHighPips` por encima y `ChannelLowPips` por debajo de la media móvil desplazada.
   - Los buffers históricos permiten referenciar barras anteriores (`MaBarShift` para la serie de MA, `PriceBarShift` para datos OHLC) exactamente como en la versión MQL.

2. **Generación de señales**
   - **Ruptura alcista**: el mínimo de la vela inspeccionada permanece entre la línea base de MA y el canal superior, mientras su apertura aparece por encima del canal superior. Si no hay exposición larga (`Position <= 0`), la estrategia compra.
   - **Ruptura bajista**: el máximo de la vela inspeccionada permanece entre la línea base de MA y el canal inferior, mientras su apertura aparece por debajo del canal inferior. Si no hay exposición corta (`Position >= 0`), la estrategia vende.
   - El volumen de la orden equivale al `TradeVolume` configurado más cualquier cantidad necesaria para aplanar una posición opuesta, reflejando el comportamiento hedge-to-net del experto fuente.

3. **Gestión de riesgo**
   - `StopLossPips` y `TakeProfitPips` se traducen en distancias de precio absolutas y se pasan a `StartProtection`, habilitando órdenes de salida automatizadas para cada posición.
   - Con valores cero, la orden protectora respectiva se deshabilita.

No se aplica lógica de salida adicional; las posiciones se cierran solo a través del módulo de protección o revirtiendo hacia la señal opuesta.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `MaPeriod` | Longitud de la media móvil. Debe ser > 0. |
| `MaShift` | Desplazamiento horizontal de la media móvil en barras. Los valores positivos desplazan la MA hacia la derecha. |
| `MaMethodParam` | Tipo de cálculo de la media móvil (`Sma`, `Ema`, `Smma`, `Lwma`). |
| `AppliedPriceType` | Precio de la vela alimentado en la MA (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). |
| `MaBarShift` | Qué valor histórico de MA usar (0 = barra procesada actual). |
| `PriceBarShift` | Qué vela histórica inspeccionar para valores OHLC. |
| `ChannelHighPips` | Distancia (en pips) desde la MA hasta el límite superior del canal. |
| `ChannelLowPips` | Distancia (en pips) desde la MA hasta el límite inferior del canal. |
| `StopLossPips` | Distancia del stop protector en pips. Cero deshabilita el stop. |
| `TakeProfitPips` | Distancia del objetivo de beneficio en pips. Cero deshabilita el objetivo. |
| `TradeVolume` | Tamaño de la orden en unidades de volumen de estrategia (mapeado a `Strategy.Volume`). |
| `CandleType` | Serie de datos de velas usada para cálculos y señales. |

## Notas de Implementación
- La conversión de pip a precio usa `PriceStep` y `Decimals`. Para símbolos con 3 o 5 decimales, el valor del pip equivale a `PriceStep * 10`, de lo contrario equivale a `PriceStep`.
- Los buffers históricos se implementan con ventanas deslizantes de tamaño fijo para que la estrategia pueda acceder a las barras por índice sin depender de llamadas `GetValue` del indicador, cumpliendo las pautas del proyecto.
- La estrategia se basa únicamente en velas terminadas; las velas no terminadas se ignoran para evitar señales prematuras.
- El renderizado opcional del gráfico dibuja velas de precio y trades ejecutados cuando hay un área de gráfico disponible en la aplicación host.

## Consejos de Uso
- Asegurarse de que el instrumento suscrito exponga datos válidos de `PriceStep`/`Decimals`; de lo contrario, ajustar los parámetros basados en pips manualmente.
- Optimizar `MaPeriod`, distancias del canal y desplazamientos de barras para adaptar el comportamiento de ruptura a mercados o marcos temporales específicos.
- Combinar con controles de riesgo a nivel de portafolio cuando se despliega en vivo, ya que la estrategia siempre tiene una posición neta por instrumento.
