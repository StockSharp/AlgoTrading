# Estrategia BrainTrend2 + AbsolutelyNoLagLWMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia combina dos módulos independientes que fueron implementados originalmente en MetaTrader 5: BrainTrend2_V2 y AbsolutelyNoLagLWMA. Cada módulo analiza su propia suscripción de velas y decide cuándo ir largo, ir corto o volver a plano. El port en C# mantiene ambos flujos de decisión intactos y agrega su exposición deseada en una única estrategia StockSharp.

* **Módulo BrainTrend2.** Usa un estado de color de seguimiento de tendencia generado por el indicador BrainTrend2. El estado se deriva de un canal basado en ATR que cambia cuando el precio viola el límite opuesto.
* **Módulo AbsolutelyNoLagLWMA.** Rastrea la pendiente de una media móvil ponderada lineal doblemente suavizada calculada en un precio aplicado seleccionable.

Cuando uno de los módulos solicita una nueva dirección de posición, la estrategia recalcula el volumen objetivo combinado y envía órdenes de mercado para alcanzar esa exposición. La configuración predeterminada opera en velas H4 para ambos indicadores, pero cada módulo puede suscribirse a un marco temporal diferente.

## Indicadores
### BrainTrend2
El indicador BrainTrend2 reconstruye la superposición de velas de cinco colores del archivo MQL original:
* Una serie de rango verdadero ponderado triangularmente (parámetro de período) se escala por un coeficiente de 0.7 para formar una banda dinámica (`widcha`).
* Un nivel de referencia flotante (`Emaxtra`) sigue los extremos de precio dentro del régimen actual.
* Cuando el mínimo cae por debajo de `Emaxtra - widcha` el régimen cambia a bajista. Cuando el máximo supera `Emaxtra + widcha` el régimen cambia a alcista.
* El régimen resultante determina el color: lima/teal (valores 0 o 1) para contextos alcistas, marrón/magenta (valores 3 o 4) para contextos bajistas, gris (valor 2) antes de que el indicador esté listo.

El indicador en C# mantiene la misma mecánica, incluyendo la estimación triangular de ATR, para que los colores generados coincidan con la referencia MQL.

### AbsolutelyNoLagLWMA
El módulo AbsolutelyNoLagLWMA aplica dos medias móviles ponderadas linealmente consecutivas a la serie de precios seleccionada. La pendiente de la línea resultante impulsa los valores de color:
* **2 (azul)** – la línea está subiendo.
* **1 (gris)** – la línea está plana.
* **0 (violeta)** – la línea está cayendo.

Ambos indicadores exponen `IsFormed` para que la estrategia espere hasta que haya suficiente historial disponible antes de reaccionar a los colores.

## Lógica de Trading
La estrategia mantiene dos objetivos internos, `_brainTrendTarget` y `_lwmaTarget`, que representan el volumen deseado para cada módulo. Cada vez que uno de los módulos cambia su objetivo, la estrategia llama a `RebalancePosition` para ajustar la posición agregada a `_brainTrendTarget + _lwmaTarget`.

### Módulo BrainTrend2
* Evalúa el color desde la vela `SignalBar` períodos atrás (predeterminado 1) y el color precedente para detectar transiciones de estado.
* Cuando el color actual es alcista (`< 2`) y el color anterior no era alcista (`> 1`), el módulo:
  * Cierra cualquier exposición corta creada por este módulo.
  * Abre una posición larga con `BrainTrendVolume` si las entradas largas están habilitadas.
* Cuando el color actual es bajista (`> 2`) y el color anterior no era bajista (`< 3`), el módulo:
  * Cierra cualquier exposición larga pendiente.
  * Abre una posición corta con `BrainTrendVolume` si las entradas cortas están habilitadas.

### Módulo AbsolutelyNoLagLWMA
* Usa la misma lógica `SignalBar` pero reacciona a los valores de color 2 (arriba) y 0 (abajo).
* Cuando el color se convierte en **2** y el color anterior era diferente:
  * Cierre opcional de exposición corta (`LwmaCloseShortAllowed`).
  * Apertura opcional de una posición larga con `LwmaVolume` si `LwmaBuyAllowed` es verdadero.
