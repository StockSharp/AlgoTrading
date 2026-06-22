# Estrategia SendClose
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
SendClose es una estrategia de ruptura basada en fractales que recrea el comportamiento del asesor experto de MT5 original. El algoritmo construye continuamente líneas dinámicas de soporte y resistencia vinculando pivotes fractales alternantes y reacciona en el momento en que el precio vuelve a esos niveles proyectados. El port de StockSharp mantiene intactas las mecánicas centrales: las líneas de tendencia se generan a partir de secuencias de fractales alternantes arriba/abajo, las rupturas desencadenan entradas de mercado, y se usan líneas de offset separadas para forzar la liquidación de posiciones.

## Flujo de detección de fractales
1. **Ventana de cinco velas** – la estrategia mantiene un buffer rodante de las últimas cinco velas completadas. Tan pronto como la ventana esté llena, evalúa la vela central frente a los dos vecinos más antiguos y los dos más nuevos.
2. **Regla de fractal ascendente** – la vela central forma un fractal ascendente cuando su máximo es mayor que los máximos de las dos velas más nuevas y estrictamente mayor que los máximos de las dos velas más antiguas. Esto coincide con la lógica `iFractals` de MT5 (>= en el lado más nuevo, > en el lado más antiguo).
3. **Regla de fractal descendente** – de manera similar, la vela central es un fractal descendente si su mínimo es menor o igual comparado con las velas más nuevas y estrictamente menor que las dos velas más antiguas.
4. **Cola de fractales** – cada fractal recién confirmado se introduce en una cola FIFO de seis elementos ordenada de más reciente a más antiguo. Esta cola se escanea posteriormente para encontrar los patrones alternantes requeridos.

## Construcción de líneas de tendencia
* **Línea de venta** – el algoritmo busca la secuencia más reciente *fractal ascendente → fractal descendente → fractal ascendente*. La línea se traza a través del primer y último fractal ascendente, conectando efectivamente dos máximos de oscilación separados por un mínimo de oscilación.
* **Línea de compra** – simétricamente, busca una cadena *fractal descendente → fractal ascendente → fractal descendente* y conecta los mínimos de oscilación circundantes.
* **Proyección** – los puntos finales almacenados (tiempo y precio) se usan para interpolar o extrapolar el valor de la línea para cualquier marca de tiempo posterior. Cuando el mercado alcanza la proyección al cierre de la vela actual, se toma una decisión de trading.
* **Líneas de cierre** – se calculan dos niveles auxiliares desplazando la línea de venta hacia arriba y la línea de compra hacia abajo por `LineOffsetSteps * PriceStep`. Actúan como disparadores de salida forzada igual que las líneas Close1/Close2 originales.

## Lógica de trading
* **Condiciones de entrada**
  * Vender cuando el precio toca la línea de venta y no hay exposición larga en conflicto. La exposición corta existente puede aumentarse hasta alcanzar el límite `MaxPositions`.
  * Comprar cuando el precio toca la línea de compra y no hay exposición corta en conflicto. La exposición larga existente puede aumentarse hasta el mismo límite.
* **Condiciones de salida**
  * El precio que toca cualquier línea de cierre cierra inmediatamente la posición abierta, emulando el comportamiento de MT5 donde tocar Close1/Close2 emite una salida completa.
  * Las señales de entrada intentan aplanar posiciones opuestas antes de colocar la nueva orden, reflejando la adaptación de cobertura a neteo dentro de StockSharp.
* **Detección de contacto** – la precisión de tick de MT5 se aproxima con datos de velas. Un nivel se considera "tocado" cuando se encuentra entre el máximo y el mínimo de la vela.

## Parámetros
| Nombre | Descripción |
|--------|-------------|
| `EnableSellLine` | Habilita o deshabilita las órdenes basadas en la línea fractal superior (de venta). |
| `EnableBuyLine` | Habilita o deshabilita las órdenes basadas en la línea fractal inferior (de compra). |
| `EnableCloseSellLine` | Activa el nivel Close1 que cierra posiciones cuando el precio sube por encima de la línea de venta más el offset. |
| `EnableCloseBuyLine` | Activa el nivel Close2 que cierra posiciones cuando el precio cae por debajo de la línea de compra menos el offset. |
| `MaxPositions` | Número máximo de lotes que pueden permanecer abiertos en una dirección. Las entradas adicionales que superen este límite se ignoran. |
| `OrderVolume` | Volumen de cada orden de mercado. El valor debe coincidir con el tamaño del contrato del instrumento. |
| `LineOffsetSteps` | Offset, medido en pasos de precio, usado al calcular los niveles Close1/Close2. El valor predeterminado de 15 replica el desplazamiento `15*Point()` de MT5. |
| `CandleType` | Serie de velas usada para el análisis. Elija un marco temporal que coincida con el gráfico que planea operar (ej., M15, H1). |

## Notas de implementación
* La estrategia se ejecuta en velas completadas para respetar el EA original, que dependía de barras MT5 confirmadas antes de evaluar los fractales.
* La igualdad a nivel de tick con bid/ask se aproxima con rangos de velas. Si se requiere mayor precisión, alimentar datos de tick en lugar de velas.
* El parámetro `MaxPositions` opera sobre la posición neta de StockSharp. Por lo tanto es adecuado para cuentas de neteo; las cuentas de cobertura aún pueden simular el escalado aumentando `MaxPositions`.
* Las líneas de cierre se evalúan antes que las entradas. Si tanto una salida como una entrada se activan en la misma vela, la salida tiene prioridad, evitando órdenes conflictivas.

## Directrices de uso
1. Configure el símbolo y el marco temporal deseados en su terminal StockSharp y asegúrese de que el instrumento proporcione información de `PriceStep`. La lógica de offset depende de ello.
2. Ajuste `CandleType` para que coincida con el marco temporal que desea analizar. El valor predeterminado es 30 minutos, que ofrece un equilibrio entre ruido y capacidad de respuesta.
3. Establezca `OrderVolume` en el tamaño de posición que desea enviar por operación. Para futuros, use recuentos de contratos; para CFDs de FX, use tamaños de lote.
4. Ajuste `LineOffsetSteps` para alinearse con la volatilidad del instrumento. Los offsets más grandes requieren un movimiento más fuerte para activar las salidas Close1/Close2.
5. Monitoree el número de lotes abiertos cuando aumente `MaxPositions`. La estrategia no superará este límite pero puede seguir piramidando posiciones en mercados con tendencia.

## Diferencias con la versión MT5
* StockSharp opera con posiciones netas, por lo que el código aplana la exposición opuesta antes de abrir una nueva operación en lugar de mantener tickets de compra/venta simultáneos.
* Los objetos de gráfico no se dibujan automáticamente. Si necesita visualización en el gráfico, conecte un módulo de gráfico y trace los valores de línea generados manualmente.
* La detección de contacto basada en velas puede activarse ligeramente más tarde que las verificaciones de tick de MT5, especialmente en mercados rápidos con velas amplias.

## Gestión del riesgo
La estrategia coloca órdenes de mercado sin stop-losses integrados. Siempre complémenlela con controles de riesgo externos como stops de capital, filtros de horario de trading o supervisión manual. Haga backtesting extensivo en el instrumento y marco temporal objetivo antes de desplegarla en vivo.
