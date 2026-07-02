# BandOsMaEstrategia personalizada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es un puerto directo del asesor experto MetaTrader 5 ubicado en
`MQL/45596/mql5/Experts/MQL5Book/p7/BandOsMACustom.mq5`. El robot original
combina el histograma MACD (también conocido como OsMA) con Bollinger bandas y un
media móvil que se aplica a los valores del histograma en lugar de a los precios brutos.
Siempre que el histograma atraviesa la banda inferior, el experto abre una operación larga,
mientras que los toques de la banda superior desencadenan entradas cortas. El histograma que cruza un
La media móvil separada cierra la posición. Un tope protector y un
El paso del trailing stop (igual a una quincuagésima parte del stop) mantiene el riesgo bajo control.

La implementación StockSharp conserva este comportamiento utilizando el nivel alto API,
por lo que la lógica comercial sigue siendo legible y depurable dentro del marco.

## Aspectos destacados de la conversión

* El histograma MACD se implementa mediante
`MovingAverageConvergenceDivergenceHistogram`, alimentado con el precio de la vela que
corresponde al modo MetaTrader `PRICE_*` seleccionado por el `AppliedPrice`
parámetro.
* Bollinger Las bandas y la media móvil de salida procesan la salida de OsMA en lugar de
que los datos de precios. Un búfer de historial compacto reproduce el MetaTrader `shift`
argumentos a favor de ambos indicadores.
* La estrategia mantiene la señalización original largo/corto: cruces por debajo del
los cortos iniciales de la banda inferior, los cruces por encima de los cortos iniciales de la banda superior y el
OsMA cruza su media móvil y cierra la operación.
* `StartProtection` refleja el bloque de stop-loss más MetaTrader trailing-stop.
El paso final se calcula como `StopLossPoints / 50`, al igual que MQL
la clase `TrailingStop` lo hizo.

## Indicadores

| Indicador | Propósito |
| --- | --- |
| `MovingAverageConvergenceDivergenceHistogram` | Recrea la salida `iOsMA` de MetaTrader. |
| `BollingerBands` | Calcula los umbrales superior e inferior sobre el histograma. |
| Media móvil (SMA/EMA/SMMA/LWMA) | El filtro sale cuando el histograma lo cruza. |

## Parámetros

| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `CandleType` | plazo de 1 hora | Plazo principal utilizado para todos los cálculos de indicadores. |
| `FastOsmaPeriod` | 12 | Longitud rápida EMA del cálculo de OsMA. |
| `SlowOsmaPeriod` | 26 | Longitud lenta de EMA a partir del cálculo de OsMA. |
| `SignalPeriod` | 9 | Longitud de la señal SMA del cálculo de OsMA. |
| `AppliedPrice` | Típico | Precio aplicado estilo MetaTrader que alimenta el histograma. |
| `BandsPeriod` | 26 | Longitud de las Bollinger Bandas dibujadas en los valores del histograma. |
| `BandsShift` | 0 | Desplazamiento a la derecha (en barras) aplicado a los valores Bollinger. |
| `BandsDeviation` | 2.0 | Multiplicador de desviación estándar para las bandas. |
| `MaPeriod` | 10 | Longitud de la media móvil de salida calculada en el histograma. |
| `MaShift` | 0 | Desplazamiento a la derecha (en barras) aplicado a la media móvil de salida. |
| `MaMethod` | Sencillo | Método de media móvil (SMA, EMA, SMMA, LWMA). |
| `StopLossPoints` | 1000 | Distancia de parada protectora expresada en incrementos de precio. |
| `OrderVolume` | 0,01 | Volumen de operaciones, idéntico a la entrada MetaTrader “Lotes”. |

## Reglas comerciales

1. Suscríbase a la serie de velas seleccionada y proporcione el precio aplicado elegido
en el histograma MACD.
2. Pase cada valor de histograma a las Bollinger Bandas y la media móvil de salida.
3. Detectar señales utilizando los buffers desplazados:
   * Si el histograma cae por la banda inferior, establece una señal alcista.
   * Si el histograma sube por la banda superior, establece una señal bajista.
   * Cuando el histograma cruza la media móvil de salida, borre el activo
señal, que permite cerrar la posición.
4. Gestionar posiciones:
   * Cierre las posiciones largas existentes cada vez que desaparezca la señal alcista; cerrar pantalones cortos
cuando la señal bajista desaparece.
   * Abrir en largo cuando la señal alcista está activa y no hay apertura
posición; abrir un corto cuando la señal bajista está activa y la posición es
plano.
5. Aplique `StartProtection` con la distancia de stop-loss configurada y un seguimiento
paso igual a `StopLossPoints / 50` pasos de precio.

## Notas

* Todos los comentarios en el código fuente están en inglés para cumplir con el repositorio.
directrices.
* Los buffers de historial garantizan que la versión StockSharp respeta MetaTrader
`BandsShift` y `MaShift` parámetros sin solicitar valores de indicador por
índice.
* La estrategia se alinea con las convenciones API de alto nivel: `SubscribeCandles`
impulsa actualizaciones de indicadores y dirige llamadas a `BuyMarket`/`SellMarket` imitador
la colocación de la orden del experto original.