* Cuando el color se convierte en **0** y el color anterior era diferente:
  * Cierre opcional de exposición larga (`LwmaCloseLongAllowed`).
  * Apertura opcional de una posición corta con `LwmaVolume` si `LwmaSellAllowed` es verdadero.

Cada módulo solo modifica su propio volumen objetivo, por lo que ambos pueden estar activos al mismo tiempo. Por ejemplo, el módulo BrainTrend2 puede mantenerse largo mientras el módulo LWMA realiza scalps cortos alrededor de la posición central.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `BrainTrendAtrPeriod` | Período del ATR triangular usado por BrainTrend2. |
| `BrainTrendSignalBar` | Número de velas terminadas usadas para desplazar señales BrainTrend2. `1` significa que la estrategia espera que la barra anterior cierre. |
| `BrainTrendBuyAllowed` / `BrainTrendSellAllowed` | Habilitar o deshabilitar entradas largas/cortas para el módulo BrainTrend2. |
| `BrainTrendVolume` | Volumen colocado por el módulo BrainTrend2 al entrar en una posición. |
| `BrainTrendCandleType` | Tipo de vela (marco temporal) suscrito por el módulo BrainTrend2. |
| `LwmaLength` | Longitud de cada media ponderada en el indicador AbsolutelyNoLagLWMA. |
| `LwmaSignalBar` | Desplazamiento de señal para el módulo LWMA (misma semántica que el módulo BrainTrend). |
| `LwmaAppliedPrice` | Precio aplicado usado para construir el LWMA (cierre, apertura, mediana, Demark, etc.). |
| `LwmaBuyAllowed` / `LwmaSellAllowed` | Habilitar o deshabilitar entradas largas/cortas para el módulo LWMA. |
| `LwmaCloseLongAllowed` / `LwmaCloseShortAllowed` | Permitir que el módulo LWMA cierre la exposición opuesta cuando una señal se invierte. |
| `LwmaVolume` | Volumen enviado por el módulo LWMA cuando abre un trade. |
| `LwmaCandleType` | Tipo de vela (marco temporal) suscrito por el módulo LWMA. |

## Gestión de Posición y Órdenes
* La estrategia siempre usa órdenes de mercado (`BuyMarket` / `SellMarket`) para alcanzar el volumen objetivo agregado.
* Los volúmenes de ambos módulos son aditivos. Por ejemplo, si cada módulo solicita `1` lote en direcciones opuestas, la posición neta se convierte en cero, cubriendo efectivamente la cuenta.
* No se recrea ningún stop-loss o take-profit automático del Asesor Experto original porque esas funciones eran específicas del broker en MQL. El control de riesgo puede agregarse a través de las protecciones StockSharp si es necesario.
* Cuando ambos módulos se suscriben a marcos temporales diferentes, la estrategia se suscribe automáticamente a ambas secuencias de velas y las dibuja en el área del gráfico junto con los fills.

## Notas
* La implementación mantiene los cálculos de indicadores autocontenidos, por lo que no se requieren bibliotecas de indicadores externas.
* `SignalBar = 0` permite reaccionar a la vela terminada más reciente de inmediato, mientras que desplazamientos mayores imponen confirmación adicional.
* BrainTrend2 requiere al menos `AtrPeriod + 2` velas históricas antes de emitir colores válidos; AbsolutelyNoLagLWMA necesita al menos `Length` velas.
* Dado que ambos módulos comparten el mismo `Strategy.Security`, sus trades se reconcilian a través de la misma conexión de portafolio igual que en el Asesor Experto MT5 original que usaba diferentes números mágicos.

## Extensión de la Estrategia
* Agregar protecciones de riesgo de StockSharp (por ejemplo, trailing stops) si se requieren stops fijos de la versión MQL.
* Ajustar `BrainTrendVolume` y `LwmaVolume` de forma independiente para enfatizar el comportamiento de seguimiento de tendencia o de reversión a la media.
* Combinar los módulos con filtros adicionales observando los valores de indicadores proporcionados dentro de `ProcessBrainTrend` y `ProcessLwma`.
