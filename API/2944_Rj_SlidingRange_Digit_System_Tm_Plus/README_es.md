# Estrategia Exp Rj SlidingRangeRj Digit System Tm Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia StockSharp es un port del asesor experto MetaTrader `Exp_Rj_SlidingRangeRj_Digit_System_Tm_Plus`. Recrea la lógica de trading original basada en el indicador de canal personalizado **Rj_SlidingRangeRj_Digit** y conserva las opciones de gestión de operaciones configurables. La estrategia monitorea velas finalizadas en un marco temporal configurable, detecta rupturas más allá del canal y reacciona a esos eventos con entradas retrasadas, salidas temporizadas opcionales y gestión de stop/objetivo basada en precio.

## Lógica del indicador

El indicador Rj_SlidingRangeRj_Digit construye un canal de precio adaptativo usando un proceso de promediado de múltiples pasos:

1. Para la banda superior, el máximo-máximo dentro de `UpCalcPeriodRange` barras se calcula para cada una de las últimas `UpCalcPeriodRange` ventanas deslizantes, desplazadas por `UpCalcPeriodShift` barras. El promedio de estos máximos se redondea a la precisión especificada por `UpDigit`.
2. La banda inferior repite la misma lógica en mínimos usando `DnCalcPeriodRange`, `DnCalcPeriodShift` y `DnDigit`.
3. Una vela se etiqueta como ruptura cuando su precio de cierre está por encima de la banda superior (colores `2` / `3`) o por debajo de la banda inferior (colores `0` / `1`). Las velas dentro del canal producen un color neutro (`4`).

La estrategia transmite velas finalizadas, reconstruye las bandas en cada actualización y almacena los códigos de color más recientes para imitar el comportamiento `CopyBuffer`/`SignalBar` de la implementación MQL.

## Reglas de trading

* **Retraso de entrada:** Las señales se evalúan en la barra definida por `SignalBar` (por defecto una barra atrás). La estrategia espera hasta que aparezca un color de ruptura y la barra anterior no tenga el mismo color de ruptura. Esto reproduce el retraso original de una barra antes de tomar una operación.
* **Entradas largas:** Habilitadas por `EnableBuyEntries`. Una ruptura alcista (`color 2` o `3`) activa una compra de mercado cuando no hay posición larga abierta (la exposición corta se compensa automáticamente).
* **Entradas cortas:** Habilitadas por `EnableSellEntries`. Una ruptura bajista (`color 0` o `1`) activa una venta de mercado cuando no hay posición corta abierta.
* **Señales de salida:**
  * Los largos se cierran con colores de ruptura bajista si `EnableBuyExits` es verdadero.
  * Los cortos se cierran con colores de ruptura alcista si `EnableSellExits` es verdadero.
  * La salida opcional basada en tiempo (`UseTimeExit`) cierra cualquier posición abierta una vez que se ha mantenido por más de `ExitMinutes`.
  * Los niveles opcionales de stop-loss y take-profit expresados en puntos (`StopLossPoints`, `TakeProfitPoints`) se convierten en desplazamientos de precio usando el `PriceStep` del instrumento.

Todas las acciones usan `BuyMarket` / `SellMarket` para que la estrategia revierta automáticamente las posiciones cuando sea necesario.

## Parámetros

| Parámetro | Descripción | Valor predeterminado |
|-----------|-------------|----------------------|
| `CandleType` | Tipo de vela (marco temporal) usado para la detección de señales. | Velas de 8 horas |
| `EnableBuyEntries` / `EnableSellEntries` | Permitir entradas de ruptura largas/cortas. | `true` |
| `EnableBuyExits` / `EnableSellExits` | Permitir salidas basadas en indicador para largos/cortos. | `true` |
| `UseTimeExit` | Cerrar operaciones después de un tiempo de mantenimiento fijo. | `true` |
| `ExitMinutes` | Límite de tiempo de mantenimiento en minutos. | `1920` |
| `UpCalcPeriodRange`, `UpCalcPeriodShift`, `UpDigit` | Parámetros de la banda del canal superior. | `5`, `0`, `2` |
| `DnCalcPeriodRange`, `DnCalcPeriodShift`, `DnDigit` | Parámetros de la banda del canal inferior. | `5`, `0`, `2` |
| `SignalBar` | Desplazamiento de barra usado para evaluar señales de ruptura. | `1` |
| `StopLossPoints`, `TakeProfitPoints` | Stop-loss / take-profit en puntos de precio (convertidos con `PriceStep`). | `1000`, `2000` |

Establezca la propiedad `Volume` de la estrategia para controlar el tamaño de posición. Los parámetros de stop-loss y take-profit son opcionales; establézcalos en `0` para deshabilitar cualquier nivel de protección.

## Notas

* La estrategia espera suficiente historial para formar el canal deslizante (aproximadamente `max(shift + 2 × range)` velas). Gestiona automáticamente los buffers internos e ignora las señales hasta que haya suficientes datos disponibles.
* El redondeo de precio se realiza usando dígitos decimales, reflejando el comportamiento de redondeo del indicador MQL.
* La implementación en Python se omite intencionalmente según las instrucciones del proyecto; solo se proporciona la versión en C#.
