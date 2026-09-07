# Diagrama de la estrategia de trampa de falsa ruptura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El diagrama opera el regreso al rango previo de veinte velas después de una salida breve por uno de sus límites. Dos compuertas por dirección limitan las entradas repetidas y una SMA proporciona salidas inmediatas para la posición abierta.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas terminadas de un minuto se separan en flujos High, Low y Close.
- Highest(20) y Lowest(20), seguidos por Previous value con Shift = 1, definen un rango que excluye la vela actual.
- Un High sobre el máximo previo con Close de vuelta por debajo es una falsa ruptura alcista; la condición reflejada en Low es una falsa ruptura bajista.
- El Flag de venta y el Flag de compra comparten un N values de 500 velas terminadas; cada lado deja pasar un evento antes del siguiente reinicio común.
- Las entradas de mercado usan un volumen fijo de uno desde posición plana y las condiciones de SMA(20) reducen la posición por el mismo volumen.

## Reglas de entrada y salida

- **Entrada en largo**: Low queda bajo el mínimo de las veinte velas previas, Close vuelve sobre ese límite y la compuerta de compra acepta el evento. Comprar una unidad a mercado solo con posición plana.
- **Entrada en corto**: High queda sobre el máximo de las veinte velas previas, Close vuelve bajo ese límite y la compuerta de venta acepta el evento. Vender una unidad a mercado solo con posición plana.
- **Salida**: Reducir un largo en una unidad cuando Close esté bajo SMA(20), o reducir un corto en una unidad cuando Close esté sobre SMA(20). Las salidas son inmediatas y no pasan por la pausa de entradas. El diagrama no tiene stop-loss ni take-profit.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles Series | 00:01:00 | Velas terminadas de un minuto usadas en todos los cálculos de rango, señal y salida. |
| Highest Length | 20 | Cantidad de máximos de vela en el límite superior móvil. |
| Highest Source | Not set | No se selecciona otro campo de entrada del indicador; Candle high está conectado directamente. |
| Lowest Length | 20 | Cantidad de mínimos de vela en el límite inferior móvil. |
| Lowest Source | Not set | No se selecciona otro campo de entrada del indicador; Candle low está conectado directamente. |
| SMA Length | 20 | Cantidad de cierres de la media móvil usada para las salidas. |
| SMA Source | Not set | No se selecciona otro campo de entrada del indicador; Candle close está conectado directamente. |
| Cooldown N | 500 | Cantidad de velas terminadas consumidas antes de emitir el reinicio común de la pausa. |
| Entry Volume | 1 | Volumen de mercado fijo para cada entrada y salida reductora. |

## Detalles del diagrama

- Highest y Lowest reciben los valores numéricos High y Low, mientras SMA recibe Close; los tres indicadores solo emiten valores formados.
- Previous value desplaza ambos indicadores de rango una actualización, por lo que la vela evaluada no interviene en su propio límite.
- El pulso final de evaluación llega a las dos puertas AND después de actualizar campos de vela, indicadores, posición y comparaciones.
- Cada evento de falsa ruptura sin filtrar arma el N values compartido. Su salida reinicia ambos Flags tras 500 velas terminadas posteriores; hasta entonces cada Flag suprime repeticiones de su lado.
- OpenPosition impide una orden nueva mientras existe posición. El evento todavía puede armar la pausa porque esa rama está antes de la acción de posición.
- Las dos salidas por SMA comprueban el signo de la posición y usan acciones de mercado ReduceOnly de una unidad. Chart recibe velas, ambos límites previos, SMA y todas las ejecuciones.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
