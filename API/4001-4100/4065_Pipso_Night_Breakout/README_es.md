# Estrategia de fuga nocturna de Pipso
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Pipso es un sistema de sesiones nocturnas convertido del MetaTrader asesor experto `Pipso.mq4`. La estrategia mide la
precios más alto y más bajo de las velas previamente completadas y reacciona cuando el mercado sale de ese rango. cada
La ruptura invierte la posición: las posiciones largas se cierran y se abre una corta cuando el precio supera los máximos recientes, mientras que
Se cubren las posiciones cortas y se establece una nueva larga cuando el precio perfora los mínimos recientes. Las paradas de protección se derivan de
el ancho del rango para que la distancia de parada se adapte automáticamente a la volatilidad actual.

## Cómo funciona
1. Suscríbase al período de tiempo configurado (15 minutos por defecto) y espere a que los indicadores generen un historial completo.
2. Para cada nueva vela terminada, calcule el máximo más alto y el mínimo más bajo de las velas `BreakoutPeriod` anteriores. la corriente
la vela no es parte de ese rango, exactamente como en el EA original donde `iHighest(..., shift = 1)` omite la barra de trabajo.
3. Vuelva a calcular la distancia de parada como `(high - low) * StopLossMultiplier` mientras aplica la distancia mínima definida por
`MinStopDistance`.
4. Mantener una ventana comercial definida por `SessionStartHour` y `SessionLengthHours`. Cuando la ventana cruza la medianoche del viernes
se extiende dos días para que las operaciones abiertas sobrevivan el fin de semana como en MetaTrader.
5. Cuando el máximo de la vela excede el máximo de ruptura almacenado:
   - Cierre cualquier posición larga existente y, si se permite operar, abra una posición corta con tamaño `OrderVolume`.
   - Adjunte un stop loss por encima del precio de entrada utilizando la distancia de stop calculada.
6. Cuando el mínimo de la vela cae por debajo del mínimo de ruptura almacenado:
   - Cierre cualquier posición corta existente y, si se permite operar, abra una posición larga con tamaño `OrderVolume`.
   - Adjunte un stop loss por debajo del precio de entrada utilizando la distancia de stop calculada.
7. Los topes de protección se evalúan en cada vela terminada. Si el mínimo toca el stop largo o el máximo alcanza el stop corto,
la posición se aplana inmediatamente.

## Lógica de la sesión de negociación
- `SessionStartHour` se expresa en horas de cambio. La longitud de la ventana se establece con `SessionLengthHours`.
- Si la sesión se extiende más allá de las 24 horas y el día actual es viernes, el final de la ventana se adelanta 48 horas para que
esa negociación se reanuda el lunes, coincidiendo con el manejo del fin de semana en el código MQL4.
- Fuera de la ventana de negociación, la estrategia sólo cierra posiciones existentes; Se permiten nuevas operaciones nuevamente una vez que se abre la ventana.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Tipo de datos de vela utilizado para el cálculo de la señal. | plazo de 15 minutos |
| `OrderVolume` | Tamaño de orden fijo para cada orden de mercado. | 1 |
| `SessionStartHour` | Hora del día en que comienza la ventana de ruptura. | 21 |
| `SessionLengthHours` | Duración de la ventana de negociación en horas. | 9 |
| `BreakoutPeriod` | Número de velas completadas que definen el rango de ruptura. | 36 |
| `StopLossMultiplier` | Multiplicador aplicado al ancho del rango para derivar la distancia de parada (el valor `3` corresponde al `SLpp = 300` original). | 3 |
| `MinStopDistance` | Distancia mínima de stop-loss en unidades de precio absoluto, emulando la restricción del nivel de stop MetaTrader. | 0 |

## Notas
- La estrategia utiliza únicamente órdenes de mercado; no hay toma de ganancias. El stop-loss protector es el único mecanismo de salida además
la señal de ruptura opuesta.
- Al cambiar de largo a corto (o viceversa), la estrategia envía una única orden de mercado que cierra la orden anterior.
posición y abre el nuevo, reflejando el comportamiento de la fuente EA que llamó secuencialmente a `OrderClose` y
`OrderSend`.
- Las líneas indicadoras para los máximos y mínimos de ruptura se trazan automáticamente en el gráfico de estrategia junto con las operaciones ejecutadas.
