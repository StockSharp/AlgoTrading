# Estrategia Profit Hunter HSI with Fibonacci Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una adaptación de C# del MetaTrader 4 asesor experto `Profit_Hunter_HSI_with_fibonacci.mq4`. The original script combines
un filtro de promedio móvil exponencial intradiario (EMA) con Fibonacci zonas de retroceso derivadas del gráfico diario. El StockSharp
La implementación sigue la misma idea usando el API de alto nivel: se suscribe a dos flujos de velas (intradiario y diario), calcula
la cuadrícula Fibonacci dinámicamente, genera señales comerciales cuando el precio interactúa con esas bandas y gestiona la posición resultante
con colocación de parada adaptativa y una lógica de parada dinámica escalonada.

## Flujo de datos de mercado
1. **Velas intradiarias**: el parámetro `TimeFrame` define la resolución de trabajo (predeterminado: 1 minuto). Cada vela terminada se alimenta
el filtro de tendencia EMA, actualiza la referencia de soporte/resistencia más reciente tomada hace `NumBars` barras y activa la negociación
lógica.
2. **Velas diarias**: una suscripción dedicada recopila datos de períodos de tiempo más altos. Dos índices configurables por el usuario marcan el máximo del swing
y columpio bajo utilizado como anclas para la grilla Fibonacci. Cada vez que llega una nueva vela diaria, toda la escalera de retroceso se
recalculado, incluidas las prórrogas (161,8%, 261,8%, 423,6%).

## Generación de señal
El asesor MQL almacenó el último swing alto/bajo descubierto y determinó cuál ocurrió primero (`highFirst`). El puerto mantiene el
same concept by comparing the day indices:
- Si el máximo seleccionado es más reciente que el mínimo seleccionado (`highFirst = true`), el mercado se trata como descendente y el
Los niveles de Fibonacci se miden hacia arriba desde el mínimo.
- De lo contrario, el movimiento se considera ascendente y la cuadrícula se proyecta hacia abajo desde lo alto.

Para cada vela intradiaria completada, las siguientes reglas reflejan la EA original:
1. **Filtro de tendencias**: un EMA con período `MaPeriod` clasifica el sesgo a corto plazo. If the close price (treated as both bid and ask)
is above the EMA the trend is "Naik" (up); if it is below, the trend is "Turun" (down). When the price hovers exactly around the
EMA no trade will be opened.
2. **Fibonacci señal** – dependiendo de `highFirst`, la interacción del precio con los niveles de 23,6%, 76,4%, 91% y 14,6% produce una de
cuatro señales de cadena del código MT4: `Reverse-Buy`, `Reverse-Sell`, `Trading-Area` o `Continuation`. Sólo los tres primeros son
utilizado para entradas reales, el último simplemente informa una continuación de la tendencia.
3. **Reglas de entrada**: el guión original contenía seis ramas de entrada. They are reproduced verbatim:
   - Tendencia alcista + zona de negociación + ruptura por encima de la resistencia de referencia → comprar con el stop protector en el soporte de referencia.
   - Tendencia alcista + venta inversa + `highFirst == false` + precio aún por debajo de la resistencia → abrir una venta corta con el stop en el nivel del 14,6%.
   - Tendencia alcista + compra inversa + `highFirst == false` + precio por debajo de la resistencia → comprar con el stop en el nivel del 91%.
   - Tendencia bajista + zona de negociación + ruptura bajo soporte → vender con stop en la línea de resistencia.
   - Tendencia bajista + venta inversa + `highFirst == true` + precio por debajo de la resistencia → vender con el stop en el nivel del 91%.
   - Tendencia bajista + compra inversa + `highFirst == true` + precio por debajo de la resistencia → comprar con el stop en el nivel del 14,6%.
Only one position may exist at a time; Las órdenes activas no se acumulan.

## Gestión de Puestos
- **Salidas de soporte/resistencia**: como en EA, una posición larga se liquida si el precio vuelve a caer hasta la referencia de soporte mientras
El corto se cierra cuando el precio sube hasta la referencia de resistencia, independientemente del beneficio actual.
- **Parada de protección inicial**: el nivel de parada calculado durante la decisión de entrada se almacena internamente y se utiliza como activador de salida.
La versión StockSharp realiza la misma verificación en cada vela en lugar de modificar las órdenes del corredor directamente.
- **Parada móvil escalonada**: el script MQL elevó el nivel de parada cada 20 puntos después de un movimiento inicial de 60 puntos (por ejemplo, +60 → parada
a +55, +80 → detener a +75, … hasta +260). El puerto mantiene la escalera exacta usando el instrumento `PriceStep` para convertir puntos en
compensaciones de precios. Para operaciones cortas, el stop se desliza hacia abajo para bloquear las ganancias, garantizando la misma distancia que el original.

## Parámetros
| Nombre | Descripción | Predeterminado | Notas |
| --- | --- | --- | --- |
| `NumBars` | Desplazamiento de la vela cuyo máximo/mínimo se convierte en resistencia/soporte temporal. | `3` | Coincide con la entrada externa `numBars`; debe ser mayor que cero. |
| `MaPeriod` | Período del EMA utilizado para la clasificación de tendencias. | `5` | Equivalent to `maPeriod` in the EA. |
| `TimeFrame` | Plazo de velas intradiarias. | `1 minute` | Mirrors the `timeFrame` extern; acepta cualquier `TimeSpan`. |
| `DaysBackForHigh` | Índice de la vela diaria que proporciona el máximo de oscilación. | `1` | Corresponde a `daysBackForHigh`. |
| `DaysBackForLow` | Índice de la vela diaria que proporciona el mínimo. | `1` | Corresponde a `daysBackForLow`. |
| `Volume` | Tamaño de la orden de mercado. | `1` | Representa lotes/acciones; validated to stay positive. |

## Notas de implementación
- El EA original creó numerosos objetos gráficos. Esas llamadas se omiten intencionalmente porque StockSharp maneja los gráficos
por separado y las formas eran puramente cosméticas.
- En lugar de consultar buffers históricos como `iLow` y `iHigh`, el puerto mantiene dos listas en memoria de velas terminadas y
lee el turno requerido directamente desde allí.
- La gestión de paradas se implementa en el código de estrategia (`ManagePosition`) en lugar de a través de `OrderModify`, lo que mantiene al agente de comportamiento
agnóstico conservando el mismo árbol de decisión.
- Los rechazos de pedidos borran el estado de entrada pendiente para que los ajustes manuales no dejen indicadores internos obsoletos, coincidiendo con la defensiva
codificación presente en muchas estrategias API existentes.

## Diferencias con la versión MetaTrader
- MetaTrader asumió acceso al nivel de tick `Ask` y `Bid`. StockSharp opera con el cierre de velas de forma predeterminada; the close price is used
como proxy de oferta y demanda, lo cual es suficiente para replicar la lógica de decisión.
- La noción de "qué extremo apareció primero" no puede basarse en la serie `High[]`/`Low[]` de MT4. El puerto lo aproxima comparando
los índices del día seleccionado, entregando resultados idénticos para la configuración predeterminada y preservando el comportamiento previsto para
otras configuraciones.
- Las órdenes de stop y toma de ganancias del corredor se reemplazan con salidas virtuales evaluadas por vela. This avoids connector-specific order
tipos, garantizando al mismo tiempo que se cumplan las mismas condiciones de salida.
