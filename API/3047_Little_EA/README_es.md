# Estrategia Little EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general
Little EA es un experto de cruce de medias móviles escrito originalmente para MetaTrader. La estrategia observa la vela seleccionada por el parámetro **OHLC bar index** y reacciona cuando esa vela cruza una media móvil desplazada desde abajo o desde arriba. El port de StockSharp mantiene la idea original de múltiples entradas al permitir varias franjas por dirección mientras respeta una exposición máxima configurable.

## Lógica de trading
1. Suscribirse a la serie de velas configurada y alimentar el tipo de media móvil seleccionado con la fuente de precio elegida (cierre, apertura, máximo, mínimo, mediana, típico o ponderado).
2. Almacenar las velas completadas para que la estrategia pueda referenciar la vela en el `OhlcBarIndex` (el valor predeterminado `1` significa la última vela completamente cerrada).
3. Aplicar el `MaShift` opcional leyendo el valor de la media móvil de varias barras atrás, replicando el desplazamiento visual de MetaTrader.
4. Cuando la vela de referencia cierra por encima de la MA desplazada, tratarla como un cruce alcista. Cuando cierra por debajo, tratarla como un cruce bajista.
5. Para un cruce alcista:
   - Si la exposición corta neta ya iguala el máximo configurado, cerrar toda la posición corta.
   - De lo contrario, si la exposición larga aún está por debajo del máximo, agregar una franja de `TradeVolume` al lado largo.
6. Para un cruce bajista:
   - Si la exposición larga ya iguala el máximo, cerrar toda la posición larga.
   - De lo contrario, si la exposición corta está por debajo del límite, agregar una franja de `TradeVolume` al lado corto.

El límite de volumen emula el límite `Int_Max_Pos` del experto original mientras trabaja con las posiciones netas de StockSharp.

## Parámetros
| Nombre | Tipo | Por defecto | Descripción |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Marco temporal de 1 minuto | Marco temporal principal usado para señales y cálculos de indicadores. |
| `OhlcBarIndex` | `int` | `1` | Índice de la vela histórica usada para la detección de cruce (0 = vela actual en formación, 1 = última vela terminada). |
| `MaxPositionsPerSide` | `int` | `15` | Número máximo de franjas de `TradeVolume` que se pueden acumular por dirección. |
| `MaPeriod` | `int` | `64` | Longitud de la media móvil. |
| `MaShift` | `int` | `0` | Número de barras para desplazar la MA hacia atrás al verificar cruces. |
| `MaType` | `MovingAverageType` | `Smoothed` | Modo de cálculo de la media móvil (Simple, Exponential, Smoothed, Weighted). |
| `AppliedPrice` | `AppliedPriceType` | `Close` | Fuente de precio usada como entrada del indicador. |
| `TradeVolume` | `decimal` | `1` | Volumen de orden enviado con cada nueva franja. |

## Diferencias con el experto original de MetaTrader
- La gestión de dinero está simplificada: solo se admiten entradas de volumen fijo. El dimensionamiento de riesgo porcentual del EA original no está implementado.
- StockSharp trabaja con posiciones netas, por lo que las posiciones en dirección opuesta se cierran antes de que se acumule nueva exposición. El límite de `MaxPositionsPerSide` se aplica a la exposición neta en lotes.
- Los valores del indicador y el historial de velas se procesan a través de la API de suscripción de velas de alto nivel en lugar de copias manuales de buffer.

## Consejos de uso
- Ajustar `TradeVolume` para que coincida con el paso de lote del instrumento antes de lanzar la estrategia; el constructor también asigna el mismo valor a `Strategy.Volume` para que los métodos de ayuda usen el tamaño deseado por defecto.
- Usar `MaShift` en combinación con `OhlcBarIndex` para recrear la alineación visual del gráfico de MetaTrader si es necesario.
- Agregar la estrategia a un gráfico para ver velas, la superposición de la media móvil y las operaciones ejecutadas, lo que ayuda a verificar el comportamiento del cruce.

## Indicadores
- Una media móvil configurable (`Simple`, `Exponential`, `Smoothed` o `Weighted`).
