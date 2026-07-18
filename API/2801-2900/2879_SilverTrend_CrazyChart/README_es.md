# Estrategia SilverTrend CrazyChart
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el experto de MetaTrader "Exp_SilverTrend_CrazyChart" usando la API de alto nivel de StockSharp. Opera en ambos lados del mercado comparando dos buffers del indicador personalizado SilverTrend CrazyChart. Cuando la banda retrasada cruza la banda actual, abre una posición en la dirección de la banda dominante y cierra cualquier exposición opuesta.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La barra de señal finalizada anterior muestra la banda actual por encima de la banda retrasada, y en la barra evaluada la banda actual cae por debajo o toca la banda retrasada. Las entradas largas pueden deshabilitarse con `AllowBuyEntry`.
  - **Corto**: La barra de señal finalizada anterior muestra la banda actual por debajo de la banda retrasada, y en la barra evaluada la banda actual sube por encima o toca la banda retrasada. Las entradas cortas pueden deshabilitarse con `AllowSellEntry`.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**:
  - Las posiciones largas se cierran cuando la banda retrasada supera a la banda actual (`AllowBuyExit`) o cuando se activan los límites de stop-loss/take-profit.
  - Las posiciones cortas se cierran cuando la banda actual supera a la banda retrasada (`AllowSellExit`) o cuando se activan los límites de stop-loss/take-profit.
- **Stops**: Utiliza desplazamientos de precio absolutos especificados por `StopLossPoints` y `TakeProfitPoints`. Si cualquiera de los valores es cero, ese límite se ignora.
- **Filtros**:
  - `SignalBar` selecciona cuántas velas completadas hacia atrás se evalúa la lógica de cruce.
  - `CandleType` controla el marco temporal utilizado para todos los cálculos.

## Parámetros

- `CandleType` – Serie de velas usada para el indicador (predeterminado: velas de 1 hora).
- `Length` – Período de oscilación (`SSP`) pasado al indicador SilverTrend CrazyChart.
- `KMin` – Coeficiente de canal inferior que controla la distancia de la banda retrasada.
- `KMax` – Coeficiente de canal superior que controla la distancia de la banda actual.
- `SignalBar` – Número de velas completadas hacia atrás usadas para evaluar el cruce (equivalente al `SignalBar` original).
- `AllowBuyEntry` / `AllowSellEntry` – Activar/desactivar entradas largas/cortas.
- `AllowBuyExit` / `AllowSellExit` – Activar/desactivar el cierre de posiciones largas/cortas existentes.
- `StopLossPoints` – Distancia absoluta de precio desde la entrada para el stop-loss largo y el take-profit corto.
- `TakeProfitPoints` – Distancia absoluta de precio desde la entrada para el take-profit largo y el stop-loss corto.
- `Volume` – Volumen de estrategia heredado que define el tamaño base de la orden.

## Lógica del indicador

El `SilverTrendCrazyChartIndicator` incluido reproduce los buffers MQL originales:

- `Length`, `KMin` y `KMax` calculan un canal de oscilación desde el máximo más alto y el mínimo más bajo sobre la ventana de retrospectiva.
- La banda "actual" corresponde al buffer 0 en MetaTrader y reacciona inmediatamente a la última barra.
- La banda "retrasada" es el buffer 1, que desplaza la banda actual `Length + 1` barras para coincidir con la lógica de dibujo original.

Se activa una compra cuando la banda retrasada, actuando como filtro de tendencia, cruza por encima de la banda actual, mientras que una venta aparece cuando la relación se invierte. El parámetro `SignalBar` garantiza que solo las velas completadas participen en la decisión, igualando el comportamiento del experto original.
