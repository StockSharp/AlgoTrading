# Estrategia Exp TrendMagic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Exp TrendMagic es una adaptación directa del asesor experto MetaTrader 5 "Exp_TrendMagic". Supervisa los cambios de color del indicador TrendMagic, que combina un índice de canal de productos básicos (CCI) con un canal de rango verdadero promedio (ATR). Cuando el indicador cambia de color, la estrategia cierra posiciones desde el lado opuesto y, opcionalmente, abre una nueva operación en la dirección de la nueva tendencia.

La conversión mantiene las opciones originales de administración de dinero, la compensación de señal configurable (`Signal Bar`) y los mismos permisos para ingresar o salir de operaciones largas y cortas.

## Lógica de trading
1. **Entradas de indicadores**
   - `CCI` (Índice de canales de productos básicos) con período configurable y precio aplicado.
   - `ATR` (Rango verdadero promedio) con período configurable.
   - El valor de TrendMagic se calcula como:
     - Cuando CCI ≥ 0: `TrendMagic = Low - ATR`, se sujeta para evitar disminuir la línea de soporte.
     - Cuando CCI < 0: `TrendMagic = High + ATR`, se sujeta para evitar aumentar la línea de resistencia.
   - El color de línea resultante es **0** para alcista (soporte por debajo del precio) y **1** para bajista (resistencia por encima del precio).

2. **Evaluación de señal**
   - La estrategia almacena los colores del indicador en orden cronológico para emular el búfer MetaTrader y utiliza el desplazamiento `Signal Bar` para leer la barra completada más reciente.
   - Si el color anterior (`Signal Bar + 1`) es **0** y el color actual (`Signal Bar`) es **1**, el algoritmo trata esto como un cambio alcista: cierra cualquier posición corta y, si se permite, abre una operación larga.
   - Si el color anterior es **1** y el color actual es **0**, el algoritmo cierra cualquier posición larga abierta y, si se permite, ingresa una operación corta.
   - Los indicadores de permiso comercial (`Allow Buy Entry`, `Allow Sell Entry`, `Allow Buy Exit`, `Allow Sell Exit`) siguen la semántica exacta de la versión MT5.

3. **Gestión del dinero**
   - `Money Management` determina cuánto capital se debe asignar por operación. Los valores negativos se interpretan como un tamaño de lote fijo.
   - `Margin Mode` selecciona la interpretación del valor de gestión del dinero:
     - `FreeMargin` / `Balance`: invierte una parte del capital de la cuenta dividida por el precio.
     - `LossFreeMargin` / `LossBalance`: arriesgar una parte del capital en relación con la distancia del stop-loss.
     - `Lot`: trata el valor como un volumen fijo.
   - Los volúmenes están alineados con `VolumeStep`, `MinVolume` y `MaxVolume` del valor seleccionado.

4. **Gestión de riesgos**
   - Cuando se realiza una nueva operación, la estrategia registra el precio de entrada y aplica las distancias originales de stop-loss y take-profit (expresadas en puntos, es decir, múltiplos de `PriceStep`).
   - Al alcanzar el stop-loss o el take-profit se cierra inmediatamente la posición y se borra el precio de entrada guardado.
   - Un acelerador impide reabrir una posición de la misma dirección antes de que se abra la siguiente vela, reproduciendo la guardia de "nivel de tiempo" MQL.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `Money Management` | Fracción de capital utilizada para el dimensionamiento (los valores negativos se convierten en volumen fijo). |
| `Margin Mode` | Modo de conversión para la gestión del dinero en volumen. |
| `Stop Loss` | Distancia de parada protectora en puntos de precio. |
| `Take Profit` | Objetivo de beneficio en puntos de precio. |
| `Deviation` | Reservado por compatibilidad con la entrada MT5 (no se usa directamente). |
| `Allow Buy Entry` / `Allow Sell Entry` | Alternar entradas largas/cortas. |
| `Allow Buy Exit` / `Allow Sell Exit` | Alternar el cierre de operaciones cortas/largas. |
| `Candle Type` | Principal marco temporal utilizado para los indicadores y la evaluación de señales. |
| `CCI Period` / `CCI Price` | CCI longitud y fuente de precio aplicada. |
| `ATR Period` | ATR longitud. |
| `Signal Bar` | Índice de la barra terminada utilizada para las señales (0 = actual, 1 = anterior, etc.). |

## Notas
- La estrategia se basa únicamente en velas terminadas (`CandleStates.Finished`) para imitar la implementación basada en ticks MT5.
- Todos los valores de los indicadores y las variables de estado se restablecen cuando se reinicia la estrategia, lo que garantiza ejecuciones de optimización deterministas.
- El parámetro `Deviation` se proporciona para compatibilidad total, aunque las órdenes de mercado StockSharp no utilizan un parámetro de deslizamiento explícito.
