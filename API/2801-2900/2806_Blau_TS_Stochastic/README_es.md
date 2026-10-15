# Estrategia Blau TS Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port de StockSharp del expert advisor de MetaTrader "Exp_BlauTSStochastic". El sistema opera con el oscilador estocástico de triple suavizado de William Blau que venía incluido con el paquete MQL original. El indicador calcula los precios máximos y mínimos durante una ventana de retroceso configurable, suaviza el numerador y denominador estocástico tres veces con la familia de media móvil seleccionada, reescala el resultado al rango [-100, 100], y finalmente produce una línea de señal suavizada. Todos los cálculos se realizan en velas terminadas que se entregan a través de la API de suscripción de velas de alto nivel.

El indicador puede construirse a partir de cualquiera de los precios aplicados soportados (cierre, apertura, máximo, mínimo, mediana, típico, ponderado, simple, cuartil, dos variantes de seguimiento de tendencia, o DeMark) y cuatro algoritmos de suavizado diferentes (SMA, EMA, SMMA/RMA, WMA). El ajuste `SignalBar` permite reproducir el desplazamiento de barra usado por el expert advisor original: la estrategia evalúa señales sobre datos que tienen `SignalBar` barras de antigüedad, por lo que con el valor predeterminado de 1 reacciona a la barra que acaba de cerrarse en el paso anterior.

## Reglas de entrada y salida

Están disponibles tres modos de trading. En cada modo, los interruptores booleanos `EnableLongEntry`, `EnableShortEntry`, `EnableLongExit` y `EnableShortExit` controlan si las acciones respectivas están permitidas.

### Modo Breakdown

*Entrada larga*: el valor anterior del histograma (desplazamiento `SignalBar+1`) está por encima de cero y el valor más reciente (desplazamiento `SignalBar`) está en o por debajo de cero. Esto refleja la condición original de "el histograma rompe a través de cero" y abre o voltea una posición larga mientras también cubre cualquier corto.

*Entrada corta*: el valor anterior del histograma está por debajo de cero y el valor más reciente está en o por encima de cero, señalando una ruptura de la línea cero en la dirección opuesta. La estrategia abre o voltea a una posición corta y opcionalmente cierra la exposición larga.

Las mismas condiciones también desencadenan salidas en el lado opuesto: cuando el histograma pasa la barra anterior por encima de cero, la estrategia cierra cortos, y cuando pasa la barra anterior por debajo de cero, cierra largos.

### Modo Twist

*Entrada larga*: el histograma forma un fondo local. Concretamente, el valor en el desplazamiento `SignalBar+1` está por debajo del valor en el desplazamiento `SignalBar+2`, pero el valor en el desplazamiento `SignalBar` gira hacia arriba y supera la barra intermedia. Eso reproduce el modo de "cambio de dirección" del expert advisor.

*Entrada corta*: el histograma forma una cima local. El valor en el desplazamiento `SignalBar+1` es mayor que el valor en el desplazamiento `SignalBar+2`, y el valor más reciente cae por debajo de la barra intermedia. Las posiciones en la dirección opuesta se cierran cuando ocurre un giro en su contra.

### Modo CloudTwist

Este modo sigue los cambios de color de la nube del indicador que está definida por el histograma y su línea de señal.

*Entrada larga*: el histograma estaba por encima de la línea de señal en la barra anterior pero el valor más reciente cruzó por debajo o tocó la línea de señal. La estrategia trata el cambio de color de la nube como una señal alcista y opcionalmente cubre cortos.

*Entrada corta*: el histograma estaba por debajo de la línea de señal en la barra anterior pero el valor más reciente cruzó por encima o tocó la línea de señal. Esto voltea a una posición corta y opcionalmente sale de largos.

## Gestión de riesgos

* `StopLossPoints` y `TakeProfitPoints` se miden en pasos de precio del instrumento. Si cualquier valor es mayor que cero, la estrategia habilita el bloque de protección incorporado de StockSharp con órdenes de mercado, por lo que los stops siguen la posición activa automáticamente.
* El tamaño de la orden se toma de la propiedad `Volume` de la estrategia. Cuando aparece una señal de reversión, la estrategia envía `Volume + |Position|` contratos, asegurando que la posición existente se cierre antes de abrir una nueva.

## Parámetros

* `CandleType` – marco temporal (tipo de datos) usado para el oscilador (por defecto: velas de 4 horas).
* `Mode` – algoritmo de detección de señales: `Breakdown`, `Twist` o `CloudTwist`.
* `AppliedPrice` – fuente de precio para el cálculo estocástico (cierre, apertura, máximo, mínimo, mediana, típico, ponderado, simple, cuartil, seguimiento de tendencia 0/1, o DeMark).
* `Smoothing` – familia de media móvil usada para todas las etapas de suavizado (`Simple`, `Exponential`, `Smoothed`, `Weighted`).
* `BaseLength` – número de barras usadas para calcular el rango máximo/mínimo.
* `SmoothLength1`, `SmoothLength2`, `SmoothLength3` – longitudes de suavizado para el numerador y denominador (aplicadas secuencialmente).
* `SignalLength` – longitud de suavizado para la línea de señal del histograma.
* `SignalBar` – desplazamiento de barra que define qué valores históricos se usan para las decisiones.
* `StopLossPoints`, `TakeProfitPoints` – tamaño de stop protector y objetivo en pasos de precio (0 deshabilita la orden correspondiente).
* `EnableLongEntry`, `EnableShortEntry`, `EnableLongExit`, `EnableShortExit` – interruptores de permiso para las cuatro acciones básicas.

Establezca el `Volume` deseado, adjunte la estrategia a un instrumento y ejecútela. Todos los cálculos dependen de velas terminadas, por lo que el sistema espera hasta que los indicadores estén formados antes de permitir operaciones.
