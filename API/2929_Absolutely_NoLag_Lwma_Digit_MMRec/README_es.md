# Estrategia AbsolutelyNoLag Lwma Digit MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Concepto

Esta estrategia es un port a StockSharp del experto MetaTrader *Exp_AbsolutelyNoLagLwma_Digit_NN3_MMRec*. Mantiene la arquitectura multi-temporal original construida en torno al indicador "AbsolutelyNoLagLWMA" y reproduce las reglas de recuperación de gestión monetaria (`MMRec`). Tres módulos independientes (A/B/C) monitorean velas de 12 horas, 4 horas y 2 horas respectivamente. Cada módulo puede abrir y cerrar su propia porción de posición mientras la estrategia rastrea la exposición combinada.

Cada módulo calcula una media móvil ponderada doble (WMA de una WMA) de una fuente de precio configurable. El valor suavizado se redondea al número de dígitos solicitado, exactamente como en el indicador MQL. Un cambio en la pendiente de la línea suavizada (el valor sube después de caer o viceversa) se trata como un cambio de dirección y genera acciones de trading para ese módulo.

## Reglas de Trading

1. Esperar una vela terminada del marco temporal del módulo.
2. Leer el precio aplicado (cierre, apertura, mediana, típico, etc.).
3. Procesar el precio a través de la WMA primaria y alimentar su resultado en una WMA secundaria para emular "AbsolutelyNoLagLWMA".
4. Redondear el valor suavizado al número configurado de dígitos y compararlo con el valor anterior.
5. **Pendiente ascendente** (`value > previous`):
   - Cerrar la pierna corta del módulo si las salidas cortas están habilitadas.
   - Si las entradas largas están habilitadas y no hay exposición larga activa, abrir una posición larga usando el volumen actual del módulo.
   - Recalcular los niveles de stop-loss y take-profit (expresados en pasos de precio) para la porción larga.
6. **Pendiente descendente** (`value < previous`):
   - Cerrar la pierna larga del módulo si las salidas largas están habilitadas.
   - Si las entradas cortas están habilitadas y no hay exposición corta activa, abrir una posición corta.
   - Actualizar los niveles protectores para la porción corta.
7. En cada vela el módulo verifica si el máximo/mínimo de la vela perforó el nivel actual de stop-loss o take-profit. Si se toca, la porción de la posición se cierra a ese precio y el resultado del trade se registra para la lógica de gestión monetaria.
8. La gestión monetaria mantiene una cola de los resultados de trades más recientes para cada dirección. Cuando los últimos *N* trades (donde *N* es igual al disparador de pérdida) fueron perdedores, la siguiente orden usa el volumen reducido; de lo contrario se usa el volumen normal. La detección de trade perdedor se basa en el precio de entrada que se almacenó cuando se abrió la porción y el precio de salida (stop/objetivo/cierre) usado para cerrarla.

La estrategia usa órdenes de mercado para entradas y salidas y asume ejecuciones al cierre de la vela para señales y al precio protector para salidas de stop/objetivo.

## Parámetros

Cada módulo posee un conjunto idéntico de parámetros. Los valores predeterminados corresponden al experto MQL fuente.

| Parámetro | Descripción |
|-----------|-------------|
| `ACandleType` / `BCandleType` / `CCandleType` | Marco temporal de las velas del módulo (12h / 4h / 2h por defecto). |
| `ALength` / `BLength` / `CLength` | Longitud del suavizado AbsolutelyNoLagLWMA (aplicada a ambas WMA). |
| `AAppliedPrice` / `BAppliedPrice` / `CAppliedPrice` | Fuente de precio usada en el indicador (close, open, high, low, median, typical, weighted, simple, quarter, TrendFollow1, TrendFollow2, Demark). |
| `ADigits` / `BDigits` / `CDigits` | Número de dígitos para redondear el valor suavizado. |
| `ABuyOpen`, `ASellOpen`, `ABuyClose`, `ASellClose` (y equivalentes de módulo B/C) | Banderas que controlan si el módulo puede abrir/cerrar porciones largas o cortas. |
| `ASmallVolume`, `ANormalVolume` | Volúmenes de orden reducido y normal. Los mismos valores se reutilizan para trades cortos. |
| `ABuyLossTrigger`, `ASellLossTrigger` | Número de trades perdedores consecutivos que activa el volumen reducido para largos/cortos. |
| `AStopLossPoints`, `ATakeProfitPoints` | Niveles protectores expresados en pasos de precio para la porción del módulo. Existen parámetros idénticos para los módulos B y C. |

Las colas de gestión monetaria se reinician cuando el disparador correspondiente se establece en cero. Los cálculos de pasos de precio dependen de `Security.Step`; si el instrumento no lo expone, se usa un paso de `1`.

## Notas

- Cada módulo gestiona su propio volumen de posición interno; por lo tanto, diferentes módulos pueden estar largos o cortos simultáneamente. La posición principal de la estrategia es la suma de todas las porciones de módulos.
- Los niveles de stop-loss y take-profit se verifican en cada vela terminada usando el máximo/mínimo de la vela para detectar perforaciones.
- La enumeración `AppliedPrices` coincide con las opciones del indicador original, incluyendo ambas fórmulas TrendFollow y la variante DeMark.
- La estrategia no agrega indicadores al gráfico; se apoya en la API `Bind` de alto nivel y mantiene las instancias de indicadores privadas a cada módulo según lo requerido por las pautas.
- La lógica cierra y abre trades solo cuando la pendiente cambia de dirección, lo que previene órdenes duplicadas en barras consecutivas con el mismo estado de tendencia.
