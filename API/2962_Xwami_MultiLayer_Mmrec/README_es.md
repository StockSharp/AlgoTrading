# Estrategia XWAMI Multi-Capa MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el asesor experto original **Exp_XWAMI_NN3_MMRec.mq5** a StockSharp. Tres capas independientes (A/B/C) ejecutan el indicador de momento XWAMI en diferentes marcos temporales y combinan sus señales dentro de una única posición neteada. Cada capa emula el MagicNumber correspondiente de la versión MetaTrader, incluyendo su contador de gestión de dinero y los niveles de protección.

## Lógica de trading

* Para cada capa se calcula una serie de momento como `precio - precio[iPeriod]` usando el precio aplicado seleccionado. La diferencia se pasa a través de cuatro suavizadores secuenciales (métodos y longitudes configurables) para obtener las líneas "up" y "down" del indicador XWAMI.
* Las señales se evalúan en el desplazamiento `SignalBar`. Cuando la barra anterior tenía `up > down`, los cortos de esa capa se cierran y se permite una entrada larga si la barra más reciente muestra `up <= down`. Cuando la barra anterior tenía `up < down`, los largos se cierran y se permite una entrada corta cuando `up >= down`.
* Antes de abrir en una nueva dirección, la estrategia aplana todas las posiciones opuestas de otras capas para respetar el modelo de neteo de StockSharp. Esto refleja el comportamiento de cerrar una operación de magic-number opuesto en el código MQL.
* Los niveles opcionales de stop-loss y take-profit (expresados en puntos de precio) se verifican en cada vela completada usando el máximo/mínimo de la vela. Si se alcanzan, fuerzan una salida inmediata de esa capa.

## Contador de gestión de dinero

Cada capa mantiene un historial continuo de sus operaciones más recientes. Cada vez que el número de pérdidas dentro del período de retroceso configurado alcanza el *LossTrigger*, el tamaño de posición cambia del volumen normal al volumen reducido ("Small"). Las operaciones exitosas o conteos de pérdidas menores revierten al tamaño normal. Las direcciones de compra y venta mantienen sus propios contadores, exactamente como en el asistente MMRec original.

## Parámetros

La estrategia expone el conjunto completo de parámetros del experto MQL:

* `Layer?CandleType` – tipo de vela (marco temporal) usado por la capa (predeterminados: A=8h, B=4h, C=1h).
* `Layer?Period` – retardo usado para construir la serie de momento.
* `Layer?Method1..4`, `Layer?Length1..4`, `Layer?Phase1..4` – configuración de suavizado para las cuatro etapas XWAMI.
* `Layer?AppliedPrice` – fórmula de precio aplicado (cierre, apertura, ponderado, Demark, etc.).
* `Layer?SignalBar` – desplazamiento de la barra de señal (0 = actual, 1 = última barra cerrada, predeterminado 1).
* `Layer?AllowBuy/SellOpen` y `Layer?AllowBuy/SellClose` – permisos para entradas y salidas.
* `Layer?NormalVolume`, `Layer?SmallVolume` – tamaño de operación en lotes (o unidades) para modos normal y reducido.
* `Layer?BuyTotalTrigger`, `Layer?BuyLossTrigger`, `Layer?SellTotalTrigger`, `Layer?SellLossTrigger` – contadores MMRec que controlan el cambio al volumen reducido.
* `Layer?StopLossPoints`, `Layer?TakeProfitPoints` – niveles de protección en puntos de precio (0 desactiva el nivel).

## Notas

* La versión StockSharp usa una única posición neta. Cuando dos capas discrepan, las posiciones opuestas se cierran antes de entrar en la nueva, preservando el orden previsto de señales mientras se evita el hedging.
* La etapa Tillson T3 está implementada directamente en C# para mantener paridad con el algoritmo de suavizado original. Otros modos de suavizado se mapean a los indicadores integrados de StockSharp (SMA, EMA, SMMA/RMA, LWMA, Jurik).
* Dado que las consultas de historial de operaciones difieren entre plataformas, la lógica MMRec rastrea operaciones completadas dentro de la estrategia y reproduce los mismos umbrales sin escanear el historial del terminal.
