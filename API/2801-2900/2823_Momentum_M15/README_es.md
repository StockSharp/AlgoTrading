# Estrategia Momentum M15
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una traducción directa del asesor experto **Momentum-M15** de MetaTrader 5 (archivo original `Momentum-M15.mq5`).
Opera con velas de 15 minutos y combina un filtro de media móvil desplazada con un oscilador momentum evaluado en
las aperturas de barras. La lógica apunta a operar en contra de momentum extremo cuando el precio se encuentra en el lado opuesto de la media desplazada, mientras que un guardián de brechas y un trailing stop opcional limitan la exposición.

## Aspectos destacados de la conversión

* Los indicadores se recrean con componentes StockSharp: una media móvil configurable (por defecto suavizada) y el oscilador
  `Momentum` integrado que trabaja con el precio de vela elegido (por defecto `Open`).
* El desplazamiento horizontal de la MA de MetaTrader se emula almacenando en búfer los valores del indicador y recuperando el valor `MaShift`
  barras terminadas hacia atrás. No se reimplementa matemática de indicador personalizada.
* Las verificaciones de monotonicidad del Momentum reutilizan los últimos valores del historial y mantienen solo los elementos necesarios para las ventanas de entrada
  o salida, reflejando los ayudantes originales `CheckMO_Up` / `CheckMO_Down`.
* El bloqueo por brecha grande (`GapLevel`/`GapTimeout`) se preserva. La información del paso de precio se usa para convertir los umbrales basados en puntos
  definidos en la versión MQL en pasos de precio de StockSharp.
* La gestión del trailing stop se maneja internamente mediante salidas de mercado cuando el precio cruza el nivel rastreado, emulando
  la rutina MQL que modificaba las órdenes de stop loss una vez por barra completada.

## Parámetros

| Nombre | Descripción | Valor predeterminado |
| --- | --- | --- |
| `TradeVolume` | Tamaño de orden usado para cada entrada. | `0.1` |
| `CandleType` | Marco temporal principal (velas de 15 minutos por defecto). | `15m` |
| `MaPeriod` | Longitud de búsqueda de la media móvil. | `26` |
| `MaShift` | Número de barras para desplazar horizontalmente la media móvil. | `8` |
| `MaMethod` | Tipo de media móvil (`Simple`, `Exponential`, `Smoothed`, `Weighted`). | `Smoothed` |
| `MaPrice` | Precio de vela alimentado a la media móvil. | `Low` |
| `MomentumPeriod` | Longitud de búsqueda del Momentum. | `23` |
| `MomentumPrice` | Precio de vela usado para el oscilador Momentum. | `Open` |
| `MomentumThreshold` | Nivel de Momentum base que separa configuraciones largas/cortas. | `100` |
| `MomentumShift` | Valor añadido/restado a `MomentumThreshold` para construir límites asimétricos. | `-0.2` |
| `MomentumOpenLength` | Barras requeridas para una secuencia de Momentum no creciente antes de abrir largos / no decreciente para cortos. | `6` |
| `MomentumCloseLength` | Barras requeridas para la misma secuencia monótona antes de cerrar posiciones. | `10` |
| `GapLevel` | Brecha positiva mínima (en pasos de precio) que pausa nuevas entradas. | `30` |
| `GapTimeout` | Número de barras para mantener el trading deshabilitado después de una brecha grande. | `100` |
| `TrailingStop` | Distancia opcional del trailing stop medida en pasos de precio. | `0` (deshabilitado) |

## Reglas de trading

### Criterios de entrada

* **Entradas largas**
  * El último Momentum está por debajo de `MomentumThreshold + MomentumShift` (para el desplazamiento por defecto de `-0.2`, esto está ligeramente
    por debajo del umbral principal).
  * Tanto el cierre de la barra anterior como la apertura de la barra actual están **por debajo** de la media móvil desplazada.
  * El Momentum ha sido no creciente durante `MomentumOpenLength` barras (coincidiendo con `CheckMO_Down` en la fuente MQL).

* **Entradas cortas**
  * El último Momentum está por encima de `MomentumThreshold - MomentumShift` (con el desplazamiento por defecto esto es ligeramente por encima de 100).
  * Tanto el cierre de la barra anterior como la apertura de la barra actual están **por encima** de la media móvil desplazada.
  * El Momentum ha sido no decreciente durante `MomentumOpenLength` barras (coincidiendo con `CheckMO_Up`).

Las entradas solo se evalúan cuando no hay posición abierta y el trading no está suspendido por el filtro de brechas.

### Criterios de salida

* Las **posiciones largas** se cierran cuando se cumple alguna de las siguientes condiciones:
  * El Momentum ha sido no creciente durante `MomentumCloseLength` barras.
  * El cierre de la barra anterior cae por debajo de la media móvil desplazada.
  * El trailing stop (si está habilitado) es tocado. El stop sigue el mínimo de la vela menos la distancia configurada expresada en
    pasos de precio.

* Las **posiciones cortas** se cierran cuando se cumple alguna de las siguientes condiciones:
  * El Momentum ha sido no decreciente durante `MomentumCloseLength` barras.
  * El cierre de la barra anterior sube por encima de la media móvil desplazada.
  * El trailing stop (si está habilitado) es tocado. El stop sigue el máximo de la vela más la distancia configurada.

### Lógica de suspensión por brecha

El asesor experto original pausaba el trading después de brechas alcistas fuertes. La versión StockSharp mide la diferencia
entre la apertura de la barra actual y el cierre anterior en pasos de precio:

1. Cuando la brecha excede `GapLevel`, el temporizador de bloqueo se reinicia a `GapTimeout`.
2. El temporizador se decrementa en cada barra cerrada; el trading se reanuda solo después de que llega a cero.

## Notas y suposiciones

* Todos los cálculos usan velas terminadas (`CandleStates.Finished`) para mantenerse alineados con las prácticas de la API de alto nivel de StockSharp.
  Como resultado, las órdenes se emiten en la siguiente barra después de observar las condiciones, lo que es consistente con cómo
  la estrategia original se activaba en el primer tick de una nueva barra.
* El concepto de "pips" de MetaTrader se aproxima mediante `Security.PriceStep`. Si el instrumento carece de datos de paso adecuados,
  el filtro de brechas y el trailing stop se deshabilitarán silenciosamente.
* Los precios de la media móvil y las entradas del Momentum pueden cambiarse de forma independiente, replicando la flexibilidad de los
  parámetros de entrada originales.
* No se registran órdenes de stop automatizadas; en su lugar, las salidas de mercado reproducen los ajustes de stop que el código MQL emitía
  mediante `PositionModify`.

## Consejos de uso

1. Asigne el instrumento deseado y asegúrese de que `CandleType` coincida con el marco temporal histórico usado durante las pruebas retrospectivas (barras de 15
   minutos en el script original).
2. Configure `TradeVolume` para el tamaño de lote admitido por el lugar de trading.
3. Ajuste `MomentumOpenLength` / `MomentumCloseLength` para controlar qué tan estricto debe ser el filtro de monotonicidad del Momentum.
4. Si prefiere reflejar exactamente la escala de "pip" por defecto, configure `TrailingStop` y `GapLevel` según la relación
   entre el paso de precio del exchange y un pip para el instrumento.
