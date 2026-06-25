# Estrategia de Histograma ATR Normalizado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia de Histograma ATR Normalizado reproduce el comportamiento del experto de MetaTrader *Exp_ATR_Normalize_Histogram* dentro de StockSharp. El sistema observa la proporción normalizada entre la distancia suavizada de cierre a mínimo y el rango verdadero suavizado. Los cambios de color del histograma impulsan tanto las entradas como las salidas, emulando la lógica multi-buffer utilizada en la implementación MQL5 original.

## Cálculo del indicador
1. Para cada vela completada, la estrategia calcula:
   - `diff = Close − Low`.
   - `range = max(High, cierre anterior) − min(Low, cierre anterior)`.
2. Cada serie se suaviza independientemente con los métodos y longitudes seleccionados. Hay cinco métodos disponibles: Simple, Exponencial, Suavizado (RMA), Ponderado y Jurik. Los métodos MQL no compatibles (JurX, Parabolic, T3, VIDYA, AMA) utilizan la media móvil simple como alternativa.
3. El valor normalizado del histograma se calcula como

   `normalized = 100 × smoothedDiff / max(|smoothedRange|, PriceStep)`.
4. Los umbrales dividen el histograma en cinco bandas. El cruce entre bandas refleja el buffer de color producido por el indicador MQL.

## Lógica de señales
- **Filtro de entrada** – `SignalBar` selecciona qué barra histórica debe evaluarse (predeterminado 1, la última barra cerrada). La estrategia compara el color de esa barra con el de la anterior:
  - Una transición desde el extremo alcista (color `0`) a cualquier otro color abre una posición larga cuando las operaciones largas están habilitadas.
  - Una transición desde el extremo bajista (color `4`) a cualquier otro color abre una posición corta cuando las operaciones cortas están habilitadas.
- **Filtro de salida** – el color de la barra anterior es suficiente por sí solo para cerrar posiciones:
  - El color `0` cierra posiciones cortas si las salidas cortas están habilitadas.
  - El color `4` cierra posiciones largas si las salidas largas están habilitadas.
- Las salidas se procesan antes de cualquier nueva entrada para que la estrategia nunca mantenga operaciones superpuestas.

## Gestión de riesgo
La estrategia lleva un registro del último precio de ejecución y opcionalmente aplica stops de protección y objetivos medidos en puntos del instrumento. La conversión utiliza `Security.PriceStep`, coincidiendo con el concepto de "puntos" del experto original. Cuando se alcanza cualquier límite dentro de la barra, la posición se cierra inmediatamente y la dirección de la operación puede cambiar en la siguiente señal.

## Parámetros
- `CandleType` – marco temporal utilizado para el cálculo.
- `FirstSmoothingMethod` / `SecondSmoothingMethod` – tipo de suavizado para los flujos `diff` y `range`.
- `FirstLength` / `SecondLength` – períodos para los suavizadores.
- `HighLevel`, `MiddleLevel`, `LowLevel` – umbrales del histograma (predeterminado 60/50/40).
- `SignalBar` – desplazamiento para la evaluación del buffer (mínimo 1).
- `EnableBuyEntries`, `EnableSellEntries`, `EnableBuyExits`, `EnableSellExits` – interruptores para gestionar las cuatro direcciones de operación.
- `TradeVolume` – tamaño de orden base. La estrategia compensa automáticamente la exposición existente al cambiar de dirección.
- `StopLossPoints`, `TakeProfitPoints` – distancias de protección opcionales en puntos; establecer en cero para deshabilitar.

## Notas y diferencias con la versión MQL
- Ambas etapas de suavizado son configurables de forma independiente, pero solo están disponibles las cinco implementaciones de media móvil de StockSharp. Cuando se selecciona otro método MQL, la estrategia usa la media móvil simple manteniendo la longitud.
- La lógica de `SignalBar` sigue el desplazamiento del buffer usado en `CopyBuffer`, por lo que desplazamientos mayores aún comparan la barra elegida con su predecesor inmediato.
- Los parámetros de gestión de dinero del experto original (`MM`, `MMMode`, `Deviation`) se simplifican a un único parámetro `TradeVolume`. La ejecución de órdenes se realiza a mercado con monitoreo opcional de stop/objetivo.
