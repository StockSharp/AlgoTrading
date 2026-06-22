# Estrategia Money Rain
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Conversión del asesor experto original **MoneyRain (edición de barabashkakvn)** de MQL5 a la API de alto nivel de StockSharp.
- Usa el oscilador DeMarker para elegir la dirección: valores por encima de 0.5 activan entradas largas, mientras que valores de 0.5 o por debajo activan entradas cortas.
- Opera solo una posición a la vez y se basa en offsets fijos de stop-loss/take-profit expresados en puntos.

## Datos de mercado e indicadores
- Se suscribe al `CandleType` configurable (por defecto: marco temporal de 30 minutos).
- Calcula un único indicador `DeMarker` con `DeMarkerPeriod` ajustable (por defecto: 31).
- Se suscribe a cotizaciones de Nivel 1 para aproximar el spread actual, que es requerido por la lógica de dimensionamiento adaptativo de posiciones.

## Lógica de trading
1. Procesa solo velas finalizadas para mantenerse alineado con la lógica original de "nueva barra" (verificación `iTime(0)` en MQL).
2. Mientras existe una posición, monitorea el máximo/mínimo de la vela contra los niveles de stop-loss y take-profit precalculados. Si uno de ellos es tocado, cierra la posición con una orden de mercado y marca el resultado como pérdida o ganancia.
3. Cuando no hay posición abierta y el límite de pérdidas no ha sido alcanzado, calcula el volumen de la operación.
4. Entra largo en `DeMarker > 0.5`; de lo contrario entra corto. La estrategia cancela cualquier orden pendiente antes de enviar la orden de mercado.

## Gestión de capital
- Reproduce la lógica `getLots()` de la versión MQL rastreando:
  - `_lossesVolume`: volumen acumulado de operaciones perdedoras recientes escalado por el tamaño de lote base.
  - `_consecutiveLosses` y `_consecutiveProfits`: contadores de rachas usados para decidir cuándo reiniciar el acumulador de pérdidas.
- Cuando aparece la primera operación rentable tras una racha perdedora (`_consecutiveProfits == 0`), el siguiente tamaño de orden se incrementa según la fórmula original:
  \[
  \text{volume} = \text{BaseVolume} \times \frac{_lossesVolume \times (\text{StopLossPoints} + \text{spread})}{\text{TakeProfitPoints} - \text{spread}}
  \]
- El spread se estima a partir de las mejores cotizaciones de compra/venta (en puntos) y se ignora cuando los datos de Nivel 1 aún no están disponibles.
- Establecer `FastOptimize = true` deshabilita el dimensionamiento adaptativo y siempre usa el lote base.

## Controles de riesgo
- `StopLossPoints` y `TakeProfitPoints` se convierten a precios absolutos usando el paso de precio del instrumento con un multiplicador adicional de 10x para símbolos de 3 o 5 dígitos (refleja la lógica `digits_adjust` de MQL).
- `LossLimit` bloquea más operaciones una vez que el número de pérdidas consecutivas supera el umbral definido por el usuario (por defecto: prácticamente deshabilitado en 1.000.000).

## Parámetros
| Parámetro | Descripción | Predeterminado |
| --- | --- | --- |
| `DeMarkerPeriod` | Período de promediado del indicador DeMarker. | 31 |
| `TakeProfitPoints` | Offset de take-profit en puntos estilo DeMarker. | 5 |
| `StopLossPoints` | Offset de stop-loss en puntos estilo DeMarker. | 20 |
| `BaseVolume` | Volumen de orden predeterminado (tamaño de lote). | 0.01 |
| `LossLimit` | Máximas pérdidas consecutivas permitidas antes de pausar. | 1.000.000 |
| `FastOptimize` | Cuando es `true`, deshabilita el dimensionamiento adaptativo de posiciones. | `false` |
| `CandleType` | Tipo de datos de velas usado para cálculos. | Velas de 30 minutos |

## Notas de implementación
- Los stops y objetivos se emulan comprobando los extremos de las velas. El orden de ejecución intrabarra no puede recuperarse, por lo que los toques simultáneos favorecen la rama del stop-loss (suposición conservadora).
- `OnOwnTradeReceived` se usa para detectar cuándo se completó una orden de salida protectora, permitiendo a la estrategia actualizar los contadores de racha y el acumulador de volumen de pérdidas.
- El código mantiene la indentación con tabulaciones y usa comentarios en inglés, siguiendo las pautas del repositorio.

## Archivos
- `CS/MoneyRainStrategy.cs` – implementación de la estrategia.
- `README.md` / `README_ru.md` / `README_zh.md` – documentación multilingüe.

## Diferencias respecto a la versión MQL
- Las órdenes protectoras del lado del bróker se reemplazan con salidas de mercado basadas en rangos de velas.
- El spread se aproxima a partir de cotizaciones de Nivel 1 en lugar de directamente desde los metadatos del símbolo.
- La funcionalidad de correo y las verificaciones explícitas de `IsTradeAllowed` se omiten porque el entorno StockSharp gestiona la conectividad por separado.
