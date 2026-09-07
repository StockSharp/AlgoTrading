# Diagrama de estrategia de acuerdo RSI en tres marcos temporales
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama espera valores RSI formados de velas de cinco, quince y treinta minutos, toma los tres en cada cierre del marco lento y los evalúa como un grupo sincronizado. El acuerdo por debajo de 30 abre o revierte a largo; el acuerdo por encima de 70 abre o revierte a corto.

![schema](schema.svg)

## Descripción de la estrategia

- Tres flujos de velas finalizadas calculan valores RSI(14) independientes en marcos de 5, 15 y 30 minutos.
- El evento RSI de treinta minutos toma el último valor de cada flujo. Sync agrupa las tres muestras con un intervalo de 30 minutos y las limpia después de liberarlas.
- Se toma una decisión por cada valor RSI formado de treinta minutos y solo después de que las tres salidas sincronizadas hayan actualizado sus comparaciones.
- Los tres RSI por debajo del umbral de compra generan una configuración larga. Los tres por encima del umbral de venta generan una configuración corta.
- Las puertas de posición impiden otra orden en la dirección ya mantenida. Una configuración opuesta envía una reversión a mercado de dos unidades; una entrada desde posición plana usa una unidad.
- No hay stop-loss, take-profit, salida temporal ni pausa independientes. La siguiente configuración opuesta cualificada es la única salida y establece inmediatamente la nueva dirección.

## Reglas de entrada y salida

- **Entrada larga**: Fast RSI, Middle RSI y Slow RSI sincronizados deben estar estrictamente por debajo de 30, y Position debe ser menor o igual que cero. Se compra `Base Volume + abs(sign(Position))` a mercado: una unidad desde posición plana o dos unidades desde el corto de una unidad creado por el diagrama.
- **Entrada corta**: Los tres RSI sincronizados deben estar estrictamente por encima de 70, y Position debe ser mayor o igual que cero. Se vende la misma cantidad calculada a mercado: una unidad desde posición plana o dos unidades desde el largo de una unidad creado por el diagrama.
- **Salida**: Un largo solo se cierra mediante una configuración corta cualificada, y un corto solo mediante una configuración larga cualificada. La orden de reversión cierra la exposición de una unidad y abre una unidad en la nueva dirección.
- **Ámbito de posición**: La fórmula normalizada está pensada para posiciones creadas por este diagrama. Si una posición externa tiene un tamaño absoluto superior a una unidad base, una reversión de dos unidades no garantiza alcanzar el objetivo.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|---|---|---|
| Serie de velas rápidas | 00:05:00 | Velas finalizadas de cinco minutos utilizadas por Fast RSI. |
| Serie de velas medias | 00:15:00 | Velas finalizadas de quince minutos utilizadas por Middle RSI. |
| Serie de velas lentas | 00:30:00 | Velas finalizadas de treinta minutos que programan decisiones sincronizadas. |
| Longitud de Fast RSI | 14 | Longitud de promediado del RSI en el flujo rápido. |
| Fuente de Fast RSI | Sin definir | No se ha seleccionado un campo de entrada alternativo para el indicador. |
| Longitud de Middle RSI | 14 | Longitud de promediado del RSI en el flujo medio. |
| Fuente de Middle RSI | Sin definir | No se ha seleccionado un campo de entrada alternativo para el indicador. |
| Longitud de Slow RSI | 14 | Longitud de promediado del RSI en el flujo lento. |
| Fuente de Slow RSI | Sin definir | No se ha seleccionado un campo de entrada alternativo para el indicador. |
| Umbral de compra | 30 | Límite superior estricto del acuerdo que permite una entrada larga. |
| Umbral de venta | 70 | Límite inferior estricto del acuerdo que permite una entrada corta. |
| Volumen base | 1 | Exposición objetivo y tamaño de entrada plana; las reversiones usan el doble del valor predeterminado. |

## Detalles del diagrama

- Cada bloque Candles emite solo valores finalizados y puede construir su marco a partir de velas almacenadas más pequeñas. Cada RSI empieza a emitir tras completar su propio calentamiento de 14 valores.
- Fast RSI y Middle RSI se forman antes que Slow RSI. Tres Variables de tipo indicator value son activadas por cada evento Slow RSI, por lo que los intervalos de calentamiento incompletos no quedan al principio de la cola de Sync.
- Sync tiene exactamente tres pares de entrada y salida conectados, Interval `00:30:00` y Clear Sockets activado. Sus salidas llevan el último RSI rápido, el último RSI medio y el RSI lento actual con un tiempo de decisión común.
- Seis bloques Comparison aplican pruebas estrictas `< Buy Threshold` y `> Sell Threshold`. Un pulso booleano de liberación llega a ambas puertas AND de cinco entradas solo después de que las seis comparaciones hayan procesado el grupo actual.
- Current Position se compara con cero mediante `<=` para la ruta larga y `>=` para la ruta corta. Estas pruebas permiten una entrada plana o una reversión de la posición opuesta y suprimen entradas repetidas en la misma dirección.
- Formula calcula `Base Volume + abs(sign(Position))`. Con la exposición mantenida, el resultado es 1 desde posición plana y 2 desde una posición, y permanece acotado incluso con varias suscripciones de datos.
- Los bloques Buy y Sell Modify position usan acciones de mercado NoCondition porque las puertas externas ya determinan la dirección y la admisibilidad. No hay bloque de protección ni de gráfico.
- Los umbrales están separados deliberadamente de las longitudes RSI y las series de velas. Cambiar un marco o una longitud modifica el calentamiento; Sync sigue esperando una terna de muestras nueva antes de evaluar.

## Uso

Importe el archivo `.json` en Designer, ejecútelo en el backtester con historial suficiente para formar el RSI de treinta minutos y ajuste las tres series de velas, longitudes RSI, umbrales y volumen base al instrumento antes de operar en vivo.
