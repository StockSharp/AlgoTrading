# Estrategia BrainTrend2 + AbsolutelyNoLagLWMA MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia recrea el experto de MetaTrader `Exp_BrainTrend2_AbsolutelyNoLagLwma_MMRec` combinando dos bloques de señales independientes: el motor de seguimiento de tendencia BrainTrend2 y el filtro adaptativo AbsolutelyNoLagLWMA. Cada bloque puede abrir y cerrar operaciones de acuerdo con sus propios permisos, imitando los interruptores de gestión monetaria de la plantilla MMRec original. Las órdenes se ejecutan con la API de alto nivel de StockSharp usando ejecuciones de mercado y el volumen predeterminado configurable.

## Lógica de trading
### Bloque BrainTrend2
* Construye un nivel trailing dinámico basado en un rango verdadero ponderado similar al ATR.
* La dirección (`river`) cambia cuando la vela perfora el buffer de trailing por más de `0.7 * ATR`.
* Las velas alcistas dentro de un river alcista activan entradas largas (si están habilitadas) y cierran posiciones cortas.
* Las velas bajistas dentro de un river bajista activan entradas cortas (si están habilitadas) y cierran posiciones largas.
* Las señales pueden retrasarse mediante el parámetro `Brain Signal Shift` para trabajar con barras anteriores.

### Bloque AbsolutelyNoLagLWMA
* Aplica una media móvil lineal ponderada de dos etapas a la fuente de precio seleccionada.
* Los colores se vuelven **alcistas (2)** cuando la LWMA doble sube, **bajistas (0)** cuando cae y **neutrales (1)** en caso contrario.
* Una transición al color 2 abre largos y opcionalmente cierra cortos; un cambio al color 0 abre cortos y opcionalmente cierra largos.
* Las señales también pueden desplazarse hacia atrás un número de barras definido por el usuario.

### Gestión de posición
* La estrategia opera una única posición neta. Cuando ambos bloques solicitan operaciones en la misma barra, las señales de cierre se ejecutan antes de cualquier nueva entrada.
* Si un bloque quiere abrir una operación pero la posición opuesta está abierta y el permiso de cierre correspondiente está desactivado, la entrada se omite (refleja la imposibilidad de mantener posiciones hedgeadas con una única cartera neta).

## Parámetros
| Grupo | Nombre | Descripción |
| --- | --- | --- |
| BrainTrend2 | Brain Candle | Tipo de vela usado para el indicador BrainTrend2. |
| BrainTrend2 | Brain ATR | Período ATR para los cálculos internos de BrainTrend2. |
| BrainTrend2 | Brain Signal Shift | Número de barras para retrasar las señales de BrainTrend2. |
| BrainTrend2 | Brain Buy / Sell | Permitir a BrainTrend2 abrir operaciones largas/cortas. |
| BrainTrend2 | Brain Close Buys / Close Sells | Permitir a las señales de BrainTrend2 cerrar posiciones existentes. |
| AbsolutelyNoLag | Abs Candle | Tipo de vela usado para el indicador LWMA. |
| AbsolutelyNoLag | Abs Length | Período de la LWMA. |
| AbsolutelyNoLag | Abs Price | Precio aplicado usado para la LWMA. Coincide con el enum `Applied_price_` de MQL. |
| AbsolutelyNoLag | Abs Signal Shift | Número de barras para retrasar las señales de la LWMA. |
| AbsolutelyNoLag | Abs Buy / Sell | Permitir al bloque LWMA abrir operaciones largas/cortas. |
| AbsolutelyNoLag | Abs Close Buys / Close Sells | Permitir al bloque LWMA cerrar posiciones. |
| AbsolutelyNoLag | Abs Shift | Agrega un desplazamiento de precio constante a la salida de la LWMA. |
| General | Order Volume | Volumen de orden de mercado predeterminado. |

## Notas
* Los cálculos de ATR y LWMA siguen las implementaciones MQL originales, incluyendo la ponderación triangular del ATR y la extensa lista de precios aplicados.
* La información de spread no está disponible en las velas de StockSharp, por lo que el rango verdadero usa solo precios de vela. Esto refleja el comportamiento del indicador cuando el spread es igual a cero.
* Las múltiples posiciones simultáneas con diferentes magic numbers se consolidan en una única posición neta, que es el estándar en las estrategias de StockSharp.
